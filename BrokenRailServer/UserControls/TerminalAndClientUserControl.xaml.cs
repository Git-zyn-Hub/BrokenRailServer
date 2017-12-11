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
        public List<byte[]> RememberBuffers = new List<byte[]>();
        private string _ipAndPort;
        private byte[] _packageUnhandled = new byte[0];

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

        public byte[] PackageUnhandled
        {
            get
            {
                return _packageUnhandled;
            }

            set
            {
                _packageUnhandled = value;
            }
        }

        //清空接受缓存，在每一次新的接收之前都要调用该方法
        public void ClearBuffer()
        {
            Rcvbuffer = new byte[2048];
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
            //if (lengthOnce == 1500)
            //{
            //    return true;
            //}
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
            RememberBuffers.Add(RememberBuffer);
        }

        public void MergeBuffer(int length2Merge)
        {
            _accumulateNumber = 0;
            _accumulateNumber += length2Merge;
            byte[] lastReceive = new byte[length2Merge];
            for (int i = 0; i < length2Merge; i++)
            {
                lastReceive[i] = this.Rcvbuffer[i];
            }
            int totalLength = 0;
            for (int i = 0; i < RememberBuffers.Count; i++)
            {
                totalLength += RememberBuffers[i].Length;
            }
            totalLength += length2Merge;
            byte[] sumReceive = new byte[totalLength];

            int offset = 0;
            for (int i = 0; i < RememberBuffers.Count; i++)
            {
                RememberBuffers[i].CopyTo(sumReceive, offset);
                offset += RememberBuffers[i].Length;
                if (i == RememberBuffers.Count - 1)
                {
                    lastReceive.CopyTo(sumReceive, offset);
                }
            }
            Rcvbuffer = new byte[totalLength];
            sumReceive.CopyTo(Rcvbuffer, 0);
            RememberBuffer = null;
            RememberBuffers.Clear();
        }

        int hitCount = 0;
        /// <summary>
        /// 处理粘包的情况
        /// </summary>
        /// <returns>表示是否有未处理的数据</returns>
        public bool HandleNianBao(ref int length)
        {
            if ((Rcvbuffer[0] != 0x66) || (Rcvbuffer[1] != 0xcc))
            {
                return false;
            }
            int len = (Rcvbuffer[2] << 8) + Rcvbuffer[3];
            if (len < length)
            {
                //原来是！=的时候进入判断，可能会造成unhandledLength为负值，导致数组越界。
                //处理粘包的情况。
                int unhandledLength = Rcvbuffer.Length - len;
                byte[] packagePrevious = new byte[len];
                PackageUnhandled = new byte[unhandledLength];
                for (int j = 0; j < len; j++)
                {
                    packagePrevious[j] = Rcvbuffer[j];
                }
                for (int i = 0; i < unhandledLength; i++)
                {
                    PackageUnhandled[i] = Rcvbuffer[len + i];
                }
                Rcvbuffer = new byte[len];
                packagePrevious.CopyTo(Rcvbuffer, 0);
                length = len;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void HandleDuanBao(ref int length)
        {
            if (length > 1)
            {
                if (PackageUnhandled.Length != 0)
                {
                    if (Rcvbuffer[0] == 0x55 && Rcvbuffer[1] == 0xaa)
                    {

                    }
                    else if (Rcvbuffer[0] == 0x66 && Rcvbuffer[1] == 0xcc)
                    {

                    }
                    else if (Rcvbuffer[0] == 0x23 && Rcvbuffer[1] == 0x23)
                    {

                    }
                    else
                    {
                        int totalLength = PackageUnhandled.Length + length;
                        byte[] sumBytes = new byte[totalLength];
                        PackageUnhandled.CopyTo(sumBytes, 0);
                        Buffer.BlockCopy(Rcvbuffer, 0, sumBytes, PackageUnhandled.Length, length);
                        Rcvbuffer = new byte[totalLength];
                        sumBytes.CopyTo(Rcvbuffer, 0);
                        length = totalLength;
                        PackageUnhandled = new byte[0];
                    }
                }
            }
        }

        /// <summary>
        /// 拷贝未处理的数组到接收缓存为了下一次循环
        /// </summary>
        /// <returns>拷贝后的数组长度为零返回false，否则为true</returns>
        public bool CopyArray4Circle(ref int length)
        {
            length = PackageUnhandled.Length;
            Rcvbuffer = new byte[length];
            PackageUnhandled.CopyTo(Rcvbuffer, 0);
            //if (length == 10)
            //{
            //    if (_packageUnhandled[0] == 0x66 && _packageUnhandled[1] == 0xcc)
            //    {
            //        Rcvbuffer = new byte[length];
            //        _packageUnhandled.CopyTo(Rcvbuffer, 0);
            //    }
            //}
            //else
            //{
            //    RememberBuffers.Add(_packageUnhandled);
            //}
            return length == 0 ? false : true;
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
