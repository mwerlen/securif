using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Drawing;
using System.Net;
using System.Threading;
using Stream;




namespace Stream
{
    public class Video_UDP_PULL : iStream
    {
        #region Variables

        //LinkedList<Image> listeImages;
        Queue<Image> images;                // Pile (FIFO) d'images reçues
        Socket data_socket, ctrl_socket;    // Sockets de donnees et de controles
        Statistics stats;                   // Pour la gestion des stats
        Semaphore semReception;             // Semaphore pour demander la reception d'une image
        Thread receiveThread;               // Thread de reception des donnees
        int idVideo,                        // Numero de la video a demander au serveur
            serverPort;                     // Numero du port sur lequel les fragments seront envoyes
        string serverAddress;               // Adresse du serveur

        protected static int tailleFragmentTheorique = 2048; // Pour choisir la taille du fragment...

        #endregion

        #region Proprietes de l'interface

        string Stream.iStream.Address { get { return serverAddress; } set { serverAddress = value; } }
        int Stream.iStream.Port { get { return serverPort; } set { serverPort = value; } }
        int Stream.iStream.idVideo { get { return idVideo; } set { idVideo = value; } }

        #endregion

        #region Constructeur

        public Video_UDP_PULL()
        {
            images = new Queue<Image>();
            stats = new Statistics();
            // Creation du semaphore. On choisit 10 images en queue par defaut, on pourrait en faire un parametre
            semReception = new Semaphore(10, 10);
        }

        #endregion

        #region Public methods

        public void Connect()
        {
            // Point de connexion pour la socket de controle
            IPEndPoint ipServer = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
            // Point de connexion pour la reception des donnees. Port arbitraire, mais on pourrait en faire un param
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, 33333);

            // Creation des sockets
            data_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            data_socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);
            ctrl_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Connnexion aux sockets
            data_socket.Bind(ipLocal);

            ctrl_socket.Connect(ipServer);
            ctrl_socket.Send(Encoding.ASCII.GetBytes(
                "GET " + idVideo.ToString() + " \r\n" +
                "LISTEN_PORT " + "33333" + " \r\n" +
                "FRAGMENT_SIZE " + "1024" + " \r\n\r\n"));

            // Creation et demarrage du Thread de reception
            receiveThread = new Thread(recepteur);
        }

        public void Play()
        {
            if(!receiveThread.IsAlive)
                receiveThread.Start();
        }

        private void recepteur()
        {
            int tailleRecue = 0;

            int numImage = 0,       // Numero d'image reçu dans l'entete
                tailleFragment = 0, // Taille du fragment (entete)
                tailleImage = 0,    // Tille de l'image (entete)
                positionPaquet = 0, // Position du paquet dans l'image (entete)
                numImageEnCours = 0,// Numero de l'image que l'on est en train de recevoir
                nbOctetsRecus;      // Nombre d'octets reçus pour l'image en cours
            
            short numParam = 0;     // Entier servant a compter les parametres d'en-tete

            bool premier_fragment = true; // Pour savoir si on commence la reception d'une nouvelle image
            bool imageTransformee;  // Pour savoir si la transformation de l'image a reussi

            string entete = "";     // variable temporaire pour recuperer l'en-tete

            byte[] fragment = new byte[tailleFragmentTheorique + 20]; // Tableau de byte contenant le fragment recu dans le socket
            byte[] image = null;    // Tableau contenant les octets representant l'image
            
            string[] chainesImage = new string[4]; // Tableau de chaines contenant les donnees de l'entete

            for (; ; )
            {
                // Initialisation
                premier_fragment = true;    // On commence la reception d'une nouvelle image
                tailleRecue = 0;            // Aucun octet recu

                // On attend l'ordre de demander l'image suivante
                semReception.WaitOne();

                // On demande au serveur l'image suivante
                #region commentaire pour raler
                // J'ai essaye de demander les images que je voulais, 
                // mais le serveur me les envoie toujours dans l'ordre... 
                // Vachement utile le get 'idImage'...
                #endregion
                if (ctrl_socket != null && ctrl_socket.Connected)
                    ctrl_socket.Send(Encoding.ASCII.GetBytes("GET -1 \r\n\r\n"));
                do
                {
                    try
                    {
                        // On attend un fragment
                        nbOctetsRecus = data_socket.Receive(fragment);
                    }
                    catch (Exception)
                    {
                        nbOctetsRecus = 0;  // Si aucune donnee recue, on passe a l'image suivante (cf while(...))
                    }
                    if (nbOctetsRecus > 0)
                    {
                        #region Gestion de l'en tete
                        int numPremierOctetDonnee = 0; numParam = 0; entete = "";
                        do
                        {
                            char c = (char)fragment[numPremierOctetDonnee];
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
                        tailleRecue += tailleFragment;
                        #endregion

                        #region Traitement du premier fragment de l'image
                        if (premier_fragment)
                        {
                            image = new byte[tailleImage];  // On cree le tableau d'octets qui va recevoir l'image
                            numImageEnCours = numImage;     // On recupere le numero de l'image que le serveur nous envoie
                            premier_fragment = false;       // On a fini les traitements impliques par le premier fragment
                        }
                        #endregion

                        // On stocke le fragment dans le tableau representant l'image
                        if (numImageEnCours == numImage) // A condition que le fragment appartienne bien a l'image...
                            for (int i = 0; i < tailleFragment; i++)
                                image[positionPaquet + i] = fragment[numPremierOctetDonnee + i];
                    }
                } while (tailleRecue < tailleImage && nbOctetsRecus > 0 && numImageEnCours == numImage);

                // On verifie que l'image est complete et on tente de la creer
                imageTransformee = numImageEnCours==numImage && tailleRecue==tailleImage;
                try
                {
                    images.Enqueue(System.Drawing.Image.FromStream(new MemoryStream(image)));
                }
                catch (Exception)
                {
                    imageTransformee = false;
                }
                if (imageTransformee)
                    stats.IncrementReceivedImages();
                else
                    stats.IncrementLooseImages();
            }
        }

        public void Pause()
        {
        }

        public void Stop()
        {
            receiveThread.Abort(); // On arrete la reception de donnees
            if (ctrl_socket != null && ctrl_socket.Connected)
            {
                ctrl_socket.Send(Encoding.ASCII.GetBytes("END \r\n\r\n")); // On signale la fin au serveur
                ctrl_socket.Close(); // On ferme le socket. Rq : on peut pas savoir si le serveur a compris qu'on est parti
            }
            if(data_socket != null)
                data_socket.Close(); // On ferme le socket
        }

        #endregion

        #region Get / Set

        public Statistics getStatistics()
        {
            return stats;
        }

        private Image lastReturnedImage; // Juste pour stocker la derniere image retournee, les variables de methode statiques n'existant pas en C# :(
        public Image GetImage()
        {
            // On verifie qu'il y a des images dans la file
            if (images.Count > 0)
            {
                lastReturnedImage = images.Dequeue(); // On recupere une image
                semReception.Release();     // On demande une reception supplementaire
            }
            return lastReturnedImage;
        } 

        #endregion
    }
}
