using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data;
using System.IO;
using System.Drawing;
using System.Timers;
using System.Threading;

namespace Stream
{
    public class Video_TCP_PULL : iStream
    {

        protected Socket socketControl;
        protected Socket socketData;
        protected Socket socketConnect;
        
        protected Image currentImage = null;
        protected int currentImageId = -1;

        protected int serverPort;
        protected IPAddress serverAddress;
        protected Thread theThread = null;
        protected int clientPort = 3456;

        protected int idVideo;
        protected Boolean isConnected = false;

        protected DateTime dateDebutReception;

        protected Statistics stats = new Statistics();

        //#endregion

        #region public methods

        public Statistics getStatistics()
        {
            return stats;
        }

        public Image GetImage()
        {
            readImage();

            return currentImage;
        }

        public void Connect()
        {
            currentImageId = -1;
            currentImage = null;
            stats.Reset();

            if (socketControl == null)
            {
                socketControl = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            if (socketConnect == null)
            {
                socketConnect = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketConnect.Blocking = true;
            }

            // Connection des sockets
            if (!socketControl.Connected)
            {
                socketControl.Connect(serverAddress, serverPort);
            }
            for (int nbTry = 0; !socketConnect.IsBound && nbTry < 3; nbTry++)
            {
                socketConnect.Bind(new IPEndPoint(serverAddress, clientPort));
                socketConnect.Listen(10);
            }
            if (!socketConnect.IsBound)
            {
                throw new Exception("connection impossible!");
            }

            // deamnde de connection à la video :
            if (!socketConnect.Connected)
            {
                Thread connectThreat = new Thread(new ThreadStart(waitConnectSocket));
                connectThreat.Start();
                socketControl.Send(Encoding.ASCII.GetBytes("GET " + idVideo + "\r\nLISTEN_PORT " + clientPort + "\r\n"));
            }


        }
        public void Play()
        {
            if (!socketControl.Connected || !socketConnect.IsBound)
            {
                throw new Exception("socket non connecté!");
            }

            if (theThread == null)
            {
                theThread = new Thread(new ThreadStart(readTCP));

                // S'il n'y a pas d'image et qu'on est connecté, on en charge une
                if (isConnected && currentImage == null)
                {
                    readImage();
                }

            }


        }
        public void Pause()
        {
        }

        public void Stop()
        {
            theThread.Abort();
            disconnect();
        }

        public void setIdVideo(int id)
        {
            idVideo = id;
        }
        public void setServerAddress(string address)
        {
            serverAddress = IPAddress.Parse(address);
        }
        public void setServerPort(int port)
        {
            serverPort = port;
        }

        #endregion

        #region private method : thread

        
        private void waitConnectSocket()
        {
            // on attends la connection
            socketData = socketConnect.Accept();
            socketConnect.Blocking = true;

            // Quand on a la Connection, on est connecté !
            isConnected = true;
        }
        private void readTCP()
        {
            Byte[] temp_data = new Byte[10000];
            Console.WriteLine("avant reception");
            int taille = socketData.Receive(temp_data, SocketFlags.None);

            DateTime dateFinReception = DateTime.Now;
            Console.WriteLine("reception : " + taille + "," + dateFinReception.Millisecond);

            // Transformation du byte[] en string
            string imgStr = "";

            for (int i = 0; i < taille; i++)
            {
                imgStr += (char)temp_data[i];
            }

            // On récupère les données concernant l'image
            string[] chainesImage = imgStr.Split(new string[] { "\r\n" }, 3, StringSplitOptions.RemoveEmptyEntries);

            if (chainesImage.Length != 3)
            {
                stats.IncrementLooseImages();
                readImage();
                return;
            }

            // int numImage = int.Parse(chainesImage[0]);
            int tailleImage = int.Parse(chainesImage[1]);

            char[] ccc = chainesImage[2].ToCharArray();
            byte[] bbb = System.Text.Encoding.Default.GetBytes(ccc);

            Image iii;
            try
            {
                MemoryStream ms = new MemoryStream(bbb);
                iii = System.Drawing.Image.FromStream(ms);
            }
            catch
            {
                stats.IncrementLooseImages();
                readImage();
                return;
            }

            currentImage = iii;
            currentImageId++;

            TimeSpan tps = dateFinReception.Subtract(dateDebutReception);
            stats.IncrementReceivedImages(tps.TotalMilliseconds, tailleImage);

        }

        #endregion

        #region private method

        private void readImage()
        {
            if (!theThread.IsAlive && isConnected)
            {
                Console.WriteLine("debut thread");
                theThread = new Thread(new ThreadStart(readTCP));
                theThread.Start();
                int imageId = currentImageId + 1;

                Console.WriteLine("demande image " + DateTime.Now.Millisecond);
                dateDebutReception = DateTime.Now;
                socketControl.Send(Encoding.ASCII.GetBytes("GET " + imageId + "\r\n"));
            }
        }
        private void disconnect()
        {
            socketControl.Send(Encoding.ASCII.GetBytes("GET \r\n\r\n"));
            socketData.Close();
            socketControl.Close();
            socketConnect.Close();

            socketData = null;
            socketControl = null;
            socketConnect = null;


            
            
            isConnected = false;

        }

        #endregion
    }
}
