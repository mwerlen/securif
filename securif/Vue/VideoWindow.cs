using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Kernel;
using Stream;

namespace Vue
{
    public partial class VideoWindow : Form
    {
        #region Variables

        protected Video theVideo;
        protected iStream theStream;
        private Timer theTimer;

        /* isPlaying
         * true : la lecture est en marche 
         * false : la video est en pause
         */
        protected bool isPlaying = false;

        /* isConnected
         * true : le stream est connecté
         * false : il ne l'est pas
         */
        protected bool isConnected = false;

        #endregion

        #region Constructeur

        public VideoWindow(Video aVideo)
        {
            theVideo = aVideo;
            try
            {
                theStream = theVideo.Stream;


                InitializeComponent();

                this.Name = theVideo.Name;
                this.Text = theVideo.Name;

                InitializeTimer();

                Play();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        #endregion
        
        #region Handlers d'evenements
        
        private void Stop_Click(object sender, EventArgs e)
        {
            Stop();
        }
        
        private void PlayPause_Click(object sender, EventArgs e)
        {
            if(isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }
        
        private void VideoWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        private void theTimer_Tick(object sender, EventArgs e)
        {
            AfficherImage();
            toolStripStatusLabel1.Text = theStream.getStatistics().ToString();
        }
        
        #endregion
        
        #region Fonction privées
        
        private void InitializeTimer()
        {
            theTimer = new Timer();
            theTimer.Interval = (int)(1000 / theVideo.Fps);// as int;
            theTimer.Tick += new EventHandler(theTimer_Tick);
        }
        
        protected void Play()
        {
            if (theStream != null)
            {
                Connect();
                try
                {
                    theStream.Play();
                }
                catch
                { return; }

                theTimer.Start();
                isPlaying = true;
                toolStripButtonPlay.Image = Vue.Properties.Resources.pause;
            }
        }
        
        protected void Pause()
        {
            if (theStream != null)
            {
                theStream.Pause();
                theTimer.Stop();
            }
            isPlaying = false;
            toolStripButtonPlay.Image = Vue.Properties.Resources.play;
        }
        
        protected void Stop()
        {
            if (theStream != null)
            {
                theStream.Stop();
                theTimer.Stop();
            }
            isPlaying = false;
            isConnected = false;
            toolStripButtonPlay.Image = Vue.Properties.Resources.play;
        }

        protected void Connect()
        {
            if (!isConnected)
            {
                try
                {
                    theStream.Connect();
                }
                catch
                {
                    toolStripStatusLabel1.Text = "erreur connection";
                    return;
                }
                isConnected = true;
            }
        }

        protected void AfficherImage()
        {
            Image img = theStream.GetImage();
            this.BackgroundImage = img;

        }

       
        #endregion
    }
}