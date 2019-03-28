using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using SharpPcap;

namespace WPFSniff{
    public struct DeviceInfo{
        public int DeviceID{get; set;}
        public string Device{get; set;}
        public string Description{get; set;}
        public string Addr{get; set;}

    }

    /// <summary>
    /// Interface.xaml 的交互逻辑
    /// </summary>
    public partial class Interface : Window{
        public Interface(){
            InitializeComponent();
            Refresh();
        }

        private void Refresh(){
            var devices = CaptureDeviceList.Instance;
            if (devices.Count < 1){
                Console.WriteLine("No devices were found on this machine");
                return;
            }
            // List<DeviceInfo> dilist = new List<DeviceInfo>();
            DeviceInfo di = new DeviceInfo();
            int i = 1;
            foreach(ICaptureDevice dev in devices){
                di.DeviceID = i++;
                string devinfo = dev.ToString();

                string devname = devinfo.Substring(devinfo.IndexOf("FriendlyName: "), 60);
                di.Device = devname.Substring("FriendlyName: ".Length, devname.IndexOf('\n') - "FriendlyName: ".Length);

                string devdescription = devinfo.Substring(devinfo.IndexOf("Description: "), 120);
                di.Description = devdescription.Substring("Description: ".Length, devdescription.IndexOf('\n') - "Description: ".Length);
                
                string devaddr = devinfo.Substring(devinfo.IndexOf("Addr:      "), 60);
                di.Addr = devaddr.Substring("Addr:      ".Length, devaddr.IndexOf('\n') - "Addr:      ".Length);
                
                // dilist.Add(di);

                DevicelistView.Items.Add(di);
            }
            // DevicelistView.ItemsSource = dilist;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e){
            Refresh();
        }

        private void Select_Click(object sender, RoutedEventArgs e){
            var currentitem = DevicelistView.SelectedItem;
            DeviceInfo di = (DeviceInfo)currentitem;
            Globalvar.DeviceID = di.DeviceID - 1;
            Globalvar.Device = di.Device;
            // MainWindow.st_interface.Text = di.Description;
            this.Close();
        }

    }

}
