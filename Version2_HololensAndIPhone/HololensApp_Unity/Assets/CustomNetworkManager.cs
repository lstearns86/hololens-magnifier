using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
#endif

public class CustomNetworkManager : MonoBehaviour {

    const int broadcastPort = 8888, commandPort = 8889, videoPort = 8899;

    private string deviceName;
    public string serverName;
    public string serverAddress;

#if !UNITY_EDITOR
    StreamSocketListener commandSocket, videoSocket;
    StreamWriter commandOut;
    DataWriter videoOut;
#endif

    private bool connected = false;

    public delegate void ConnectedDelegate();
    public event ConnectedDelegate Connected;

    public delegate void LostConnectionDelegate();
    public event LostConnectionDelegate LostConnection;

    public delegate void TextReceivedDelegate(string text);
    public event TextReceivedDelegate TextReceived;

    public delegate void ImageReceivedDelegate(byte[] jpegImage);
    public event ImageReceivedDelegate ImageReceived;

    public delegate void ImageDecodedDelegate(byte[] image, int width, int height);
    public event ImageDecodedDelegate ImageDecoded;

    // Use this for initialization
    async void Start ()
    {

#if !UNITY_EDITOR

        // broadcast a service over mDNS / Bonjour / Zeroconfig to avoid having to enter an IP addres
        // arguably this should be the client and the camera should be the server, but I couldn't get it to work that way around for some reason
        LoggingManager.Log("Starting Bonjour");
        mDNS.Logging.LogManager.MessageReceived += LogManager_MessageReceived;
        var service = new mDNS.mDNS(false);
        await service.Init();
        LoggingManager.Log("Initialized Bonjour, broadcasting service");
        service.RegisterService(new mDNS.ServiceInfo("_hololens3._tcp.local.", SystemInfo.deviceName, broadcastPort, "HELLO"));

        // establish command connection
        commandSocket = new StreamSocketListener();
        commandSocket.ConnectionReceived += Server_ConnectionReceived;
        await commandSocket.BindServiceNameAsync(commandPort.ToString());

        // establish video connection
        videoSocket = new StreamSocketListener();
        videoSocket.ConnectionReceived += Server_ConnectionReceived;
        await videoSocket.BindServiceNameAsync(videoPort.ToString());
#endif

    }

#if !UNITY_EDITOR
    private void Server_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        if (sender == commandSocket)
        {
            LoggingManager.Log("Command connection received");
            Connected?.Invoke();
            _ = Task.Run(async () =>
            {
                try
                {
                    var commandStreamIn = args.Socket.InputStream.AsStreamForRead();
                    var reader = new StreamReader(commandStreamIn, Encoding.UTF8);

                    var commandStreamOut = args.Socket.OutputStream.AsStreamForWrite();
                    commandOut = new StreamWriter(commandStreamOut, Encoding.UTF8);                    

                    while (true)
                    {
                        string message = await reader.ReadLineAsync();
                        if (message.Length > 0)
                        {
                            TextReceived?.Invoke(message);
                            //LoggingManager.Log("Received message: " + message);
                        }
                    }
                }
                catch { LostConnection?.Invoke(); connected = false; }
            });
        }
        else if (sender == videoSocket)
        {
            LoggingManager.Log("Video connection received");
            _ = Task.Run(async () =>
            {
                try
                {
                    var videoStreamIn = new DataReader(args.Socket.InputStream);
                    videoStreamIn.ByteOrder = ByteOrder.LittleEndian; // need to set this or the bytes will be read in the wrong order

                    // load the bytes for the first five integers (4 bytes each), the image size and four values for the time (h, m, s, ms)
                    var count = await videoStreamIn.LoadAsync(20);

                    double avgLag = 0, avgLagCount = 0;
                    double avgFps = 0, frameCount = 0;
                    Queue<double> fpsQueue = new Queue<double>();
                    double prevFps = 0;
                    double minFps = 0;

                    while (true)
                    {
                        int length = videoStreamIn.ReadInt32(); // first read the image size (in # of bytes)

                        int h = videoStreamIn.ReadInt32(); // hours
                        int m = videoStreamIn.ReadInt32(); // minutes
                        int s = videoStreamIn.ReadInt32(); // seconds
                        int ms = videoStreamIn.ReadInt32(); // milliseconds

                        DateTime timeReceived = DateTime.Now;
                        DateTime timeSent = new DateTime(timeReceived.Year, timeReceived.Month, timeReceived.Day, h, m, s, ms);

                        double elapsed = (timeReceived - timeSent).TotalMilliseconds;
                        //LoggingManager.Log("Frame Lag: " + elapsed.ToString("0") + " ms");

                        // average the first several frames to measure base lag
                        if(avgLagCount < 10)
                        {
                            avgLag += elapsed / 10;
                            avgLagCount++;
                        }

                        //LoggingManager.Log("Attempting to read image of length " + length);

                        // read the image
                        DateTime start = DateTime.Now;
                        byte[] buffer = new byte[length];
                        count = await videoStreamIn.LoadAsync((uint)length);
                        videoStreamIn.ReadBytes(buffer);
                        //ImageReceived?.Invoke(buffer);
                        //ImageDecoded?.Invoke(buffer, 720, 1280);

                        // Fixes lag getting worse over time, but causes image freezing issue
                        //if(videoStreamIn.UnconsumedBufferLength > 0 || (elapsed > avgLag + 200))
                        //{
                        //    LoggingManager.Log("Frame Dropped (" + (elapsed > avgLag + 200 ? "excess lag" : "next frame ready") + ")");

                        //    // clean up used memory
                        //    buffer = null;
                        //    GC.GetTotalMemory(true);

                        //    // start to read the next image
                        //    count = await videoStreamIn.LoadAsync(20);

                        //    // skip to next frame without procesing the current one
                        //    continue;
                        //}

                        // send confirmation that we received this frame along with the minimum fps over the past couple of seconds (rounded down to prevent lag)
                        if(commandOut != null) {
                            string confirmationString = string.Format("{0},{1},{2},{3},{4}\n", h, m, s, ms, (int)Math.Ceiling(minFps - 1));
                            commandOut.Write(confirmationString);
                            commandOut.Flush();
                            //Debug.Log(confirmationString);
                        }

                        //LoggingManager.Log("Read Image: " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0"));
                        //start = DateTime.Now;

                        // convert the bytes to a stream
                        var imgStream = buffer.AsBuffer().AsStream().AsRandomAccessStream();

                        //LoggingManager.Log("Read image, decoding");

                        // decode the image
                        var decoder = await BitmapDecoder.CreateAsync(imgStream);
                        imgStream.Seek(0);
                        int width = (int)decoder.PixelWidth;
                        int height = (int)decoder.PixelHeight;

                        //LoggingManager.Log("Create Decoder: " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0"));
                        //start = DateTime.Now;

                        var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);

                        //LoggingManager.Log("Get Pixel Data: " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0"));
                        //start = DateTime.Now;

                        var decodedBytes = data.DetachPixelData();

                        //LoggingManager.Log("Detach Pixel Data: " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0"));
                        //start = DateTime.Now;

                        //LoggingManager.Log("Decode Image: " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0"));
                        //start = DateTime.Now;

                        // display the image
                        ImageDecoded?.Invoke(decodedBytes, width, height);
                        //LoggingManager.Log("Received image (" + width + " x " + height + "), " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0") + "ms");
                        //LoggingManager.Log("Received Image: " + (DateTime.Now - start).TotalMilliseconds.ToString("0.0"));

                        // clean up memory
                        buffer = null;
                        data = null;
                        decodedBytes = null;
                        GC.GetTotalMemory(true);

                        // update fps
                        double currFps = 1.0 / (DateTime.Now - start).TotalSeconds;
                        if (frameCount > 0) avgFps *= frameCount;
                        avgFps += currFps;
                        frameCount++;
                        avgFps /= frameCount;
                        prevFps = currFps;
                        fpsQueue.Enqueue(currFps);
                        if (fpsQueue.Count > 20) fpsQueue.Dequeue();
                        minFps = double.MaxValue;
                        foreach (double val in fpsQueue) if (val < minFps) minFps = val;

                        // start to read the next image
                        count = await videoStreamIn.LoadAsync(20);
                    }
                }
                catch { LostConnection?.Invoke(); connected = false; }
            });
        }
    }

    private void LogManager_MessageReceived(object sender, mDNS.Logging.LogMessageEventArgs e)
    {
        // ignore
    }
#endif

    // Update is called once per frame
    void Update ()
    {
		// don't need to do anything in the main loop
	}
}
