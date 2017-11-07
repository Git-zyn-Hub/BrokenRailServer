using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

namespace BrokenRailServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int _packageCount = 0;
        //保存与客户相关的信息列表
        ArrayList friends = new ArrayList();
        //负责监听的套接字
        TcpListener listener;
        //只是是否启动了监听
        bool IsStart = false;

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
                AppendMethod(string.Format("服务器已经启动监听！端点为：{0}。", listener.LocalEndpoint.ToString()));
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
            catch (Exception ee)
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
                        data = string.Format("From[{0}]:{1}", frd.SocketImport.RemoteEndPoint.ToString(), data);
                        PackageCount++;
                        this.Dispatcher.Invoke(new Action(() => { AppendMethod(data); }));
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

        private void SendData(TerminalAndClientUserControl frd, string data)
        {
            try
            {
                byte[] msg = Encoding.UTF8.GetBytes(data);
                AsyncCallback callback = new AsyncCallback(SendCallback);
                frd.SocketImport.BeginSend(msg, 0, msg.Length, SocketFlags.None, callback, frd);
                data = string.Format("To[{0}]:{1}", frd.SocketImport.RemoteEndPoint.ToString(), data);
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
