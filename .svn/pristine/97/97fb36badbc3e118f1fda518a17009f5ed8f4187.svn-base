using System;
using System.Collections.Generic;
using System.Text;

namespace Stream
{
    public class Statistics
    {
        #region stats
        protected int nbImagesPerdues;
        protected double debitMoyen;
        protected int nbImagesRecues;

        protected int nbImagesCalculDebit;
        #endregion

        #region public functions

        public override string ToString()
        {
            string retour = "";
            if (debitMoyen > -1)
            {
                if (debitMoyen > 10)
                {
                    retour += (int)(debitMoyen / 1.024) + "ko/s;";
                }
                else
                {
                    retour += (int)(debitMoyen * 1000) + "o/s; ";
                }
            }

            if (nbImagesPerdues > -1)
            {
                retour += "Images perdues : " + (nbImagesPerdues + 1);
                int taux = (int)(((double)(nbImagesPerdues + 1) / (double)(nbImagesRecues + nbImagesPerdues + 2)) * 100);
                retour += " / " + (nbImagesPerdues + nbImagesRecues + 2) + " (" + taux + "%)";
            }
            else
            {
                if (nbImagesRecues == 0)
                {
                    retour += "1 image reçue ";
                }
                if (nbImagesRecues > 0)
                {
                    retour += (nbImagesRecues + 1) + " images reçues ";
                }
            }

            return retour;
        }
        public void Reset()
        {
            nbImagesPerdues = -1;
            debitMoyen = -1;
            nbImagesRecues = -1;
        }
        public void IncrementLooseImages()
        {
            nbImagesPerdues++;
        }
        public void IncrementReceivedImages()
        {
            nbImagesRecues++;
        }
        public void IncrementReceivedImages(double rate)
        {
            debitMoyen = (double)((debitMoyen * nbImagesCalculDebit) + rate )/ (double)(++nbImagesCalculDebit);
            IncrementReceivedImages();
        }
        public void IncrementReceivedImages(long time, int size)
        {
            if (time > 0)
            {
                IncrementReceivedImages((double)size / (double)time);
            }
            else
            {
                IncrementReceivedImages();
            }

        }
        public void IncrementReceivedImages(double time, int size)
        {
            if (time > 0)
            {
                IncrementReceivedImages((double)size / (double)time);
            }
            else
            {
                IncrementReceivedImages();
            }
        }

        #endregion

        #region Constructeurs

        public Statistics()
        {
            Reset();
        }

        #endregion

        #region accesseurs
        public int getNumberOfImages()
        {
            return nbImagesRecues+nbImagesPerdues+2;
        }
        #endregion

    }
}
