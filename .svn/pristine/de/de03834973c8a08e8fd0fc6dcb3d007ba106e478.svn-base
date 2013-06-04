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
    public class Video_UDP_PUSH : iStream
    {
        #region variables declaration

        // Les sockets
        protected Socket socket_control;        // Le socket de control
        protected Socket socket_data;           // Le socket Data

        // Les données serveur et video
        protected int server_port;              //Le port avec lequel on communique avec le server en control
        protected string server_address;        //L'adresse avec lequel on communique avec le server en control
        protected int client_port = 4586;       //Le port de communication sur lequel on écoute Data
        protected int tailleFragmentTheorique = 2048;   // C'est juste pour dire que l'on demande quelquechose, mais en fait le sreveur fait n'importe quoi
        protected int idVideo;                  //Le numéro de la video que l'on va regarder
        EndPoint ep;                            // Notre point d'accroche local.

        protected int bufferSize = 2;           // La taille de notre buffer d'image (qui ne sert à rien vu que le server nous envoie les données au fur et à mesur au bon rythme)
        protected Queue<Image> lesImages;       // Le buffer d'image en question
        protected Thread monThread;             // Le thread qui écoute les images

        protected Statistics mesStats;          // L'objet stat qui calcul les stats
        DateTime startTime = DateTime.MinValue; //La date (pour les stats) de début du chargement de l'image courante

        Image lastImage = null;                 // La denière image (pour pas que ça fasse moche avec les images non reçues)

        System.Timers.Timer clock = null;       // Le timer pour lancer le ALIVE

        #endregion

        #region Variables de l'interface

        string Stream.iStream.Address { get { return server_address; } set { server_address = value; } }
        int Stream.iStream.Port { get { return server_port; } set { server_port = value; } }
        int Stream.iStream.idVideo { get { return idVideo; } set { idVideo = value; } }

        #endregion

        #region public methods

        /*** public Void Connect
         * Initialise les objets (file d'attente, stats, endpoint...)
         * Ouvre la connection avec le serveur pour le control
         * demand l'envoie de Data
         ***/
        public void Connect()
        {
            // Initialisation des statistiques
            mesStats = new Statistics();

            //Initialisation de liste d'images
            lesImages = new Queue<Image>(bufferSize);

            //Création des sockets
            serveur_connect();

            // Creation du End-Point
            System.Net.IPAddress ip = IPAddress.Any;
            ep = new System.Net.IPEndPoint(ip, client_port);

            //Début du chargement des images
            monThread = new Thread(new ThreadStart(EcouteImages));
            monThread.Start();

            // Envoie de la commande GET
            socket_control.Send(Encoding.ASCII.GetBytes("GET " + idVideo + "\r\nLISTEN_PORT " + client_port + "\r\nFRAGMENT_SIZE " + tailleFragmentTheorique + "\r\n\r\n"));
       
            // On fait le resetAlive
            resetAlive();
        }

        /** public void disconnect
         * Demande l'arret de la video
         * arrête le thread de reception
         * clot les sockets
         ***/
        public void disconnect()
        {
            // Envoie du stop (annulé puisque c'est Stop qui disconnecte)
            //Stop();

            // Arret du SayAlive
            clock.Stop();
            clock.Dispose();

            // Arrêt du thread de reception
            monThread.Abort();

            // Cloture des sockets
            socket_control.Close();
            socket_data.Close();
            socket_data = null;
            socket_control = null;
        }

        /*** getImage
         * Si on a une image dans la file on l'envoie
         * Sinon on renvoie la dernière image
         * Si on de la place dans la file d'attente, on relance l'acquisition
         ***/
        public Image GetImage()
        {
            // On aplus d'images
            if (lesImages.Count == 0)
            {
                return lastImage;
            }
            // On a encore des images
            else
            {
                //On redemande de faire de l'acquisition, si c'est à l'arret
                if (monThread.ThreadState == ThreadState.Suspended)
                {
                    monThread.Resume();
                }
                return (lastImage = lesImages.Dequeue());
            }
        }

        /** resetAlive
         * crée un timer et lance l'envoie du Alive
         ***/
		private void resetAlive()
		{
            // le timer
			clock = new System.Timers.Timer();
			clock.Elapsed += new ElapsedEventHandler(sayAlive);
			clock.Interval = 30000; // 30 secondes
			clock.Start();
		}

        // On dit ALIVE au server
        private void sayAlive(object sender, EventArgs e)
		{
			socket_control.Send(Encoding.ASCII.GetBytes("ALIVE" + idVideo + "\r\nLISTEN PORT " + client_port + "\r\n\r\n"));
			resetAlive();
		}

        // ON DIT START
        public void Play()
        {
            if (socket_control == null || !socket_control.Connected) throw new Exception("La connexion n'est pas établie");
            startTime = DateTime.MinValue;
            socket_control.Send(Encoding.ASCII.GetBytes("START \r\n\r\n"));
        }
        
        // ON DIT PAUSE
        public void Pause()
        {
            if (socket_control == null || !socket_control.Connected) throw new Exception("La connexion n'est pas établie");
            startTime = DateTime.MinValue;
            socket_control.Send(Encoding.ASCII.GetBytes("PAUSE \r\n\r\n"));
        }

        // ON DIT STOP
        public void Stop()
        {
            if (socket_control!=null && socket_control.Connected) 
            socket_control.Send(Encoding.ASCII.GetBytes("END \r\n\r\n"));
            disconnect();
        }

        #endregion

        #region private methods

        /*** serveur_connect
         * Initialise la connection de control au server (3 tentatives)
         ***/
        private void serveur_connect()
        {
            // On a besoin d'un serveur, d'un port et d'un IdVideo
            if (server_address == null)
            {
                throw new Exception("Adresse du serveur non renseignée !");
            }
            if (server_port == 0)
            {
                throw new Exception("Port du serveur non renseigné !");
            }
            if (idVideo == 0)
            {
                throw new Exception("Identifiant de la vidéo non renseigné !");
            }

            // Initilisation de la connexion de contrôle
            if (socket_control == null)
            {
                socket_control = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            // Si la connexion de contrôle est déjà établie
            if (socket_control.Connected)
            {
                resetAlive();
            }
            else
            {
                // Sinon on essaye de le faire 3 fois
                int tentativeDeConnexion = 0;
                while(tentativeDeConnexion<3 && !socket_control.Connected)
                {
                    socket_control.Connect(server_address, server_port);
                    tentativeDeConnexion++;
                }
            }

            // Si la connexion de contrôle est déjà établie
            if (!socket_control.Connected)
            {
                throw new Exception("La connexion au serveur a échoué !");
            }
        }


        /*** EcouteImages
         * Ecoute le socket data pour récupérer les fragments et les aggréger en images
         ***/
        protected void EcouteImages()
        {
            bool premier_fragment = true;   // Indique s'il on est sur le premier fragment 
            int image_actuelle = 0;         // Le numéro de l'image actuelle
            int numImage = 0;               // Le numéro de l'image que l'on vient de lire
            int tailleImage = 0;            // La taille de l'image
            int tailleFragment = 0;         // Taille du fragment (entete)
            int positionPaquet = 0;         // La position du paquet dans l'image
            string entete = "";             // Les entetes
            int numPremierOctetDonnee;      // Début des bytes de l'image dans le fragment
            int numParam;                   // le nombre de paramètres
            char c;                         // un char tout bête pour faire une conversion char à char
            string[] parametres = new string[5];         //les parametres de l'image
            byte[] image = null;            // Tableau contenant les octets representant l'image
            byte[] fragment = new byte[tailleFragmentTheorique];  // le fragment que l'on va recevoir   

            // Connection du socket
            if (socket_data == null)
            {
                socket_data = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket_data.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);
                socket_data.Bind(ep);
            }

            // On boucle sur la lecture des images (en fragments)
            for (; ; )
            {
                socket_data.ReceiveFrom(fragment, ref ep);

                // Transformation du byte[] en string
                numPremierOctetDonnee = 0;
                numParam = 0;
                entete = "";
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


                // On récupère les données concernant l'image
                parametres = entete.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                numImage = int.Parse(parametres[0]);
                tailleImage = int.Parse(parametres[1]);
                positionPaquet = int.Parse(parametres[2]);
                tailleFragment = int.Parse(parametres[3]);

                if (premier_fragment)
                    image_actuelle = numImage;

                // Si on change d'image
                if (image_actuelle != numImage)
                {
                    // On sauvegarde l'image
                    Image imageActuelle = System.Drawing.Image.FromStream(new MemoryStream(image));

                    // Si le buffer d'image est plein, on s'arrête
                    if (lesImages.Count == bufferSize)
                    {
                        monThread.Suspend();
                    }

                    // On empile l'image
                    lesImages.Enqueue(imageActuelle);

                    // On ajoute aux stats une nouvelle image
                    if (startTime != DateTime.MinValue)
                    {
                        TimeSpan time = DateTime.Now.Subtract(startTime);
                        mesStats.IncrementReceivedImages(time.TotalMilliseconds, tailleImage);
                    }
                    else
                    {
                        mesStats.IncrementReceivedImages();
                    }
                    
                    // Le redémarre le compteur.
                    startTime = DateTime.Now;

                    while (mesStats.getNumberOfImages() < numImage)
                    {
                        mesStats.IncrementLooseImages();
                    }

                    // On met à jour notre numéro d'image
                    image_actuelle = numImage;
                    premier_fragment = true;
                }

                if (premier_fragment)
                {
                    // On crée le tableau d'octets qui va recevoir l'image
                    image = new byte[tailleImage];
                    premier_fragment = false;
                }

                // On stocke le fragment dans le tableau représentant l'image
                for (int i = 0; i < tailleFragment; i++)
                    image[positionPaquet + i] = fragment[numPremierOctetDonnee + i];
            }
        }
        #endregion

        #region getter/setter

        public Statistics getStatistics()
        {
            return mesStats;
        }
        public void setIdVideo(int id)
        {
            idVideo = id;
        }
        /*public void setServerAddress(string address)
        {
            this.server_address = address;
        }

        public void setServerPort(int port)
        {
            this.server_port = port;
        }

        public string Address
        {
            set { this.server_address = value; }
        }*/
        public int Port
        {
            set { this.server_port = value; }
        }
        public int ClientPort
        {
            set { this.client_port = value; }
        }
        #endregion

    }
}
