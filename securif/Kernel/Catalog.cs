
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Globalization;

namespace Kernel
{
    public class Catalog
    {
        
        //Déclaration des objets
        protected ArrayList videoList;    // La liste des videos
        
        /***
         * Catalog : constructeur de catalog
         * param : txtCatalog, le texte brut renvoyé par le serveur (String)
         ***/
        public Catalog(String txtCatalog)
        {
            // Initialisation de la liste des video
            videoList = new ArrayList();

            // Creation d'une liste de mots
            String[] lineList = txtCatalog.Split(new String[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            // On fait notre analyse ligne par ligne
            //(en zappant les 2 premieres lignes et la dernière ligne vide)
            for (int j = 2; j < lineList.Length-1; j++)
            {
                String[] wordList = lineList[j].Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
                
                // On crée une video courante.
                Video currentVideo = new Video();

                // On parcours les doublets de la liste en zappant le mot Object
                for (int i = 1; i < wordList.Length; i++)
                {
                    switch (wordList[i])
                    {
                        case "ID":
                            // On associe l'id et on saute l'analyse de l'ID
                            currentVideo.Id = Int32.Parse(wordList[++i]);
                            break;
                        case "name":
                            // On associe le nom et saute l'analyse de la valeur
                            currentVideo.Name = wordList[++i];
                            break;
                        case "type":
                            // On associe le nom et saute l'analyse de la valeur
                            currentVideo.Type = typeParse(wordList[++i]);
                            break;
                        case "address":
                            // On associe le nom et saute l'analyse de la valeur
                            currentVideo.ServerAddress = wordList[++i];
                            break;
                        case "port":
                            // On associe le nom et saute l'analyse de la valeur
                            currentVideo.ServerPort = Int32.Parse(wordList[++i]);
                            break;
                        case "protocol":
                            // On associe le nom et saute l'analyse de la valeur
                            currentVideo.Protocol = wordList[++i];
                            break;
                        case "ips":
                            // On associe le nom et saute l'analyse de la valeur
                            NumberFormatInfo nfi = new CultureInfo( "en-US", false ).NumberFormat;
                            currentVideo.Fps = float.Parse(wordList[++i], nfi);
                            break;
                        default:
                            break;
                    }// fin du switch
                }// Fin de la boucle sur wordList

                // On ajoute notre nouvelle video à la liste des video
                videoList.Add(currentVideo);
                
            }//fin de la boucle sur les lignes
        }

        #region public getter/setter

        public ArrayList ListeVideos
        {
            get { return this.videoList; }

        }
        #endregion

        /***
         * FindVideo : recherche d'un video par son ID dans la liste des video
         * param : VideoId, l'identifiant de la video (int)
         ***/
        public Video FindVideo(int videoID)
        {
            foreach(Video currentVideo in videoList)
            {
                if (currentVideo.Id == videoID)
                {
                    return currentVideo;
                }
            } 
            return null;
        }


        private Kernel.Video.typeImage typeParse(String type)
        {
            
            switch (type.ToUpper())
            {
                case "PNG":
                    return Kernel.Video.typeImage.PNG;
                case "GIF":
                    return Kernel.Video.typeImage.GIF;
                case "JPEG":
                    return Kernel.Video.typeImage.JPEG;
                case "BMP":
                    return Kernel.Video.typeImage.BMP;
            }
            return Kernel.Video.typeImage.Other;
        }
    }
}
