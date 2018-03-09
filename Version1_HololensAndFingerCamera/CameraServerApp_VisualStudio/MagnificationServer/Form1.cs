using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.Text;
using Emgu.CV.Util;

using HandSight;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MagnificationServer
{
    public partial class Form1 : Form
    {
        Server server;
        DateTime lastFrameSent = DateTime.Now;
        float frameRate = Properties.Settings.Default.FrameRate;
        float prescale = Properties.Settings.Default.Prescaling;
        float exposure = Properties.Settings.Default.Exposure;
        float size = 1.0f;
        float zoom = 1.0f;
        float distance = 1.0f;
        int mode = 0;
        bool enableThreshold = false, invertImage = false, enableCamera = false;
        int thresholdMode = 0;
        bool pauseStreaming = false;

        bool showDraggingGuide = false;
        PointF mouseStartPosition;
        bool draggingLeft = false, draggingRight = false;
        float locationX = 0, locationY = 0, locationZ = 0;

        Bitmap placeholderImage, invertedPlaceholderImage;

        //Tesseract ocr = null;
        //ERFilterNM2 textDetector = new ERFilterNM2("trained_classifierNM2.xml");

        DateTime lastMoveMessageSent = DateTime.Now;
        float maxMessageRate = 200;

        List<Server.ClientInfo> connectedClients = new List<Server.ClientInfo>();

        public Form1()
        {
            InitializeComponent();

            {
                placeholderImage = new Bitmap(640, 640);
                Graphics g = Graphics.FromImage(placeholderImage);
                g.Clear(Color.White);
                
                Font font = new Font(FontFamily.GenericSansSerif, 36);
                g.DrawString("Placeholder Text", font, Brushes.Black, (placeholderImage.Width - g.MeasureString("Placeholder Text", font).Width) / 2, placeholderImage.Height / 2 - 25 - g.MeasureString("Placeholder Text", font).Height);
                g.DrawString("(Camera Disabled)", font, Brushes.Black, (placeholderImage.Width - g.MeasureString("(Camera Disabled)", font).Width) / 2, placeholderImage.Height / 2 + 25);

                placeholderImage.RotateFlip(RotateFlipType.Rotate180FlipNone);

                var temp = new Image<Gray, byte>(placeholderImage);
                temp = 255 - temp;
                invertedPlaceholderImage = temp.ToBitmap();
            }

            // set up the camera in a background thread so that it doesn't block the UI
            Task.Factory.StartNew(() =>
            {
                Camera.Instance.FrameAvailable += Camera_FrameAvailable;
                Camera.Instance.Connect();
                Camera.Instance.Brightness = 10;
                Camera.Instance.Exposure = (int)exposure;
            });

            server = new Server();
            server.ClientsUpdated += (List<Server.ClientInfo> clients) => 
            {
                Invoke(new MethodInvoker(delegate
                {
                    try
                    {
                        if (connectedClients.Count == 0)
                        {
                            string text = "Clients detected:";
                            foreach (var client in clients) text += client.Name + " (" + client.Address.ToString() + "), ";
                            text = text.TrimEnd(',', ' ');
                            Text = text;
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine("Error updating list of detected clients: " + ex.ToString()); }
                }));

                server.Connect();
            };
            server.ClientConnected += (Server.ClientInfo client) =>
            {
                Invoke(new MethodInvoker(delegate
                {
                    try
                    {
                        connectedClients.Add(client);
                        string text = "Connected to ";
                        foreach (var listClient in connectedClients) text += client.Name + " (" + client.Address.ToString() + "), ";
                        text = text.TrimEnd(',', ' ');
                        Text = text;
                    }
                    catch (Exception ex) { Debug.WriteLine("Error updating client list after connect: " + ex.ToString()); }
                }));
            };
            server.ClientDisconnected += (Server.ClientInfo client) =>
            {
                Invoke(new MethodInvoker(delegate
                {
                    try
                    {
                        connectedClients.Remove(client);
                        string text = "Connected to ";
                        foreach (var listClient in connectedClients) text += client.Name + " (" + client.Address.ToString() + "), ";
                        if (connectedClients.Count == 0) text = "Waiting for Connection";
                        text = text.TrimEnd(',', ' ');
                        Text = text;
                    }
                    catch (Exception ex) { Debug.WriteLine("Error updating client list after disconnect: " + ex.ToString()); }
                }));
            };
            server.MessageReceived += (string message) =>
            {
                if (message == null || message.Length == 0) return;

                Invoke(new MethodInvoker(delegate
                {
                    Debug.WriteLine("Received text: " + message);

                    try
                    {
                        // TODO: use a better JSON parser
                        var match = Regex.Match(message, "\\{\"([a-zA-Z0-9-_.]+)\"\\: ?\"?([a-zA-Z0-9-_.]+)\"?\\}");
                        if (match != null && match.Success)
                        {
                            string command = match.Groups.Count > 1 ? match.Groups[1].Value : null;
                            string argument = match.Groups.Count > 2 ? match.Groups[2].Value : null;

                            Debug.WriteLine("Command recognized: " + command + ", " + argument);
                            if (command == "size")
                            {
                                float value = float.Parse(argument);
                                SizeSlider.Value = (int)((value - 1) * 10.0f);
                            }
                            else if (command == "zoom")
                            {
                                float value = float.Parse(argument);
                                ZoomSlider.Value = (int)(value * 25.0f);
                            }
                            else if (command == "distance")
                            {
                                float value = float.Parse(argument);
                                //DistanceSlider.Value = (int)((value - 0.5f) * 10.0f);
                                //DistanceSlider.Value = (int)(2 * Math.Exp(value - 0.5f));

                                if (value < 1)
                                    DistanceSlider.Value = (int)((value - 0.5f) * 100.0f);
                                else
                                    DistanceSlider.Value = (int)((value - 1) * 5.0f + 50);
                            }
                            else if (command == "mode")
                            {
                                if(argument == "disable")
                                {
                                    DesignChooser.SelectedIndex = 0;
                                }
                                else if (argument == "design1")
                                {
                                    DesignChooser.SelectedIndex = 1;
                                }
                                else if (argument == "design2a")
                                {
                                    DesignChooser.SelectedIndex = 2;
                                }
                                else if (argument == "design2b")
                                {
                                    DesignChooser.SelectedIndex = 3;
                                }
                                else if (argument == "design3")
                                {
                                    DesignChooser.SelectedIndex = 4;
                                }
                            }
                            else if (command == "dragmode")
                            {
                                if(argument == "disable")
                                {
                                    DragModeChooser.SelectedIndex = 0;
                                }
                                if (argument == "move")
                                {
                                    DragModeChooser.SelectedIndex = 1;
                                }
                                else if (argument == "resize")
                                {
                                    DragModeChooser.SelectedIndex = 2;
                                }
                                else if (argument == "zoom")
                                {
                                    DragModeChooser.SelectedIndex = 3;
                                }
                            }
                            else if(command == "menu")
                            {
                                if(argument == "enable")
                                {
                                    EnableMenuCheckbox.Checked = true;
                                }
                                else if (argument == "disable")
                                {
                                    EnableMenuCheckbox.Checked = false;
                                }
                                // TODO: show/hide?
                            }
                            else if (command == "placeholder")
                            {
                                if (argument == "local")
                                {
                                    ShowHololensPlaceholderTextCheckbox.Checked = true;
                                }
                                else
                                {
                                    ShowHololensPlaceholderTextCheckbox.Checked = false;
                                }
                            }
                            else if(command == "verbose")
                            {
                                if(argument == "yes")
                                {
                                    VerboseLoggingCheckbox.Checked = true;
                                }
                                else if(argument == "no")
                                {
                                    VerboseLoggingCheckbox.Checked = false;
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Couldn't parse command: " + message);
                        }
                    }
                    catch { Debug.WriteLine("Couldn't parse command: " + message); }
                }));
            };
            server.ImageReceived += (Bitmap image) =>
            {
                try
                {
                    Bitmap imgCopy = (Bitmap)image.Clone();
                    Invoke(new MethodInvoker(delegate
                    {
                        try
                        {
                            if (ServerDisplay.Image != null) ServerDisplay.Image.Dispose();
                            ServerDisplay.Image = imgCopy;
                        }
                        catch (Exception ex) { Debug.WriteLine("Error updating image from server: " + ex.ToString()); }
                    }));
                }
                catch (Exception ex) { Debug.WriteLine("Error displaying image received from server: " + ex.ToString()); }
            };

            server.ScanForClients();

            DesignChooser.SelectedIndex = mode;
            DragModeChooser.SelectedIndex = 0;
            PrescaleSlider.Value = (int)(prescale * 100);
            RateSlider.Value = (int)(frameRate / 60.0f * 100);
            //SizeSlider.Value = (int)((size - 1) * 100);
            //ZoomSlider.Value = (int)((zoom - 1) / 4.0f * 99) + 1;
            ExposureSlider.Value = (int)exposure;

            //Task.Run(() =>
            //{
            //    InitOcr("", "eng", OcrEngineMode.TesseractOnly);
            //});
        }

        //private static void TesseractDownloadLangFile(String folder, String lang)
        //{
        //    String subfolderName = "tessdata";
        //    String folderName = System.IO.Path.Combine(folder, subfolderName);
        //    if (!System.IO.Directory.Exists(folderName))
        //    {
        //        System.IO.Directory.CreateDirectory(folderName);
        //    }
        //    String dest = System.IO.Path.Combine(folderName, String.Format("{0}.traineddata", lang));
        //    if (!System.IO.File.Exists(dest))
        //        using (System.Net.WebClient webclient = new System.Net.WebClient())
        //        {
        //            String source =
        //                String.Format("https://github.com/tesseract-ocr/tessdata/blob/4592b8d453889181e01982d22328b5846765eaad/{0}.traineddata?raw=true", lang);

        //            Console.WriteLine(String.Format("Downloading file from '{0}' to '{1}'", source, dest));
        //            webclient.DownloadFile(source, dest);
        //            Console.WriteLine(String.Format("Download completed"));
        //        }
        //}

        //private void InitOcr(String path, String lang, OcrEngineMode mode)
        //{
        //    try
        //    {
        //        if (ocr != null)
        //        {
        //            ocr.Dispose();
        //            ocr = null;
        //        }

        //        if (String.IsNullOrEmpty(path))
        //            path = ".";

        //        TesseractDownloadLangFile(path, lang);
        //        TesseractDownloadLangFile(path, "osd"); //script orientation detection
        //        String pathFinal = path.Length == 0 ||
        //                           path.Substring(path.Length - 1, 1).Equals(Path.DirectorySeparatorChar.ToString())
        //            ? path
        //            : String.Format("{0}{1}", path, System.IO.Path.DirectorySeparatorChar);

        //        ocr = new Tesseract(pathFinal, lang, mode);
        //    }
        //    catch (System.Net.WebException e)
        //    {
        //        ocr = null;
        //        throw new Exception("Unable to download tesseract lang file. Please check internet connection.", e);
        //    }
        //    catch (Exception e)
        //    {
        //        ocr = null;
        //    }
        //}

        private void Camera_FrameAvailable(Bitmap frame)
        {
            try
            {
                if (frame == null) return;

                FPS.Camera.Update();

                if(enableThreshold || invertImage)
                {
                    var img = new Image<Gray, byte>(frame);
                    if (enableThreshold)
                    {
                        //CvInvoke.EqualizeHist(img, img);

                        if(thresholdMode == 0)
                            CvInvoke.Threshold(img, img, 100, 255, Emgu.CV.CvEnum.ThresholdType.Otsu);
                        else if(thresholdMode == 1)
                            CvInvoke.AdaptiveThreshold(img, img, 255, Emgu.CV.CvEnum.AdaptiveThresholdType.GaussianC, Emgu.CV.CvEnum.ThresholdType.Binary, 31, 15);

                        //CvInvoke.MedianBlur(img, img, 7);
                        //CvInvoke.Dilate(img, img, null, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(255));
                        //CvInvoke.Erode(img, img, null, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(255));
                    }
                    if (invertImage) img = 255 - img;
                    frame = img.ToBitmap();
                }
                //else if(gamma != 1)
                //{
                //    var img = new Image<Gray, float>(frame);
                //    CvInvoke.Pow(img, gamma, img);
                //    frame = img.ToBitmap();
                //}

                Bitmap imageCopy = (Bitmap)frame.Clone();
                Invoke(new MethodInvoker(delegate
                {
                    try
                    {
                        if (Display.Image != null) Display.Image.Dispose();
                        Display.Image = imageCopy;
                    }
                    catch(Exception ex) { Debug.WriteLine("Error updating camera image: " + ex.ToString()); }
                }));

                //var img = new Image<Gray, byte>(frame);

                ////if (ocr != null)
                ////{
                ////    ocr.SetImage(img);
                ////    ocr.Recognize();
                ////    var chars = ocr.GetCharacters();
                ////}

                //VectorOfERStat regions = new VectorOfERStat();
                //textDetector.Run(img, regions);

                //img.Dispose();

                double frameTime = enableCamera ? 1.0 / frameRate : 1;
                DateTime currTime = DateTime.Now;
                if ((currTime - lastFrameSent).TotalSeconds > frameTime && !pauseStreaming)
                {
                    FPS.Instance("Stream").Update();





                    frame.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    Bitmap newFrame = new Bitmap((int)(frame.Width * prescale), (int)(frame.Height * prescale));
                    Graphics g = Graphics.FromImage(newFrame);
                    g.DrawImage(enableCamera ? frame : (invertImage ? invertedPlaceholderImage : placeholderImage), 0, 0, newFrame.Width, newFrame.Height);
                    server.SendImage(newFrame);
                    newFrame.Dispose();
                    //frame.Dispose();

                    lastFrameSent = currTime;

                    Invoke(new MethodInvoker(delegate
                    {
                        try
                        {
                            InfoLabel.Text = "Camera " + FPS.Camera.Average.ToString("0") + " fps, Stream " + FPS.Instance("Stream").Average.ToString("0") + " fps";
                        }
                        catch (Exception ex) { Debug.WriteLine("Error setting info label text: " + ex.ToString()); }
                    }));
                }
            }
            catch (Exception ex) { Debug.WriteLine("Error processing frame from finger camera: " + ex.ToString()); }
        }

        bool closing = false;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!closing)
            {
                closing = true;
                Camera.Instance.Disconnect();
                server.DisconnectAll();
                e.Cancel = true;
                Task.Run(() => { Thread.Sleep(500); Invoke(new MethodInvoker(delegate { Close(); })); });
            }
        }

        private void PrescaleSlider_Scroll(object sender, EventArgs e)
        {
            prescale = PrescaleSlider.Value / 100.0f;
            Properties.Settings.Default.Prescaling = prescale;
            Properties.Settings.Default.Save();
            ValuesTooltip.SetToolTip(PrescaleSlider, prescale.ToString());
        }

        private void RateSlider_Scroll(object sender, EventArgs e)
        {
            frameRate = RateSlider.Value / 100.0f * 60.0f;
            Properties.Settings.Default.FrameRate = frameRate;
            Properties.Settings.Default.Save();
            ValuesTooltip.SetToolTip(RateSlider, frameRate.ToString());
        }

        private void SizeSlider_Scroll(object sender, EventArgs e)
        {
            size = SizeSlider.Value / 10.0f + 1;
            server.SendText("{\"size\": " + size + "}");
            ValuesTooltip.SetToolTip(SizeSlider, size.ToString());
        }

        private void ZoomSlider_Scroll(object sender, EventArgs e)
        {
            zoom = ZoomSlider.Value / 25.0f;
            server.SendText("{\"zoom\": " + zoom + "}");
            ValuesTooltip.SetToolTip(ZoomSlider, zoom.ToString());
        }

        private void DistanceSlider_Scroll(object sender, EventArgs e)
        {
            //distance = DistanceSlider.Value / 10.0f + 0.5f;
            //distance = (float)Math.Log(distance) + 0.5f;
            if (DistanceSlider.Value < 50)
                distance = 0.5f + DistanceSlider.Value / 100.0f;
            else if (DistanceSlider.Value >= 50)
                distance = 1 + (DistanceSlider.Value - 50) / 5.0f;
            server.SendText("{\"distance\": " + distance + "}\"");

            ValuesTooltip.SetToolTip(DistanceSlider, distance.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void ResetPositionButton_Click(object sender, EventArgs e)
        {
            server.SendText("{\"position\": \"reset\"}");
            ValuesTooltip.SetToolTip(ServerDisplay, "reset");
        }

        private void InvertCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            invertImage = InvertCheckbox.Checked;
        }

        private void OtsuThresholdButton_CheckedChanged(object sender, EventArgs e)
        {
            if(OtsuThresholdButton.Checked) thresholdMode = 0;
        }

        private void AdaptiveThresholdButton_CheckedChanged(object sender, EventArgs e)
        {
            if (AdaptiveThresholdButton.Checked) thresholdMode = 1;
        }

        private void ServerDisplay_MouseDown(object sender, MouseEventArgs e)
        {
            mouseStartPosition = e.Location;
            server.SendText("{\"dragmode\": \"move\"}");
            server.SendText("{\"position\": \"absolute\"}");
            draggingLeft = e.Button == MouseButtons.Left;
            draggingRight = e.Button == MouseButtons.Right;
            SendPosition(e.X, e.Y);
        }

        private void ServerDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                SendPosition(e.X, e.Y);
        }

        private void ExposureSlider_Scroll(object sender, EventArgs e)
        {
            exposure = ExposureSlider.Value;
            Properties.Settings.Default.Exposure = exposure;
            Properties.Settings.Default.Save();

            Camera.Instance.Exposure = (int)exposure;
            ValuesTooltip.SetToolTip(ExposureSlider, exposure.ToString());
        }

        private void EnableMenuCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            server.SendText("{\"menu\": \"" + (EnableMenuCheckbox.Checked ? "enable" : "disable") + "\"}");
        }

        private void ShowMenuButton_Click(object sender, EventArgs e)
        {
            server.SendText("{\"menu\": \"show\"}");
        }

        private void HideMenuButton_Click(object sender, EventArgs e)
        {
            server.SendText("{\"menu\": \"hide\"}");
        }

        private void DesignChooser_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(DesignChooser.SelectedIndex)
            {
                default:
                case 0:
                    server.SendText("{\"mode\": \"disable\"}");
                    break;
                case 1:
                    server.SendText("{\"mode\": \"design1\"}");
                    break;
                case 2:
                    server.SendText("{\"mode\": \"design2a\"}");
                    break;
                case 3:
                    server.SendText("{\"mode\": \"design2b\"}");
                    break;
                case 4:
                    server.SendText("{\"mode\": \"design3\"}");
                    break;
            }
            EnableCameraCheckbox.Checked = false;
            DragModeChooser.SelectedIndex = 0;
        }

        private void DragModeChooser_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (DragModeChooser.SelectedIndex)
            {
                default:
                case 0:
                    server.SendText("{\"dragmode\": \"disable\"}");
                    break;
                case 1:
                    server.SendText("{\"dragmode\": \"move\"}");
                    break;
                case 2:
                    server.SendText("{\"dragmode\": \"resize\"}");
                    break;
                case 3:
                    server.SendText("{\"dragmode\": \"zoom\"}");
                    break;
            }
        }

        private async void FixLagButton_Click(object sender, EventArgs e)
        {
            pauseStreaming = true;
            await Task.Delay(3000);
            pauseStreaming = false;
        }

        private void VerboseLoggingCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            server.SendText("{\"verbose\": \"" + (VerboseLoggingCheckbox.Checked ? "on" : "off") + "\"}");
        }

        private void ResetCameraButton_Click(object sender, EventArgs e)
        {
            server.SendText("{\"camera\": \"reset\"}");
        }

        private void ServerDisplay_MouseEnter(object sender, EventArgs e)
        {
            showDraggingGuide = true;
            ServerDisplay.Refresh();
        }

        private void ServerDisplay_MouseLeave(object sender, EventArgs e)
        {
            showDraggingGuide = false;
            ServerDisplay.Refresh();
        }

        private void ShowHololensPlaceholderTextCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            server.SendText("{\"placeholder\": \"" + (ShowHololensPlaceholderTextCheckbox.Checked ? "local" : "server") + "\"}");
        }

        private void ServerDisplay_Paint(object sender, PaintEventArgs e)
        {
            if(showDraggingGuide)
            {
                e.Graphics.DrawRectangle(Pens.Red, ServerDisplay.Width * 0.15f, ServerDisplay.Height / 2 - (ServerDisplay.Width * 9.0f / 16.0f) * 0.35f, ServerDisplay.Width * 0.6f, (ServerDisplay.Width * 9.0f / 16.0f) * 0.65f);

                //Pen backgroundPen = new Pen(Brushes.Black, 3);
                //e.Graphics.DrawLine(backgroundPen, ServerDisplay.Width / 2, 0, ServerDisplay.Width / 2, ServerDisplay.Height);
                //e.Graphics.DrawLine(backgroundPen, 0, ServerDisplay.Height / 2, ServerDisplay.Width, ServerDisplay.Height / 2);
                //e.Graphics.DrawLine(Pens.White, ServerDisplay.Width / 2, 0, ServerDisplay.Width / 2, ServerDisplay.Height);
                //e.Graphics.DrawLine(Pens.White, 0, ServerDisplay.Height / 2, ServerDisplay.Width, ServerDisplay.Height / 2);
            }
        }

        private void ClientStreamingRateSlider_Scroll(object sender, EventArgs e)
        {
            server.SendText("{\"streamingrate\": " + ClientStreamingRateSlider.Value + "}");
            ValuesTooltip.SetToolTip(ClientStreamingRateSlider, ClientStreamingRateSlider.Value.ToString());
        }

        private void EnableCameraCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            enableCamera = EnableCameraCheckbox.Checked;
            server.SendText("{\"placeholder\": \"" + (!enableCamera && ShowHololensPlaceholderTextCheckbox.Checked ? "local" : "server") + "\"}");
        }

        private void ServerDisplay_MouseUp(object sender, MouseEventArgs e)
        {
            SendPosition(e.X, e.Y);

            draggingLeft = false;
            draggingRight = false;
        }

        private void EnableThresholdCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            enableThreshold = EnableThresholdCheckbox.Checked;
        }

        private void SendPosition(float mouseX, float mouseY)
        {
            //float distX = (float)(x - mouseStartPosition.X) / ServerDisplay.Width;
            //float distY = (float)(y - mouseStartPosition.Y) / ServerDisplay.Height;

            //distY = -distY;

            //distX /= 3;
            //distY /= 3;

            //if (draggingLeft)
            //{
            //    server.SendText("{\"position\": {\"x\": " + distX + ", \"y\": " + distY + ", \"z\": 0}}");
            //}
            //else if (draggingRight)
            //{
            //    server.SendText("{\"position\": {\"x\": 0, \"y\": 0, \"z\": " + distY + "}}");
            //}

            mouseX = (mouseX - ServerDisplay.Width * 0.15f) / (ServerDisplay.Width * 0.6f);
            mouseY = (mouseY - (ServerDisplay.Height / 2 - ServerDisplay.Width * 9.0f / 16.0f * 0.35f)) / (ServerDisplay.Width * 9.0f / 16.0f * 0.65f);

            float x = 2 * mouseX - 1;
            float y = 2 * mouseY - 1;

            y = -y;

            if(draggingLeft)
            {
                locationX = x;
                locationY = y;
                server.SendText("{\"position\": {\"x\": " + locationX + ", \"y\": " + locationY + "}}");
                ValuesTooltip.SetToolTip(ServerDisplay, "(" + locationX + ", " + locationY + ", " + locationZ + ")");
            }
            else if(draggingRight)
            {
                locationZ = y;
                server.SendText("{\"position\": {\"z\": " + locationZ + "}}");
                ValuesTooltip.SetToolTip(ServerDisplay, "(" + locationX + ", " + locationY + ", " + locationZ + ")");
            }
        }
    }
}
