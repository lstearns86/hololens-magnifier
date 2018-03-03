using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MagnificationServer
{
    class Server
    {
        public enum State { Searching, Connecting, Connected }
        State state = State.Searching;
        public State CurrentState { get { return state; } }

        internal class ClientInfo
        {
            public string Name;
            public IPAddress Address;
            public bool Connected = false;
            public TcpListener CommandListener = null;
            public TcpClient CommandClient = null;
            public NetworkStream CommandStream = null;
            public TcpListener VideoListener = null;
            public TcpClient VideoClient = null;
            public NetworkStream VideoStream = null;
        }

        /// <summary>
        /// Gets the current Wifi IP address (if connected). May return null if no connection is available
        /// </summary>
        IPAddress WifiAddress
        {
            get
            {
                List<string> ipAddrList = new List<string>();
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && item.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
                return null;
            }
        }

        private int udpPort = 8888, commandPort = 8889, videoPort = 8899;

        Dictionary<string, ClientInfo> clients = new Dictionary<string, ClientInfo>();

        public delegate void ClientsUpdatedDelegate(List<ClientInfo> clients);
        public event ClientsUpdatedDelegate ClientsUpdated;
        private void OnClientsUpdated() { ClientsUpdated?.Invoke(clients.Values.ToList()); }

        public delegate void ClientConnectedDelegate(ClientInfo client);
        public event ClientConnectedDelegate ClientConnected;
        private void OnClientConnected(ClientInfo client) { ClientConnected?.Invoke(client); }

        public delegate void ClientDisconnectedDelegate(ClientInfo client);
        public event ClientDisconnectedDelegate ClientDisconnected;
        private void OnClientDisconnected(ClientInfo client) { ClientDisconnected?.Invoke(client); }

        public delegate void MessageReceivedDelegate(string message);
        public event MessageReceivedDelegate MessageReceived;
        private void OnMessageReceived(string message) { MessageReceived?.Invoke(message); }

        public delegate void ImageReceivedDelegate(Bitmap img);
        public event ImageReceivedDelegate ImageReceived;
        private void OnImageReceived(Bitmap img) { ImageReceived?.Invoke(img); }

        public void ScanForClients()
        {
            Task.Run(() =>
            {
                var server = new UdpClient(udpPort);
                var responseData = Encoding.ASCII.GetBytes(Environment.MachineName);

                while (true)
                {
                    try
                    {
                        var clientEp = new IPEndPoint(IPAddress.Any, 0);
                        var clientRequestData = server.Receive(ref clientEp);
                        var clientRequest = Encoding.ASCII.GetString(clientRequestData);

                        Debug.WriteLine("Detected {0} ({1}), sending response", clientRequest, clientEp.Address.ToString());
                        server.Send(responseData, responseData.Length, clientEp);

                        if (!clients.ContainsKey(clientEp.Address.ToString()))
                            clients.Add(clientEp.Address.ToString(), new ClientInfo() { Name = clientRequest, Address = clientEp.Address });
                        OnClientsUpdated();
                    }
                    catch (Exception ex) { Debug.WriteLine("Error scanning for UDP clients: " + ex.ToString()); }
                }
            });
        }

        TcpListener tcpListener = null;
        public async void Connect()
        {
            try
            {
                if (tcpListener != null) { tcpListener.Stop(); }
                Debug.WriteLine("Attempting to accept TCP connection");
                tcpListener = new TcpListener(WifiAddress, commandPort);
                tcpListener.Start();
                var tempClient = await tcpListener.AcceptTcpClientAsync();
                var tempStream = tempClient.GetStream();

                var endpoint = tempClient.Client.RemoteEndPoint as IPEndPoint;

                if (!clients.ContainsKey(endpoint.Address.ToString())) { Debug.WriteLine("Invalid connection"); tempStream.Close(); tempClient.Close(); return; }

                var client = clients[endpoint.Address.ToString()];
                client.CommandListener = tcpListener;
                client.CommandClient = tempClient;
                client.CommandStream = tempStream;

                Debug.WriteLine("Connected to " + endpoint.Address.ToString() + ", receiving handshake");

                try
                {
                    var reader = new StreamReader(client.CommandStream, Encoding.UTF8);
                    string clientName = reader.ReadLine();
                    if (clientName != client.Name) client.Name = clientName;

                    Debug.WriteLine("Handshake successful, connected to " + clientName);

                    Debug.WriteLine("Establishing video connection");

                    client.VideoListener = new TcpListener(WifiAddress, videoPort);
                    client.VideoListener.Start();
                    client.VideoClient = await client.VideoListener.AcceptTcpClientAsync();
                    client.VideoStream = client.VideoClient.GetStream();

                    Debug.WriteLine("Video connection successful");

                    // start listening to streams in
                    _ = Task.Run(() =>
                    {
                        var commandReader = new StreamReader(client.CommandStream, Encoding.UTF8); ;
                        try
                        {
                            while (true)
                            {
                                string message = commandReader.ReadLine();
                                OnMessageReceived(message);
                            }
                        }
                        catch { Disconnect(client); }
                    });

                    _ = Task.Run(async () =>
                    {
                        //BinaryReader videoReader = new BinaryReader(client.VideoStream);
                        int length;
                        try
                        {
                            while (true)
                            {
                                if (client.VideoStream.DataAvailable)
                                {
                                    byte[] metadataBuffer = new byte[4];
                                    client.VideoStream.Read(metadataBuffer, 0, 4);
                                    length = BitConverter.ToInt32(metadataBuffer, 0);
                                    if (length > 0 && length < 10 * 1024 * 1024)
                                    {
                                        byte[] data = new byte[length];
                                        int lenRead = await client.VideoStream.ReadAsync(data, 0, length);
                                        if (lenRead == length)
                                        {
                                            using (var imgStream = new MemoryStream(data))
                                            {
                                                try
                                                {
                                                    Bitmap img = new Bitmap(imgStream);
                                                    img.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                                                    OnImageReceived(img);
                                                    img.Dispose();
                                                }
                                                catch { Debug.WriteLine("Error: received corrupted image"); }
                                            }
                                        }
                                        data = null;
                                        GC.GetTotalMemory(true);
                                    }
                                }
                            }
                        }
                        catch { Disconnect(client); }
                    });

                    Thread.Sleep(3000);

                    client.Connected = true;

                    OnClientConnected(client);
                }
                catch { Debug.WriteLine("Handshake failed"); }
            }
            catch
            {
                Debug.WriteLine("Connection failed");
            }
        }

        public void DisconnectAll()
        {
            foreach (string ip in clients.Keys)
            {
                ClientInfo client = clients[ip];
                if (client.VideoStream != null) { client.VideoStream.Close(); client.VideoStream = null; }
                if (client.VideoClient != null) { client.VideoClient.Close(); client.VideoClient = null; }
                if (client.VideoListener != null) { client.VideoListener.Stop(); client.VideoListener = null; }
                if (client.CommandStream != null) { client.CommandStream.Close(); client.CommandStream = null; }
                if (client.CommandClient!= null) { client.CommandClient.Close(); client.CommandClient = null; }
                if (client.CommandListener != null) { client.CommandListener.Stop(); client.CommandListener = null; }
            }
            clients.Clear();
        }

        public void Disconnect(ClientInfo client)
        {
            if (client.VideoStream != null) { client.VideoStream.Close(); client.VideoStream = null; }
            if (client.VideoClient != null) { client.VideoClient.Close(); client.VideoClient = null; }
            if (client.VideoListener != null) { client.VideoListener.Stop(); client.VideoListener = null; }
            if (client.CommandStream != null) { client.CommandStream.Close(); client.CommandStream = null; }
            if (client.CommandClient != null) { client.CommandClient.Close(); client.CommandClient = null; }
            if (client.CommandListener != null) { client.CommandListener.Stop(); client.CommandListener = null; }

            OnClientDisconnected(client);
        }

        public void SendImage(Bitmap img)
        {
            List<ClientInfo> disconnectedClients = new List<ClientInfo>();

            try
            {
                // encode the image as a jpeg and convert it to a byte array
                MemoryStream imgStream = new MemoryStream();
                img.Save(imgStream, ImageFormat.Jpeg);
                byte[] bytes = imgStream.ToArray();

                foreach (string ip in clients.Keys)
                {
                    ClientInfo client = clients[ip];
                    if (!client.Connected) continue;
                    try
                    {
                        // write first the size (# of bytes) and then the image data
                        client.VideoStream.Write(BitConverter.GetBytes((int)imgStream.Length), 0, 4);
                        client.VideoStream.Write(bytes, 0, (int)imgStream.Length);
                        client.VideoStream.Flush();
                        //Debug.WriteLine("Sent image of size " + bytes.Length + " to " + client.Name);

                        //if (client.VideoStream.DataAvailable)
                        //    Debug.WriteLine("Client has data available");
                    }
                    catch { Disconnect(client); disconnectedClients.Add(client); }
                }
            }
            catch(Exception ex) { Debug.WriteLine("Error sending image: " + ex.ToString()); }

            foreach (ClientInfo client in disconnectedClients) clients.Remove(client.Address.ToString());
        }

        public void SendText(string message)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
                foreach (string ip in clients.Keys)
                {
                    ClientInfo client = clients[ip];
                    if (!client.Connected) continue;
                    try
                    {
                        client.CommandStream.Write(bytes, 0, bytes.Length);
                        Debug.WriteLine("Sent message to " + client.Name + ": " + message);
                    }
                    catch { Disconnect(client); }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Error sending text: " + ex.ToString()); }
        }
    }
}
