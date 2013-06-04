using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Kernel;
using System.Collections;
using System.Threading;

namespace Vue
{
    public partial class MainWindow : Form
    {
        protected ArrayList listeVuesServeur = new ArrayList(); // liste repr�sentant l'ensemble des serveurs
        Video selectedVideo; // Video s�lectionn�e dans l'arbre

        public MainWindow()
        {
            InitializeComponent();
        }

        #region fonctions privees

        private void AddServerTree(VueServeur leServeur)
        {
            TreeNode newTreeNode = new System.Windows.Forms.TreeNode(leServeur.Name);
            this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] { newTreeNode });
            foreach (Video v in leServeur.Videos)
                newTreeNode.Nodes.Add(v.Name);
        }

        #endregion

        #region Handlers d'evenements

        private void toolStripButtonAjouter_Click(object sender, EventArgs e)
        {
            string serverName = toolStripTextBoxAdresse.Text;
            string serverPort = toolStripTextBoxPort.Text;
            bool serverValide = true;
            int port;
            IPAddress ip;

            if (!int.TryParse(serverPort, out port))
            {
                MessageBox.Show("La valeur du port doit etre num�rique", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                serverValide = false;
            }
            if (!IPAddress.TryParse(serverName, out ip))
            {
                MessageBox.Show("Le format de l'adresse du serveur n'est pas correct", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                serverValide = false;
            }
            if (serverValide)
            {
                try
                {
                    //creation du nouveau serveur
                    Server newServer = new Server(serverName, int.Parse(serverPort));
                    VueServeur newVue = new VueServeur(serverName, newServer.ReadCatalog());
                    listeVuesServeur.Add(newVue);
                    this.AddServerTree(newVue);

                    // R�cup�ration et affichage du catalogue
                    Catalog newCatalog = newServer.ReadCatalog();
                    this.LabelInfos.Text = newCatalog.ListeVideos.Count.ToString() + " vid�os ajout�es";
                }
                catch (Exception)
                {
                    MessageBox.Show("La connexion au serveur n'a pas pu aboutir!", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            // R�cup�ration des informations et de la vid�o
            this.treeView1_Click(sender, e);

            // Lancement de la nouvelle fen�tre*/
            VideoWindow newVideo = new VideoWindow(selectedVideo);
            if (!newVideo.IsDisposed)
                newVideo.Show();
        }

        #endregion

        private void treeView1_Click(object sender, EventArgs e)
        {
            TreeNode selected = (sender as TreeView).SelectedNode;

            // Si on n'a pas double cliqu� sur le nom d'une vid�o on ne fait rien
            if (selected == null || selected.Level == 0)
            {
                return;
            }

            // Sinon on ouvre une fen�tre pour la vid�o
            TreeNode papa = selected.Parent;
            selectedVideo = (listeVuesServeur[papa.Index] as VueServeur).Videos[selected.Index] as Video;

            // Affichage des infos sur la vid�o
            LabelInfos.Text =
                "Protocole : " + selectedVideo.Protocol + "\n" +
                "IPS : " + selectedVideo.Fps.ToString() + "\n";
        }

    }

    // Classe de repr�sentation d'un serveur et de ses vid�os
    public class VueServeur
    {
        //protected ArrayList videoList;
        protected Catalog catalog;
        protected string name;

        public VueServeur(string leName, Catalog leCatalog)
        {
            name = leName;
            catalog = leCatalog;
        }

        public ArrayList Videos
        {
            get { return catalog.ListeVideos; }
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}