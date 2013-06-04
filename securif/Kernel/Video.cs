using System;
using System.Collections.Generic;
using System.Text;
using Stream;

namespace Kernel
{
	public class Video
    {
        public enum typeImage { PNG, GIF, JPEG, BMP, Other };

		#region variables declarations
		/**
		 *	@var         id
		 *	@type        int
		 *	@scope       protected
		 *	@description Video ID
		 **/
		protected int id;

		/**
		 *	@var         name
		 *	@type        string
		 *	@scope       protected
		 *	@description Video Name
		 **/
		protected string name;

		/**
		 *	@var         type
		 *	@type        enum
		 *	@scope       protected
		 *	@description Image type contained in the video
		 **/
		protected typeImage type;

		/**
		 *	@var         stream
		 *	@type        iStream
		 *	@scope       protected
		 *	@description Stream corresponding to the video
		 **/
		protected iStream stream;

		/**
		 *	@var         fps
		 *	@type        float
		 *	@scope       protected
		 *	@description Frame per second of the video
		 **/
		protected float fps;

		/**
		 *	@var         server_address
		 *	@type        string
		 *	@scope       protected
		 *	@description Address to use for connection
		 **/
		protected string server_address;

		/**
		 *	@var         server_port
		 *	@type        int
		 *	@scope       protected
		 *	@description Port to use for connection
		 **/
		protected int server_port;

		/**
		 *	@var         protocol
		 *	@type        string
		 *	@scope       protected
		 *	@description Name of the protocol to connect the plugin
		 **/
        protected string protocol;

		#endregion

		#region constructors

		public Video(int idVideo, string transfertProtocol, string serverAddress, int serverPort)
		{
            protocol = transfertProtocol;
            id = idVideo;
            server_address = serverAddress;
            server_port = serverPort;

            ConnectProtocol();
        }
        public Video()
        {
        }

		#endregion

        #region public methods
        public void ConnectProtocol()
        {
            if (stream != null)
            {
                return;
            }

            try
            {
                // On cree un objet en fonction du protocole utilise (parametrable, mais non fait par manque de temps)
                stream = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(
                    "../DLL/Video_" + protocol + ".dll", "Stream.Video_" + protocol) as iStream;
                stream.Address = server_address;
                stream.Port = server_port;
                stream.idVideo = id;
            }
            catch (Exception)
            {
                throw new Exception("Protocole introuvable !");
            } 
        }

        #endregion
        #region public getter/setter

        public int Id
        {
            get { return this.id; }
			set { this.id = value; }
		}
		public string Name
        {
            get { return this.name; }
			set { this.name = value; }
		}
		public typeImage Type
		{
			set { this.type = value; }
		}
        public float Fps
        {
            get { return this.fps; }
            set { this.fps = value; }
		}
		public string Protocol
		{
			get { return this.protocol; }
			set { this.protocol = value; }
		}
		public string ServerAddress
		{
            get { return this.server_address; }
			set { this.server_address = value; }
		}
		public int ServerPort
        {
            get { return this.server_port; }
			set { this.server_port = value; }
		}
		public iStream Stream
		{
			get 
            {
                if (stream == null)
                {
                    ConnectProtocol();
                }
                return this.stream; 
            }
		}
		#endregion
	}
}
