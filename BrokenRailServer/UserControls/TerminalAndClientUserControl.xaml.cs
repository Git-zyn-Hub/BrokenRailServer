using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    /// Interaction logic for TerminalAndClientUserControl.xaml
    /// </summary>
    public partial class TerminalAndClientUserControl : UserControl, INotifyPropertyChanged
    {
        private AccessPointType _apType = AccessPointType.Default;
        //用于保存与每个客户相关信息：套接字与接收缓存
        private Socket _socketImport;
        private int _clientID = 0;
        public IPAddress ClientAddress { get; set; }
        private int _accumulateNumber = 0;

        public byte[] Rcvbuffer;
        public byte[] RememberBuffer;
        private string _ipAndPort;

        public AccessPointType ApType
        {
            get
            {
                return _apType;
            }

            set
            {
                if (_apType != value)
                {
                    _apType = value;
                    if (_apType == AccessPointType.Terminal)
                    {
                        lblClientID.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    OnPropertyChanged("ApType");
                }
            }
        }

        public Socket SocketImport
        {
            get
            {
                return _socketImport;
            }

            set
            {
                _socketImport = value;
            }
        }

        public int ClientID
        {
            get
            {
                return _clientID;
            }

            set
            {
                if (_clientID != value)
                {
                    _clientID = value;
                    OnPropertyChanged("ClientID");
                }
            }
        }
        public string IpAndPort
        {
            get
            {
                return _ipAndPort;
            }

            set
            {
                _ipAndPort = value;
            }
        }

        //清空接受缓存，在每一次新的接收之前都要调用该方法
        public void ClearBuffer()
        {
            Rcvbuffer = new byte[1024];
        }
        public void Dispose()
        {
            try
            {
                SocketImport.Shutdown(SocketShutdown.Both);
                SocketImport.Close();
            }
            finally
            {
                SocketImport = null;
                Rcvbuffer = null;
            }
        }

        public TerminalAndClientUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public TerminalAndClientUserControl(Socket socket)
        {
            InitializeComponent();
            this.SocketImport = socket;
            this.DataContext = this;
            IpAndPort = socket.RemoteEndPoint.ToString();
            this.lblIpAndPort.Content = IpAndPort;
        }

        public void TerminalOnline()
        {
            if (ApType == AccessPointType.Terminal)
            {
                this.pathAccessPoint.Fill = new SolidColorBrush(Colors.Green);
            }
        }

        public void TerminalOffline()
        {
            if (ApType == AccessPointType.Terminal)
            {
                this.pathAccessPoint.Fill = new SolidColorBrush(Colors.Red);
            }
        }

        public override string ToString()
        {
            string header = GetAccessPointTypeString(ApType);
            return header + this.ClientID + ":" + this.IpAndPort;
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

        public bool Recognize1024(int lengthOnce)
        {
            //V519发满1024字节之后会截断一下，在下一个1024字节继续发送
            //long beforePlusRemainder = accumulateNumber % 1024;
            _accumulateNumber += lengthOnce;
            int afterPlusRemainder = _accumulateNumber % 1024;
            if (afterPlusRemainder == 0)
            {
                //等于0的时候说明接收的字段跨过1024字节，再收一组数据。
                //有一种特殊情况，就是收到1024字节的时候正好是一整包，这样进入判断的话就会将两个本来就应该分开的包连起来，这种情况没有处理。
                return true;
            }
            return false;
        }

        public void RememberRecv(int length)
        {
            RememberBuffer = new byte[length];
            Buffer.BlockCopy(this.Rcvbuffer, 0, RememberBuffer, 0, length);
        }

        public void MergeBuffer(int length2Merge)
        {
            _accumulateNumber = 0;
            _accumulateNumber += length2Merge;
            byte[] secondReceive = new byte[length2Merge];
            for (int i = 0; i < length2Merge; i++)
            {
                secondReceive[i] = this.Rcvbuffer[i];
            }
            byte[] sumReceive = new byte[RememberBuffer.Length + length2Merge];
            RememberBuffer.CopyTo(sumReceive, 0);
            secondReceive.CopyTo(sumReceive, RememberBuffer.Length);
            Rcvbuffer = new byte[sumReceive.Length];
            sumReceive.CopyTo(Rcvbuffer, 0);
            RememberBuffer = null;
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

    public enum AccessPointType
    {
        Default,
        Terminal,
        PCClient,
        AndroidClient

    }

    public class AccessPointTerminal2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AccessPointType apt = (AccessPointType)value;
            switch (apt)
            {
                case AccessPointType.Default:
                    return Visibility.Collapsed;
                case AccessPointType.Terminal:
                    return Visibility.Visible;
                case AccessPointType.PCClient:
                    return Visibility.Collapsed;
                case AccessPointType.AndroidClient:
                    return Visibility.Collapsed;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class AccessPointPCClient2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AccessPointType apt = (AccessPointType)value;
            switch (apt)
            {
                case AccessPointType.Default:
                    return Visibility.Collapsed;
                case AccessPointType.Terminal:
                    return Visibility.Collapsed;
                case AccessPointType.PCClient:
                    return Visibility.Visible;
                case AccessPointType.AndroidClient:
                    return Visibility.Collapsed;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AccessPointAndroidClient2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AccessPointType apt = (AccessPointType)value;
            switch (apt)
            {
                case AccessPointType.Default:
                    return Visibility.Collapsed;
                case AccessPointType.Terminal:
                    return Visibility.Collapsed;
                case AccessPointType.PCClient:
                    return Visibility.Collapsed;
                case AccessPointType.AndroidClient:
                    return Visibility.Visible;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
