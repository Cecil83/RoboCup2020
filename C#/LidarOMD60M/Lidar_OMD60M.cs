﻿using AdvancedTimers;
using Constants;
using EventArgsLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPAdapter;
using TCPClient;
using Timer = System.Timers.Timer;

namespace LidarOMD60M
{
    public class Lidar_OMD60M
    {
        int robotId = 0;

        private static readonly HttpClient HttpClient = new HttpClient();
        IPAddress LidarIpAddress = new IPAddress(new byte[] { 169, 254, 235, 44 });
        IPAddress LocalIp;
        
        int TCPPort = 555;
        string TCPHandle = "";

        TCPAdapter.TCPAdapter TcpLidarAdapter;

        Timer watchDogFeedTimer;
        Timer LidarDisplayTimer;
        public Lidar_OMD60M(int id)
        {
            robotId = id;
            LocalIp = GetComputerIp();

            //On lance un tmer de fedd watchdog pour éviter la déconnexion du LIDAR
            watchDogFeedTimer = new Timer(2000);
            watchDogFeedTimer.Elapsed += WatchDogFeedTimer_Elapsed;
            LidarDisplayTimer = new Timer(80);
            LidarDisplayTimer.Elapsed += LidarDisplayTimer_Elapsed;

            //On configure les options du LIDAR
            LidarSetApplicationBitmapMode();
            LidarSetImage(0);
            //LidarSetBarSetText("RCT forever !", "GOOAAL !", "Passe la balle !", "Tire tout de suite !");
            LidarSetRotationFrequency(50);
            
            //On récupère le handle de connection TCP/IP
            LidarTCPInit();

            //On crée un client TCP/IP corrresponant au handle, pour recevoir le flux de data de scan.
            //TcpLidarClient = new TCPClientWithEvents();
            //TcpLidarClient.ConnectToServer(LidarIpAddress.ToString(), TCPPort);
            //TcpLidarClient.OnDataReceived += TcpLidarClient_OnDataReceived;

            TcpLidarAdapter = new TCPAdapter.TCPAdapter(LidarIpAddress.ToString(), TCPPort, "Lidar TCP Adapter");
            TcpLidarAdapter.OnDataReceivedEvent += TcpLidarAdapter_OnDataReceivedEvent;

            watchDogFeedTimer.Start();
            LidarDisplayTimer.Start();

            //On démarre le scan
            LidarStartTCPScan();
        }

        private void TcpLidarAdapter_OnDataReceivedEvent(object sender, DataReceivedArgs e)
        {
            DecodeLidarScanData(e.Data, e.Data.Length);
        }

        int horizontalShift = -50;
        private void LidarDisplayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            horizontalShift += 2;
            if (horizontalShift > 130)
                horizontalShift = -100;
            LidarSetImage(horizontalShift);
        }

        private void TcpLidarClient_OnDataReceived(byte[] data, int bytesRead)
        {
            DecodeLidarScanData(data, bytesRead);
        }

        private void WatchDogFeedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            LidarFeedWatchdogTCP();
        }


        /********************************************** Version TCP ******************************************/
        private async Task LidarTCPInit()
        {
            // On demande un handle UDP : cf. page 32 de la doc R2000 Ehternet Protocol
            Task<string[]> tcpHandleRequestTask = Task.Run(() => LidarRequestTCPHandle());
            tcpHandleRequestTask.Wait();
            TCPHandle = tcpHandleRequestTask.Result[0];
            TCPPort = Int32.Parse(tcpHandleRequestTask.Result[1]);
        }

        async Task<string[]> LidarRequestTCPHandle() 
        {
            string[] result = new string[2];
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/request_handle_tcp?packet_type=A&watchdog=on&watchdogtimeout=10000";
            var content = await HttpClient.GetStringAsync(request);
            JObject info = (JObject)JsonConvert.DeserializeObject(content);
            if (info["error_text"].ToString() == "success")
            {
                result[0] = info["handle"].ToString();
                result[1] = info["port"].ToString();
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine(content);
            }
            return result;
        }

        async Task LidarStartTCPScan()
        {
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/start_scanoutput?handle=" + TCPHandle;
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarStopTCPScan()
        {
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/stop_scanoutput?handle=" + TCPHandle;
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarFeedWatchdogTCP()
        {
            // On lance un feedwatchdog
            string request = @"http://" + LidarIpAddress + "/cmd/feed_watchdog?handle=" + TCPHandle;
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarSetRotationFrequency(int freq)
        {
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?scan_frequency=" + freq.ToString();
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarSetBarGraphDistance()
        {
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_display_mode=bargraph_distance";
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarSetBarSetText(string text1, string text2, string text3, string text4)
        {
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_display_mode=application_text";
            var content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_static_text_1=" + text1;
            content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_static_text_2=" + text2;
            content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_application_text_1=" + text3;
            content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_application_text_2=" + text4;
            content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarSetApplicationBitmapMode()
        {
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_display_mode=application_bitmap";
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine(content);
        }

        async Task LidarSetImage(int horizontalShift)
        {
            Image image = DrawText("Merci Pepperl+Fuchs", new Font("Verdana", 12.0f, FontStyle.Bold), -horizontalShift);

            var array = ConvertBitmapToArray(new Bitmap(image));
            var bitmapString = ConvertToBase64StringForLidar(array);
            
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_application_bitmap="+ bitmapString;
            var content = await HttpClient.GetStringAsync(request);
        }

        ///********************************************** Version UDP ******************************************/
        //private void LidarUDPInit()
        //{
        //    // Lance une tâche asynchrone qui permet de récupérer le message de retour avant de continuer
        //    // On demande un handle UDP : cf. page 32 de la doc R2000 Ehternet Protocol
        //    Task<string> udpHandleRequestTask = Task.Run(() => LidarRequestUDPHandle());
        //    // Wait for it to finish
        //    udpHandleRequestTask.Wait();
        //    UDPHandle = udpHandleRequestTask.Result;

        //    // Lance une tâche asynchrone qui permet de récupérer le message de retour avant de continuer
        //    // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
        //    Task startLidarTask = Task.Run(() => LidarStartScan());
        //    // Wait for it to finish
        //    startLidarTask.Wait();

        //    //Etablit la liaison UDP
        //    UDPLidarClient = new UdpClient(UDPport);
        //    IPEndPoint ep = new IPEndPoint(LidarIpAddress, 11000); // endpoint where server is listening
        //    UDPLidarClient.Connect(ep);
        //    UDPLidarClient.BeginReceive(new AsyncCallback(UDPPacketReceived), null);
        //}

        ////CallBack
        //private void UDPPacketReceived(IAsyncResult res)
        //{
        //    //page 39 de la doc R2000 Ehternet Protocol
        //    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
        //    byte[] received = UDPLidarClient.EndReceive(res, ref RemoteIpEndPoint);

        //    //Process codes
        //    Console.WriteLine(Encoding.UTF8.GetString(received));
        //    UDPLidarClient.BeginReceive(new AsyncCallback(UDPPacketReceived), null);
        //}

        //async Task<string> LidarRequestUDPHandle()
        //{
        //    // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
        //    string handle = "";
        //    string request = @"http://" + LidarIpAddress + "/cmd/request_handle_udp?address=" + LocalIp + "&port=" + UDPport + "&packet_type=C";
        //    var content = await HttpClient.GetStringAsync(request);
        //    JObject info = (JObject)JsonConvert.DeserializeObject(content);
        //    if (info["error_text"].ToString() == "success")
        //    {
        //        handle = info["handle"].ToString();
        //        //Console.WriteLine(content);
        //    }
        //    return handle;
        //}

        //async Task LidarStartScan()
        //{
        //    // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
        //    string request = @"http://" + LidarIpAddress + "/cmd/start_scanoutput?handle=" + UDPHandle;
        //    var content = await HttpClient.GetStringAsync(request);
        //    Console.WriteLine(content);
        //}
        //async Task LidarReboot()
        //{
        //    // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
        //    string request = @"http://" + LidarIpAddress + "/cmd/reboot_device";
        //    var content = await HttpClient.GetStringAsync(request);
        //    Console.WriteLine(content);
        //}

        List<double> distance = new List<double>();
        List<double> angle = new List<double>();
        List<double> RSSI = new List<double>();
                           
        /**************************************************** Fonctions d'analyse **************************************************************/
        private void DecodeLidarScanData(byte[] buffer, int bufferSize)
        {         
            int pos = 0;

            try
            {

                // TODO : A blinder si les packets sont trop petits +++++++++++++++++++++++++++++++++++++++66666666666666

                while (pos < bufferSize)
                {
                    //On stocke la position initiale pour pouvoir la restaurer au moment de la lecture des datas
                    int initPos = pos;

                    UInt16 magic = buffer[pos++];
                    magic += (UInt16)(buffer[pos++] << 8);
                    if (magic != 0xA25C)
                        return;

                    UInt16 packet_type = buffer[pos++];
                    packet_type += (UInt16)(buffer[pos++] << 8);

                    UInt32 packet_size = buffer[pos++];
                    packet_size += (UInt32)(buffer[pos++] << 8);
                    packet_size += (UInt32)(buffer[pos++] << 16);
                    packet_size += (UInt32)(buffer[pos++] << 24);

                    UInt16 header_size = buffer[pos++];
                    header_size += (UInt16)(buffer[pos++] << 8);

                    UInt16 scan_number = buffer[pos++];
                    scan_number += (UInt16)(buffer[pos++] << 8);

                    UInt16 packet_number = buffer[pos++];
                    packet_number += (UInt16)(buffer[pos++] << 8);
                    //Console.WriteLine("Packet Number : " + packet_number);

                    if (packet_number == 1)
                    {
                        OnLidar((int)TeamId.Team1, angle, distance);
                        distance = new List<double>();
                        angle = new List<double>();
                        RSSI = new List<double>();
                    }

                    //Console.WriteLine("Scan Number : " + scan_number);

                    UInt64 timestamp_raw = buffer[pos++];
                    timestamp_raw += (UInt64)(buffer[pos++] << 8);
                    timestamp_raw += (UInt64)(buffer[pos++] << 16);
                    timestamp_raw += (UInt64)(buffer[pos++] << 24);
                    timestamp_raw += (UInt64)(buffer[pos++] << 32);
                    timestamp_raw += (UInt64)(buffer[pos++] << 40);
                    timestamp_raw += (UInt64)(buffer[pos++] << 48);
                    timestamp_raw += (UInt64)(buffer[pos++] << 56);

                    UInt64 timestamp_sync = buffer[pos++];
                    timestamp_sync += (UInt64)(buffer[pos++] << 8);
                    timestamp_sync += (UInt64)(buffer[pos++] << 16);
                    timestamp_sync += (UInt64)(buffer[pos++] << 24);
                    timestamp_sync += (UInt64)(buffer[pos++] << 32);
                    timestamp_sync += (UInt64)(buffer[pos++] << 40);
                    timestamp_sync += (UInt64)(buffer[pos++] << 48);
                    timestamp_sync += (UInt64)(buffer[pos++] << 56);

                    UInt32 status_flags = buffer[pos++];
                    status_flags += (UInt32)(buffer[pos++] << 8);
                    status_flags += (UInt32)(buffer[pos++] << 16);
                    status_flags += (UInt32)(buffer[pos++] << 24);

                    UInt32 scan_frequency = buffer[pos++];
                    scan_frequency += (UInt32)(buffer[pos++] << 8);
                    scan_frequency += (UInt32)(buffer[pos++] << 16);
                    scan_frequency += (UInt32)(buffer[pos++] << 24);

                    UInt16 num_points_scan = buffer[pos++];
                    num_points_scan += (UInt16)(buffer[pos++] << 8);

                    UInt16 num_points_packet = buffer[pos++];
                    num_points_packet += (UInt16)(buffer[pos++] << 8);

                    UInt16 first_index = buffer[pos++];
                    first_index += (UInt16)(buffer[pos++] << 8);

                    Int32 first_angle = buffer[pos++];
                    first_angle += (Int32)(buffer[pos++] << 8);
                    first_angle += (Int32)(buffer[pos++] << 16);
                    first_angle += (Int32)(buffer[pos++] << 24);
                    //Console.WriteLine("First Angle : " + first_angle);

                    Int32 angular_increment = buffer[pos++];
                    angular_increment += (Int32)(buffer[pos++] << 8);
                    angular_increment += (Int32)(buffer[pos++] << 16);
                    angular_increment += (Int32)(buffer[pos++] << 24);

                    UInt32 iq_input = buffer[pos++];
                    iq_input += (UInt32)(buffer[pos++] << 8);
                    iq_input += (UInt32)(buffer[pos++] << 16);
                    iq_input += (UInt32)(buffer[pos++] << 24);

                    UInt32 iq_overload = buffer[pos++];
                    iq_overload += (UInt32)(buffer[pos++] << 8);
                    iq_overload += (UInt32)(buffer[pos++] << 16);
                    iq_overload += (UInt32)(buffer[pos++] << 24);

                    UInt64 iq_timestamp_raw = buffer[pos++];
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 8);
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 16);
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 24);
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 32);
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 40);
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 48);
                    iq_timestamp_raw += (UInt64)(buffer[pos++] << 56);

                    UInt64 iq_timestamp_sync = buffer[pos++];
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 8);
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 16);
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 24);
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 32);
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 40);
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 48);
                    iq_timestamp_sync += (UInt64)(buffer[pos++] << 56);

                    //On initialise le pointeur au début de scan data
                    pos = initPos + header_size;
                    for (int index = 0; index < num_points_packet; index++)
                    {
                        if (packet_type == 'A')
                        {
                            UInt32 dist = buffer[pos++];
                            dist += (UInt32)(buffer[pos++] << 8);
                            dist += (UInt32)(buffer[pos++] << 16);
                            dist += (UInt32)(buffer[pos++] << 24);
                            distance.Add(dist / 1000.0);
                            angle.Add((first_angle + index * angular_increment) / 10000.0 * Math.PI / 180.0);
                        }
                    }
                }
            }
            catch
            {
                distance = new List<double>();
                angle = new List<double>();
                RSSI = new List<double>();
            }
        }

        IPAddress GetComputerIp()
        {
            //TODO : basé sur le fait que la bonne adresse est la première...
            IPAddress localIp = null;
            //Get Local IP Address
            Console.WriteLine(Dns.GetHostName());
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = addr;
                    return localIp;
                }
            }
            return localIp;
        }

        private Image DrawText(String text, Font font, int horizontalShift)
        {
            Color textColor = Color.Black;
            Color backColor = Color.White;

            int width = 252;
            int height = 24;
            //On créé une image de taille réduite par trois pour avoir le vrai affichage
            var img = new Bitmap(width/3, height);
            Graphics drawing = Graphics.FromImage(img);

            //measure the string to see how big the image needs to be
            //SizeF textSize = drawing.MeasureString(text, font);

            //paint the background
            drawing.Clear(backColor);

            //create a brush for the text
            Brush textBrush = new SolidBrush(textColor);
            drawing.DrawString(text, font, textBrush, horizontalShift, 0);
            drawing.Save();
            textBrush.Dispose();
            drawing.Dispose();

            Bitmap finalImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.DrawImage(img, 0, 0, width, height);
            }

            return finalImage;

        }

        public static byte[] ConvertBitmapToArray(Bitmap bmp, int horizontalShift=0)
        {
            var size = bmp.Width * bmp.Height / 8;
            var buffer = new byte[size];

            var i = 0;
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = bmp.Height-1; y >= 0; y--)
                {                
                    var color = bmp.GetPixel(x, y);
                    var intensite = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B; //equation RGB -> YUV

                    if (intensite >= 255 / 2)
                    {
                        int index = i + horizontalShift* bmp.Height;
                        if (index >= bmp.Width* bmp.Height)
                            index -= bmp.Width * bmp.Height;

                        int pos = index/8;                        
                        var bitInByteIndex = index % 8;

                        //On calcule la position décalée

                        buffer[pos] |= (byte)(1 << 7 - bitInByteIndex);
                    }
                    i++;
                }
            }

            return buffer;
        }

        string ConvertToBase64StringForLidar(byte[] bytes)
        {
            string s = System.Convert.ToBase64String(bytes);
            s=s.Replace('+', '-');
            s=s.Replace('/', '_');
            return s;
        }


        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarEvent;
        public virtual void OnLidar(int id, List<double> angleList, List<double> distanceList)
        {
            var handler = OnLidarEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, AngleList = angleList, DistanceList = distanceList });
            }
        }
    }
}
