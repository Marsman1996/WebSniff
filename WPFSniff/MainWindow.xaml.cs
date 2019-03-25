using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
// using System.Windows.Forms;

using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.AirPcap;
using SharpPcap.WinPcap;
using PacketDotNet;

namespace WPFSniff{
    public struct PacketsInfo{
        public int ID{get; set;}
        public string ArriveTime{get; set;}
        public string SourceAddr{get; set;}
        public string SourcePort{get; set;}
        public string DestAddr{get; set;}  
        public string DestPort{get; set;}  
        public string Protocol{get; set;}  
        public int Length{get; set;}
        public string Color{get; set;}
    }

    /// <summary>
    /// for global data trans
    /// </summary>
    public class Globalvar{
        public static int DeviceID = -1;
    }
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window{
        public MainWindow(){
            var devices = CaptureDeviceList.Instance;
            // System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private ICaptureDevice device;
        private int timeout = 2000;
        private string filter;
        private bool deviceIsOpen;

        public ArrayList reassemblyPac = new ArrayList();
        public ArrayList simpackets = new ArrayList();
        private ArrayList packets = new ArrayList();

        private void InterfacesChoose_Click(object sender, RoutedEventArgs e){
            Interface i = new Interface();
            i.ShowDialog();
        }

        private void InterfacesStart_Click(object sender, RoutedEventArgs e){
            if(Globalvar.DeviceID == -1){
                MessageBox.Show("You didn't select any device!");
                return ;
            }
            var devices = CaptureDeviceList.Instance;
            this.device = devices[Globalvar.DeviceID];
            this.deviceIsOpen = true;
            PacketsInfolistView.Items.Clear();

            Thread newThread = new Thread(new ThreadStart(threadHandler));
            newThread.Start();
        }

        private void PacketsInfoList_MouseDoubleClick(object sender, RoutedEventArgs e){

        }

        private void threadHandler(){
            this.device.OnPacketArrival += new PacketArrivalEventHandler(packetarrive);

            if (device is AirPcapDevice){
                // NOTE: AirPcap devices cannot disable local capture
                var airPcap = device as AirPcapDevice;
                airPcap.Open(SharpPcap.WinPcap.OpenFlags.DataTransferUdp, timeout);
            }
            else if(device is WinPcapDevice){
                var winPcap = device as WinPcapDevice;
                // winPcap.Open(SharpPcap.WinPcap.OpenFlags.DataTransferUdp, timeout);
                // winPcap.Open(SharpPcap.WinPcap.OpenFlags.NoCaptureLocal, timeout);
                winPcap.Open(DeviceMode.Promiscuous, timeout);
            }
            else if (device is LibPcapLiveDevice){
                var livePcapDevice = device as LibPcapLiveDevice;
                livePcapDevice.Open(DeviceMode.Promiscuous, timeout);
            }
            else{
                throw new System.InvalidOperationException("unknown device type of " + device.GetType().ToString());
            }
            // this.device.Open(DeviceMode.Promiscuous, timeout);
            this.device.Filter = this.filter;

            this.device.StartCapture();
        }

        public void packetarrive(object sender, CaptureEventArgs e){
            ProcessContext(e.Packet);
        }

        public void ProcessContext(RawCapture pac){
            packet p = new packet(pac);
            packets.Add(p);
            p.index = (PacketsInfolistView.Items.Count + 1);
            // ListViewItem item = new ListViewItem(new string[] { p.index.ToString(), p.time, p.source, p.destination, p.protocol, p.information });
            // item.BackColor = Color.FromName(p.color);
            PacketsInfo psi = new PacketsInfo();
            psi.ID         = p.index;
            psi.ArriveTime = p.time;
            psi.SourceAddr = p.source;
            psi.SourcePort = p.srcPort;
            psi.DestAddr   = p.destination;
            psi.DestPort   = p.desPort;
            psi.Protocol   = p.protocol;
            psi.Length     = p.data.Length;
            psi.Color      = p.color;
            this.Dispatcher.Invoke(
                DispatcherPriority.Normal, (ThreadStart)delegate(){
                    PacketsInfolistView.Items.Add(psi);
                    
                }
            );
        }

        public void InterfacesSStop_Click(object sender, RoutedEventArgs e){
            try {
                this.device.StopCapture();
            }
            catch { 
                ;
            }
            this.device.Close();
            this.deviceIsOpen = false;
        }

    }
}