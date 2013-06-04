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
        #region Variables locales

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
        protected DateTime dateFinReception;

        protected Statistics stats = new Statistics();

        #endregion

        #region Variables de l'interface

        string Stream.iStream.Address { get { return serverAddress.ToString(); } set { serverAddress = IPAddress.Parse(value); } }
        int Stream.iStream.Port { get { return serverPort; } set { serverPort = value; } }
        int Stream.iStream.idVideo { get { return idVideo; } set { idVideo = value; } }

        #endregion

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
                try
                {
                    socketControl.Connect(serverAddress, serverPort);
                }
                catch
                {
                    return;
                }
            }
            for (int nbTry = 0; !socketConnect.IsBound && nbTry < 3; nbTry++)
            {
                try
                {
                    socketConnect.Bind(new IPEndPoint(IPAddress.Any, clientPort + nbTry));
                    socketConnect.Listen(10);
                }
                catch { }
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
            if (theThread != null)
            {
                theThread.Abort();
            }
            disconnect();
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
            for (; ; )
            {
                Byte[] temp_data = new Byte[10000];

                // reception du debut (on verifie la reception de l'entete)
                bool enteteRecue = false;
                string[] chainesImage = null;
                int taillePaquet;

                byte[] imgBuff = new byte[1];
                string entBuff = "";

                int tailleImage = 0;
                int taillePaquet1 = 0;

                while (!enteteRecue)
                {

                    taillePaquet = socketData.Receive(temp_data, SocketFlags.None);
                    dateFinReception = DateTime.Now;

                    bool entente1Recue = false;

                    // Transformation du byte[] de l'entete en string
                    int i;
                    for (i = 1; i < taillePaquet && !enteteRecue; i++)
                    {
                        if ((char)temp_data[i] == '\n' && (char)temp_data[i - 1] == '\r')
                        {
                            if (entente1Recue)
                            {
                                enteteRecue = true;
                            }
                            entente1Recue = true;
                        }
                        entBuff += (char)temp_data[i];
                    }

                    // si l'entete a été recue, on recupère le reste, le debut de l'image
                    if (enteteRecue)
                    {
                        chainesImage = entBuff.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                        tailleImage = int.Parse(chainesImage[1]);

                        imgBuff = new byte[tailleImage];
                        for (int j = i; j < taillePaquet; j++)
                        {
                            imgBuff[j - i] = temp_data[j];
                        }
                        taillePaquet1 = taillePaquet - i;
                    }

                }

                // reception de la fin (on verifie que l'on recoit limage totale (taille spécifiée dans l'entête)
                for (int tailleTotale = taillePaquet1; tailleTotale < tailleImage; tailleTotale += taillePaquet)
                {
                    taillePaquet = socketData.Receive(temp_data, SocketFlags.None);
                    dateFinReception = DateTime.Now;

                    // Transformation du byte[] en string
                    for (int i = 0; i < taillePaquet; i++)
                    {

                        imgBuff[i + tailleTotale] = temp_data[i];
                    }
                }

                MemoryStream ms = new MemoryStream(imgBuff);

                Image iii;
                try
                {

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

        }

        #endregion

        #region private method

        private void readImage()
        {
            if (!isConnected)
            {
                return;
            }
            if(!theThread.IsAlive)
            {
                theThread = new Thread(new ThreadStart(readTCP));
                theThread.Start();
            }

            int imageId = currentImageId + 1;

            dateDebutReception = DateTime.Now;
            socketControl.Send(Encoding.ASCII.GetBytes("GET " + imageId + "\r\n"));
        }
        private void disconnect()
        {
            try
            {
                socketControl.Send(Encoding.ASCII.GetBytes("GET \r\n\r\n"));
                socketData.Close();
                socketControl.Close();
                socketConnect.Close();

                socketData = null;
                socketControl = null;
                socketConnect = null;

                theThread.Abort();
                theThread = null;

                isConnected = false;
            }
            catch
            {
            }

        }

        #endregion

    }
}
