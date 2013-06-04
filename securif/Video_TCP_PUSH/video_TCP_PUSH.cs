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
	public class Video_TCP_PUSH : iStream
	{
		#region Variables locales

		protected Socket socketControl;
		protected Socket socketData;
		protected Socket socketConnect;

		protected Image currentImage = null;

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
			return currentImage;
		}

		public void Connect()
		{
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
					socketConnect.Bind(new IPEndPoint(IPAddress.Any, clientPort));// + nbTry));
					socketConnect.Listen(10);
				}
				catch { }
			}
			if (!socketConnect.IsBound)
			{
				throw new Exception("connection impossible!");
			}

			// demande de connection à la video :
			if (!isConnected)
			{
				Thread connectThread = new Thread(new ThreadStart(waitConnectSocket));
				connectThread.Start();
			}

			socketControl.Send(Encoding.ASCII.GetBytes("GET " + idVideo + "\r\nLISTEN_PORT " + clientPort + "\r\n"));
		}
		public void Play()
		{
			if (!socketData.Connected || !socketConnect.IsBound)
			{
				throw new Exception("socket non connecté!");
			}
			
			if (theThread == null)
			{
				theThread = new Thread(new ThreadStart(readTCP));
				if (!theThread.IsAlive)
				{
					theThread = new Thread(new ThreadStart(readTCP));
					theThread.Start();
				}

				// S'il n'y a pas d'image et qu'on est connecté, on en charge une
				// Pas d'image : tant pis, on ne choisit pas en push...
			}

			socketControl.Send(Encoding.ASCII.GetBytes("START \r\n\r\n"));
		}
		public void Pause()
		{
			if (!socketData.Connected || !socketConnect.IsBound || !isConnected)
			{
				throw new Exception("socket non connecté!");
			}
			socketControl.Send(Encoding.ASCII.GetBytes("PAUSE \r\n\r\n"));
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
				string imgStr = "";
				string[] chainesImage = null;
				int taillePaquet;

				while (!enteteRecue)
				{

					taillePaquet = socketData.Receive(temp_data, SocketFlags.None);
					dateFinReception = DateTime.Now;

					// Transformation du byte[] en string
					for (int i = 0; i < taillePaquet; i++)
					{
						imgStr += (char)temp_data[i];
					}

					// L'entete est recu dès que les données sont divisibles en 3 (Num image/ taille / image)
					chainesImage = imgStr.Split(new string[] { "\r\n" }, 3, StringSplitOptions.RemoveEmptyEntries);
					if (chainesImage.Length == 3)
					{
						enteteRecue = true;
					}
				}

				int tailleImage = int.Parse(chainesImage[1]);
				string sss = chainesImage[2];

				// reception de la fin (on verifie que l'on recoit limage totale (taille spécifiée dans l'entête)
				for (int tailleTotale = sss.Length; tailleTotale < tailleImage; tailleTotale += taillePaquet)
				{
					taillePaquet = socketData.Receive(temp_data, SocketFlags.None);
					dateFinReception = DateTime.Now;

					// Transformation du byte[] en string
					for (int i = 0; i < taillePaquet; i++)
					{
						sss += (char)temp_data[i];
					}
				}


				// conversion en image
				char[] ccc = sss.ToCharArray();
				byte[] bbb = System.Text.Encoding.Default.GetBytes(ccc);

				MemoryStream ms = new MemoryStream(bbb);

				Image iii;
				try
				{
					iii = System.Drawing.Image.FromStream(ms);
				}
				catch
				{
					stats.IncrementLooseImages();
					return;
				}

				currentImage = iii;

				TimeSpan tps = dateFinReception.Subtract(dateDebutReception);
				stats.IncrementReceivedImages(tps.TotalMilliseconds, tailleImage);
			}

		}
		#endregion

		#region private method

		private void disconnect()
		{
			try
			{
				socketControl.Send(Encoding.ASCII.GetBytes("STOP \r\n\r\n"));
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
