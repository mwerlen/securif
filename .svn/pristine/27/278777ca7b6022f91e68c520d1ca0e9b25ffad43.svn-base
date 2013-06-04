using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Stream
{
    public interface iStream
    {
		Image GetImage();
        void Play();
        void Pause();
        void Stop();
        void Connect();

        string Address { get; set;}
        int Port { get; set;}
        int idVideo { get; set;}


		/*void setServerAddress(string address);
		void setServerPort(int port);
        void setIdVideo(int id);*/

        Statistics getStatistics();
    }
}
