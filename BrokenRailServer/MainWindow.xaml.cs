using BrokenRailMonitorViaWiFi;
using BrokenRailServer.SendReceiveFile;
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
        private readonly int _fileReceivePort = 18527;
        private int _packageCount = 0;
        //保存与客户相关的信息列表
        List<TerminalAndClientUserControl> friends = new List<TerminalAndClientUserControl>();
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
        private Stack _clientIDStack = new Stack();
        private object lockObject = new object();

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
            try
            {
                InitializeComponent();
                this.DataContext = this;
                for (int i = 100; i > 0; i--)
                {
                    _clientIDStack.Push(i);
                }
            }
            catch (Exception ee)
            {
                AppendMessage("主窗口构造异常：" + ee.Message, DataLevel.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            PackageCount = 0;
            this.lblPackageCount.Content = PackageCount.ToString();
            this.dataShowUserCtrl.ClearContainer();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtSend.Text.Trim() == "")
                {
                    AppendMessage("不能发送空字符串！", DataLevel.Error);
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
                            AppendMessage(item.lblIpAndPort.Content + "网络未连接！", DataLevel.Error);
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
                AppendMessage("发送异常：" + ee.Message, DataLevel.Error);
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
        public void AppendMessage(string str, DataLevel level)
        {
            dataShowUserCtrl.AddShowData(str, level);
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
                if (!_clientIDStack.Contains(frd.ClientID) && frd.ClientID != 0)
                {
                    if (frd.ApType == AccessPointType.PCClient || frd.ApType == AccessPointType.AndroidClient)
                    {
                        _clientIDStack.Push(frd.ClientID);
                    }
                }
                stpIpAndPortContainer.Children.RemoveAt(i);
                lock (friends)
                {
                    friends.Remove(frd);
                }
                frd.Dispose();
                disregistSocketAndOfflineTerminal(frd);
                AppendOfflineMsg(frd);
            }
        }

        private void clearFriends()
        {
            while (friends.Count != 0)
            {
                RemoveMethod(friends[0] as TerminalAndClientUserControl);
            }
            //item.Dispose();
            //stpIpAndPortContainer.Children.Clear();
            friends.Clear();
        }

        private void clearTerminal()
        {
            for (int i = 0; i < friends.Count; i++)
            {
                TerminalAndClientUserControl tOrC = friends[i] as TerminalAndClientUserControl;
                if (tOrC.ApType == AccessPointType.Terminal)
                {
                    RemoveMethod(tOrC);
                    i--;
                }
            }
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
                AppendMessage(string.Format("服务器已经启动监听！端点为：{0}。", listener.LocalEndpoint.ToString()), DataLevel.Normal);
                //接受连接请求的异步调用
                AsyncCallback callback = new AsyncCallback(AcceptCallBack);
                listener.BeginAcceptSocket(callback, listener);
                this.btnStartListening.IsEnabled = false;
            }
            catch (Exception ee)
            {
                AppendMessage("开始监听异常：" + ee.Message, DataLevel.Error);
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
                //AppendMessage(ee.Message);
                IsStart = false;
                this.Dispatcher.Invoke(new Action(() => { this.btnStartListening.IsEnabled = true; }));
            }
        }
        bool canMerge = false;
        private void ReceiveCallback(IAsyncResult ar)
        {
            TerminalAndClientUserControl frd = (TerminalAndClientUserControl)ar.AsyncState;
            try
            {
                if (frd != null && frd.SocketImport != null)
                {
                    int length = frd.SocketImport.EndReceive(ar);

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        AppendMessage("处理前长度" + length, DataLevel.Normal);
                    }));
                    if (length == 0)
                    {
                        this.Dispatcher.Invoke(new Action(() => { RemoveMethod(frd); }));
                        return;
                    }
                    else
                    {
                        //this.Dispatcher.Invoke(new Action(() =>
                        //{
                        //    string originDataDebug = bytesToHexString(frd.Rcvbuffer, length);
                        //    string dataDebug = string.Format("From[{0}]:{1}", frd.ToString(), originDataDebug);
                        //    AppendMessage(dataDebug, DataLevel.Default);
                        //}));

                        if (frd.Recognize1024(length))
                        {
                            frd.RememberRecv(length);
                            canMerge = true;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage("From[" + frd.ToString() + "]:累积收到1024字节", DataLevel.Warning);
                            }));
                            goto skipHandle;
                        }
                        if (canMerge)
                        {
                            frd.MergeBuffer(length);
                            canMerge = false;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage("From[" + frd.ToString() + "]:发生合并", DataLevel.Warning);
                            }));
                            length = frd.Rcvbuffer.Length;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage("合并后长度" + length, DataLevel.Default);
                            }));
                        }

                        lock (lockObject)
                        {
                            frd.HandleDuanBao(ref length);
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage("处理断包后长度" + length, DataLevel.Error);
                            }));

                            handleNianBao: bool nianBao = frd.HandleNianBao(ref length);

                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage("处理粘包后长度" + length, DataLevel.Error);
                            }));

                            handleData:
                            string originData = preAnalyseData(frd.Rcvbuffer, length);
                            setTerminalNoAndRegistSocket(frd, originData);
                            string data = string.Format("From[{0}]:{1}", frd.ToString(), originData);
                            PackageCount++;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage(data, DataLevel.Default);
                                handleData(frd, length);
                                setAccessPointTypeAndClientID(frd, originData);
                                transmitData(frd, length);
                                setLabelPackageCountColor();
                            }));

                            if (nianBao)
                            {
                                if (frd.CopyArray4Circle(ref length))
                                {
                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        AppendMessage("From[" + frd.ToString() + "]:粘包拆分", DataLevel.Warning);
                                        AppendMessage("未处理长度" + length, DataLevel.Normal);
                                        string unhandle = bytesToHexString(frd.PackageUnhandled, length);
                                        AppendMessage("未处理数据" + unhandle, DataLevel.Default);
                                    }));
                                    if (length >= 1018)//1018是一个历史数据返回包的长度
                                    {
                                        goto handleNianBao;
                                    }
                                    else if (length > 1 && frd.PackageUnhandled[0] == 0x66 && frd.PackageUnhandled[1] == 0xcc)
                                    {
                                        goto handleNianBao;
                                    }
                                    if (length > 1 && frd.PackageUnhandled[0] == 0x23 && frd.PackageUnhandled[1] == 0x23)
                                    {//最后一个心跳包。
                                        nianBao = false;
                                        goto handleData;
                                    }
                                }
                                if (length == 10 && frd.Rcvbuffer[0] == 0x66 && frd.Rcvbuffer[1] == 0xcc)
                                {
                                    goto handleNianBao;
                                }
                            }
                        }

                        skipHandle: frd.ClearBuffer();
                        AsyncCallback callback = new AsyncCallback(ReceiveCallback);
                        frd.SocketImport.BeginReceive(frd.Rcvbuffer, 0, frd.Rcvbuffer.Length, SocketFlags.None, callback, frd);
                    }
                }
            }
            catch (Exception ee)
            {

                this.Dispatcher.Invoke(new Action(() =>
                {
                    RemoveMethod(frd);
                    AppendMessage("接收回调异常：" + ee.Message, DataLevel.Error);
                }));
            }
        }

        private void transmitData(TerminalAndClientUserControl sourceFriend, int length)
        {
            Thread.Sleep(10);
            if (sourceFriend.ApType == AccessPointType.Terminal)
            {
                if (length > 1)
                {
                    if (sourceFriend.Rcvbuffer[0] != 0x66 || sourceFriend.Rcvbuffer[1] != 0xcc)
                    {
                        if (sourceFriend.Rcvbuffer[0] == 0x23 && sourceFriend.Rcvbuffer[1] == 0x23)
                        {
                            //0x23是#的ASCII码。
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                foreach (var item in decideDestFriendsUp(sourceFriend, length))
                {
                    TerminalAndClientUserControl destFriend = item as TerminalAndClientUserControl;
                    if (destFriend != null && (destFriend.ApType == AccessPointType.PCClient || destFriend.ApType == AccessPointType.AndroidClient))
                    {
                        byte[] data = new byte[length];
                        Buffer.BlockCopy(sourceFriend.Rcvbuffer, 0, data, 0, length);
                        SendData(destFriend, data);
                    }
                }
            }
            else if (sourceFriend.ApType == AccessPointType.PCClient || sourceFriend.ApType == AccessPointType.AndroidClient)
            {
                if (length > 1)
                {
                    if (sourceFriend.Rcvbuffer[0] != 0x55 || sourceFriend.Rcvbuffer[1] != 0xaa)
                    {
                        return;
                    }
                }
                foreach (var item in decideDestFriendsDown(sourceFriend, length))
                {
                    TerminalAndClientUserControl destFriend = item as TerminalAndClientUserControl;
                    if (destFriend != null && destFriend.ApType == AccessPointType.Terminal)
                    {
                        byte[] data = new byte[length];
                        Buffer.BlockCopy(sourceFriend.Rcvbuffer, 0, data, 0, length);
                        SendData(destFriend, data);
                    }
                }
            }
        }

        private List<TerminalAndClientUserControl> decideDestFriendsDown(TerminalAndClientUserControl sourceFriend, int length)
        {
            List<TerminalAndClientUserControl> result = new List<TerminalAndClientUserControl>();
            if (length > 6)
            {
                switch (sourceFriend.Rcvbuffer[5])
                {
                    case (byte)CommandType.ReadPointInfo:
                        {
                            int destTerminalNo = sourceFriend.Rcvbuffer[4];
                            int indexDest = FindMasterControlIndex(destTerminalNo);

                            for (int i = indexDest; i >= 0; i--)
                            {
                                if (MasterControlList[i].Is4G && MasterControlList[i].IsOnline)
                                {
                                    result.Add(MasterControlList[i].Terminal);
                                    return result;
                                }
                            }
                        }
                        break;
                    case (byte)CommandType.RealTimeConfig:
                        {
                            int destTerminalNo = sourceFriend.Rcvbuffer[6];
                            int indexDest = FindMasterControlIndex(destTerminalNo);

                            for (int i = indexDest; i >= 0; i--)
                            {
                                if (MasterControlList[i].Is4G && MasterControlList[i].IsOnline)
                                {
                                    result.Add(MasterControlList[i].Terminal);
                                    return result;
                                }
                            }
                        }
                        break;
                    case (byte)CommandType.RequestConfig:
                    case (byte)CommandType.UploadConfig:
                        return result;
                    default:
                        break;
                }
            }
            return friends;
        }
        private List<TerminalAndClientUserControl> decideDestFriendsUp(TerminalAndClientUserControl sourceFriend, int length)
        {
            List<TerminalAndClientUserControl> result = new List<TerminalAndClientUserControl>();
            if (length > 7 && sourceFriend.Rcvbuffer[0] == 0x66 && sourceFriend.Rcvbuffer[1] == 0xcc)
            {
                switch (sourceFriend.Rcvbuffer[6])
                {
                    case (byte)CommandType.GetOneSectionInfo:
                        {

                        }
                        break;
                    default:
                        break;
                }
            }
            return friends;
        }

        private void handleData(TerminalAndClientUserControl frd, int length)
        {
            try
            {
                byte[] data = frd.Rcvbuffer;
                if (data.Length > 1)
                {
                    if (data[0] == 0x55 && data[1] == 0xaa)
                    {
                        int checksum = 0;
                        for (int i = 0; i < length - 1; i++)
                        {
                            checksum += data[i];
                        }
                        if ((checksum & 0xff) != data[length - 1])
                        {
                            AppendMessage("校验和出错！", DataLevel.Error);
                            return;
                        }

                        if (data[4] == 0xff)
                        {
                            switch (data[5])
                            {
                                case (byte)CommandType.RequestConfig:
                                    {
                                        AppendMessage(data[3] + "号设备下载配置文件", DataLevel.Default);
                                        Thread sendFileThread = new Thread(sendConfigFile);
                                        sendFileThread.Start();
                                    }
                                    break;
                                case (byte)CommandType.UploadConfig:
                                    {
                                        AppendMessage(data[3] + "号设备上传配置文件", DataLevel.Default);
                                        Thread recvFileThread = new Thread(receiveConfigFile);
                                        recvFileThread.Start();
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else if (data[0] == 0x66 && data[1] == 0xcc)
                    {
                        if (checkDataChecksum(data, length))
                        {
                            byte[] actualReceive = getActualReceive(frd, length);
                            switch (data[6])
                            {
                                case (byte)CommandType.GetOneSectionInfo:
                                    handleOneSectionInfo(actualReceive);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                AppendMessage("处理数据异常：" + ee.Message, DataLevel.Error);
            }
        }

        private byte[] getActualReceive(TerminalAndClientUserControl frd, int length)
        {
            byte[] actualReceive = new byte[length];
            for (int i = 0; i < length; i++)
            {
                actualReceive[i] = frd.Rcvbuffer[i];
            }
            return actualReceive;
        }

        private void handleOneSectionInfo(byte[] actualReceive)
        {
            if (this._svtThumbnail == null)
            {
                AppendMessage("设备及铁轨未初始化！", DataLevel.Error);
                return;
            }

            this.WaitingRingDisable();
            //this.WaitReceiveTimer.Stop();

            int length = (actualReceive[2] << 8) + actualReceive[3];
            byte[] bytesOnOffContent = new byte[length - 9];
            byte[] bytesTemp = new byte[length - 9];
            for (int i = 7; i < length - 2; i++)
            {
                bytesOnOffContent[i - 7] = actualReceive[i];
            }
            for (int i = 0; i < bytesOnOffContent.Length; i += 3)
            {
                bytesTemp[i] = bytesOnOffContent[bytesOnOffContent.Length - i - 3];
                bytesTemp[i + 1] = bytesOnOffContent[bytesOnOffContent.Length - i - 2];
                bytesTemp[i + 2] = bytesOnOffContent[bytesOnOffContent.Length - i - 1];
            }
            bytesTemp.CopyTo(bytesOnOffContent, 0);
            int contentLength = bytesOnOffContent.Length;
            if (contentLength % 3 == 0)
            {
                if (contentLength == 3)
                {
                    //如果只有一个终端的数据就不存在两个终端数据冲突的情况。
                    int index = FindMasterControlIndex(bytesOnOffContent[0]);
                    //if (_terminalsReceiveFlag != null)
                    //{
                    //    _terminalsReceiveFlag[bytesOnOffContent[0]] = true;
                    //}
                    //检查1号铁轨
                    if (index != 0)
                    {
                        //第一个终端没有左边的铁轨
                        int onOffRail1Left = bytesOnOffContent[1] & 0x0f;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            setRail1State(index - 1, onOffRail1Left);
                        }));
                    }
                    if (index != MasterControlList.Count - 1)
                    {
                        //最后一个终端没有右边的铁轨
                        int onOffRail1Right = (bytesOnOffContent[1] & 0xf0) >> 4;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            setRail1State(index, onOffRail1Right);
                        }));
                    }

                    //检查2号铁轨
                    if (index != 0)
                    {
                        //第一个终端没有左边的铁轨
                        int onOffRail2Left = bytesOnOffContent[2] & 0x0f;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            setRail2State(index - 1, onOffRail2Left);
                        }));
                    }
                    if (index != MasterControlList.Count - 1)
                    {
                        //最后一个终端没有右边的铁轨
                        int onOffRail2Right = (bytesOnOffContent[2] & 0xf0) >> 4;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            setRail2State(index, onOffRail2Right);
                        }));
                    }
                }
                else
                {
                    //如果有多个终端的数据，需要处理冲突。
                    for (int i = 0; i < contentLength - 3; i++, i++, i++)
                    {
                        int index = FindMasterControlIndex(bytesOnOffContent[i]);
                        //if (_terminalsReceiveFlag != null)
                        //{
                        //    _terminalsReceiveFlag[bytesOnOffContent[i]] = true;
                        //}
                        //检查1号铁轨
                        if (i == 0 && index != 0)
                        {
                            //第一个终端没有左边的铁轨
                            int onOffRail1Left = bytesOnOffContent[1] & 0x0f;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                setRail1State(index - 1, onOffRail1Left);
                            }));
                        }
                        else
                        {
                            if (((bytesOnOffContent[i + 1] & 0xf0) >> 4) == (bytesOnOffContent[i + 4] & 0x0f))
                            {
                                //不冲突
                                int onOff = (bytesOnOffContent[i + 1] & 0xf0) >> 4;
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    setRail1State(index, onOff);
                                }));
                            }
                            else if (((bytesOnOffContent[i + 1] & 0xf0) >> 4) == 9 || (bytesOnOffContent[i + 4] & 0x0f) == 9)
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    setRail1State(index, 9);
                                }));
                            }
                            else
                            {
                                //冲突
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    this._svtThumbnail.Different(new int[1] { index }, 1);
                                    Rail rail = this.cvsRail1.Children[index] as Rail;
                                    rail.Different();

                                    int tNo = MasterControlList[index].TerminalNumber;
                                    int tNextNo = MasterControlList[index + 1].TerminalNumber;
                                    string errorTerminal = string.Empty;
                                    if ((bytesOnOffContent[i + 1] & 0xf0) == 0x70)
                                    {
                                        errorTerminal = tNo.ToString() + "号终端接收异常";
                                    }
                                    else if ((bytesOnOffContent[i + 4] & 0x0f) == 0x07)
                                    {
                                        errorTerminal = tNextNo.ToString() + "号终端接收异常";
                                    }
                                    this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨通断信息矛盾！" + errorTerminal +
                                        "，请检查", DataLevel.Warning);
                                }));
                            }
                        }
                        if (i == (contentLength - 6))
                        {
                            int indexLastTerminal = FindMasterControlIndex(bytesOnOffContent[i + 3]);
                            //if (_terminalsReceiveFlag != null)
                            //{
                            //    _terminalsReceiveFlag[bytesOnOffContent[i + 3]] = true;
                            //}
                            if (indexLastTerminal != MasterControlList.Count - 1)
                            {
                                //最后一个终端没有右边的铁轨
                                int onOffRail1Right = (bytesOnOffContent[i + 4] & 0xf0) >> 4;
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    setRail1State(indexLastTerminal, onOffRail1Right);
                                }));
                            }
                        }

                        //检查2号铁轨
                        if (i == 0 && index != 0)
                        {
                            //第一个终端没有左边的铁轨
                            int onOffRail2Left = bytesOnOffContent[2] & 0x0f;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                setRail2State(index - 1, onOffRail2Left);
                            }));
                        }
                        else
                        {
                            if (((bytesOnOffContent[i + 2] & 0xf0) >> 4) == (bytesOnOffContent[i + 5] & 0x0f))
                            {
                                //不冲突
                                int onOff = (bytesOnOffContent[i + 2] & 0xf0) >> 4;
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    setRail2State(index, onOff);
                                }));
                            }
                            else if (((bytesOnOffContent[i + 2] & 0xf0) >> 4) == 9 || (bytesOnOffContent[i + 5] & 0x0f) == 9)
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    setRail2State(index, 9);
                                }));
                            }
                            else
                            {
                                //冲突
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    this._svtThumbnail.Different(new int[1] { index }, 2);
                                    Rail rail = this.cvsRail2.Children[index] as Rail;
                                    rail.Different();

                                    int tNo = MasterControlList[index].TerminalNumber;
                                    int tNextNo = MasterControlList[index + 1].TerminalNumber;
                                    string errorTerminal = string.Empty;
                                    if ((bytesOnOffContent[i + 2] & 0xf0) == 0x70)
                                    {
                                        errorTerminal = tNo.ToString() + "号终端接收异常";
                                    }
                                    else if ((bytesOnOffContent[i + 5] & 0x0f) == 0x07)
                                    {
                                        errorTerminal = tNextNo.ToString() + "号终端接收异常";
                                    }
                                    this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨通断信息矛盾！" + errorTerminal +
                                        "，请检查", DataLevel.Warning);
                                }));
                            }
                        }
                        if (i == (contentLength - 6))
                        {
                            int indexLastTerminal = FindMasterControlIndex(bytesOnOffContent[i + 3]);
                            if (indexLastTerminal != MasterControlList.Count - 1)
                            {
                                //最后一个终端没有右边的铁轨
                                int onOffRail2Right = (bytesOnOffContent[i + 5] & 0xf0) >> 4;
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    setRail2State(indexLastTerminal, onOffRail2Right);
                                }));
                            }
                        }
                    }
                }

                int rail1NormalCount = 0;
                int rail2NormalCount = 0;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < this.cvsRail1.Children.Count; i++)
                    {
                        var rail1 = this.cvsRail1.Children[i] as Rail;
                        if (rail1.RailState == RailStates.IsNormal)
                        {
                            rail1NormalCount++;
                        }
                        if (rail1NormalCount == this.cvsRail1.Children.Count)
                        {
                            this.dataShowUserCtrl.AddShowData("1号铁轨正常", DataLevel.Normal);
                        }
                        var rail2 = this.cvsRail2.Children[i] as Rail;
                        if (rail2.RailState == RailStates.IsNormal)
                        {
                            rail2NormalCount++;
                        }
                        if (rail2NormalCount == this.cvsRail2.Children.Count)
                        {
                            this.dataShowUserCtrl.AddShowData("2号铁轨正常", DataLevel.Normal);
                        }
                    }
                }));
            }
            else
            {
                AppendMessage("发送数据内容的长度错误，应该是3的倍数", DataLevel.Error);
            }
        }


        private void setRail1State(int index, int onOff)
        {
            if (onOff == 0)
            {//通的
                this._svtThumbnail.Normal(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.Normal();
            }
            else if (onOff == 7)
            {//断的
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨断开！", DataLevel.Error);
                this._svtThumbnail.Error(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.Error();
            }
            else if (onOff == 9)
            {//超时
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨超时！", DataLevel.Timeout);
                this._svtThumbnail.Timeout(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.Timeout();
            }
            else if (onOff == 0x0a)
            {//持续干扰
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨持续干扰！", DataLevel.ContinuousInterference);
                this._svtThumbnail.ContinuousInterference(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.ContinuousInterference();
            }
            else
            {
                AppendMessage("收到未定义数据！", DataLevel.Error);
            }
        }

        private void setRail2State(int index, int onOff)
        {
            if (onOff == 0)
            {//通的
                this._svtThumbnail.Normal(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.Normal();
            }
            else if (onOff == 7)
            {//断的
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨断开！", DataLevel.Error);
                this._svtThumbnail.Error(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.Error();
            }
            else if (onOff == 9)
            {//超时
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨超时！", DataLevel.Timeout);
                this._svtThumbnail.Timeout(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.Timeout();
            }
            else if (onOff == 0x0a)
            {//持续干扰
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨持续干扰！", DataLevel.ContinuousInterference);
                this._svtThumbnail.ContinuousInterference(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.ContinuousInterference();
            }
            else
            {
                AppendMessage("收到未定义数据！", DataLevel.Error);
            }
        }

        /// <summary>
        /// 检查数据部分的校验和
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="length">有效数据的长度</param>
        /// <returns>返回true，校验和没问题。否则校验和出错</returns>
        private bool checkDataChecksum(byte[] data, int length)
        {
            int checksum = 0;
            for (int i = 0; i < length - 2; i++)
            {
                checksum += data[i];
            }
            int sumHigh;
            int sumLow;
            sumHigh = (checksum & 0xff00) >> 8;
            sumLow = checksum & 0xff;
            if (sumHigh != data[length - 2] || sumLow != data[length - 1])
            {
                AppendMessage("校验和出错！", DataLevel.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void receiveConfigFile()
        {
            try
            {
                //获取远程客户端的位置  
                //IPAddress ip = frd.ClientAddress;
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                //使用新端口,获得远程用于接收文件的端口  
                IPEndPoint endPoint = new IPEndPoint(ip, _fileReceivePort);
                //连接到远程客户端  
                TcpListener fileListener = new TcpListener(endPoint);
                fileListener.Start(10);
                TcpClient fileClient = fileListener.AcceptTcpClient();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    FileServer server = new FileServer(fileClient, AppendMessage);
                    server.FreshDevices += devicesInitial;
                    server.Listener = fileListener;
                    server.BeginRead();
                }));
            }
            catch (Exception ee)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    AppendMessage("接收配置文件异常：" + ee.Message, DataLevel.Error);
                }));
            }
        }

        private void sendConfigFile()
        {
            try
            {
                //获取远程客户端的位置  
                //IPAddress ip = frd.ClientAddress;
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                //使用新端口,获得远程用于接收文件的端口  
                IPEndPoint endPoint = new IPEndPoint(ip, _fileReceivePort);
                //连接到远程客户端  
                TcpListener fileListener = new TcpListener(endPoint);
                fileListener.Start(10);
                TcpClient fileClient = fileListener.AcceptTcpClient();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    FileServer server = new FileServer(fileClient, AppendMessage);
                    server.Listener = fileListener;
                    server.SendFile(AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
                }));
            }
            catch (Exception ee)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    AppendMessage("发送配置文件异常：" + ee.Message, DataLevel.Error);
                }));
            }
        }

        private string preAnalyseData(byte[] data, int length)
        {
            if (data.Length > 1)
            {
                if ((data[0] == 0x55 && data[1] == 0xaa) || (data[0] == 0x66 && data[1] == 0xcc))
                {
                    return bytesToHexString(data, length);
                }
                else if ((data[0] == Encoding.UTF8.GetBytes("C")[0] && data[1] == Encoding.UTF8.GetBytes("l")[0])
                    || (data[0] == Encoding.UTF8.GetBytes("电")[0] && data[1] == Encoding.UTF8.GetBytes("电")[1])
                    || (data[0] == Encoding.UTF8.GetBytes("手")[0] && data[1] == Encoding.UTF8.GetBytes("手")[1])
                    || (data[0] == Encoding.UTF8.GetBytes("#")[0] && data[1] == Encoding.UTF8.GetBytes("#")[0]))
                {
                    return Encoding.UTF8.GetString(data, 0, length);
                }
                else
                {
                    return bytesToHexString(data, length);
                }
            }
            return null;
        }

        private void setAccessPointTypeAndClientID(TerminalAndClientUserControl frd, string data)
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
                        int indexOfMaoHao = data.IndexOf(':');
                        frd.ClientAddress = IPAddress.Parse(data.Substring(2, indexOfMaoHao - 2));
                    }
                    else if (data.Substring(0, 1) == "电")
                    {
                        if (data.Length > 1 && data.Substring(0, 2) == "电脑")
                        {
                            frd.ApType = AccessPointType.PCClient;
                        }
                    }

                    if (frd.ApType == AccessPointType.Terminal)
                    {

                    }
                    else if (frd.ApType == AccessPointType.AndroidClient || frd.ApType == AccessPointType.PCClient)
                    {
                        if (_clientIDStack.Count > 0)
                        {
                            frd.ClientID = (int)_clientIDStack.Pop();
                            byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)frd.ClientID, (byte)CommandType.AssignClientID, new byte[0]);
                            SendData(frd, sendData);
                            AppendOnlineMsg(frd);
                        }
                        else
                        {
                            AppendMessage("100个用户号已分配完毕。", DataLevel.Error);
                        }
                    }
                }
            }
        }

        private void setTerminalNoAndRegistSocket(TerminalAndClientUserControl frd, string data)
        {
            if (data != null && data.Length > 5)
            {
                string strReceiveFirst3Letter = data.Substring(0, 3);
                if (strReceiveFirst3Letter == "###")
                {
                    //处理心跳包
                    //根据心跳包里面包含的终端号添加4G点中的socket。
                    string strTerminalNo = data.Substring(3, 3);
                    int intTerminalNo = Convert.ToInt32(strTerminalNo);

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        frd.ClientID = intTerminalNo;
                        removeRepeatTerminal(frd, intTerminalNo);
                        registSocketAndOnlineTerminal(frd, intTerminalNo);
                    }));
                }
            }
        }

        private void registSocketAndOnlineTerminal(TerminalAndClientUserControl frd, int intTerminalNo)
        {
            foreach (var item in MasterControlList)
            {
                if (item.TerminalNumber == intTerminalNo)
                {
                    if (!item.Is4G)
                    {
                        AppendMessage("心跳包中包含的终端号" + intTerminalNo.ToString() + "所示终端不是4G点，\r\n请检查心跳数据内容配置或者config文档！", DataLevel.Error);
                        return;
                    }
                    else if (!socketIsRegisted(intTerminalNo))
                    {
                        //socket已经导入，注册socket。
                        item.Terminal = frd;
                        SocketRegister.Add(intTerminalNo);
                        AppendMessage(intTerminalNo.ToString() + "号终端4G点Socket注册", DataLevel.Normal);
                    }
                    item.Online();
                    frd.TerminalOnline();
                }
            }
        }

        private bool socketIsRegisted(int intTerminalNo)
        {
            for (int i = 0; i < SocketRegister.Count; i++)
            {
                if (SocketRegister[i] == intTerminalNo)
                {
                    //找到已经注册的终端就跳出循环，不再找了，也不进行Socket赋值。
                    return true;
                }
                else if (i == SocketRegister.Count - 1)
                {
                    return false;
                }
            }
            return false;
        }

        private void setLabelPackageCountColor()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.lblPackageCount.Content = PackageCount.ToString();
                bool isWhite = (this.elpIndicator.Fill as SolidColorBrush).Color.Equals(Colors.White);
                bool isGreen = (this.elpIndicator.Fill as SolidColorBrush).Color.Equals(Colors.Green);
                if (isGreen)
                {
                    this.elpIndicator.Fill = new SolidColorBrush(Colors.White);
                }
                else if (isWhite)
                {
                    this.elpIndicator.Fill = new SolidColorBrush(Colors.Green);
                }
            }));
        }

        private void AppendOfflineMsg(TerminalAndClientUserControl frd)
        {
            string header = GetAccessPointTypeString(frd.ApType);
            string msg = header + frd.ClientID + ":" + frd.IpAndPort + "下线";
            AppendMessage(msg, DataLevel.Error);
        }

        private void AppendOnlineMsg(TerminalAndClientUserControl frd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("为");
            sb.Append(GetAccessPointTypeString(frd.ApType));
            sb.Append("分配用户ID：");
            sb.Append(frd.ClientID);
            AppendMessage(sb.ToString(), DataLevel.Normal);
        }

        private void disregistSocketAndOfflineTerminal(TerminalAndClientUserControl frd)
        {
            if (frd.ApType == AccessPointType.Terminal && socketIsRegisted(frd.ClientID))
            {
                if (SocketRegister.Contains(frd.ClientID))
                {
                    SocketRegister.Remove(frd.ClientID);
                }
                int index = FindMasterControlIndex(frd.ClientID);
                if (index != -1)
                {
                    MasterControlList[index].Offline();
                }
                frd.TerminalOffline();
            }
        }

        private void removeRepeatTerminal(TerminalAndClientUserControl frd, int terminalNo)
        {
            for (int i = 0; i < friends.Count; i++)
            {
                TerminalAndClientUserControl terminal = friends[i] as TerminalAndClientUserControl;
                if (terminal != null && terminal.ApType == AccessPointType.Terminal)
                {
                    if (terminal.ClientID == terminalNo && terminal.IpAndPort != frd.IpAndPort)
                    {
                        RemoveMethod(terminal);
                        break;
                    }
                }
            }
        }

        public string GetAccessPointTypeString(AccessPointType apType)
        {
            switch (apType)
            {
                case AccessPointType.Default:
                    return "未赋值";
                case AccessPointType.Terminal:
                    return "终端";
                case AccessPointType.PCClient:
                    return "电脑";
                case AccessPointType.AndroidClient:
                    return "手机";
                default:
                    return "未赋值";
            }
        }
        private void SendData(TerminalAndClientUserControl frd, string data)
        {
            try
            {
                byte[] msg = Encoding.UTF8.GetBytes(data);
                AsyncCallback callback = new AsyncCallback(SendCallback);
                frd.SocketImport.BeginSend(msg, 0, msg.Length, SocketFlags.None, callback, frd);
                data = string.Format("To[{0}]:{1}", frd.ToString(), data);
                this.Dispatcher.Invoke(new Action(() => { AppendMessage(data, DataLevel.Default); }));
            }
            catch (Exception ee)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    RemoveMethod(frd);
                    AppendMessage("发送数据异常：" + ee.Message, DataLevel.Error);
                }));
            }
        }

        private void SendData(TerminalAndClientUserControl frd, byte[] data)
        {
            try
            {
                AsyncCallback callback = new AsyncCallback(SendCallback);
                frd.SocketImport.BeginSend(data, 0, data.Length, SocketFlags.None, callback, frd);
                string msg = string.Format("To[{0}]:{1}", frd.ToString(), preAnalyseData(data, data.Length));
                this.Dispatcher.Invoke(new Action(() => { AppendMessage(msg, DataLevel.Default); }));
            }
            catch (Exception ee)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    RemoveMethod(frd);
                    AppendMessage("发送数据异常：" + ee.Message, DataLevel.Error);
                }));
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
        public static String bytesToHexString(byte[] src, int length)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            if (src == null || src.Length <= 0)
            {
                return null;
            }
            for (int i = 0; i < length; i++)
            {
                String hv = src[i].ToString("x2");
                stringBuilder.Append(hv);
            }
            return stringBuilder.ToString();
        }

        private void btnStopListening_Click(object sender, RoutedEventArgs e)
        {
            if (!IsStart)
                return;
            listener.Stop();
            IsStart = false;
            clearFriends();
            this.btnStartListening.IsEnabled = true;
            resetClientIDStack();
            AppendMessage("已经结束了服务器的侦听！", DataLevel.Error);
        }

        private void resetClientIDStack()
        {
            _clientIDStack.Clear();
            for (int i = 100; i > 0; i--)
            {
                _clientIDStack.Push(i);
            }
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
                clearTerminal();
                foreach (var item in MasterControlList)
                {
                    item.Dispose();
                }
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
                            AppendMessage("第一个终端的NeighbourSmall标签未设置为0", DataLevel.Warning);
                        }
                    }
                    else
                    {
                        if (MasterControlList[i - 1].TerminalNumber != neighbourSmall)
                        {
                            AppendMessage("终端" + terminalNo.ToString() + "的小相邻终端不匹配，请检查配置文件", DataLevel.Warning);
                        }
                        if (oneMasterControl.TerminalNumber != neighbourBigRemember)
                        {
                            AppendMessage("终端" + MasterControlList[i - 1].TerminalNumber.ToString() + "的大相邻终端不匹配，请检查配置文件", DataLevel.Warning);
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
                        int neighbourBig = Convert.ToInt32(innerTextNeighbourBig);
                        if (neighbourBig != 255)
                        {
                            AppendMessage("最末终端" + terminalNo.ToString() + "的大相邻终端不是255，请检查配置文件", DataLevel.Warning);
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
                SocketRegister.Clear();
            }
            catch (Exception ee)
            {
                AppendMessage("设备初始化异常：" + ee.Message, DataLevel.Error);
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
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
