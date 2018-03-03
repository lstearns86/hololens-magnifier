using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Awaiba.Drivers.Grabbers;
using Awaiba.FrameProcessing;

using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HandSight
{
    public class Camera
    {
        int prescaler = 1;
        int exposure = 255;
        IduleProviderCsCam provider;

        // singleton instance
        static Camera instance;
        public static Camera Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Camera();
                }
                return instance;
            }
        }

        // public events
        public delegate void FrameAvailableDelegate(Bitmap frame);
        public event FrameAvailableDelegate FrameAvailable;
        private void OnFrameAvailable(Bitmap frame) { FrameAvailable?.Invoke(frame); }

        public delegate void ErrorDelegate(string msg);
        public event ErrorDelegate Error;
        private void OnError(string msg) { Error?.Invoke(msg); }

        /// <summary>
        /// The camera's pre-scaler brightness, between 0-255. Note that the higher this is, the lower the frame rate will be.
        /// </summary>
        public int Brightness
        {
            get
            {
                return prescaler;
            }
            set
            {
                prescaler = value;
                try
                {
                    if (provider.IsConnected)
                    {
                        provider.WriteRegister(new NanEyeGSRegisterPayload(false, 0x05, true, 0, prescaler));
                    }
                }
                catch (Exception ex) { Debug.WriteLine("Error changing camera brightness: " + ex.ToString()); }
            }
        }

        /// <summary>
        /// The camera's pre-scaler brightness, between 0-255. Note that the higher this is, the lower the frame rate will be.
        /// </summary>
        public int Exposure
        {
            get
            {
                return exposure;
            }
            set
            {
                exposure = value;
                try
                {
                    if (provider.IsConnected)
                    {
                        provider.WriteRegister(new NanEyeGSRegisterPayload(false, 0x06, true, 0, exposure));
                    }
                }
                catch (Exception ex) { Debug.WriteLine("Error changing camera exposure: " + ex.ToString()); }
            }
        }

        /// <summary>
        /// Private Constructor
        /// </summary>
        Camera()
        {
            try
            {
                provider = new IduleProviderCsCam(0);
                provider.Initialize();
                if (provider.IsConnected)
                {
                    provider.ImageProcessed += provider_ImageProcessed;
                    provider.Interrupt += provider_Interrupt;
                    provider.Exception += camera_Exception;
                    provider.WriteRegister(new NanEyeGSRegisterPayload(false, 0x05, true, 0, prescaler));
                    provider.WriteRegister(new NanEyeGSRegisterPayload(false, 0x06, true, 0, exposure));
                    //ProcessingWrapper.pr[0].ReduceProcessing = true;
                }
            }
            catch (Exception ex) { OnError(ex.Message); }
        }

        /// <summary>
        /// Deconstructor
        /// </summary>
        ~Camera()
        {
            if (provider.IsConnected && provider.IsCapturing)
            {
                provider.StopCapture();
                provider.Dispose();
            }
        }

        public bool IsConnected
        {
            get
            {
                return provider.IsConnected;
            }
        }

        public bool IsCapturing
        {
            get
            {
                return provider.IsCapturing;
            }
        }

        /// <summary>
        /// Call this method once you've finished setting up the camera and are ready to begin capturing video
        /// </summary>
        public void Connect()
        {
            try
            {
                stopping = false;
                provider.StartCapture();
            }
            catch (Exception ex) { OnError(ex.Message); }
        }

        /// <summary>
        /// Call this method to stop capturing video
        /// </summary>
        bool stopping = false;
        public void Disconnect()
        {
            try
            {
                stopping = true;
                Task.Factory.StartNew(() => { provider.StopCapture(); }); // has to run in another thread for some reason... it never returns, but must be called or the program crashes
            }
            catch (Exception ex) { OnError(ex.Message); }
        }

        void provider_Interrupt(object sender, InterruptEventArgs e)
        {
            OnError("Camera provider interrupted: flags=" + e.Flags);
        }

        /// <summary>
        /// Called every time a new frame shows up from the camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void provider_ImageProcessed(object sender, OnImageReceivedBitmapEventArgs e)
        {
            if (stopping || e == null || e.PixelData == null) return;

            if (Monitor.TryEnter(this))
            {
                try
                {
                    Awaiba.Imaging.PPMImage ppm = new Awaiba.Imaging.PPMImage(e.Width, e.Height, e.BitsPerPixel, e.PixelData);
                    Bitmap bmp = ppm.ConvertToBitmap();
                    //Bitmap bmp = ArrayToBitmap(e.PixelData, e.Width, e.Height, PixelFormat.Format24bppRgb);

                    // trigger a new frame event
                    OnFrameAvailable(bmp);
                }
                catch (Exception ex) { Debug.WriteLine("Error receiving image from camera: " + ex.ToString()); }

                Monitor.Exit(this);
            }
        }

        /// <summary>
        /// Called every time a new frame shows up from the camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void provider_ImageTransaction(object sender, ImageReceivedEventArgs e)
        {
            if (stopping || e == null || e.PixelData == null) return;

            if (Monitor.TryEnter(this))
            {
                try
                {
                    Bitmap bmp = ArrayToBitmap(e.PixelData, e.Width, e.Height, PixelFormat.Format8bppIndexed);

                    // trigger a new frame event
                    OnFrameAvailable(bmp);
                }
                catch (Exception ex) { Debug.WriteLine("Error receiving image from camera: " + ex.ToString()); }

                Monitor.Exit(this);
            }
        }

        public static Bitmap ArrayToBitmap(byte[] bytes, int width, int height, PixelFormat pixelFormat)
        {
            var image = new Bitmap(width, height, pixelFormat);
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                ImageLockMode.ReadWrite, pixelFormat);
            try
            {
                Marshal.Copy(bytes, 0, imageData.Scan0, bytes.Length);
            }
            catch (Exception ex) { Debug.WriteLine("Error converting bytes to bitmap: " + ex.ToString()); }
            finally
            {
                image.UnlockBits(imageData);
            }
            return image;
        }

        private void camera_Exception(object sender, OnExceptionEventArgs e)
        {
            OnError("Camera exception: " + e.ex.Message);
        }
    }
}
