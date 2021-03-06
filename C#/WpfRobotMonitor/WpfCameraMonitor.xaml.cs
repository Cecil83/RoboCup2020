﻿using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RobotMonitor
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfCameraMonitor : Window
    {
        DispatcherTimer timerAffichage;
        public WpfCameraMonitor()
        {
            InitializeComponent();

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 50);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();
        }
        int nbMsgSent = 0;

        int nbMsgReceived = 0;
        public void DisplayMessageDecoded(object sender, MessageDecodedArgs e)
        {
            nbMsgReceived += 1;
        }

        int nbMsgReceivedErrors = 0;
        public void MessageDecodedErrorCB(object sender, MessageDecodedArgs e)
        {
            nbMsgReceivedErrors += 1;
        }

        Queue<string> RefBoxEventQueue = new Queue<string>();
        public void DisplayRefBoxCommand(object sender, EventArgsLibrary.StringArgs e)
        {
            lock (RefBoxEventQueue)
            {
                RefBoxEventQueue.Enqueue(e.Value);
            }
        }

        BitmapImage Image1;
        BitmapImage Image2;
        BitmapImage Image3;
        BitmapImage Image4;
        public void DisplayOpenCvMatImage(object sender, EventArgsLibrary.OpenCvMatImageArgs e)
        {
            var image = e.Mat.Bitmap;
            string descriptor = e.Descriptor;

            switch(descriptor)
            {
                case "ImageFromCamera":
                    Image1 = BitmapToImageSource(image);
                    break;
                case "ImageFromCameraViaProcessing":
                    Image2 = BitmapToImageSource(image);
                    break;
                case "ImageDebug2":
                    Image2 = BitmapToImageSource(image);
                    break;
                case "ImageDebug3":
                    Image3 = BitmapToImageSource(image);
                    break;
                default:
                    Image4 = BitmapToImageSource(image);
                    break;
            }

        }

        private BitmapImage BitmapToImageSource(Bitmap sourceImage)
        {
            var copyImage = CopyImage(sourceImage);
            MemoryStream memory = new MemoryStream();
            copyImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmapimage = new BitmapImage();
            bitmapimage.BeginInit();
            bitmapimage.StreamSource = memory;
            bitmapimage.EndInit();

            bitmapimage.Freeze();
            return bitmapimage;
        }

        private Bitmap CopyImage(Bitmap sourceImage)
        {
            var targetImage = new Bitmap(sourceImage.Width, sourceImage.Height,
                  sourceImage.PixelFormat);
            var sourceData = sourceImage.LockBits(
              new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
              ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            var targetData = targetImage.LockBits(
              new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
              ImageLockMode.WriteOnly, targetImage.PixelFormat);
            CopyMemory(targetData.Scan0, sourceData.Scan0,
              (uint)sourceData.Stride * (uint)sourceData.Height);
            sourceImage.UnlockBits(sourceData);
            targetImage.UnlockBits(targetData);
            //targetImage.Palette = sourceImage.Palette;
            return targetImage;
        }


        [DllImport("Kernel32.dll", EntryPoint = "CopyMemory")]
        private extern static void CopyMemory(IntPtr dest, IntPtr src, uint length);


        private void TimerAffichage_Tick(object sender, EventArgs e)
        {
            //textBoxDebug.Text = "Nb Message Sent : " + nbMsgSent + " Nb Message Received : " + nbMsgReceived + " Nb Message Received Errors : " + nbMsgReceivedErrors;
            lock(RefBoxEventQueue)
            {
                for(int i=0; i< RefBoxEventQueue.Count; i++)
                    textBoxDebug.Text += RefBoxEventQueue.Dequeue()+" ";
            }

            if (Image1 != null)
            {
                imageCamera1.Source = Image1;
                Image1 = null;
            }
            if (Image2 != null)
            {
                imageCamera2.Source = Image2;
                Image2 = null;
            }
            if (Image3 != null)
            {
                imageCamera3.Source = Image3;
                Image3 = null;
            }
            if (Image4 != null)
            {
                imageCamera4.Source = Image4;
                Image4 = null;
            }
        }
    }
}
