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
        public static int pacnum = -1;
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
            psi.Length     = p.paclen;
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

        private void PacketsInfoList_MouseDoubleClick(object sender, RoutedEventArgs e){
            var listview = sender as ListView;
            var currentitem = listview.SelectedItem;
            PacketsInfo psi = (PacketsInfo)currentitem;
            packet pac = (packet)packets[psi.ID - 1];

            PackettreeView.Items.Clear();

            if(pac.PacketInforArray.Count > 0){
                TreeViewItem item1 = new TreeViewItem();
                item1.Header = "Frame:";
                for(int i = 0; i < pac.PacketInforArray.Count; i++){
                    item1.Items.Add(pac.PacketInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item1);

                // PackettreeView.Items.Add("Frame: ");
                // DockPanel dp= new DockPanel();
                // for(int i = 0; i < pac.PacketInforArray.Count; i++){
                //     TextBlock tb = new TextBlock();
                //     tb.Text = pac.PacketInforArray[i].ToString();
                //     dp.Children.Add(tb);
                // }
                // (PackettreeView.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem).Items.Add(dp);
            }
            
            if(pac.EthernetInforArray.Count > 0){
                TreeViewItem item2 = new TreeViewItem();
                item2.Header = "Ethernet:";
                for(int i = 0; i < pac.EthernetInforArray.Count; i++){
                    item2.Items.Add(pac.EthernetInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item2);
            }

            if(pac.IpInforArray.Count > 0){
                TreeViewItem item3 = new TreeViewItem();
                item3.Header = "Internet Protocol:";
                for(int i = 0; i < pac.IpInforArray.Count; i++){
                    item3.Items.Add(pac.IpInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item3);
            }

            if(pac.IcmpInforArray.Count > 0){
                TreeViewItem item4 = new TreeViewItem();
                item4.Header = "Internet Control Message Protocol:";
                for(int i = 0; i < pac.IcmpInforArray.Count; i++){
                    item4.Items.Add(pac.IcmpInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item4);
            }

            if(pac.UdpInforArray.Count > 0){
                TreeViewItem item5 = new TreeViewItem();
                item5.Header = "Internet Control Message Protocol:";
                for(int i = 0; i < pac.UdpInforArray.Count; i++){
                    item5.Items.Add(pac.UdpInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item5);
            }

            if(pac.TcpInforArray.Count > 0){
                TreeViewItem item6 = new TreeViewItem();
                item6.Header = "Transmission Control Protocol:";
                for(int i = 0; i < pac.TcpInforArray.Count; i++){
                    item6.Items.Add(pac.TcpInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item6);
            }

            if(pac.ArpInforArray.Count > 0){
                TreeViewItem item7 = new TreeViewItem();
                item7.Header = "Address Resolution Protocol:";
                for(int i = 0; i < pac.ArpInforArray.Count; i++){
                    item7.Items.Add(pac.ArpInforArray[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item7);
            }

            if(pac.ApplicationInfor.Count > 0){
                TreeViewItem item8 = new TreeViewItem();
                item8.Header = "Application Layer Protocol:";
                for(int i = 0; i < pac.ApplicationInfor.Count; i++){
                    item8.Items.Add(pac.ApplicationInfor[i].ToString().Replace("\n", ""));
                }
                PackettreeView.Items.Add(item8);
            }

            PacketStatus.Text = str2hex(pac.rawp.Data);
        }

        public string str2hex(byte[] b){
            // if(str.Length % 2 != 0)
            //     str+=" ";
            string hexstr = string.Empty;
            // byte[] b = Encoding.GetEncoding("UTF-8").GetBytes(str);
            for (int i = 0; i < b.Length; i++){
                if(i % 16 == 0){
                    string len = string.Empty;
                    len += Convert.ToString(i, 16);
                    for(int j = 0; j < 8-len.Length; j++){
                        hexstr += "0";
                    }
                    hexstr += len;
                    hexstr += "\t";
                }
                string hexchar = string.Empty;
                hexchar = Convert.ToString(b[i], 16).ToUpper();
                hexchar = (hexchar.Length == 1) ? "0"+hexchar : hexchar;
                hexstr += hexchar + " ";
                // hexstr += " ";
                if (i % 16 == 15){
                    hexstr += "\n";
                }
            }
            return hexstr;
        }

    }
}