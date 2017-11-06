using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
        
        public byte[] Rcvbuffer;

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
            this.SocketImport = socket;
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
