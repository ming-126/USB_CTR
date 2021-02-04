using CyUSB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace USB_CTR
{
    public partial class MainWindow : Window
    {
        public void initDevice()
        {
            usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);
            if (usbDevices.Count != 0) {
                LogListBox.AppendText("Found " + usbDevices.Count + " USB Devices\n\n");
                setDevice();
            }

            //Adding event handlers for device attachment and device removal
            usbDevices.DeviceAttached += new EventHandler(usbDevices_DeviceAttached);
            usbDevices.DeviceRemoved += new EventHandler(usbDevices_DeviceRemoved);
        }

        void usbDevices_DeviceAttached(object sender, EventArgs e)
        {
            LogListBox.AppendText("Connected New USB Device\n\n");
            setDevice();
        }

        void usbDevices_DeviceRemoved(object sender, EventArgs e)
        {
            LogListBox.AppendText("Removed USB Device\n\n");

            //when device is removed, rd/rw thread will be shutdown.
            tXfers_Read.Abort();
            tXfers_Read.Join();
            tXfers_Read = null;
            outEndpoint.Reset();

            tXfers_Write.Abort();
            tXfers_Write.Join();
            tXfers_Write = null;
            inEndpoint.Reset();
            setDevice();
        }

        public void setDevice()
        {
            int nCurSelection = 0;
            int nDeviceList = usbDevices.Count;

            // 기존 데이터가 있으면 비우기
            if (ConnectCyUsbComboBox.Items.Count > 0)
            {
                nCurSelection = ConnectCyUsbComboBox.SelectedIndex;

                ConnectCyUsbComboBox.Items.Clear();
                ConnectCyUsbInEndpointComboBox.Items.Clear();
                ConnectCyUsbOutEndpointComboBox.Items.Clear();
            }

            // 기기 리스트 추가
            for (int nCount = 0; nCount < nDeviceList; nCount++)
            {
                USBDevice nDevice = usbDevices[nCount];
                String DeviceID = "(0x" + nDevice.ProductID.ToString("X4") + ") MITS_US_BOARD Rev1.0";
                ConnectCyUsbComboBox.Items.Add(DeviceID);
            }

            if (ConnectCyUsbComboBox.Items.Count > 0)
            {
                ConnectCyUsbComboBox.SelectedIndex = nCurSelection;

                loopDevice = usbDevices[ConnectCyUsbComboBox.SelectedIndex] as CyUSBDevice;

                GetEndpointsOfNode(loopDevice.Tree);

                // 아이템이 있으면 인덱스 0
                if (ConnectCyUsbInEndpointComboBox.Items.Count > 0) ConnectCyUsbInEndpointComboBox.SelectedIndex = 0;
                if (ConnectCyUsbOutEndpointComboBox.Items.Count > 0) ConnectCyUsbOutEndpointComboBox.SelectedIndex = 0;

                // Set the IN and OUT endpoints per the selected radio buttons.
                ConstructEndpoints();

                if ((outEndpoint != null) && (inEndpoint != null))
                {
                    if (loopDevice.bSuperSpeed)
                    {
                        xferLen_Write = 16384;
                        xferLen_Read = 16384;
                    }
                    else if (loopDevice.bHighSpeed)
                    {
                        xferLen_Write = 512;
                        xferLen_Read = 512;
                    }

                    string str = string.Format("Data Speed Write : {0} / Read {1}", xferLen_Write, xferLen_Read);
                    LogListBox.AppendText(str);

                    tXfers_Read = new Thread(new ThreadStart(ReadThread));
                    tXfers_Read.IsBackground = true;
                    tXfers_Read.Priority = ThreadPriority.Highest;

                    tXfers_Write = new Thread(new ThreadStart(WriteThread));
                    tXfers_Write.IsBackground = true;
                    tXfers_Write.Priority = ThreadPriority.Lowest;

                    tXfers_Read.Start();
                    tXfers_Write.Start();
                    LogListBox.AppendText("thstarted");

                }

            }
                
        }


        // Endpoint 
        private void GetEndpointsOfNode(TreeNode devTree)
        {
            // 박스 클리어
            ConnectCyUsbInEndpointComboBox.Items.Clear();
            ConnectCyUsbOutEndpointComboBox.Items.Clear();

            // in Endpoint, out Endpoint 박스에 텍스트 추가
            foreach (TreeNode node in devTree.Nodes)
            {
                // 노드 맨 하위에 위치한 Endpoint 찾기
                if (node.Nodes.Count > 0)
                    GetEndpointsOfNode(node);
                else
                {
                    CyUSBEndPoint ept = node.Tag as CyUSBEndPoint;
                    if (ept == null)
                    {
                        // Tag가 없으면 pass
                    }
                    // 벌크 인 엔드포인트 정보 찾기
                    else if (node.Text.Contains("Bulk in"))
                    {
                        // Alterate Setting ??
                        CyUSBInterface ifc = node.Parent.Tag as CyUSBInterface;
                        string s = string.Format("ALT-{0}, {1} Byte {2}", ifc.bAlternateSetting, ept.MaxPktSize, node.Text);
                        ConnectCyUsbInEndpointComboBox.Items.Add(s);
                    }
                    // 벌크 아웃 엔드포인트 정보 찾기
                    else if (node.Text.Contains("Bulk out"))
                    {
                        CyUSBInterface ifc = node.Parent.Tag as CyUSBInterface;
                        string s = string.Format("ALT-{0}, {1} Byte {2}", ifc.bAlternateSetting, ept.MaxPktSize, node.Text);
                        ConnectCyUsbOutEndpointComboBox.Items.Add(s);
                    }

                }

            }

        }

        private void ConstructEndpoints()
        {
            if (loopDevice != null && ConnectCyUsbOutEndpointComboBox.Items.Count > 0 && ConnectCyUsbInEndpointComboBox.Items.Count > 0)
            {
                // string에서 bAlternateSetting 파싱
                string sAltOut = ConnectCyUsbOutEndpointComboBox.Text.Substring(4, 1);
                byte outAltInferface = Convert.ToByte(sAltOut);

                string sAltIn = ConnectCyUsbInEndpointComboBox.Text.Substring(4, 1);
                byte inAltInferface = Convert.ToByte(sAltIn);

                if (outAltInferface != inAltInferface)
                {
                    // BULK LOOP 인터페이스는 ALT 인터페이스 세팅 코드가 일치하게 되어있음
                    // IN, OUT ENDPOINT가 같은 ALT 인터페이스가 아니면 로그 출력 후 리턴
                    LogListBox.AppendText("Output Endpoint and Input Endpoint should present in the same ALT interface\n\n");

                    return;
                }

                // STRING에서 ENDPOINT 주소 파싱
                int aX = ConnectCyUsbInEndpointComboBox.Text.LastIndexOf("0x");
                string sAddr = ConnectCyUsbInEndpointComboBox.Text.Substring(aX, 4);
                byte addrIn = (byte)Util.HexToInt(sAddr);

                aX = ConnectCyUsbOutEndpointComboBox.Text.LastIndexOf("0x");
                sAddr = ConnectCyUsbOutEndpointComboBox.Text.Substring(aX, 4);
                byte addrOut = (byte)Util.HexToInt(sAddr);

                outEndpoint = loopDevice.EndPointOf(addrOut) as CyBulkEndPoint;
                inEndpoint = loopDevice.EndPointOf(addrIn) as CyBulkEndPoint;

                if ((outEndpoint != null) && (inEndpoint != null))
                {
                    //make sure that the device configuration doesn't contain the other than bulk endpoint

                    if ((outEndpoint.Attributes & 0x03/*0,1 bit for type of transfer*/) != 0x02/*Bulk endpoint*/)
                    {
                        LogListBox.AppendText("Device Configuration mismatch\n\n");
                        return;

                    }
                    if ((inEndpoint.Attributes & 0x03) != 0x02)
                    {
                        LogListBox.AppendText("Device Configuration mismatch\n\n");
                        return;
                    }
                    outEndpoint.TimeOut = 1;
                    inEndpoint.TimeOut = 1;
                }
                else
                {
                    LogListBox.AppendText("Device Configuration mismatch\n\n");
                    return;
                }

            }

        }

    }

}
