using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace UDPTool
{
    public delegate void DelegateVoid();

    public partial class MainForm : Form
    {
        public UdpClient _udpClient = null;
        public IPEndPoint _endPt = null;
        private Queue<Byte[]> _receptionList;
        private Thread _queueThread;
        private EventWaitHandle _queueThreadStop;
        private bool _FirstPacketReceived;

        public MainForm()
        {
            InitializeComponent();
            _receptionList = new Queue<byte[]>();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            IPAddress ipAddrBind = null;
            int udpPort = 0;

            lock (_receptionList)
            {
                _receptionList.Clear();
            }

            if (_udpClient != null)
            {
                try
                {
                    _udpClient.Close();
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }
            }
            bool okToCreate = true;
            try
            {
                if (textBoxAdapterAddress.Text.Length == 0)
                    ipAddrBind = IPAddress.Any; //any adapter
                else
                    ipAddrBind = IPAddress.Parse(textBoxAdapterAddress.Text);
            }
            catch (Exception eee)
            {
                MessageBox.Show(eee.Message);
                okToCreate = false;
            }
            try
            {
                udpPort = Convert.ToInt32(textBoxListenPort.Text);
            }
            catch (Exception eee)
            {
                MessageBox.Show(eee.Message);
                okToCreate = false;
            }
            if (okToCreate)
            {
                try
                {
                    _endPt = new IPEndPoint(ipAddrBind, udpPort);
                }
                catch (Exception eee)
                {
                    MessageBox.Show(eee.Message);
                    okToCreate = false;
                }
            }

            if (okToCreate)
            {
                try
                {
                    _udpClient = new UdpClient(_endPt);
                }
                catch (Exception eeee)
                {
                    MessageBox.Show(eeee.Message);
                    okToCreate = false;
                }
                if (okToCreate)
                {
                    _queueThreadStop = new EventWaitHandle(false, EventResetMode.AutoReset);
                    _queueThread = new Thread(QueueProc);
                    _queueThread.Start();
                    buttonStop.Enabled = true;
                    buttonStart.Enabled = false;
                }
            }
        }

        public void QueueProc()
        {
            while (_queueThreadStop.WaitOne(100, false) != true)
            {
                try
                {
                    Byte[] receiveBytes = _udpClient.Receive(ref _endPt);
                    if (receiveBytes != null && receiveBytes.Length > 0)
                    {
                        lock (_receptionList)
                        {
                            _receptionList.Enqueue(receiveBytes);
                        }
                        DisplayData();
                    }
                }
                catch
                {
                    break;
                }
            }

            try
            {
                _udpClient.Close();
                _udpClient = null;
            }
            catch
            {
            }

            DoStop();
        }

        public void DoStop()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new DelegateVoid(DoStop));
                return;
            }
            buttonStop_Click(null, null);
            textBoxReception.Text += "\r\nThread stopped\r\n";
        }

        public void DisplayData()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new DelegateVoid(DisplayData));
                return;
            }

            lock (_receptionList)
            {
                byte[] datagram = null;
                try
                {
                    datagram = _receptionList.Dequeue();
                }
                catch 
                {
                    datagram = null;
                }
                    while (datagram != null && datagram.Length > 0)
                    {
                        string displayData = Encoding.ASCII.GetString(datagram);
                        textBoxReception.Text += displayData;
                        try
                        {
                            datagram = _receptionList.Dequeue();
                        }
                        catch 
                        { 
                            break;  
                        }
                    }
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (_queueThreadStop != null)
            {
                _queueThreadStop.Set();
                try
                {
                    _udpClient.Close();
                }
                catch
                {
                }
                _queueThreadStop = null;
                _queueThread = null;
            }
            buttonStop.Enabled = false;
            buttonStart.Enabled = true;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxReception.Clear();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            UdpClient udpClient = new UdpClient();
            try
            {
                udpClient.Connect(textBoxDestAddress.Text, Convert.ToInt32(textBoxDestPort.Text));
                Byte[] sendBytes = Encoding.ASCII.GetBytes(textBoxFrame.Text);
                udpClient.Send(sendBytes, sendBytes.Length);
                udpClient.Close();
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.ToString());
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DoStop();
        }

    }
}
