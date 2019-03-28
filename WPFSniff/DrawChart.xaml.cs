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
using Visifire.Charts;

namespace WPFSniff
{
    /// <summary>
    /// DrawChart.xaml 的交互逻辑
    /// </summary>
    public partial class DrawChart : Window{
        public DrawChart(string chart_name, Dictionary<string, int> temp_dic){
            InitializeComponent();

            List<string> xval = new List<string>();
            List<string> yval = new List<string>();
            foreach (string key in temp_dic.Keys){
                xval.Add(key);
                yval.Add(temp_dic[key].ToString());
            }

            chart_display.Children.Clear();
            if(chart_name == "Network Layer"){
                CreateChartPie("Network Layer", xval, yval);
            }
            else if(chart_name == "Transport Layer"){
                CreateChartPie("Transport Layer", xval, yval);
            }
        }

        public void CreateChartPie(string name, List<string> valuex, List<string> valuey){
            //创建一个图表
            Chart chart = new Chart();

            //设置图标的宽度和高度
            chart.Width = 580;
            chart.Height = 380;
            chart.Margin = new Thickness(100, 5, 10, 5);
            //是否启用打印和保持图片
            chart.ToolBarEnabled = false;

            //设置图标的属性
            chart.ScrollingEnabled = false;//是否启用或禁用滚动
            chart.View3D = true;//3D效果显示

            //创建一个标题的对象
            Title title = new Title();

            //设置标题的名称
            title.Text = name;
            title.Padding = new Thickness(0, 10, 5, 0);

            //向图标添加标题
            chart.Titles.Add(title);

            //Axis yAxis = new Axis();
            ////设置图标中Y轴的最小值永远为0           
            //yAxis.AxisMinimum = 0;
            ////设置图表中Y轴的后缀          
            //yAxis.Suffix = "斤";
            //chart.AxesY.Add(yAxis);

            // 创建一个新的数据线。               
            DataSeries dataSeries = new DataSeries();

            // 设置数据线的格式
            dataSeries.RenderAs = RenderAs.Pie;//柱状Stacked


            // 设置数据点              
            DataPoint dataPoint;
            for (int i = 0; i < valuex.Count; i++)
            {
                // 创建一个数据点的实例。                   
                dataPoint = new DataPoint();
                // 设置X轴点                    
                dataPoint.AxisXLabel = valuex[i];

                dataPoint.LegendText = "##" + valuex[i];
                //设置Y轴点                   
                dataPoint.YValue = double.Parse(valuey[i]);
                //添加一个点击事件        
                dataPoint.MouseLeftButtonDown += new MouseButtonEventHandler(dataPoint_MouseLeftButtonDown);
                //添加数据点                   
                dataSeries.DataPoints.Add(dataPoint);
            }

            // 添加数据线到数据序列。                
            chart.Series.Add(dataSeries);

            //将生产的图表增加到Grid，然后通过Grid添加到上层Grid.
            Grid gr = new Grid();
            gr.Children.Add(chart);
            chart_display.Children.Add(gr);
        }

        void dataPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e){
            //DataPoint dp = sender as DataPoint;
            //MessageBox.Show(dp.YValue.ToString());
        }

    }
}
