using BrokenRailMonitorViaWiFi;
using BrokenRailServer.UserControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace BrokenRailServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly int MasterControlWidth = 26;
        private static readonly int RailWidth = 104;
        private int _packageCount = 0;
        //保存与客户相关的信息列表
        ArrayList friends = new ArrayList();
        //负责监听的套接字
        TcpListener listener;
        //只是是否启动了监听
        bool IsStart = false;
        private List<MasterControl> _masterControlList = new List<MasterControl>();
        private List<int> _sendTime = new List<int>();
        private List<Rail> _rail1List = new List<Rail>();
        private List<Rail> _rail2List = new List<Rail>();
        private ScrollViewerThumbnail _svtThumbnail;
        private List<int> _4GPointIndex = new List<int>();
        private List<int> _socketRegister = new List<int>();
        private bool _stopScroll = false;

        public int PackageCount
        {
            get
            {
                return _packageCount;
            }

            set
            {
                if (_packageCount != value)
                {
                    _packageCount = value;
                    OnPropertyChanged("PackageCount");
                }
            }
        }
        public List<MasterControl> MasterControlList
        {
            get
            {
                return _masterControlList;
            }

            set
            {
                _masterControlList = value;
            }
        }
        public List<int> SocketRegister
        {
            get
            {
                return _socketRegister;
            }

            set
            {
                _socketRegister = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            PackageCount = 0;
            this.lblPackageCount.Content = PackageCount.ToString();
            this.txtReceive.Text = string.Empty;
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtSend.Text.Trim() == "")
                {
                    AppendMethod("不能发送空字符串！");
                    txtSend.Focus();
                    return;
                }
                string strSend = this.txtSend.Text;
                foreach (TerminalAndClientUserControl item in this.stpIpAndPortContainer.Children)
                {
                    if (item.cbxSelected.IsChecked == true)
                    {
                        if (item.SocketImport == null)
                        {
                            MessageBox.Show(item.lblIpAndPort.Content + "网络未连接！");
                            continue;
                        }
                        SendData(item, strSend);
                    }
                }
                //if (comboBoxClient.SelectedIndex < 0)
                //{
                //    listBoxStatu.Items.Add("请在列表中选择发送对象！");
                //    return;
                //}
            }
            catch (Exception ee)
            {
                MessageBox.Show("发送异常：" + ee.Message);
            }
        }

        private void cbxSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (TerminalAndClientUserControl item in this.stpIpAndPortContainer.Children)
            {
                item.cbxSelected.IsChecked = true;
            }
        }

        private void cbxSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (TerminalAndClientUserControl item in this.stpIpAndPortContainer.Children)
            {
                item.cbxSelected.IsChecked = false;
            }
        }



        //在列表中写字符串的委托方法
        private void AppendMethod(string str)
        {
            txtReceive.Text += str;
            txtReceive.Text += "\r\n";
        }
        //向下拉列表中添加信息的委托方法
        private void AddMethod(TerminalAndClientUserControl frd)
        {
            lock (friends)
            {
                friends.Add(frd);
            }
            stpIpAndPortContainer.Children.Add(frd);
        }
        //从下拉列表中删除信息的委托方法
        private void RemoveMethod(TerminalAndClientUserControl frd)
        {
            int i = friends.IndexOf(frd);
            if (i != -1)
            {
                stpIpAndPortContainer.Children.RemoveAt(i);
                lock (friends)
                {
                    friends.Remove(frd);
                }
                frd.Dispose();
            }
        }

        private void clearFriends()
        {
            foreach (TerminalAndClientUserControl item in friends)
            {
                item.Dispose();
            }
            stpIpAndPortContainer.Children.Clear();
            friends.Clear();
        }

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //服务器已在其中监听，则返回
                if (IsStart)
                    return;
                //服务器启动侦听
                IPEndPoint localep = new IPEndPoint(IPAddress.Any, int.Parse(txtServerPort.Text));
                listener = new TcpListener(localep);
                listener.Start(100);
                IsStart = true;
                AppendMethod(string.Format(DateTime.Now.ToString("HH:mm:ss.fff") + "->服务器已经启动监听！端点为：{0}。", listener.LocalEndpoint.ToString()));
                //接受连接请求的异步调用
                AsyncCallback callback = new AsyncCallback(AcceptCallBack);
                listener.BeginAcceptSocket(callback, listener);
                this.btnStartListening.IsEnabled = false;
            }
            catch (Exception ee)
            {
                MessageBox.Show("开始监听异常：" + ee.Message);
            }
        }
        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                //完成异步接收连接请求的异步调用
                //将连接信息添加到列表和下拉列表中
                Socket handle = listener.EndAcceptSocket(ar);

                this.Dispatcher.Invoke(new Action(() =>
                {
                    TerminalAndClientUserControl frd = new TerminalAndClientUserControl(handle);
                    AddMethod(frd);

                    AsyncCallback callback;
                    //继续调用异步方法接收连接请求
                    if (IsStart)
                    {
                        callback = new AsyncCallback(AcceptCallBack);
                        listener.BeginAcceptSocket(callback, listener);
                    }
                    //开始在连接上进行异步的数据接收
                    frd.ClearBuffer();
                    callback = new AsyncCallback(ReceiveCallback);
                    frd.SocketImport.BeginReceive(frd.Rcvbuffer, 0, frd.Rcvbuffer.Length, SocketFlags.None, callback, frd);
                }));
            }
            catch
            {
                //在调用EndAcceptSocket方法时可能引发异常
                //套接字Listener被关闭，则设置为未启动侦听状态
                //MessageBox.Show(ee.Message);
                IsStart = false;
                this.Dispatcher.Invoke(new Action(() => { this.btnStartListening.IsEnabled = true; }));
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            TerminalAndClientUserControl frd = (TerminalAndClientUserControl)ar.AsyncState;
            try
            {
                if (frd != null && frd.SocketImport != null)
                {
                    int i = frd.SocketImport.EndReceive(ar);
                    if (i == 0)
                    {
                        this.Dispatcher.Invoke(new Action(() => { RemoveMethod(frd); }));
                        return;
                    }
                    else
                    {
                        string data = Encoding.UTF8.GetString(frd.Rcvbuffer, 0, i);
                        setAccessPointType(frd, data);
                        data = string.Format(DateTime.Now.ToString("HH:mm:ss.fff") + "->From[{0}]:{1}", frd.SocketImport.RemoteEndPoint.ToString(), data);
                        PackageCount++;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            AppendMethod(data);
                            ScrollControl();
                        }));
                        frd.ClearBuffer();
                        AsyncCallback callback = new AsyncCallback(ReceiveCallback);
                        frd.SocketImport.BeginReceive(frd.Rcvbuffer, 0, frd.Rcvbuffer.Length, SocketFlags.None, callback, frd);
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show("接收回调异常：" + ee.Message);
                this.Dispatcher.Invoke(new Action(() => { RemoveMethod(frd); }));
            }
        }

        private void setAccessPointType(TerminalAndClientUserControl frd, string data)
        {
            if (frd.ApType == AccessPointType.Default)
            {
                if (data.Length > 0)
                {
                    if (data.Substring(0, 1) == "C")
                    {
                        if (data.Length > 5 && data.Substring(0, 6) == "Client")
                        {
                            frd.ApType = AccessPointType.Terminal;
                        }
                    }
                    else if (data.Substring(0, 1) == "手")
                    {
                        if (data.Length > 1 && data.Substring(0, 2) == "手机")
                        {
                            frd.ApType = AccessPointType.AndroidClient;
                        }
                    }
                }
            }
        }

        private void SendData(TerminalAndClientUserControl frd, string data)
        {
            try
            {
                byte[] msg = Encoding.UTF8.GetBytes(data);
                AsyncCallback callback = new AsyncCallback(SendCallback);
                frd.SocketImport.BeginSend(msg, 0, msg.Length, SocketFlags.None, callback, frd);
                data = string.Format(DateTime.Now.ToString("HH:mm:ss.fff") + "->To[{0}]:{1}", frd.SocketImport.RemoteEndPoint.ToString(), data);
                this.Dispatcher.Invoke(new Action(() => { AppendMethod(data); }));
            }
            catch (Exception ee)
            {
                MessageBox.Show("发送数据异常：" + ee.Message);
                this.Dispatcher.Invoke(new Action(() => { RemoveMethod(frd); }));
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            TerminalAndClientUserControl frd = (TerminalAndClientUserControl)ar.AsyncState;
            try
            {
                frd.SocketImport.EndSend(ar);
            }
            catch
            {
                this.Dispatcher.Invoke(new Action(() => { RemoveMethod(frd); }));
            }
        }

        private void btnStopListening_Click(object sender, RoutedEventArgs e)
        {
            if (!IsStart)
                return;
            listener.Stop();
            IsStart = false;
            AppendMethod("已经结束了服务器的侦听！");
            clearFriends();
            this.btnStartListening.IsEnabled = true;
        }

        public void WaitingRingEnable()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.modernProgressRing.IsActive = true;
                this.gridMain.IsEnabled = false;
            }));
        }

        public void WaitingRingDisable()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.modernProgressRing.IsActive = false;
                this.gridMain.IsEnabled = true;
            }));
        }
        public void DecideDelayOrNot()
        {
            DateTime now = System.DateTime.Now;
            int totalSecondToNow = now.Hour * 3600 + now.Minute * 60 + now.Second;
            int timeIn75Second = totalSecondToNow % 75;

            if (FindIntInSendTime(timeIn75Second))
            {
                Thread.Sleep(2000);
                //this.dataShowUserCtrl.AddShowData("延时2秒发送指令！", DataLevel.Warning);
            }
        }
        public bool FindIntInSendTime(int destInt)
        {
            foreach (var item in _sendTime)
            {
                if (item == destInt)
                {
                    return true;
                }
            }
            return false;
        }


        private void miRefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            devicesInitial();
        }

        private void miGetAllRailInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void miGetOneSectionInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void miRealTimeConfig_Click(object sender, RoutedEventArgs e)
        {

        }

        private void miEraseFlash_Click(object sender, RoutedEventArgs e)
        {

        }

        private void miViewHistory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void devicesInitial()
        {
            try
            {
                this.MasterControlList.Clear();
                _4GPointIndex.Clear();
                _sendTime.Clear();
                string fileName = System.Environment.CurrentDirectory + @"\config.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                XmlNodeList xnList = xmlDoc.SelectSingleNode("Devices").ChildNodes;
                int nodeCount = xnList.Count;

                if (this.cvsRail1.Children.Count != 0 || this.cvsRail2.Children.Count != 0 || this.cvsDevices.Children.Count != 0)
                {
                    this.cvsRail1.Children.Clear();
                    this.cvsRail2.Children.Clear();
                    this.cvsDevices.Children.Clear();
                }

                int i = 0;
                int neighbourBigRemember = 0;
                foreach (XmlNode device in xnList)
                {
                    XmlNode terminalNoNode = device.SelectSingleNode("TerminalNo");
                    string innerTextTerminalNo = terminalNoNode.InnerText.Trim();
                    int terminalNo = Convert.ToInt32(innerTextTerminalNo);
                    MasterControl oneMasterControl = new MasterControl(this);
                    oneMasterControl.lblNumber.Content = terminalNo;
                    this.MasterControlList.Add(oneMasterControl);

                    //根据终端号计算发射占用无线串口的时机
                    int t = 4 + (terminalNo % 5) * 15;
                    if (!FindIntInSendTime(t))
                    {
                        _sendTime.Add(t);
                    }

                    XmlNode is4GNode = device.SelectSingleNode("Is4G");
                    string innerTextIs4G = is4GNode.InnerText.Trim();
                    bool is4G = Convert.ToBoolean(innerTextIs4G);
                    oneMasterControl.Is4G = is4G;
                    if (is4G)
                    {
                        _4GPointIndex.Add(this.MasterControlList.Count - 1);
                    }

                    Rail rail1 = new Rail(terminalNo);
                    Rail rail2 = new Rail(terminalNo);
                    this._rail1List.Add(rail1);
                    this._rail2List.Add(rail2);
                    this.cvsDevices.Children.Add(this.MasterControlList[this.MasterControlList.Count - 1]);
                    Canvas.SetLeft(this.MasterControlList[this.MasterControlList.Count - 1], (2 + RailWidth) * i);
                    if (i < nodeCount - 1)
                    {
                        this.cvsRail1.Children.Add(rail1);
                        Canvas.SetLeft(rail1, (2 + RailWidth) * i + MasterControlWidth / 2 + 1);

                        this.cvsRail2.Children.Add(rail2);
                        Canvas.SetLeft(rail2, (2 + RailWidth) * i + MasterControlWidth / 2 + 1);
                    }
                    XmlNode neighbourSmallNode = device.SelectSingleNode("NeighbourSmall");
                    string innerTextNeighbourSmall = neighbourSmallNode.InnerText.Trim();
                    int neighbourSmall = Convert.ToInt32(innerTextNeighbourSmall);
                    XmlNode isEndNode = device.SelectSingleNode("IsEnd");
                    string innerTextIsEnd = isEndNode.InnerText.Trim();
                    bool isEnd = Convert.ToBoolean(innerTextIsEnd);
                    this.MasterControlList[this.MasterControlList.Count - 1].IsEnd = isEnd;

                    //检查工程文档配置文件是否正确
                    if (i == 0)
                    {
                        if (neighbourSmall != 0)
                        {
                            MessageBox.Show("第一个终端的NeighbourSmall标签未设置为0");
                        }
                    }
                    else
                    {
                        if (MasterControlList[i - 1].TerminalNumber != neighbourSmall)
                        {
                            MessageBox.Show("终端" + terminalNo.ToString() + "的小相邻终端不匹配，请检查配置文件");
                        }
                        if (oneMasterControl.TerminalNumber != neighbourBigRemember)
                        {
                            MessageBox.Show("终端" + MasterControlList[i - 1].TerminalNumber.ToString() + "的大相邻终端不匹配，请检查配置文件");
                        }
                    }
                    oneMasterControl.NeighbourSmall = neighbourSmall;
                    if (i >= 1)
                    {
                        MasterControlList[i - 1].NeighbourBig = neighbourBigRemember;
                    }
                    XmlNode neighbourBigNode = device.SelectSingleNode("NeighbourBig");
                    string innerTextNeighbourBig = neighbourBigNode.InnerText.Trim();
                    if (!isEnd)
                    {
                        int neighbourBig = Convert.ToInt32(innerTextNeighbourBig);
                        neighbourBigRemember = neighbourBig;
                        oneMasterControl.NeighbourBig = neighbourBig;
                    }

                    if (isEnd)
                    {
                        oneMasterControl.NeighbourBig = 0xff;
                        if (!(innerTextNeighbourBig == "ff" || innerTextNeighbourBig == "FF"))
                        {
                            MessageBox.Show("最末终端" + terminalNo.ToString() + "的大相邻终端不是ff，请检查配置文件");
                        }
                    }
                    i++;
                }
                this.cvsRail1.Width = (2 + RailWidth) * nodeCount;

                this._svtThumbnail = new ScrollViewerThumbnail(nodeCount - 1);
                this._svtThumbnail.ScrollViewerTotalWidth = (2 + RailWidth) * nodeCount;
                this._svtThumbnail.MouseClickedEvent += _svtThumbnail_MouseClickedEvent;
                this.gridMain.Children.Add(_svtThumbnail);
                this._svtThumbnail.SetValue(Grid.RowProperty, 1);
                this._svtThumbnail.SetValue(VerticalAlignmentProperty, VerticalAlignment.Bottom);
                this._svtThumbnail.SetValue(MarginProperty, new Thickness(20, 0, 20, 0));
                //重新刷新之后需要清空Socket注册。
                //SocketRegister.Clear();
            }
            catch (Exception ee)
            {
                MessageBox.Show("设备初始化异常：" + ee.Message);
            }
        }

        private void _svtThumbnail_MouseClickedEvent()
        {
            double offset = this._svtThumbnail.XPosition / this._svtThumbnail.CvsFollowMouseWidth * this._svtThumbnail.ScrollViewerTotalWidth;
            this.svContainer.ScrollToHorizontalOffset(offset);
        }
        /// <summary>
        /// 根据终端号寻找终端所在List的索引。
        /// </summary>
        /// <param name="terminalNo">终端号</param>
        /// <returns>如果找到返回索引，否则返回-1</returns>
        public int FindMasterControlIndex(int terminalNo)
        {
            int i = 0;
            foreach (var item in this.MasterControlList)
            {
                if (item.TerminalNumber == terminalNo)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            devicesInitial();
        }

        private void ScrollControl()
        {
            if (!_stopScroll)
            {
                //自动滚动到底部
                txtReceive.ScrollToEnd();
            }
        }
        private void tbtnPin_Checked(object sender, RoutedEventArgs e)
        {
            _stopScroll = true;
        }

        private void tbtnPin_Unchecked(object sender, RoutedEventArgs e)
        {
            _stopScroll = false;
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            ToggleButton b = sender as ToggleButton;
            if (b != null)
            {
                b.BorderThickness = new Thickness(0);
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            ToggleButton b = sender as ToggleButton;
            if (b != null)
            {
                b.BorderThickness = new Thickness(1);
            }
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
