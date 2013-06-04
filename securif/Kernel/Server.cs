using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net;

namespace Kernel
{
    public class Server
    {
        public const int MAX_CHAR_IN_CATALOG = 1000;
        protected String serverAdress;
        protected int serverPort;

        //Constructeur de la classe serveur
        public Server(String theServerAdress, int theServerPort)
        {
            serverAdress = theServerAdress;
            serverPort = theServerPort;
        }

        public Catalog ReadCatalog()
        {
            // tableau contenant l'ensemble des octets du document
            byte[] messageB = new byte[MAX_CHAR_IN_CATALOG];
            
            // tableau équivalent au précédent mais contenant des caractères
            // nécessaire pour créer un string
            char[] messageC;

            // Création de la requete http
            string s = serverAdress + ":" + serverPort.ToString();
            HttpWebRequest req;
            HttpWebResponse res;
            int longueurResultat =0;
            try
            {
                 req = (HttpWebRequest)WebRequest.Create("http://" + serverAdress + ":" + serverPort.ToString());
            
            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://" + serverAdress + ":" + serverPort.ToString());

           
            
            // récupération de la réponse
             res = (HttpWebResponse)req.GetResponse();
              longueurResultat = res.GetResponseStream().Read(messageB, 0, MAX_CHAR_IN_CATALOG);
             }
             catch (System.Net.WebException)
            {
               
                 throw(new Exception("Serveur introuvable"));
               
            }
           
           /* req = (HttpWebRequest)WebRequest.Create("http://" + serverAdress + ":" + serverPort.ToString());
            res = (HttpWebResponse)req.GetResponse();*/
            // Récupération des données de la page
           
            
            // Transformation du table de byte en tableau de char
            messageC = new char[longueurResultat];
            for (int i = 0; i < longueurResultat; i++)
                messageC[i] = (char)messageB[i];
            
            // Création du catalog
            return new Catalog(new String(messageC));
        }
    }
}
