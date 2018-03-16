using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour {

    const double videoDefaultScaleX = 6.564815e-05, videoDefaultScaleY = 7.479166e-05;
    const double staticVideoDefaultScaleX = 2, staticVideoDefaultScaleY = 2;

    enum UIMode { Default, Calibration, FineCalibration }
    private UIMode uiMode = UIMode.Default;

    enum Attachment { Self, World, Phone }
    private Attachment attachmentMode = Attachment.Phone;

    enum Positioning { Fixed, Active }
    private Positioning positioningMode = Positioning.Fixed;

    private System.Random rand = new System.Random();

    private Quaternion lastRotation, lastCorrectedRotation;
    private Vector3 lastPosition, lastCorrectedPosition;
    private Quaternion worldRotation;
    private Vector3 worldTranslation;
    private Vector3 worldOrigin;
    private Vector3 calibrationPosition;
    private Quaternion calibrationRotation;
    private float calibrationInitRotation = 0;
    private bool imageFrozen = false;

    private Vector3 selfOffset = Vector3.zero;
    private Quaternion selfRotation = Quaternion.identity;
    private Vector3 phoneOffset = Vector3.zero;
    private Quaternion phoneRotation = Quaternion.identity;

    private List<Tuple<Transform, Transform>> correspondences = new List<Tuple<Transform, Transform>>();

    public GameObject VirtualPhone;
    public Canvas VideoCanvas;
    public RawImage VideoFrame;
    public TextMesh StatusText;

    private CustomNetworkManager _networkManager = null;
    private CustomNetworkManager NetworkManager { get { if (_networkManager == null) _networkManager = GetComponent<CustomNetworkManager>(); return _networkManager; } }

    // Use this for initialization
    void Start() {

        VideoCanvas.gameObject.SetActive(false);
        VirtualPhone.gameObject.SetActive(false);
        StatusText.gameObject.SetActive(true);

        VideoFrame.texture = new Texture2D(1080, 1920, TextureFormat.BGRA32, false);

        NetworkManager.Connected += () =>
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                StatusText.gameObject.SetActive(false);
                //VirtualPhone.SetActive(true);
                VideoCanvas.gameObject.SetActive(true);
            }, false);
        };
        // TODO: handle lost connection / reconnection
        NetworkManager.TextReceived += (string message) =>
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                try
                {
                    // TODO: use a better JSON parser
                    var match = Regex.Match(message, "\\{\"?([a-zA-Z0-9-_.]+)\"?\\: ?\"?(.+)\"?\\}");
                    if (match != null && match.Success)
                    {
                        string command = match.Groups.Count > 1 ? match.Groups[1].Value : null;
                        string argument = match.Groups.Count > 2 ? match.Groups[2].Value : null;
                        if(argument != null) argument = argument.TrimEnd('"');

                        //LoggingManager.Log("Command recognized: " + command + ", " + argument);
                        if (command == "transform")
                        {
                            var values = Regex.Match(argument, "\\[([0-9-.e]+),([0-9-.e]+),([0-9-.e]+),([0-9-.e]+);([0-9-.e]+),([0-9-.e]+),([0-9-.e]+),([0-9-.e]+);([0-9-.e]+),([0-9-.e]+),([0-9-.e]+),([0-9-.e]+)\\]");

                            float m00 = 0, m01 = 0, m02 = 0, m03 = 0, m10 = 0, m11 = 0, m12 = 0, m13 = 0, m20 = 0, m21 = 0, m22 = 0, m23 = 0;
                            bool success = false;
                            if (values != null && values.Success)
                            {
                                string m00Str = values.Groups.Count > 1 ? values.Groups[1].Value : null;
                                string m01Str = values.Groups.Count > 2 ? values.Groups[2].Value : null;
                                string m02Str = values.Groups.Count > 3 ? values.Groups[3].Value : null;
                                string m03Str = values.Groups.Count > 4 ? values.Groups[4].Value : null;
                                string m10Str = values.Groups.Count > 5 ? values.Groups[5].Value : null;
                                string m11Str = values.Groups.Count > 6 ? values.Groups[6].Value : null;
                                string m12Str = values.Groups.Count > 7 ? values.Groups[7].Value : null;
                                string m13Str = values.Groups.Count > 8 ? values.Groups[8].Value : null;
                                string m20Str = values.Groups.Count > 9 ? values.Groups[9].Value : null;
                                string m21Str = values.Groups.Count > 10 ? values.Groups[10].Value : null;
                                string m22Str = values.Groups.Count > 11 ? values.Groups[11].Value : null;
                                string m23Str = values.Groups.Count > 12 ? values.Groups[12].Value : null;

                                try
                                {
                                    m00 = m00Str == null ? 0 : float.Parse(m00Str);
                                    m01 = m01Str == null ? 0 : float.Parse(m01Str);
                                    m02 = m02Str == null ? 0 : float.Parse(m02Str);
                                    m03 = m03Str == null ? 0 : float.Parse(m03Str);
                                    m10 = m10Str == null ? 0 : float.Parse(m10Str);
                                    m11 = m11Str == null ? 0 : float.Parse(m11Str);
                                    m12 = m12Str == null ? 0 : float.Parse(m12Str);
                                    m13 = m13Str == null ? 0 : float.Parse(m13Str);
                                    m20 = m20Str == null ? 0 : float.Parse(m20Str);
                                    m21 = m21Str == null ? 0 : float.Parse(m21Str);
                                    m22 = m22Str == null ? 0 : float.Parse(m22Str);
                                    m23 = m23Str == null ? 0 : float.Parse(m23Str);
                                    success = true;
                                }
                                catch { LoggingManager.LogError("Bad argument: " + m00Str + ", " + m01Str + ", " + m02Str + ", " + m03Str + ", " + m10Str + ", " + m11Str + ", " + m12Str + ", " + m13Str + ", " + m20Str + ", " + m21Str + ", " + m22Str + ", " + m23Str); }
                            }
                            else { LoggingManager.LogError("Couldn't parse coordinates: " + argument); }

                            if(success)
                            {
                                //Matrix4x4 m = new Matrix4x4(new Vector4(m00, m10, m20, m30), new Vector4(m01, m11, m21, m31), new Vector4(m02, m12, m22, m32), new Vector4(m03, m13, m23, m33));

                                // extract rotation
                                Vector3 forward = new Vector3(m02, m12, m22);
                                Vector3 upward = new Vector3(m01, m11, m21);
                                Quaternion rotation = Quaternion.LookRotation(forward, upward);

                                // correct rotation (iOS convention: +x = long axis toward home button, +y = short axis toward right, +z upward from screen)
                                rotation = Quaternion.Euler(-rotation.eulerAngles.x, -rotation.eulerAngles.y, rotation.eulerAngles.z);

                                // extract position
                                Vector3 position = new Vector3(m03, m13, -m23);

                                Transform phoneTransform = new GameObject().transform;
                                phoneTransform.rotation = rotation;
                                phoneTransform.Rotate(Vector3.forward, -90);
                                phoneTransform.Rotate(Vector3.up, 180);
                                phoneTransform.position = position;
                                phoneTransform.RotateAround(Vector3.zero, Vector3.up, worldRotation.eulerAngles.y);
                                //phoneTransform.rotation *= worldRotation;
                                phoneTransform.position += worldTranslation;

                                lastCorrectedPosition = phoneTransform.position;
                                lastCorrectedRotation = phoneTransform.rotation;

                                if ((uiMode == UIMode.FineCalibration && positioningMode == Positioning.Fixed) || 
                                (uiMode == UIMode.Default && attachmentMode == Attachment.Phone && positioningMode == Positioning.Fixed) ||
                                (uiMode == UIMode.Default && attachmentMode == Attachment.World && positioningMode == Positioning.Active) ||
                                (uiMode == UIMode.Default && attachmentMode == Attachment.Self && positioningMode == Positioning.Active))
                                {
                                    VideoCanvas.transform.rotation = phoneTransform.rotation;
                                    VideoCanvas.transform.position = phoneTransform.position;

                                    // store the current offsets from the default self position and rotation
                                    if (attachmentMode == Attachment.Self)
                                    {
                                        Vector3 displayPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
                                        Vector3 directionToCamera = Camera.main.transform.position - displayPosition;
                                        Transform tempTransform = new GameObject().transform;
                                        tempTransform.position = displayPosition;
                                        tempTransform.rotation = Quaternion.LookRotation(-directionToCamera);
                                        tempTransform.Rotate(Vector3.right, 180);

                                        selfOffset = Camera.main.transform.InverseTransformVector(VideoCanvas.transform.position - tempTransform.position);
                                        selfRotation = Quaternion.Inverse(tempTransform.localRotation) * VideoCanvas.transform.localRotation;
                                    }

                                    // apply offsets
                                    if(attachmentMode == Attachment.Phone && uiMode == UIMode.Default)
                                    {
                                        VideoCanvas.transform.position += Camera.main.transform.TransformVector(phoneOffset);
                                        VideoCanvas.transform.localRotation *= phoneRotation;
                                    }
                                }
                                else if (uiMode == UIMode.FineCalibration && positioningMode == Positioning.Active)
                                {
                                    phoneTransform.position -= worldTranslation;
                                    //phoneTransform.rotation *= Quaternion.Inverse(worldRotation);
                                    phoneTransform.RotateAround(Vector3.zero, Vector3.up, -worldRotation.eulerAngles.y);
                                    worldRotation = Quaternion.Euler(0, VideoCanvas.transform.localRotation.eulerAngles.y - phoneTransform.localRotation.eulerAngles.y, 0);
                                    //worldRotation = Quaternion.Inverse(VideoCanvas.transform.rotation) * phoneTransform.rotation;
                                    worldTranslation = VideoCanvas.transform.position - worldRotation * phoneTransform.position;
                                }
                                else if(uiMode == UIMode.Default && attachmentMode == Attachment.Phone && positioningMode == Positioning.Active)
                                {
                                    phoneOffset = Camera.main.transform.InverseTransformVector(VideoCanvas.transform.position - phoneTransform.position);
                                    phoneRotation = Quaternion.Inverse(phoneTransform.localRotation) * VideoCanvas.transform.localRotation;
                                }
                                else if(uiMode == UIMode.Calibration)
                                {
                                    // TODO: highlight phone if close enough
                                }

                                lastRotation = rotation;
                                lastPosition = position;
                            }
                        }
                        else if (command == "calibration")
                        {
                            if(argument == "start")
                            {
                                uiMode = UIMode.Calibration;
                                LoggingManager.Log("Calibration Started");
                                StatusText.text = "Calibrating";

                                VirtualPhone.SetActive(true);
                                VideoCanvas.gameObject.SetActive(false);
                                StatusText.gameObject.SetActive(true);

                                correspondences.Clear();
                                worldTranslation = Vector3.zero;
                                worldRotation = Quaternion.identity;

                                VirtualPhone.transform.rotation = Quaternion.identity;
                                VirtualPhone.transform.Rotate(Vector3.right, -60);
                                VirtualPhone.transform.position = Camera.main.transform.position + new Vector3(0, -0.1f, 0.5f);
                                calibrationInitRotation = Camera.main.transform.rotation.eulerAngles.y;
                                VirtualPhone.transform.RotateAround(Camera.main.transform.position, Vector3.up, calibrationInitRotation);
                                calibrationPosition = VirtualPhone.transform.position;
                                calibrationRotation = VirtualPhone.transform.rotation;
                            }
                            else if(argument == "stop")
                            {
                                // reset alignment since calibration has likely changed how the display will appear
                                phoneOffset = Vector3.zero;
                                phoneRotation = Quaternion.identity;
                                selfOffset = Vector3.zero;
                                selfRotation = Quaternion.identity;

                                VirtualPhone.SetActive(false);
                                VideoCanvas.gameObject.SetActive(true);
                                StatusText.gameObject.SetActive(false);

                                uiMode = UIMode.Default;
                                LoggingManager.Log("Calibration Ended");
                            }
                            else if(argument == "fine")
                            {
                                uiMode = UIMode.FineCalibration;
                                LoggingManager.Log("Fine Calibration Started");

                                StatusText.text = "Calibrating (Fine)";
                                StatusText.gameObject.SetActive(true);
                                VirtualPhone.SetActive(false);
                                VideoCanvas.gameObject.SetActive(true);
                            }
                            else if(argument == "store")
                            {
                                Transform a = new GameObject().transform; a.position = calibrationPosition; a.rotation = calibrationRotation;
                                Transform b = new GameObject().transform; b.position = lastPosition; b.rotation = lastRotation;
                                correspondences.Add(new Tuple<Transform, Transform>(a, b));

                                double averageRotationX = 0, averageRotationY = 0;
                                foreach (var corr in correspondences)
                                {
                                    var target = corr.Item1;
                                    var actual = corr.Item2;
                                    float diff = target.eulerAngles.y - actual.eulerAngles.y;
                                    averageRotationX += Math.Cos(diff * Math.PI / 180);
                                    averageRotationY += Math.Sin(diff * Math.PI / 180);
                                }
                                float averageRotation = (float)(Math.Atan2(averageRotationY, averageRotationX) * 180.0 / Math.PI);
                                worldRotation = Quaternion.Euler(0, averageRotation, 0);

                                Vector3 averageTranslation = Vector3.zero;
                                foreach(var corr in correspondences)
                                {
                                    var target = corr.Item1.position;
                                    var actual = corr.Item2.position;
                                    actual = worldRotation * actual;

                                    averageTranslation += target - actual;
                                }
                                averageTranslation /= correspondences.Count;
                                worldTranslation = averageTranslation;

                                LoggingManager.Log("World rotation: " + averageRotation + ", translation: " + worldTranslation);

                                //// compute the centroids of the correspondence points and use them to set the translation
                                //Vector3 centroid = new Vector3(0, 0, 0), targetCentroid = new Vector3(0, 0, 0);
                                //foreach (var corr in correspondences)
                                //{
                                //    targetCentroid += corr.Item1;
                                //    centroid += corr.Item2;
                                //}
                                //centroid /= correspondences.Count;
                                //targetCentroid /= correspondences.Count;
                                //worldTranslation = targetCentroid - centroid;
                                //worldOrigin = targetCentroid;

                                //// compute the rotation of the correspondences relative to the centroids and use them to set the rotation
                                //float averageRotation = 0;
                                //foreach (var corr in correspondences)
                                //{
                                //    Vector2 target = new Vector2(corr.Item1.x - targetCentroid.x, corr.Item1.z - targetCentroid.z);
                                //    Vector2 actual = new Vector2(corr.Item2.x - centroid.x, corr.Item2.z - centroid.z);

                                //    double angle = (float)Math.Acos((target.x * actual.x + target.y * actual.y) / (target.magnitude * actual.magnitude));
                                //    angle *= 180.0 / Math.PI;
                                //    if (angle > 180) angle -= 360;

                                //    averageRotation += (float)angle;
                                //}
                                //averageRotation /= correspondences.Count;
                                //worldRotation = Quaternion.Euler(0, averageRotation2, 0);

                                // move the virtual phone target
                                VirtualPhone.transform.rotation = Quaternion.identity;
                                VirtualPhone.transform.Rotate(Vector3.right, -60);
                                VirtualPhone.transform.position = Camera.main.transform.position + new Vector3(0, -0.1f, 0.5f);
                                float rotation = calibrationInitRotation + correspondences.Count * 45;
                                while (rotation >= 360) rotation -= 360;
                                VirtualPhone.transform.RotateAround(Camera.main.transform.position, Vector3.up, rotation);
                                //VirtualPhone.transform.RotateAround(Camera.main.transform.position, Vector3.right, rand.Next(20) - 10);
                                calibrationPosition = VirtualPhone.transform.position;
                                calibrationRotation = VirtualPhone.transform.rotation;
                            }
                        }
                        else if(command == "scale")
                        {
                            float value = float.Parse(argument);

                            // Locate pivot point for scaling by raycasting from the camera to the canvas
                            RaycastHit hit;
                            Vector3 pivot = VideoCanvas.transform.position;
                            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
                            {
                                Transform objectHit = hit.transform;
                                Vector3 hitPoint3D = hit.point;
                                //Vector3 hitPoint2D = VideoCanvas.transform.TransformPoint(hit.point);

                                pivot = hitPoint3D;
                            }

                            // Apply scale factor
                            double scaleX = VideoCanvas.transform.localScale.x * value;
                            double scaleY = VideoCanvas.transform.localScale.y * value;
                            if (scaleX < videoDefaultScaleX) scaleX = videoDefaultScaleX;
                            if (scaleY < videoDefaultScaleY) scaleY = videoDefaultScaleY;
                            VideoCanvas.transform.localScale = new Vector3((float)scaleX, (float)scaleY, 1);

                            // update position / offset depending on current mode
                            Vector3 offset = (float)(value - 1) * (pivot - VideoCanvas.transform.position);
                            switch (attachmentMode)
                            {
                                case Attachment.Phone:
                                    phoneOffset -= offset;
                                    break;
                                case Attachment.Self:
                                    selfOffset -= offset;
                                    break;
                                case Attachment.World:
                                    VideoCanvas.transform.position -= offset;
                                    break;
                            }
                            //LoggingManager.Log("Updated offset by (" + offset.x + ", " + offset.y + ", " + offset.z + ")");

                            //LoggingManager.Log("Scaled by " + value);
                        }
                        else if(command == "pan")
                        {
                            var values = Regex.Match(argument, "\\{\"?x\"?\\: ?([0-9-.e]+), ?\"?y\"?\\: ?([0-9-.e]+), ?\"?touches\"?\\: ?([0-9]+)\\}");
                            bool success = false;
                            float x = 0, y = 0;
                            int touches = 0;
                            if (values != null && values.Success)
                            {
                                string xStr = values.Groups.Count > 1 ? values.Groups[1].Value : null;
                                string yStr = values.Groups.Count > 2 ? values.Groups[2].Value : null;
                                string tStr = values.Groups.Count > 3 ? values.Groups[3].Value : null;

                                try
                                {
                                    x = xStr == null ? 0 : float.Parse(xStr);
                                    y = yStr == null ? 0 : float.Parse(yStr);
                                    touches = tStr == null ? 0 : int.Parse(tStr);
                                    success = true;
                                }
                                catch { LoggingManager.LogError("Bad argument: " + xStr + ", " + yStr + ", " + tStr); }
                            }

                            if (uiMode == UIMode.FineCalibration)
                            {
                                if(success)
                                {
                                    if(touches == 1)
                                    {
                                        Vector3 translation = new Vector3(x * 5, y * 5, 0);
                                        worldTranslation += VideoCanvas.transform.TransformVector(translation);
                                        //LoggingManager.Log("Set translation: " + worldTranslation.x + ", " + worldTranslation.y);
                                    }
                                    else if(touches == 2)
                                    {
                                        Vector3 translation = new Vector3(0, 0, y / 2000);
                                        worldTranslation += VideoCanvas.transform.TransformVector(translation);
                                        worldRotation = Quaternion.Euler(0, worldRotation.eulerAngles.y + x / 10, 0);
                                        //LoggingManager.Log("Set rotation: " + worldRotation.eulerAngles.y);
                                    }
                                }
                            }
                            else if (imageFrozen)
                            {
                                if(success && touches == 1)
                                {
                                    Vector3 translation = new Vector3(x * 5, y * 5, 0);
                                    switch (attachmentMode)
                                    {
                                        case Attachment.Phone:
                                            phoneOffset += VideoCanvas.transform.TransformVector(translation);
                                            break;
                                        case Attachment.Self:
                                            selfOffset += VideoCanvas.transform.TransformVector(translation);
                                            break;
                                        case Attachment.World:
                                            VideoCanvas.transform.position += VideoCanvas.transform.TransformVector(translation);
                                            break;
                                    }
                                }
                            }
                        }
                        else if (command == "attach")
                        {
                            if(argument == "self")
                            {
                                attachmentMode = Attachment.Self;
                                positioningMode = Positioning.Fixed;
                                selfOffset = Vector3.zero;
                                selfRotation = Quaternion.identity;
                            }
                            else if(argument == "world")
                            {
                                attachmentMode = Attachment.World;
                            }
                            else if(argument == "phone")
                            {
                                attachmentMode = Attachment.Phone;
                                positioningMode = Positioning.Fixed;
                                phoneOffset = Vector3.zero;
                                phoneRotation = Quaternion.identity;
                                VideoCanvas.transform.rotation = lastCorrectedRotation;
                                VideoCanvas.transform.position = lastCorrectedPosition;
                            }
                        }
                        else if (command == "positioning")
                        {
                            if (argument == "start")
                            {
                                //if (attachmentMode == Attachment.Phone)
                                //{
                                //    VideoCanvas.transform.rotation = lastCorrectedRotation;
                                //    VideoCanvas.transform.position = lastCorrectedPosition;
                                //    phoneOffset = Vector3.zero;
                                //    phoneRotation = Quaternion.identity;
                                //}
                                positioningMode = Positioning.Active;
                            }
                            else if (argument == "stop")
                            {
                                positioningMode = Positioning.Fixed;
                            }
                        }
                        else if (command == "frozen")
                        {
                            if (argument == "true")
                            {
                                imageFrozen = true;
                            }
                            else
                            {
                                imageFrozen = false;
                            }
                        }
                    }
                }
                catch { LoggingManager.LogError("Couldn't parse command: " + message); }
            }, false);
        };
        NetworkManager.ImageDecoded += (byte[] image, int width, int height) =>
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                // TODO: check if resolution is changing, adjust zoom level to maintain same size
                //if(VideoFrame.texture != null && (VideoFrame.texture.width != width || VideoFrame.texture.height != height))
                //{
                //    float scaleChange = (float)width / VideoFrame.texture.width;
                //    VideoCanvas.transform.localScale = new Vector3(VideoFrame.transform.localScale.x * scaleChange, VideoFrame.transform.localScale.y * scaleChange, 1);
                //}

                ShowImage(VideoFrame, image, width, height);
            }, false);
        };
    }
	
    private void ShowImage(RawImage target, byte[] image, int width, int height)
    {
        if (target.texture != null && width == target.texture.width && height == target.texture.height)
        {
            var texture = target.texture as Texture2D;
            texture.LoadRawTextureData(image);
            texture.Apply();
        }
        else
        {
            var texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            texture.LoadRawTextureData(image);
            target.texture = texture;
            texture.Apply();
        }
    }

	// Update is called once per frame
	void Update () {
        
        if (uiMode == UIMode.Default && attachmentMode == Attachment.Self && positioningMode == Positioning.Fixed)
        {
            Vector3 displayPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
            Vector3 directionToCamera = Camera.main.transform.position - displayPosition;
            VideoCanvas.transform.position = displayPosition;
            VideoCanvas.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            VideoCanvas.transform.Rotate(Vector3.right, 180);

            // apply offset
            VideoCanvas.transform.position += Camera.main.transform.TransformVector(selfOffset);
            VideoCanvas.transform.localRotation *= selfRotation;
        }
	}
}
