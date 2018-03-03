using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
#endif

public class NetworkManager : MonoBehaviour {

    const int broadcastPort = 8888, commandPort = 8889, videoPort = 8899;

    private string deviceName;
    public string serverName;
    public string serverAddress;

    private bool connected = false;

#if !UNITY_EDITOR
    StreamSocket commandSocket, videoSocket;
    StreamWriter commandOut;
    DataWriter videoOut;
#endif

    public delegate void ConnectedDelegate();
    public event ConnectedDelegate Connected;

    public delegate void LostConnectionDelegate();
    public event LostConnectionDelegate LostConnection;

    public delegate void TextReceivedDelegate(string text);
    public event TextReceivedDelegate TextReceived;

    public delegate void ImageReceivedDelegate(byte[] image, int width, int height);
    public event ImageReceivedDelegate ImageReceived;

    // Use this for initialization
    void Start () {

        deviceName = SystemInfo.deviceName;

        ResetConnection();
	}

    public async void ResetConnection()
    {
#if !UNITY_EDITOR
        DatagramSocket socket = new DatagramSocket();
        socket.MessageReceived += Socket_MessageReceived;

        try
        {
            LoggingManager.Log("Broadcasting over UDP");
            using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), broadcastPort.ToString()))
            {
                using (var writer = new DataWriter(stream))
                {
                    var data = Encoding.ASCII.GetBytes(deviceName);

                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                }
            }
            
        }
        catch(Exception e)
        {
            LoggingManager.LogError(e.ToString());
            LoggingManager.LogError(SocketError.GetStatus(e.HResult).ToString());
            return;
        }

        LoggingManager.Log("exit start");
#endif
    }

    // Update is called once per frame
    void Update () {
		
	}

#if !UNITY_EDITOR
    private async void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        Stream streamIn = args.GetDataStream().AsStreamForRead();
        HostName serverAddressFull;
        using (var reader = new StreamReader(streamIn))
        {
            serverName = await reader.ReadToEndAsync();
            serverAddressFull = args.RemoteAddress;
            serverAddress = serverAddressFull.ToString();
            LoggingManager.Log("Detected " + serverName + " (" + serverAddress + "), attempting to establish tcp connection");
        }

        commandSocket = new StreamSocket();
        await commandSocket.ConnectAsync(serverAddressFull, commandPort.ToString());

        LoggingManager.Log("Tcp connection established, sending handshake");

        var commandStreamOut = commandSocket.OutputStream.AsStreamForWrite();
        commandOut = new StreamWriter(commandStreamOut, Encoding.UTF8);
        commandOut.WriteLine(deviceName);
        commandOut.Flush();

        LoggingManager.Log("Handshake sent, establishing video connection");

        videoSocket = new StreamSocket();
        await videoSocket.ConnectAsync(serverAddressFull, videoPort.ToString());

        Connected?.Invoke();

        connected = true;

        LoggingManager.Log("Video connection established, starting new threads");

        _ = Task.Run(async () =>
        {
            try
            {
                var commandStreamIn = commandSocket.InputStream.AsStreamForRead();
                var reader = new StreamReader(commandStreamIn, Encoding.UTF8);

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

        _ = Task.Run(async () =>
        {
            try
            {
                var videoStreamIn = new DataReader(videoSocket.InputStream);
                videoStreamIn.ByteOrder = ByteOrder.LittleEndian; // need to set this or the bytes will be read in the wrong order

                videoOut = new DataWriter(videoSocket.OutputStream);
                videoOut.ByteOrder = ByteOrder.LittleEndian;

                //byte[] buffer = new byte[640 * 640 * 3];

                // load the bytes for the first integer, the image size
                var count = await videoStreamIn.LoadAsync(4);

                while (true)
                {
                    int length = videoStreamIn.ReadInt32(); // first read the image size (in # of bytes)

                    //Debug.Log("Attempting to read image of length " + length);

                    // read the image
                    byte[] buffer = new byte[length];
                    count = await videoStreamIn.LoadAsync((uint)length);
                    videoStreamIn.ReadBytes(buffer);
                    //ImageReceived?.Invoke(buffer);

                    // convert the bytes to a stream
                    var imgStream = buffer.AsBuffer().AsStream().AsRandomAccessStream();

                    //Debug.Log("Read image, decoding");

                    // decode the image
                    var decoder = await BitmapDecoder.CreateAsync(imgStream);
                    imgStream.Seek(0);
                    int width = (int)decoder.PixelWidth;
                    int height = (int)decoder.PixelHeight;
                    var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                    var decodedBytes = data.DetachPixelData();

                    // display the image
                    ImageReceived?.Invoke(decodedBytes, width, height);
                    //Debug.Log("Received image");

                    // clean up memory
                    buffer = null;
                    data = null;
                    decodedBytes = null;
                    GC.GetTotalMemory(true);

                    // start to read the next image
                    count = await videoStreamIn.LoadAsync(4);
                }
            }
            catch { LostConnection?.Invoke(); connected = false; }
        });
    }
#endif

    public async void SendText(string text, bool logResult = false)
    {
#if !UNITY_EDITOR
        if(connected)
        {
            await commandOut.WriteLineAsync(text);
            commandOut.Flush();
            if(logResult) LoggingManager.Log("Sent " + text + " to " + serverName + " (" + serverAddress.ToString() + ")");
        }
#endif
    }

    public async void SendImage(byte[] image, int width, int height, bool logResult = false)
    {
        if(connected)
        {
#if !UNITY_EDITOR
            using (var stream = new InMemoryRandomAccessStream())
            {
                var propertySet = new BitmapPropertySet();
                propertySet.Add("ImageQuality", new BitmapTypedValue(0.5, Windows.Foundation.PropertyType.Single));
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream, propertySet);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                encoder.BitmapTransform.ScaledHeight = 320;
                encoder.BitmapTransform.ScaledWidth = (uint)((float)width / height * encoder.BitmapTransform.ScaledHeight);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 72, 72, image);
                try
                {
                    await encoder.FlushAsync();
                }
                catch { LoggingManager.LogError("Couldn't encode image"); }

                var data = new byte[stream.Size];
                await stream.ReadAsync(data.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);

                videoOut.WriteInt32((int)stream.Size);
                await videoOut.StoreAsync();
                videoOut.WriteBytes(data);
                await videoOut.StoreAsync();
                await videoOut.FlushAsync();

                if(logResult) LoggingManager.Log("Sent image of size " + stream.Size + " to " + serverName + " (" + serverAddress + ")");
            }
#endif
        }
    }
}
