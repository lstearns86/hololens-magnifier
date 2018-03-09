using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloLensCameraStream;
using UnityEngine.VR.WSA.Input;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Cursor = HoloToolkit.Unity.InputModule.Cursor;
using HoloToolkit.Unity;

public class MagnificationManager : MonoBehaviour {

    enum DisplayMode { Disable, Fixed2D, Fixed3D, Flat3D, DynamicFingerTracking }
    DisplayMode displayMode = DisplayMode.Disable;

    public enum DragMode { Disable, Move, Scale, Zoom }
    DragMode currDragMode = DragMode.Disable;

    private bool relativePositioning = true;
    
    public GameObject HeadsUpDisplayObject, HeadsUpDisplayImageObject;
    private HeadsUpDisplay _hud = null;
    private HeadsUpDisplay Hud { get { if (_hud == null) _hud = HeadsUpDisplayObject.GetComponent<HeadsUpDisplay>(); return _hud; } }

    //public Camera Camera;

    public GameObject Display3DObject, Display3dImageObject;
    private Display3D _display3D = null;
    private Display3D Display3D { get { if (_display3D == null) _display3D = Display3DObject.GetComponent<Display3D>(); return _display3D; } }

    public GameObject MenuObject, DebugMenuObject;
    public Text ConnectionStatusLabel;
    private bool useDebugMenu = false;
    private bool enableMenu = false;
    private bool useLocalPlaceholderText = false;

    private NetworkManager _networkManager = null;
    private NetworkManager NetworkManager { get { if (_networkManager == null) _networkManager = GetComponent<NetworkManager>(); return _networkManager; } }

    private SpatialMappingObserver _spatialObserver = null;
    private SpatialMappingObserver SpatialObserver { get { if (_spatialObserver == null) _spatialObserver = GetComponent<SpatialMappingObserver>(); return _spatialObserver; } }

    Dictionary<uint, Tuple<Vector3, bool>> handTracker = new Dictionary<uint, Tuple<Vector3, bool>>();

    byte[] _latestImageBytes, _processingImageBytes, _tempGrayBytes;
    HoloLensCameraStream.Resolution _resolution;

    VideoCapture _videoCapture;
    CameraParameters camParams;

    bool dragging = false, menuVisible = false;
    Vector3 dragStartPosition;

    Vector3 handPosition, ledPosition, cameraPosition;
    //Vector3 cameraStartDraggingPosition, cameraStartDraggingLookDirection;

    Vector3 targetDisplayPosition = Vector3.zero;
    Vector3 displayOffset = Vector3.zero, startOffset = Vector3.zero;

    int texWidth = 256, texHeight = 256;

    bool includeHologramsInCameraView = true;
    bool useHololensCamera = true;
    bool showCameraPreview = false;
    bool enableDebugText = false;
    bool useHololensFingerTracking = false;
    bool connected = false;
    bool verboseLogging = false;

    DateTime lastFrameSent = DateTime.Now, lastFrameProcessed = DateTime.Now, lastConnectionAttempt = DateTime.Now;
    float streamingFrameRate = 5, processingFrameRate = 10;
    float initFov = 1, currFov = 1;

    object processingLock = new object();

    public GameObject CursorObject;
    private ObjectCursor _cursor;
    private ObjectCursor Cursor { get { if (_cursor == null) _cursor = CursorObject.GetComponent<ObjectCursor>(); return _cursor; } }

    private TextToSpeechManager _speech;
    private TextToSpeechManager Speech { get { if (_speech == null) _speech = GetComponent<TextToSpeechManager>(); return _speech; } }

    private int NumHandsVisible { get { return handTracker.Count; } }
    private int NumHandsPinched { get { int count = 0; foreach (var hand in handTracker) { if (hand.Value.Item2) count++; } return count; } }

    public AudioSource AudioSource;
    public AudioClip HandDetectedSound;
    public AudioClip HandLostSound;
    public AudioClip PinchStartedSound;
    public AudioClip PinchEndedSound;

    // Use this for initialization
    void Start () {

        initFov = HeadsUpDisplayObject.GetComponent<Canvas>().worldCamera.fieldOfView;
        currFov = initFov;

        InteractionManager.SourceDetected += InteractionManager_SourceDetected;
        InteractionManager.SourceLost += InteractionManager_SourceLost;
        InteractionManager.SourceUpdated += InteractionManager_SourceUpdated;

        NetworkManager.Connected += () =>
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                connected = true;
                string text = "Connected to " + NetworkManager.serverName + " (" + NetworkManager.serverAddress.ToString() + ")";
                ConnectionStatusLabel.text = text;

                float size = displayMode == DisplayMode.Fixed2D ? Hud.GetScale() : 0;
                float zoom = displayMode == DisplayMode.Fixed2D ? Hud.GetZoom() : 0;
                float distance = Hud.GetDistance();
                string mode = "";
                switch (displayMode)
                {
                    default:
                    case DisplayMode.Disable: mode = "disable"; break;
                    case DisplayMode.Fixed2D: mode = "design1"; break;
                    case DisplayMode.Fixed3D: mode = "design2a"; break;
                    case DisplayMode.Flat3D: mode = "design2b"; break;
                    case DisplayMode.DynamicFingerTracking: mode = "design3"; break;
                }
                string dragmode = "";
                switch (currDragMode)
                {
                    default:
                    case DragMode.Disable: dragmode = "disable"; break;
                    case DragMode.Move: dragmode = "move"; break;
                    case DragMode.Scale: dragmode = "resize"; break;
                    case DragMode.Zoom: dragmode = "zoom"; break;
                }
                NetworkManager.SendText("{\"size\": " + size + "}", verboseLogging);
                NetworkManager.SendText("{\"zoom\": " + zoom + "}", verboseLogging);
                NetworkManager.SendText("{\"distance\": " + distance + "}", verboseLogging);
                NetworkManager.SendText("{\"mode\": \"" + mode + "\"}", verboseLogging);
                NetworkManager.SendText("{\"dragmode\": \"" + dragmode + "\"}", verboseLogging);
                NetworkManager.SendText("{\"menu\": \"" + (enableMenu ? "enable" : "disable") + "\"}", verboseLogging);
                NetworkManager.SendText("{\"placeholder\": \"" + (useLocalPlaceholderText ? "local" : "server") + "\"}", verboseLogging);
                NetworkManager.SendText("{\"verbose\": \"" + (verboseLogging ? "yes" : "no") + "\"}", verboseLogging);
                NetworkManager.SendText("{\"streamingrate\": " + streamingFrameRate + "}");

                if(verboseLogging) LoggingManager.Log("Finished Connecting");
            }, false);
        };

        NetworkManager.LostConnection += () =>
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                string text = "Lost Connection";
                ConnectionStatusLabel.text = text;
                connected = false;
                LoggingManager.Log("Lost Connection");
            }, false);
        };

        NetworkManager.TextReceived += (string message) =>
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                try
                {
                    // TODO: use a better JSON parser
                    var match = Regex.Match(message, "\\{\"([a-zA-Z0-9-_.]+)\"\\: ?\"?(.+)\"?\\}");
                    if (match != null && match.Success)
                    {
                        string command = match.Groups.Count > 1 ? match.Groups[1].Value : null;
                        string argument = match.Groups.Count > 2 ? match.Groups[2].Value : null;
                        argument = argument.TrimEnd('"');

                        if(verboseLogging) LoggingManager.Log("Command recognized: " + command + ", " + argument);
                        if (command == "size")
                        {
                            float value = float.Parse(argument);
                            Hud.SetScale(value);
                            Display3D.SetScale(value);
                        }
                        else if (command == "zoom")
                        {
                            float value = float.Parse(argument);
                            Hud.SetZoom(value);
                            Display3D.SetZoom(value);
                        }
                        else if (command == "distance")
                        {
                            float value = float.Parse(argument);
                            Hud.SetDistance(value);
                        }
                        else if (command == "position")
                        {
                            if(argument == "reset")
                            {
                                if (displayMode == DisplayMode.Fixed2D)
                                    Hud.ResetPosition();
                                else if (displayMode != DisplayMode.DynamicFingerTracking)
                                    Display3D.ResetPosition();
                                else if (displayMode == DisplayMode.DynamicFingerTracking)
                                    displayOffset = Vector3.zero;
                            }
                            else if(argument == "relative")
                            {
                                relativePositioning = true;

                                Vector3 position = new Vector3(0, 0, 0);
                                position = CameraCache.Main.transform.TransformPoint(position);

                                if (displayMode == DisplayMode.Fixed2D)
                                    Hud.StartDragging(position);
                                else if (displayMode != DisplayMode.DynamicFingerTracking)
                                    Display3D.StartDragging(position);
                                else if(displayMode == DisplayMode.DynamicFingerTracking)
                                    startOffset = displayOffset;
                            }
                            else if(argument == "absolute")
                            {
                                relativePositioning = false;
                            }
                            else
                            {
                                var argMatchAll = Regex.Match(argument, "\\{\"x\"\\: ?([0-9-.]+), \"y\"\\: ?([0-9-.]+), \"z\"\\: ?([0-9-.]+)\\}");
                                var argMatchXY = Regex.Match(argument, "\\{\"x\"\\: ?([0-9-.]+), \"y\"\\: ?([0-9-.]+)\\}");
                                var argMatchZ = Regex.Match(argument, "\\{\"z\"\\: ?([0-9-.]+)\\}");
                                float x = 0, y = 0, z = 0;
                                bool success = false;
                                if (argMatchAll != null && argMatchAll.Success)
                                {
                                    string xStr = argMatchAll.Groups.Count > 1 ? argMatchAll.Groups[1].Value : null;
                                    string yStr = argMatchAll.Groups.Count > 2 ? argMatchAll.Groups[2].Value : null;
                                    string zStr = argMatchAll.Groups.Count > 3 ? argMatchAll.Groups[3].Value : null;

                                    try
                                    {
                                        x = xStr == null ? 0 : float.Parse(xStr);
                                        y = yStr == null ? 0 : float.Parse(yStr);
                                        z = zStr == null ? 0 : float.Parse(zStr);
                                        success = true;
                                    }
                                    catch { LoggingManager.LogError("Bad argument: " + xStr + ", " + yStr + ", " + zStr); }
                                }
                                else if (argMatchXY != null && argMatchXY.Success)
                                {
                                    string xStr = argMatchXY.Groups.Count > 1 ? argMatchXY.Groups[1].Value : null;
                                    string yStr = argMatchXY.Groups.Count > 2 ? argMatchXY.Groups[2].Value : null;

                                    try
                                    {
                                        x = xStr == null ? 0 : float.Parse(xStr);
                                        y = yStr == null ? 0 : float.Parse(yStr);
                                        success = true;
                                    }
                                    catch { LoggingManager.LogError("Bad argument: " + xStr + ", " + yStr); }
                                }
                                else if (argMatchZ != null && argMatchZ.Success)
                                {
                                    string zStr = argMatchZ.Groups.Count > 1 ? argMatchZ.Groups[1].Value : null;

                                    try
                                    {
                                        z = zStr == null ? 0 : float.Parse(zStr);
                                        success = true;
                                    }
                                    catch { LoggingManager.LogError("Bad argument: " + zStr); }
                                }
                                else
                                {
                                    LoggingManager.LogError("Argument not recognized: " + argument);
                                }

                                if(success)
                                {
                                    Vector3 originalPosition = new Vector3(x, y, z);
                                    Vector3 position = CameraCache.Main.transform.TransformPoint(originalPosition);

                                    if (displayMode == DisplayMode.Fixed2D)
                                    {
                                        if (relativePositioning)
                                            Hud.UpdateDragging(position, DragMode.Move);
                                        else if (argMatchAll.Success || argMatchXY.Success)
                                            Hud.SetPosition(new Vector2(originalPosition.x * 425, originalPosition.y * 208), false);
                                        //Hud.SetPosition(new Vector2((originalPosition.x + 1) / 2.0f, (originalPosition.y + 1) / 2.0f), true);
                                    }
                                    else if (displayMode == DisplayMode.Fixed3D || displayMode == DisplayMode.Flat3D)
                                    {
                                        if (relativePositioning)
                                            Display3D.UpdateDragging(position, DragMode.Move);
                                        else if (argMatchAll.Success)
                                            Display3D.SetPosition(CameraCache.Main.transform.position + CameraCache.Main.transform.forward + CameraCache.Main.transform.TransformDirection(originalPosition / 3));
                                        else if (argMatchXY.Success)
                                        {
                                            Vector3 modifiedVector = new Vector3(originalPosition.x / 3, originalPosition.y / 3, 0);
                                            float distance = (Display3D.GetPosition() - CameraCache.Main.transform.position).magnitude;
                                            Display3D.SetPosition(CameraCache.Main.transform.position + (CameraCache.Main.transform.forward + CameraCache.Main.transform.TransformDirection(modifiedVector)).normalized * distance);
                                        }
                                        else if (argMatchZ.Success)
                                        {
                                            Display3D.SetPosition(CameraCache.Main.transform.position + (Display3D.GetPosition() - CameraCache.Main.transform.position).normalized * (1 + originalPosition.z / 3));
                                        }

                                    }
                                    else if (displayMode == DisplayMode.DynamicFingerTracking)
                                    {
                                        if (relativePositioning)
                                        {
                                            Vector3 delta = position - dragStartPosition;
                                            displayOffset = startOffset + delta * 2;
                                        }
                                        else
                                        {
                                            if (argMatchAll.Success)
                                                displayOffset = originalPosition;
                                            else if (argMatchXY.Success)
                                                displayOffset = new Vector3(originalPosition.x, originalPosition.y, displayOffset.z);
                                            else if (argMatchZ.Success)
                                                displayOffset = new Vector3(displayOffset.x, displayOffset.y, originalPosition.z);
                                        }
                                    }
                                }
                            }
                        }
                        else if (command == "mode")
                        {
                            if(argument == "disable")
                            {
                                DisableDisplaySelected();
                            }
                            else if (argument == "design1")
                            {
                                Design1Selected();
                            }
                            else if (argument == "design2a")
                            {
                                Design2Selected();
                            }
                            else if (argument == "design2b")
                            {
                                Design2bSelected();
                            }
                            else if (argument == "design3")
                            {
                                Design3Selected();
                            }
                        }
                        else if (command == "dragmode")
                        {
                            if(argument == "disable")
                            {
                                DisableDraggingButtonSelected();
                            }
                            else if(argument == "move")
                            {
                                MoveButtonSelected();
                            }
                            else if(argument == "resize")
                            {
                                ScaleButtonSelected();
                            }
                            else if(argument == "zoom")
                            {
                                ZoomButtonSelected();
                            }
                        }
                        else if(command == "menu")
                        {
                            if(argument == "enable")
                            {
                                enableMenu = true;
                            }
                            else if(argument == "disable")
                            {
                                enableMenu = false;
                            }
                            else if(argument == "show")
                            {
                                HideEverything();
                                ShowMenu();
                            }
                            else if(argument == "hide")
                            {
                                HideEverything();
                                ShowCurrentDisplay();
                            }
                        }
                        else if (command == "placeholder")
                        {
                            if (argument == "local")
                            {
                                useLocalPlaceholderText = true;
                                Hud.ShowTestMessage();
                            }
                            else
                            {
                                useLocalPlaceholderText = false;
                                Hud.HideTestMessage();
                            }
                        }
                        else if(command == "verbose")
                        {
                            if(argument == "on")
                            {
                                verboseLogging = true;
                            }
                            else if (argument == "off")
                            {
                                verboseLogging = false;
                            }
                        }
                        else if(command == "camera")
                        {
                            if(argument == "reset")
                            {
                                if(_videoCapture != null)
                                    _videoCapture.StopVideoModeAsync(OnVideoModeStopped);
                            }
                        }
                        else if(command == "streamingrate")
                        {
                            float value = float.Parse(argument);
                            if (value < 1) value = 1;
                            streamingFrameRate = (int)value;
                        }
                    }
                    else
                    {
                        LoggingManager.LogError("Couldn't parse command: " + message);
                    }
                }
                catch { LoggingManager.LogError("Couldn't parse command: " + message); }
            }, false);
        };

        NetworkManager.ImageReceived += (byte[] image, int width, int height) =>
        {
            if (!showCameraPreview)
            {
                if (texWidth != width || texHeight != height)
                {
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        texWidth = width; texHeight = height;
                        Hud.SetResolution(width, height);
                        Display3D.SetResolution(width, height);
                    }, false);
                }

                UpdateThumbnail(image);
            }
        };

        if(!showCameraPreview)
        {
            int resolution = 256;
            Hud.SetResolution(resolution, resolution);
            Display3D.SetResolution(resolution, resolution);
        }

        HideEverything();
        ShowCurrentDisplay();

        //SpatialObserver.StartObserving();

        if (useHololensCamera)
        {
            CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
        }

        if (!enableDebugText) Hud.DebugText.text = "";

        GazeManager.Instance.FocusedObjectChanged += Gaze_FocusChanged;

        Speech.Voice = TextToSpeechVoice.Mark;

        //Hud.SetPosition(new Vector2(0, 0), true);
        Hud.ResetPosition();
    }

    private void Gaze_FocusChanged(GameObject previousObject, GameObject newObject)
    {
        if(newObject != null && newObject.name == "Text")
        {
            string label = newObject.GetComponent<Text>().text;
            //Debug.Log(label);
            Speech.SpeakText(label);
            Cursor.gameObject.SetActive(true);
        }
        else if (handTracker.Count == 0)
        {
            Cursor.gameObject.SetActive(false);
        }
        else
        {
            Cursor.gameObject.SetActive(true);
        }
    }

    private void InteractionManager_SourceUpdated(InteractionSourceState state)
    {
        if (state.source.kind == InteractionSourceKind.Hand)
        {
            uint id = state.source.id;
            if (handTracker.ContainsKey(id))
            {
                Vector3 position;
                bool hasPosition = state.properties.location.TryGetPosition(out position);
                bool pressed = state.pressed;

                //position = CameraCache.Main.transform.InverseTransformPoint(position);
                //position = CameraCache.Main.transform.InverseTransformDirection(position - CameraCache.Main.transform.position);

                handTracker[id] = new Tuple<Vector3, bool>(position, pressed);

                if (NumHandsPinched > 0) Cursor.OnInputDown(null); else Cursor.OnInputUp(null);

                if (!dragging && pressed)
                {
                    dragging = true;
                    dragStartPosition = position;

                    AudioSource.PlayOneShot(PinchStartedSound);

                    if (currDragMode != DragMode.Disable)
                    {
                        if (displayMode == DisplayMode.Fixed2D)
                            Hud.StartDragging(position);
                        else if (displayMode == DisplayMode.Fixed3D || displayMode == DisplayMode.Flat3D)
                            Display3D.StartDragging(position);
                        else if (displayMode == DisplayMode.DynamicFingerTracking)
                        {
                            if (currDragMode == DragMode.Move)
                                startOffset = displayOffset;
                            else if(currDragMode == DragMode.Scale || currDragMode == DragMode.Zoom)
                                Display3D.StartDragging(position);
                        }
                    }
                }
                else if (dragging && pressed)
                {
                    if (currDragMode != DragMode.Disable)
                    {
                        if (displayMode == DisplayMode.Fixed2D)
                        {
                            Hud.UpdateDragging(position, currDragMode);
                            if (currDragMode == DragMode.Scale)
                                NetworkManager.SendText("{\"size\":" + Hud.GetScale() + "}", verboseLogging);
                            else if (currDragMode == DragMode.Zoom)
                                NetworkManager.SendText("{\"zoom\":" + Hud.GetZoom() + "}", verboseLogging);
                        }
                        else if (displayMode == DisplayMode.Fixed3D || displayMode == DisplayMode.Flat3D)
                        {
                            Display3D.UpdateDragging(position, currDragMode);
                            if (currDragMode == DragMode.Scale)
                                NetworkManager.SendText("{\"size\":" + Display3D.GetScale() + "}", verboseLogging);
                            else if (currDragMode == DragMode.Zoom)
                                NetworkManager.SendText("{\"zoom\":" + Display3D.GetZoom() + "}", verboseLogging);
                        }
                        else if (displayMode == DisplayMode.DynamicFingerTracking)
                        {
                            if(currDragMode == DragMode.Move)
                            {
                                Vector3 delta = position - dragStartPosition;
                                displayOffset = startOffset + delta * 2;
                            }
                            else if(currDragMode == DragMode.Scale || currDragMode == DragMode.Zoom)
                            {
                                Display3D.UpdateDragging(position, currDragMode);
                                if (currDragMode == DragMode.Scale)
                                    NetworkManager.SendText("{\"size\":" + Display3D.GetScale() + "}", verboseLogging);
                                else if (currDragMode == DragMode.Zoom)
                                    NetworkManager.SendText("{\"zoom\":" + Display3D.GetZoom() + "}", verboseLogging);
                            }
                        }
                    }
                }
                else if (dragging && !pressed)
                {
                    dragging = false;

                    AudioSource.PlayOneShot(PinchEndedSound);

                    if ((position - dragStartPosition).magnitude < 0.03 || currDragMode == DragMode.Disable) // tap without drag
                    {
                        if (!GazeManager.Instance.IsGazingAtObject || GazeManager.Instance.HitObject == Display3dImageObject)
                        {
                            if (enableMenu)
                            {
                                menuVisible = !menuVisible;
                                HideEverything();
                                if (menuVisible) ShowMenu();
                                else ShowCurrentDisplay();
                            }
                        }
                    }
                }

                cameraPosition = Camera.main.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
                handPosition = new Vector3(position.x, position.y, position.z);
                handPosition += 3 * (handPosition - cameraPosition);
                if (displayMode == DisplayMode.DynamicFingerTracking && useHololensFingerTracking) targetDisplayPosition = handPosition + CameraCache.Main.transform.TransformDirection(displayOffset);
            }
        }
    }

    private bool ChildOfObject(GameObject targetObject, GameObject testObject)
    {
        GameObject parent = testObject;
        while (parent != null)
        {
            if (parent == targetObject) return true;
            try
            {
                parent = parent.transform.parent.gameObject;
            }
            catch { parent = null; } 
        }

        return false;
    }

    private void InteractionManager_SourceLost(InteractionSourceState state)
    {
        if (state.source.kind == InteractionSourceKind.Hand)
        {
            uint id = state.source.id;

            handTracker.Remove(id);
        }

        Cursor.OnSourceLost(null);

        if (handTracker.Count == 0) { AudioSource.PlayOneShot(HandLostSound); dragging = false; if (!menuVisible) Cursor.gameObject.SetActive(false); }
    }

    private void InteractionManager_SourceDetected(InteractionSourceState state)
    {
        if (state.source.kind == InteractionSourceKind.Hand)
        {
            uint id = state.source.id;
            Vector3 position;
            bool hasPosition = state.properties.location.TryGetPosition(out position);
            bool pressed = state.pressed;
            handTracker.Add(id, new Tuple<Vector3, bool>(position, pressed));
        }

        Cursor.OnSourceDetected(null);

        if (handTracker.Count == 1) { AudioSource.PlayOneShot(HandDetectedSound); Cursor.gameObject.SetActive(true); }
    }

    private void OnDestroy()
    {
        if (_videoCapture != null)
        {
            _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
            _videoCapture.Dispose();
        }
    }

    void OnVideoCaptureCreated(VideoCapture videoCapture)
    {
        if (videoCapture == null)
        {
            LoggingManager.LogError("Did not find a video capture object. You may not be using the Hololens.");
            return;
        }

        this._videoCapture = videoCapture;

        _resolution = CameraStreamHelper.Instance.GetLowestResolution();
        float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);
        videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

        camParams = new CameraParameters();
        camParams.cameraResolutionHeight = _resolution.height;
        camParams.cameraResolutionWidth = _resolution.width;
        camParams.frameRate = Mathf.RoundToInt(frameRate);
        camParams.pixelFormat = CapturePixelFormat.BGRA32;
        camParams.rotateImage180Degrees = true;
        camParams.enableHolograms = includeHologramsInCameraView;

        if(showCameraPreview)
            if (texWidth != _resolution.width || texHeight != _resolution.height)
            {
                texWidth = _resolution.width; texHeight = _resolution.height;
                Hud.SetResolution(_resolution.width, _resolution.height);
                Display3D.SetResolution(_resolution.width, _resolution.height);
            }

        videoCapture.StartVideoModeAsync(camParams, OnVideoModeStarted);

        if (verboseLogging) LoggingManager.Log("Starting video capture");
    }

    void OnVideoModeStarted(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            LoggingManager.LogError("Could not start video mode.");
            return;
        }

        if (verboseLogging) LoggingManager.Log("Video capture started.");
    }

    void OnVideoModeStopped(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            LoggingManager.LogError("Could not stop video mode.");
            return;
        }

        if (verboseLogging) LoggingManager.Log("Video capture stopped.");
        _videoCapture.Dispose();

        CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
    }

    void FindMarker(out float x, out float y)
    {
        x = 0; y = 0;
#if !UNITY_EDITOR
        if (_tempGrayBytes == null || _tempGrayBytes.Length < _processingImageBytes.Length / 4) _tempGrayBytes = new byte[_processingImageBytes.Length / 4];

        ImageProcessing.Bgra2Gray(_processingImageBytes, _tempGrayBytes);

        ImageProcessing.Threshold(_tempGrayBytes, _tempGrayBytes, 250, 255);
        ImageProcessing.LabelConnectedComponents(_tempGrayBytes, _resolution.width, _resolution.height, out short[] labels, out Dictionary<short, int> labelCounts);
        ImageProcessing.FilterLargestComponent(labels, labelCounts, _tempGrayBytes);
        ImageProcessing.FindCenterOfMass(_tempGrayBytes, _resolution.width, _resolution.height, out x, out y);
        
        ImageProcessing.Gray2Bgra(_tempGrayBytes, _processingImageBytes);
#endif
    }

    void OnFrameSampleAcquired(VideoCaptureSample sample)
    {
        try
        {
            if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength)
            {
                _latestImageBytes = new byte[sample.dataLength];
            }
            sample.CopyRawImageDataIntoBuffer(_latestImageBytes);

            var now = DateTime.Now;

            // Do anything else that requires the sample, e.g. get matrices

#if !UNITY_EDITOR
            sample.TryGetProjectionMatrix(out float[] projectionMatrixRaw);
            Matrix4x4 projectionMatrix = new Matrix4x4(new Vector4(projectionMatrixRaw[0], projectionMatrixRaw[4], projectionMatrixRaw[8], projectionMatrixRaw[12]),
                                                       new Vector4(projectionMatrixRaw[1], projectionMatrixRaw[5], projectionMatrixRaw[9], projectionMatrixRaw[13]),
                                                       new Vector4(projectionMatrixRaw[2], projectionMatrixRaw[6], projectionMatrixRaw[10], projectionMatrixRaw[14]),
                                                       new Vector4(projectionMatrixRaw[3], projectionMatrixRaw[7], projectionMatrixRaw[11], projectionMatrixRaw[15]));

            if ((now - lastFrameProcessed).TotalMilliseconds >= 1000.0 / processingFrameRate)
            {
                Task.Run(() =>
                {
                    if (!Monitor.TryEnter(processingLock)) return;
                    lastFrameProcessed = now;
                    DateTime start = DateTime.Now;

                    // copy the image for processing
                    if (_processingImageBytes == null || _processingImageBytes.Length != _latestImageBytes.Length)
                        _processingImageBytes = new byte[_latestImageBytes.Length];
                    _latestImageBytes.CopyTo(_processingImageBytes, 0);

                    FindMarker(out float x, out float y);

                    // image is flipped for some reason, correct the coordinates now
                    x = _resolution.width - x;
                    y = _resolution.height - y;

                    double elapsed = (DateTime.Now - start).TotalMilliseconds;

                    if (showCameraPreview) UpdateThumbnail(_processingImageBytes);

                    if (displayMode == DisplayMode.DynamicFingerTracking) NetworkManager.SendImage(_processingImageBytes, _resolution.width, _resolution.height, verboseLogging);

                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        if (x > 0 && y > 0)
                        {
                            var cameraToWorldMatrix = Camera.main.cameraToWorldMatrix;

                            float xNorm = (x / _resolution.width) * 2 - 1, yNorm = (1 - y / _resolution.height) * 2 - 1;

                            Vector3 pc = new Vector3(0, 0, 0);
                            var axsX = projectionMatrix.GetRow(0);
                            var axsY = projectionMatrix.GetRow(1);
                            var axsZ = projectionMatrix.GetRow(2);
                            pc.z = 1 / axsZ.z;
                            pc.y = (yNorm - (pc.z * axsY.z)) / axsY.y;
                            pc.x = (xNorm - (pc.z * axsX.z)) / axsX.x;

                            pc.z = -(float)Math.Sqrt(1 - pc.x * pc.x - pc.y - pc.y);

                            //pc.x *= 3f;
                            //pc.y *= 3f;
                            //pc.z *= 3f;

                            Vector3 position = cameraToWorldMatrix.MultiplyPoint(pc);
                            ledPosition = position;

                            cameraPosition = Camera.main.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);

                            position += 1 * (position - cameraPosition);

                            if (displayMode == DisplayMode.DynamicFingerTracking && !useHololensFingerTracking) targetDisplayPosition = position + CameraCache.Main.transform.TransformDirection(displayOffset); //Display3D.SetPosition(position + offset);
                        }
                        //Hud.DebugText.text = "X: + " + x.ToString("0") + ", Y: " + y.ToString("0") + "\n" + "Processing time: " + elapsed.ToString("0.0") + " ms";
                    }, true);

                    Monitor.Exit(processingLock);
                });
            }
#endif

            sample.Dispose();

            //if (useHololensCamera)
            //{
            //    UpdateThumbnail(_latestImageBytes);
            //}

            if (displayMode != DisplayMode.DynamicFingerTracking && (now - lastFrameSent).TotalMilliseconds >= 1000.0 / streamingFrameRate)
            {
                NetworkManager.SendImage(_latestImageBytes, _resolution.width, _resolution.height, verboseLogging);
                lastFrameSent = now;
            }
        }
        catch (Exception ex) { LoggingManager.LogError("Problem processing video frame: " + ex.ToString()); }
    }

    void UpdateThumbnail(byte[] image, bool decoded = true)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            if (displayMode == DisplayMode.Fixed2D)
            {
                var texture = Hud.Thumbnail.texture as Texture2D;
                if (decoded)
                {
                    texture.LoadRawTextureData(image);
                    texture.Apply();
                }
                else
                    texture.LoadImage(image);
            }
            else
            {
                var texture = Display3D.Thumbnail.texture as Texture2D;
                if (decoded)
                {
                    texture.LoadRawTextureData(image);
                    texture.Apply();
                }
                else
                    texture.LoadImage(image);
            }
        }, false);
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            DateTime now = DateTime.Now;
            if (!connected && (now - lastConnectionAttempt).TotalSeconds > 10) { lastConnectionAttempt = now; NetworkManager.ResetConnection(); }

            float fov = HeadsUpDisplayObject.GetComponent<Canvas>().worldCamera.fieldOfView;
            if (Math.Abs(fov - currFov) > 0.1)
            {
                currFov = fov;
                if(verboseLogging) LoggingManager.Log("Updated FoV: " + fov);
                Hud.GetComponent<CanvasScaler>().scaleFactor = initFov / fov;
            }

            if(displayMode == DisplayMode.DynamicFingerTracking)
            {
                var currPosition = Display3D.GetPosition();
                Display3D.SetPosition(currPosition + 0.2f * (targetDisplayPosition - currPosition));
            }

            if (enableDebugText)
            {
                //string debugText = "";
                //int handIndex = 1;
                //foreach (uint key in handTracker.Keys)
                //    debugText += "Hand " + (handIndex++) + (handTracker[key].Item2 ? " pressed" : " visible") + " | ";
                //debugText = debugText.TrimEnd(' ', '|') + "\n";
                //debugText += (dragging ? "Dragging" : "Not Dragging") + "\n" +
                //            "Menu " + (menuVisible ? "Visible" : "Hidden") + "\n" +
                //            (GazeManager.Instance.IsGazingAtObject ? "Gazing at " + (GazeManager.Instance.HitObject.name) : "Not Gazing at object") + "\n";
                
                string debugText = "";
                debugText += "Hand Position: " + handPosition.x.ToString("0.0") + ", " + handPosition.y.ToString("0.0") + ", " + handPosition.z.ToString("0.0") + "\n";
                debugText += "LED Position: " + ledPosition.x.ToString("0.0") + ", " + ledPosition.y.ToString("0.0") + ", " + ledPosition.z.ToString("0.0");

                Hud.DebugText.text = debugText;
            }

            if(menuVisible)
            {
                var menu = (useDebugMenu ? DebugMenuObject : MenuObject);
                var optimalPosition = CameraCache.Main.transform.position + CameraCache.Main.transform.forward * 2;
                var offsetDir = menu.transform.position - optimalPosition;

                float maxMenuOffset = 0.5f;
                if (offsetDir.magnitude > maxMenuOffset)
                {
                    var targetPosition = optimalPosition + offsetDir.normalized * maxMenuOffset;
                    float deltaTime = Time.unscaledDeltaTime;
                    menu.transform.position = Vector3.Lerp(menu.transform.position, targetPosition, 5 * deltaTime);
                    //transform.position = targetPosition;
                }
            }
        }
        catch { }
    }

    public void DisableDisplaySelected()
    {
        if (displayMode != DisplayMode.Disable) NetworkManager.SendText("{\"mode\": \"disable\"}", verboseLogging);

        if (displayMode == DisplayMode.DynamicFingerTracking)
        {
            includeHologramsInCameraView = true;
            _videoCapture.StopVideoModeAsync(OnVideoModeStopped);
        }

        //Hud.DebugText.text = "Disable";
        displayMode = DisplayMode.Disable;
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;
    }

    public void Design1Selected()
    {
        if (displayMode != DisplayMode.Fixed2D) NetworkManager.SendText("{\"mode\": \"design1\"}", verboseLogging);

        if(displayMode == DisplayMode.DynamicFingerTracking)
        {
            includeHologramsInCameraView = true;
            _videoCapture.StopVideoModeAsync(OnVideoModeStopped);
        }

        //Hud.DebugText.text = "Design 1";
        displayMode = DisplayMode.Fixed2D;
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;
    }

    public void Design2Selected()
    {
        if (displayMode != DisplayMode.Fixed3D) NetworkManager.SendText("{\"mode\": \"design2a\"}", verboseLogging);

        if (displayMode == DisplayMode.DynamicFingerTracking)
        {
            includeHologramsInCameraView = true;
            _videoCapture.StopVideoModeAsync(OnVideoModeStopped);
        }

        //Hud.DebugText.text = "Design 2";
        displayMode = DisplayMode.Fixed3D;
        Display3D.SetDisplayModeBillboard();
        Display3D.ResetPosition();
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;
    }

    public void Design2bSelected()
    {
        if (displayMode != DisplayMode.Flat3D) NetworkManager.SendText("{\"mode\": \"design2b\"}", verboseLogging);

        if (displayMode == DisplayMode.DynamicFingerTracking)
        {
            includeHologramsInCameraView = true;
            _videoCapture.StopVideoModeAsync(OnVideoModeStopped);
        }

        displayMode = DisplayMode.Flat3D;
        Display3D.ResetPosition();
        Display3D.SetDisplayModeFlat();
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;
    }

    public void Design3Selected()
    {
        if (displayMode != DisplayMode.DynamicFingerTracking) NetworkManager.SendText("{\"mode\": \"design3\"}", verboseLogging);

        if (displayMode != DisplayMode.DynamicFingerTracking)
        {
            includeHologramsInCameraView = false;
            _videoCapture.StopVideoModeAsync(OnVideoModeStopped);
        }

        //Hud.DebugText.text = "Design 3";
        displayMode = DisplayMode.DynamicFingerTracking;
        Display3D.SetDisplayModeBillboard();
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;
    }

    public void DisableDraggingButtonSelected()
    {
        if (currDragMode != DragMode.Disable) NetworkManager.SendText("{\"dragmode\": \"disable\"}", verboseLogging);

        currDragMode = DragMode.Disable;
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;

        var buttons = MenuObject.GetComponentsInChildren<Button>();
        foreach (var button in buttons) button.GetComponentInChildren<Text>().color = Color.black;
    }

    public void MoveButtonSelected()
    {
        if (currDragMode != DragMode.Move) NetworkManager.SendText("{\"dragmode\": \"move\"}", verboseLogging);

        currDragMode = DragMode.Move;
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;

        var buttons = MenuObject.GetComponentsInChildren<Button>();
        foreach(var button in buttons) button.GetComponentInChildren<Text>().color = Color.black;
    }

    public void ScaleButtonSelected()
    {
        if (currDragMode != DragMode.Scale) NetworkManager.SendText("{\"dragmode\": \"scale\"}", verboseLogging);

        currDragMode = DragMode.Scale;
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;

        var buttons = MenuObject.GetComponentsInChildren<Button>();
        foreach (var button in buttons) button.GetComponentInChildren<Text>().color = Color.black;
    }

    public void ZoomButtonSelected()
    {
        if (currDragMode != DragMode.Zoom) NetworkManager.SendText("{\"dragmode\": \"zoom\"}", verboseLogging);

        currDragMode = DragMode.Zoom;
        HideEverything();
        ShowCurrentDisplay();
        menuVisible = false;

        var buttons = MenuObject.GetComponentsInChildren<Button>();
        foreach (var button in buttons) button.GetComponentInChildren<Text>().color = Color.black;
    }

    private void HideEverything()
    {
        HeadsUpDisplayImageObject.SetActive(false);
        Display3DObject.SetActive(false);
        DebugMenuObject.SetActive(false);

        HideMenu();
    }

    private void ShowCurrentDisplay()
    {
        switch(displayMode)
        {
            default:
                break;
            case DisplayMode.Fixed2D:
                HeadsUpDisplayImageObject.SetActive(true);
                break;
            case DisplayMode.Fixed3D:
            case DisplayMode.Flat3D:
            case DisplayMode.DynamicFingerTracking:
                Display3DObject.SetActive(true);
                break;
        }
    }

    private void ShowMenu()
    {
        Vector3 direction = GazeManager.Instance.GazeNormal;
        direction.Normalize(); // probably not necessary
        MenuObject.transform.position = new Vector3(direction.x * 2, direction.y * 2, direction.z * 2);

        if (useDebugMenu)
            DebugMenuObject.SetActive(true);
        else
            MenuObject.SetActive(true);
    }

    private void HideMenu()
    {
        MenuObject.SetActive(false);
        DebugMenuObject.SetActive(false);
    }
}
