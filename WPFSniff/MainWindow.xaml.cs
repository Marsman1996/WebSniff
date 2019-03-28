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
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using ListView = System.Windows.Controls.ListView;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.IO;

using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.AirPcap;
using SharpPcap.WinPcap;
using PacketDotNet;
using ListViewItem = System.Windows.Forms.ListViewItem;

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
        public static string Device = "None";
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

        Dictionary<string, int> Network_dic = new Dictionary<string, int>();
        Dictionary<string, int> TRANS_dic = new Dictionary<string, int>();

        private void AnalyseNL_Click(Object sender, RoutedEventArgs e){
            Dictionary<string, int> temp_dic = new Dictionary<string, int>(Network_dic);
            DrawChart nl_chart = new DrawChart("Network Layer", temp_dic);
            nl_chart.Show();
        }

        private void AnalyseTL_Click(Object sender, RoutedEventArgs e){
            Dictionary<string, int> temp_dic = new Dictionary<string, int>(TRANS_dic);
            DrawChart nl_chart = new DrawChart("Transport Layer", temp_dic);
            nl_chart.Show();
        }

        private void InterfacesChoose_Click(object sender, RoutedEventArgs e){
            Interface i = new Interface();
            i.ShowDialog();
            st_interface.Text=Globalvar.Device;
        }

        private void InterfacesStart_Click(object sender, RoutedEventArgs e){
            if(this.deviceIsOpen == false){
                if(Globalvar.DeviceID == -1){
                    MessageBox.Show("You didn't select any device!");
                    return ;
                }
                var devices = CaptureDeviceList.Instance;
                this.device = devices[Globalvar.DeviceID];
                this.deviceIsOpen = true;
                PacketsInfolistView.Items.Clear();
                Network_dic = new Dictionary<string, int>();
                TRANS_dic = new Dictionary<string, int>();

                Thread newThread = new Thread(new ThreadStart(threadHandler));
                newThread.Start();
                st_st.Text="Running";
                ss_image.Source = new BitmapImage(new Uri("Image/stop.png",UriKind.RelativeOrAbsolute));
            }
            else{
                try {
                    this.device.StopCapture();
                }
                catch { 
                    ;
                }
                this.device.Close();
                this.deviceIsOpen = false;
                st_st.Text = "Not Run";
                ss_image.Source = new BitmapImage(new Uri("Image/start.png", UriKind.RelativeOrAbsolute));
            }
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
            packet p = new packet(e.Packet);
            packets.Add(p);
            ProcessContext(p);
        }

        public void ProcessContext(packet p){
            if(p.Network_type == null){
                ;
            }
            else if(Network_dic.ContainsKey(p.Network_type)){
                Network_dic[p.Network_type]++;
            }
            else{
                Network_dic[p.Network_type] = 1;
            }

            if(p.TRANS_type == null){
                ;
            }
            else if(TRANS_dic.ContainsKey(p.TRANS_type)){
                TRANS_dic[p.TRANS_type]++;
            }
            else{
                TRANS_dic[p.TRANS_type] = 1;
            }
            
            p.index = (p.index == 0) ? (PacketsInfolistView.Items.Count + 1) : p.index;
            PacketsInfo psi = new PacketsInfo{
                ID = p.index,
                ArriveTime = p.time,
                SourceAddr = p.source,
                SourcePort = p.srcPort,
                DestAddr = p.destination,
                DestPort = p.desPort,
                Protocol = p.protocol,
                Length = p.paclen,
                Color = p.color
            };
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
            st_st.Text = "Not Run";
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

        private void Filterrule_Keydown(object sender, KeyEventArgs e){
            if(e.Key.ToString() != "Return")
                return ;
            this.filter = "";
            int item = filterchoice.SelectedIndex;
            if (item != -1){
                switch (item){
                    case 0:
                        filter = "" + Filterrule.Text;
                        break;
                    case 1:
                        filter = "dst port " + Filterrule.Text;
                        break;
                    case 2:
                        filter = "src port " + Filterrule.Text;
                        break;
                    case 3:
                        filter = "des host " + Filterrule.Text;
                        break;
                    case 4:
                        filter = "src host " + Filterrule.Text;
                        break;
                }
            }

            if (this.deviceIsOpen)
                MessageBox.Show("This will work during the next Capture!");
            st_filter.Text=this.filter;
        }

        private void Searchrule_Keydown(object sender, KeyEventArgs e){
            if (e.Key.ToString() != "Return")
                return ;
            if(this.deviceIsOpen){
                MessageBox.Show("You must stop Capture first!");
                return ;
            }
            string stext = Searchrule.Text;
            PacketsInfolistView.Items.Clear();
            Network_dic = new Dictionary<string, int>();
            TRANS_dic = new Dictionary<string, int>();
            if(stext != ""){
                foreach (packet p in this.packets){
                    for (int i = 0; i < p.KeyWords.Count; i++){
                        if (stext.ToUpper() == p.KeyWords[i].ToString().ToUpper()){
                            ProcessContext(p);
                            continue;
                        }
                    }
                }
                st_search.Text = stext;
            }
            else{
                foreach (packet p in this.packets){
                    ProcessContext(p);
                    continue;
                }
                st_search.Text = "None";
            }

        }

        private void SaveFile_Click(object sender, RoutedEventArgs e){
            System.Windows.Forms.SaveFileDialog savefile1 = new SaveFileDialog();
            savefile1.InitialDirectory = Environment.CurrentDirectory;
            savefile1.Filter = "pcap files (*.pcap)|*.pcap";
            savefile1.AddExtension = true;
            savefile1.RestoreDirectory = true;
            savefile1.ShowDialog();

            if (savefile1.FileName.ToString() != ""){
                try{
                    string name = savefile1.FileName;
                    this.device.Open();
                    SharpPcap.LibPcap.CaptureFileWriterDevice captureFileWriter = new SharpPcap.LibPcap.CaptureFileWriterDevice((SharpPcap.LibPcap.LibPcapLiveDevice)this.device, name);
                    foreach (packet pac in this.packets){
                        captureFileWriter.Write(pac.rawp);
                    }
                    MessageBox.Show("Svae Success!");
                }
                catch (Exception){
                    MessageBox.Show("Save Fail!");
                }
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e){
            System.Windows.Forms.OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = Environment.CurrentDirectory;
            openfile.Filter = "pcap files (*.pcap)|*.pcap";
            openfile.CheckFileExists = true;
            openfile.RestoreDirectory = true;

            if (openfile.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                string name = openfile.FileName;

                PacketsInfolistView.Items.Clear();
                Network_dic = new Dictionary<string, int>();
                TRANS_dic = new Dictionary<string, int>();

                this.packets = new ArrayList();

                SharpPcap.LibPcap.CaptureFileReaderDevice reader = new SharpPcap.LibPcap.CaptureFileReaderDevice(name);
                RawCapture rawp;

                try{
                    rawp = reader.GetNextPacket();
                    while (rawp != null){
                        packet temp = new packet(rawp);
                        packets.Add(temp);

                        ProcessContext(temp);

                        rawp = reader.GetNextPacket();
                    }
                    MessageBox.Show("success!");
                }
                catch (Exception){
                    MessageBox.Show("fail!");
                }
            }
        }


    }
}