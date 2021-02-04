using System;
using CyUSB;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace USB_CTR
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        CyUSBDevice loopDevice = null;
        USBDeviceList usbDevices = null;
        CyBulkEndPoint inEndpoint = null;
        CyBulkEndPoint outEndpoint = null;

        Thread tXfers_Read;
        Thread tXfers_Write;

        const int XFERSIZE = 16384;             // Maximium 32bit * 16384 transfer(4bulk)
        byte[] outData = new byte[XFERSIZE];    // for out bulk transfer
        byte[] inData = new byte[XFERSIZE];     // for in  bulk transfer
        int xferLen_Write;                      // for bulk trasfer size set
        int xferLen_Read;                       // for bulk trasfer size set
        bool updateflag = false;                // for A/D data plot trigger
        int num = 0;                            // for Tx data array count

        int[] Tx_data = new int[4096];          // for Tx data buffer. 
        int[] Rx_data = new int[4096];          // for Tx data buffer. 

        bool writeflag = false;                 // out data transfer trigger.

        public MainWindow()
        {
            InitializeComponent();
            initDevice();

        }

        private void AddDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddDataTextBox.Text.Length != 0)                        // AddDataTextBox에 1글자 이상 입력되었으면
            {
                SendDataListBox.Items.Add(AddDataTextBox.Text);         // AddDataTextBox에 입력받은 데이터를 SendDataListBox에 추가
                AddDataTextBox.Clear();                                 // AddDataTextBox 비우기
            }

        }

        private void SendDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Tx data 리셋
            for (int i = 0; i < 4096; i++)
            {
                Tx_data[i] = 0x00000000;
            }
            // TxDataListBox 리셋
            TxDataListBox.Clear();

            // SendDataListBox에 기록된 Data를 Tx_data에 16진수 형태로 담음
            for (int i = 0; i < SendDataListBox.Items.Count; i++)                                           // SendDataListBox 아이템 수만큼 반복
            {
                String Databuff = SendDataListBox.Items[i].ToString();                                      // String으로 전환
                TxDataListBox.AppendText(Databuff + "\n");                                                  // TxDataListBox에 추가
                LogListBox.AppendText("Send Data : " + Databuff + "\n\n");                                    // Send Log 추가
                Tx_data[i] = Int32.Parse(Databuff, System.Globalization.NumberStyles.HexNumber);            // String을 16진수 숫자로 변환하여 Tx_data에 저장
            }
            writeflag = true;
        }

        private void ResetDataButton_Click(object sender, RoutedEventArgs e)
        {
            SendDataListBox.Items.Clear();
            TxDataArrayReset();
            writeflag = true;
        }

        private void TxDataArrayReset()
        {
            // Tx data 리셋
            for (int i = 0; i < 4096; i++)
            {
                Tx_data[i] = 0x00000000;
            }
            LogListBox.AppendText("Reset Tx data array\n\n");
        }

        private void ResetLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Clear();
        }

    }

}
