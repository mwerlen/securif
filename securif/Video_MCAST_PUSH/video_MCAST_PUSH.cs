using System;
using System.Collections.Generic;
using System.Text;
//using System.Net;
using System.Net.Sockets;
//using System.Data;
using System.Drawing;
using System.IO;
//using System.Timers;
using System.Threading;

namespace Stream
{
    public class Video_MCAST_PUSH : iStream
    {
        public const int TAILLE_MAX_FRAGMENT = 1024;

        #region variables declaration

        protected string server_address;
        protected int server_port;
        protected int idVideo;
        protected Image imageComplete;
        protected Socket socket_data;
        protected Thread threadLecture;
        protected Statistics stats;

        LinkedList<byte[]> listeSegments;
        Semaphore s;
        Thread threadTraitement;

        #endregion

        #region Variables de l'interface

        string Stream.iStream.Address { get { return server_address; } set { server_address = value; } }
        int Stream.iStream.Port { get { return server_port; } set { server_port = value; } }
        int Stream.iStream.idVideo { get { return idVideo; } set { idVideo = value; } }

        #endregion

        #region constructeur

        public Video_MCAST_PUSH()
        {
            stats = new Statistics();
            listeSegments = new LinkedList<byte[]>();
            s = new Semaphore(0, 1000);
        }

        #endregion

        #region public methods

        public void Connect()
        {
        }
        
        public Image GetImage()
        {
            return imageComplete;
        }

        public void Play()
        {
            threadLecture = new Thread(ecouteur);
            threadLecture.Priority = ThreadPriority.Highest;
            threadTraitement = new Thread(analyser);
            threadLecture.Priority = ThreadPriority.BelowNormal;
            threadTraitement.Start();
            threadLecture.Start();
        }
        
        public void Pause()
        {
            threadLecture.Abort();
            threadTraitement.Abort();
        }

        public void Stop()
        {
            threadLecture.Abort();
            threadTraitement.Abort();
        }

        #endregion

        #region protected methods

        protected void analyser()
        {
            bool premier_fragment = true, // Pour savoir si il faut créer le nouveau tableau contenant l'image
                imageTransformee; // Pour savoir si la création de l'image a réussi
            int image_actuelle = 0, // Pour garder en mémoire le numImage qu'on traite (permet de détecter la fin d'une image)
                numImage = 0,       // Numéro d'image reçu dans l'entete
                tailleFragment = 0, // Taille du fragment (entete)
                tailleImage = 0,    // Tille de l'image (entete)
                positionPaquet = 0; // Position du paquet dans l'image (entete)
            string[] chainesImage = new string[4]; // Tableau de chaines contenant les données de l'entete
            byte[] fragment = new byte[TAILLE_MAX_FRAGMENT + 20]; // Tableau de byte contenant le fragment recu dans le socket
            int numPremierOctetDonnee = 0; // Entier indiquant la position de premier octet de l'image dans le fragment
            short numParam = 0;     // Entier provisoire pour savoir combien de parametres de l'entete on a récupéré
            char c;                 // Caractere provisoire pour creer l'entete
            string entete = "";     // Pour stocker l'entete
            byte[] image = null;    // Tableau contenant les octets representant l'image

            for (; ; )
            {
                s.WaitOne();
                fragment = listeSegments.First.Value;

                #region Récupération des données de l'entete
                numPremierOctetDonnee = 0; numParam = 0; entete = "";
                do
                {
                    c = (char)fragment[numPremierOctetDonnee];
                    if (c == '\r')
                    {
                        numPremierOctetDonnee++;
                        numParam++;
                    }
                    entete += c;
                    numPremierOctetDonnee++;
                } while (numParam < 4);

                chainesImage = entete.Split(new string[] { "\r" }, StringSplitOptions.None);
                numImage = int.Parse(chainesImage[0]);
                tailleImage = int.Parse(chainesImage[1]);
                positionPaquet = int.Parse(chainesImage[2]);
                tailleFragment = int.Parse(chainesImage[3]);
                #endregion

                #region Traitement du tout premier fragment
                if (premier_fragment)
                {
                    // On récupère le numéro de la premiere image en reception
                    image_actuelle = numImage;
                    // On crée le tableau d'octets qui va recevoir l'image
                    image = new byte[tailleImage];
                    premier_fragment = false;
                }

                #endregion

                #region Traitement de la fin de la réception d'une image
                if (image_actuelle != numImage)
                {
                    imageTransformee = true;

                    try
                    {
                        imageComplete = System.Drawing.Image.FromStream(new MemoryStream(image));
                    }
                    catch (Exception)
                    {
                        imageTransformee = false;
                    }
                    if (imageTransformee)
                        stats.IncrementReceivedImages();
                    else
                        stats.IncrementLooseImages();

                    image_actuelle = numImage;
                    image = new byte[tailleImage];
                }
                #endregion

                // On stocke le fragment dans le tableau représentant l'image
                for (int i = 0; i < tailleFragment; i++)
                    image[positionPaquet + i] = fragment[numPremierOctetDonnee + i];
                listeSegments.RemoveFirst();
            }
        }

        protected void ecouteur()
        {
            byte[] fragment = new byte[TAILLE_MAX_FRAGMENT + 20]; // Tableau de byte contenant le fragment recu dans le socket
            
            #region Création / Initialisation du socket
            System.Net.IPAddress ip = System.Net.IPAddress.Any;
            socket_data = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket_data.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);
            socket_data.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            socket_data.Bind(new System.Net.IPEndPoint(ip, server_port));
            socket_data.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 0);
            socket_data.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(System.Net.IPAddress.Parse(server_address)));
            #endregion

            for (; ; )
            {
                socket_data.Receive(fragment);
                listeSegments.AddLast(fragment);
                s.Release();
            }
        }

        #endregion

        #region getter/setter

        public Statistics getStatistics()
        {
            return stats;
        }

        /*public void setIdVideo(int id)
        {
        }

        public void setServerAddress(string address)
        {
            this.server_address = address;
        }

        public void setServerPort(int port)
        {
            this.server_port = port;
        }
        */
        #endregion
    }
}
