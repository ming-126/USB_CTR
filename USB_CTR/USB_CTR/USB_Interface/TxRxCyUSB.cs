using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace USB_CTR
{

    public partial class MainWindow : Window
    {
        public void ReadThread()
        {
            while (true)
            {
                Thread.Sleep(1);

                bool bResult1 = false;
                int xferlength = xferLen_Read;
                bResult1 = inEndpoint.XferData(ref inData, ref xferlength);

                if (bResult1)
                {
                    int counter = 0;
                    for (int i = 0; i < XFERSIZE; i += 4, counter++)
                    {
                        int Data03 = inData[i + 3] << 24;
                        int Data02 = inData[i + 2] << 16;
                        int Data01 = inData[i + 1] << 8;
                        int Data00 = inData[i + 0];
                        this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                             (ThreadStart)delegate ()
                             {
                                 RxDataListBox.AppendText("RX_DATA [" + counter + "] : " + Data03.ToString("X") + Data02.ToString("X") + Data01.ToString("X") + Data00.ToString("X") + "\n");
                             }
                        );

                    }

                }

            }

        }


        public void WriteThread()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (writeflag == true)
                {
                    bool bResult = true;
                    int j = 0;
                    int counter = 0;
                    for (int i = 0; i < XFERSIZE; i += 4, counter++)
                    {
                        outData[i + 3] = (byte)(Tx_data[j] >> 24);
                        outData[i + 2] = (byte)(Tx_data[j] >> 16);
                        outData[i + 1] = (byte)(Tx_data[j] >> 8);
                        outData[i + 0] = (byte)Tx_data[j];
                        j++;

                    }
                    int xferlengths = xferLen_Write;
                    bResult = outEndpoint.XferData(ref outData, ref xferlengths);
                    if (bResult == false)
                        outEndpoint.Reset();

                    writeflag = false;
                }

            }

        }

    }

}
