using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml;

namespace BrokenRailMonitorViaWiFi.Windows
{
    /// <summary>
    /// Interaction logic for GetHistoryWindow.xaml
    /// </summary>
    public partial class GetHistoryWindow : Window
    {
        public int YearStart { get; set; }
        public int MonthStart { get; set; }
        public int DayStart { get; set; }
        public int HourStart { get; set; }
        public int MinuteStart { get; set; }
        public int SecondStart { get; set; }
        public int YearEnd { get; set; }
        public int MonthEnd { get; set; }
        public int DayEnd { get; set; }
        public int HourEnd { get; set; }
        public int MinuteEnd { get; set; }
        public int SecondEnd { get; set; }
        public GetHistoryWindow()
        {
            InitializeComponent();
            try
            {
                if (File.Exists("remember.xml"))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load("remember.xml");

                    XmlNode nodeDayStart = xmlDoc.SelectSingleNode("config/GetHistoryDayStart");
                    if (nodeDayStart != null)
                    {
                        DateTime result;
                        if (DateTime.TryParse(nodeDayStart.InnerText.Trim(), out result))
                        {
                            this.dpStartDate.SelectedDate = result;
                        }
                    }
                    else
                    {
                        XmlNode xn1 = xmlDoc.SelectSingleNode("config");
                        XmlElement xe2 = xmlDoc.CreateElement("GetHistoryDayStart");//创建一个<AimFrameNo8Way>节点
                        xe2.InnerText = this.dpStartDate.SelectedDate.ToString();
                        xn1.AppendChild(xe2);
                    }

                    XmlNode nodeTimeStart = xmlDoc.SelectSingleNode("config/GetHistoryTimeStart");
                    if (nodeTimeStart != null)
                    {
                        DateTime result;
                        if (DateTime.TryParse(nodeTimeStart.InnerText.Trim(), out result))
                        {
                            this.tpStartTime.SelectedTime = result;
                        }
                    }
                    else
                    {
                        XmlNode xn1 = xmlDoc.SelectSingleNode("config");
                        XmlElement xe2 = xmlDoc.CreateElement("GetHistoryTimeStart");//创建一个<AimFrameNo8Way>节点
                        xe2.InnerText = this.tpStartTime.SelectedTime.ToString();
                        xn1.AppendChild(xe2);
                    }

                    XmlNode nodeDayEnd = xmlDoc.SelectSingleNode("config/GetHistoryDayEnd");
                    if (nodeDayEnd != null)
                    {
                        DateTime result;
                        if (DateTime.TryParse(nodeDayEnd.InnerText.Trim(), out result))
                        {
                            this.dpEndDate.SelectedDate = result;
                        }
                    }
                    else
                    {
                        XmlNode xn1 = xmlDoc.SelectSingleNode("config");
                        XmlElement xe2 = xmlDoc.CreateElement("GetHistoryDayEnd");//创建一个<AimFrameNo8Way>节点
                        xe2.InnerText = this.dpEndDate.SelectedDate.ToString();
                        xn1.AppendChild(xe2);
                    }

                    XmlNode nodeTimeEnd = xmlDoc.SelectSingleNode("config/GetHistoryTimeEnd");
                    if (nodeTimeEnd != null)
                    {
                        DateTime result;
                        if (DateTime.TryParse(nodeTimeEnd.InnerText.Trim(), out result))
                        {
                            this.tpEndTime.SelectedTime = result;
                        }
                    }
                    else
                    {
                        XmlNode xn1 = xmlDoc.SelectSingleNode("config");
                        XmlElement xe2 = xmlDoc.CreateElement("GetHistoryTimeEnd");//创建一个<AimFrameNo8Way>节点
                        xe2.InnerText = this.tpEndTime.SelectedTime.ToString();
                        xn1.AppendChild(xe2);
                    }
                    xmlDoc.Save("remember.xml");
                }
                else
                {
                    XmlTextWriter writer = new XmlTextWriter("remember.xml", null);
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("config");
                    writer.WriteStartElement("GetHistoryDayStart");
                    writer.WriteEndElement();
                    writer.WriteStartElement("GetHistoryTimeStart");
                    writer.WriteEndElement();
                    writer.WriteStartElement("GetHistoryDayEnd");
                    writer.WriteEndElement();
                    writer.WriteStartElement("GetHistoryTimeEnd");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show("获取历史信息窗口初始化异常：" + ee.Message);
            }
        }
        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (this.dpStartDate.SelectedDate.HasValue)
            {
                this.YearStart = this.dpStartDate.SelectedDate.Value.Year % 100;//年只有一字节，不算2000，如2016只记做16.
                this.MonthStart = this.dpStartDate.SelectedDate.Value.Month;
                this.DayStart = this.dpStartDate.SelectedDate.Value.Day;
            }
            else
            {
                MessageBox.Show("‘开始日期’必须选择一个值！");
                return;
            }
            if (this.tpStartTime.SelectedTime.HasValue)
            {
                this.HourStart = this.tpStartTime.SelectedTime.Value.Hour;
                this.MinuteStart = this.tpStartTime.SelectedTime.Value.Minute;
                this.SecondStart = this.tpStartTime.SelectedTime.Value.Second;
            }
            if (this.dpEndDate.SelectedDate.HasValue)
            {
                this.YearEnd = this.dpEndDate.SelectedDate.Value.Year % 100;//年只有一字节，不算2000，如2016只记做16.
                this.MonthEnd = this.dpEndDate.SelectedDate.Value.Month;
                this.DayEnd = this.dpEndDate.SelectedDate.Value.Day;
            }
            else
            {
                MessageBox.Show("‘结束日期’必须选择一个值！");
                return;
            }
            if (this.tpEndTime.SelectedTime.HasValue)
            {
                this.HourEnd = this.tpEndTime.SelectedTime.Value.Hour;
                this.MinuteEnd = this.tpEndTime.SelectedTime.Value.Minute;
                this.SecondEnd = this.tpEndTime.SelectedTime.Value.Second;
            }
            if (File.Exists("remember.xml"))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load("remember.xml");
                XmlNode nodeDayStart = xmlDoc.SelectSingleNode("config/GetHistoryDayStart");
                if (nodeDayStart != null)
                {
                    nodeDayStart.InnerText = this.dpStartDate.SelectedDate.ToString();
                }
                XmlNode nodeTimeStart = xmlDoc.SelectSingleNode("config/GetHistoryTimeStart");
                if (nodeTimeStart != null)
                {
                    nodeTimeStart.InnerText = this.tpStartTime.SelectedTime.ToString();
                }
                XmlNode nodeDayEnd = xmlDoc.SelectSingleNode("config/GetHistoryDayEnd");
                if (nodeDayEnd != null)
                {
                    nodeDayEnd.InnerText = this.dpEndDate.SelectedDate.ToString();
                }
                XmlNode nodeTimeEnd = xmlDoc.SelectSingleNode("config/GetHistoryTimeEnd");
                if (nodeTimeEnd != null)
                {
                    nodeTimeEnd.InnerText = this.tpEndTime.SelectedTime.ToString();
                }
                xmlDoc.Save("remember.xml");
            }
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnToNow_Click(object sender, RoutedEventArgs e)
        {
            this.dpEndDate.SelectedDate = System.DateTime.Now;
            this.tpEndTime.SelectedTime = System.DateTime.Now;
        }
    }
}
