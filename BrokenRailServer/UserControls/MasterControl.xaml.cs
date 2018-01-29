using BrokenRailMonitorViaWiFi.Windows;
using BrokenRailMonitorViaWiFi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrokenRailServer.UserControls
{
    /// <summary>
    /// Interaction logic for MasterControl.xaml
    /// </summary>
    public partial class MasterControl : UserControl, INotifyPropertyChanged
    {
        private int _terminalNumber;
        private int _neighbourSmall;
        private int _neighbourBig;
        private bool _isEnd;
        private string _ipAndPort;
        private string _find4GErrorMsg;
        private MainWindow _mainWin;
        public static readonly DependencyProperty Is4GProperty = DependencyProperty.Register("Is4G", typeof(bool), typeof(MasterControl), new PropertyMetadata(false, OnIs4GChanged));
        private TerminalAndClientUserControl _terminal = null;
        private DispatcherTimer _offlineTimer;
        private bool _isOnline = false;
        private int _rail1Stress;
        private int _rail1Temperature;
        private int _rail2Stress;
        private int _rail2Temperature;
        private int _masterCtrlTemperature;

        public bool Is4G
        {
            get { return (bool)GetValue(Is4GProperty); }
            set { SetValue(Is4GProperty, value); }
        }
        public int TerminalNumber
        {
            get
            {
                return _terminalNumber;
            }

            set
            {
                if (_terminalNumber != value)
                {
                    _terminalNumber = value;
                    OnPropertyChanged("TerminalNumber");
                }
            }
        }

        public int NeighbourSmall
        {
            get
            {
                return _neighbourSmall;
            }

            set
            {
                _neighbourSmall = value;
            }
        }

        public int NeighbourBig
        {
            get
            {
                return _neighbourBig;
            }

            set
            {
                _neighbourBig = value;
            }
        }

        public bool IsEnd
        {
            get
            {
                return _isEnd;
            }

            set
            {
                _isEnd = value;
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

        public string Find4GErrorMsg
        {
            get
            {
                return _find4GErrorMsg;
            }

            set
            {
                _find4GErrorMsg = value;
            }
        }

        public TerminalAndClientUserControl Terminal
        {
            get
            {
                return _terminal;
            }

            set
            {
                _terminal = value;
                //IpAndPort = _terminal.SocketImport.RemoteEndPoint.ToString();
            }
        }

        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }

            set
            {
                _isOnline = value;
            }
        }
        public int Rail1Stress
        {
            get
            {
                return _rail1Stress;
            }

            set
            {
                if (_rail1Stress != value)
                {
                    _rail1Stress = value;
                    OnPropertyChanged("Rail1Stress");
                }
            }
        }

        public int Rail1Temperature
        {
            get
            {
                return _rail1Temperature;
            }

            set
            {
                if (_rail1Temperature != value)
                {
                    _rail1Temperature = value;
                    OnPropertyChanged("Rail1Temperature");
                }
            }
        }

        public int Rail2Stress
        {
            get
            {
                return _rail2Stress;
            }

            set
            {
                if (_rail2Stress != value)
                {
                    _rail2Stress = value;
                    OnPropertyChanged("Rail2Stress");
                }
            }
        }

        public int Rail2Temperature
        {
            get
            {
                return _rail2Temperature;
            }

            set
            {
                if (_rail2Temperature != value)
                {
                    _rail2Temperature = value;
                    OnPropertyChanged("Rail2Temperature");
                }
            }
        }

        public int MasterCtrlTemperature
        {
            get
            {
                return _masterCtrlTemperature;
            }

            set
            {
                if (_masterCtrlTemperature != value)
                {
                    _masterCtrlTemperature = value;
                    OnPropertyChanged("MasterCtrlTemperature");
                }
            }
        }

        public MasterControl()
        {
            InitializeComponent();
        }
        public MasterControl(MainWindow mainWin)
        {
            InitializeComponent();
            _mainWin = mainWin;
            this.DataContext = this;
            _offlineTimer = new DispatcherTimer();
            _offlineTimer.Interval = new TimeSpan(0, 2, 5);
            _offlineTimer.Tick += offlineTimer_Tick;
        }

        private void offlineTimer_Tick(object sender, EventArgs e)
        {
            _mainWin.AppendMessage("超过2分钟没有收到终端" + TerminalNumber + "的心跳包，它可能已经下线", DataLevel.Error);
            Offline();
            Terminal.TerminalOffline();
        }

        private static void OnIs4GChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ((MasterControl)d).cvs4G.Visibility = Visibility.Visible;
            }
            else
            {
                ((MasterControl)d).cvs4G.Visibility = Visibility.Collapsed;
            }
        }
        private void miGetPointRailInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWin != null)
                {
                    _mainWin.WaitingRingEnable();
                    //_mainWin.WaitReceiveTimer.Start();
                }
                byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber,
                    (byte)CommandType.GetPointRailInfo, new byte[2] { 0, 0 });
                TerminalAndClientUserControl socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    _mainWin.DecideDelayOrNot();
                    _mainWin.SendData(socketGet, sendData);
                }
                else
                {
                    if (_mainWin != null)
                    {
                        _mainWin.WaitingRingDisable();
                        //_mainWin.WaitReceiveTimer.Stop();
                    }
                    _mainWin.AppendMessage(Find4GErrorMsg, DataLevel.Error);
                }
            }
            catch (Exception ee)
            {
                _mainWin.AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        //private void miGetPointSignalAmplitude_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (SocketImport != null)
        //        {
        //            byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf3, new byte[2] { 0, 0 });
        //            SocketImport.Send(sendData, SocketFlags.None);
        //        }
        //        else
        //        {
        //            _mainWin.AppendMessage("Socket未导入！");
        //        }
        //    }
        //    catch (Exception ee)
        //    {
        //        _mainWin.AppendMessage(ee.Message);
        //    }
        //}

        private void miConfigInitialInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InitialInfoConfigWindow newInitialInfoConfigWin = new InitialInfoConfigWindow();

                if (_mainWin != null)
                {
                    newInitialInfoConfigWin.Owner = _mainWin;
                    //初始化默认的终端号。
                    newInitialInfoConfigWin.TerminalNo = this._terminalNumber;
                    int index = _mainWin.FindMasterControlIndex(this._terminalNumber);
                    if (index == 0)
                    {
                        newInitialInfoConfigWin.NeighbourSmallSecondary = 0;
                        newInitialInfoConfigWin.NeighbourSmall = 0;
                    }
                    else if (index == 1)
                    {
                        newInitialInfoConfigWin.NeighbourSmallSecondary = 0;
                        newInitialInfoConfigWin.NeighbourSmall = _mainWin.MasterControlList[0].TerminalNumber;
                    }
                    else
                    {
                        newInitialInfoConfigWin.NeighbourSmallSecondary = _mainWin.MasterControlList[index - 2].TerminalNumber;
                        newInitialInfoConfigWin.NeighbourSmall = _mainWin.MasterControlList[index - 1].TerminalNumber;
                    }
                    if (index == _mainWin.MasterControlList.Count - 2)
                    {
                        newInitialInfoConfigWin.NeighbourBig = _mainWin.MasterControlList[_mainWin.MasterControlList.Count - 1].TerminalNumber;
                        newInitialInfoConfigWin.NeighbourBigSecondary = 0xff;
                    }
                    else if (index == _mainWin.MasterControlList.Count - 1)
                    {
                        newInitialInfoConfigWin.NeighbourBig = 0xff;
                        newInitialInfoConfigWin.NeighbourBigSecondary = 0xff;
                    }
                    else
                    {
                        newInitialInfoConfigWin.NeighbourBig = _mainWin.MasterControlList[index + 1].TerminalNumber;
                        newInitialInfoConfigWin.NeighbourBigSecondary = _mainWin.MasterControlList[index + 2].TerminalNumber;
                    }
                }
                if (!newInitialInfoConfigWin.ShowDialog().Value)
                {
                    return;
                }

                byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, (byte)CommandType.ConfigInitialInfo, new byte[6] { (byte)newInitialInfoConfigWin.TerminalNo,
                    (byte)newInitialInfoConfigWin.NeighbourSmallSecondary, (byte)newInitialInfoConfigWin.NeighbourSmall,
                    (byte)newInitialInfoConfigWin.NeighbourBig, (byte)newInitialInfoConfigWin.NeighbourBigSecondary,0x00 });
                TerminalAndClientUserControl socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    _mainWin.DecideDelayOrNot();
                    _mainWin.SendData(socketGet, sendData);
                }
                else
                {
                    _mainWin.AppendMessage(Find4GErrorMsg, DataLevel.Error);
                }
            }
            catch (Exception ee)
            {
                _mainWin.AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        private void miReadPointInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWin != null)
                {
                    _mainWin.WaitingRingEnable();
                    //_mainWin.WaitReceiveTimer.Start();
                }
                else
                {
                    return;
                }
                byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, (byte)CommandType.ReadPointInfo, new byte[1] { (byte)_terminalNumber });
                TerminalAndClientUserControl socketGet = GetNearest4GTerminalSocket(true);

                if (socketGet != null)
                {
                    _mainWin.DecideDelayOrNot();
                    _mainWin.SendData(socketGet, sendData);
                    _mainWin.ServerRequest["服务器请求单点配置信息"] = true;
                    _mainWin.AppendMessage("服务器请求单点配置信息", DataLevel.Default);
                }
                else
                {
                    if (_mainWin != null)
                    {
                        _mainWin.WaitingRingDisable();
                        //_mainWin.WaitReceiveTimer.Stop();
                    }
                    _mainWin.AppendMessage(Find4GErrorMsg, DataLevel.Error);
                }
            }
            catch (Exception ee)
            {
                _mainWin.AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        //private void miSignalSendConfig_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        SignalSendConfigWindow newSignalSendConfigWin = new SignalSendConfigWindow();

        //        if (_mainWin != null)
        //        {
        //            newSignalSendConfigWin.Owner = _mainWin;
        //        }
        //        if (!newSignalSendConfigWin.ShowDialog().Value)
        //        {
        //            return;
        //        }
        //        byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf4,
        //            new byte[4] { (byte)newSignalSendConfigWin.SendInterval,(byte)newSignalSendConfigWin.SendTimeOpportunity,
        //                              (byte)newSignalSendConfigWin.NeighbourSmallOpportunity,(byte)newSignalSendConfigWin.NeighbourBigOpportunity  });
        //        Socket socketGet = GetNearest4GTerminalSocket(true);
        //        if (socketGet != null)
        //        {
        //            _mainWin.DecideDelayOrNot();
        //            socketGet.Send(sendData, SocketFlags.None);
        //        }
        //        else
        //        {
        //            _mainWin.AppendMessage(Find4GErrorMsg);
        //        }
        //    }
        //    catch (Exception ee)
        //    {
        //        _mainWin.AppendMessage(ee.Message);
        //    }
        //}

        private void miThresholdSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThresholdSettingWindow newThresholdSettingWin = new ThresholdSettingWindow();

                if (_mainWin != null)
                {
                    newThresholdSettingWin.Owner = _mainWin;
                }
                if (!newThresholdSettingWin.ShowDialog().Value)
                {
                    return;
                }
                byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf2,
                    new byte[2] { (byte)newThresholdSettingWin.ThresholdRail1, (byte)newThresholdSettingWin.ThresholdRail2 });
                TerminalAndClientUserControl socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    _mainWin.DecideDelayOrNot();
                    _mainWin.SendData(socketGet, sendData);
                }
                else
                {
                    _mainWin.AppendMessage(Find4GErrorMsg, DataLevel.Error);
                }
            }
            catch (Exception ee)
            {
                _mainWin.AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        private void miGetHistory_Click(object sender, RoutedEventArgs e)
        {
            GetHistoryWindow newGetHistoryWin = new GetHistoryWindow();
            if (_mainWin != null)
            {
                newGetHistoryWin.Owner = this._mainWin;
            }
            if (!newGetHistoryWin.ShowDialog().Value)
            {
                return;
            }
            byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, (byte)CommandType.GetHistory,
                new byte[12] { (byte)newGetHistoryWin.YearStart, (byte)newGetHistoryWin.MonthStart, (byte)newGetHistoryWin.DayStart,
                               (byte)newGetHistoryWin.HourStart,(byte)newGetHistoryWin.MinuteStart,(byte)newGetHistoryWin.SecondStart,
                               (byte)newGetHistoryWin.YearEnd,(byte)newGetHistoryWin.MonthEnd,(byte)newGetHistoryWin.DayEnd,
                               (byte)newGetHistoryWin.HourEnd,(byte)newGetHistoryWin.MinuteEnd,(byte)newGetHistoryWin.SecondEnd });
            TerminalAndClientUserControl socketGet = GetNearest4GTerminalSocket(true);
            if (socketGet != null)
            {
                _mainWin.DecideDelayOrNot();
                _mainWin.SendData(socketGet, sendData);
            }
            else
            {
                _mainWin.AppendMessage(Find4GErrorMsg, DataLevel.Error);
            }
        }


        private void miTerminalReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWin == null)
                {
                    return;
                }
                byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, (byte)CommandType.TerminalReset, new byte[0]);
                TerminalAndClientUserControl socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    _mainWin.DecideDelayOrNot();
                    _mainWin.SendData(socketGet, sendData);
                }
                else
                {
                    _mainWin.AppendMessage(Find4GErrorMsg, DataLevel.Error);
                }
            }
            catch (Exception ee)
            {
                _mainWin.AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        public TerminalAndClientUserControl GetNearest4GTerminalSocket(bool isForward)
        {
            if (this.Is4G)
            {
                if (_mainWin != null)
                {
                    for (int j = 0; j < _mainWin.SocketRegister.Count; j++)
                    {
                        if (_mainWin.SocketRegister[j] == this.TerminalNumber)
                        {
                            return getTerminalSocket();
                        }
                        else if (j == _mainWin.SocketRegister.Count - 1)
                        {
                            break;
                        }
                    }
                    Find4GErrorMsg = "该终端本身为4G点，但此4G点的Socket连接未注册！";
                    return null;
                }
                Find4GErrorMsg = "主窗口句柄为空！";
                return null;
            }
            else
            {
                if (_mainWin != null)
                {
                    if (_mainWin.SocketRegister.Count == 0)
                    {
                        Find4GErrorMsg = "注册的4G点Socket连接个数为0";
                        return null;
                    }
                    else if (_mainWin.SocketRegister.Count == 1)
                    {
                        foreach (var item in _mainWin.MasterControlList)
                        {
                            if (item.TerminalNumber == _mainWin.SocketRegister[0])
                            {
                                return item.getTerminalSocket();
                            }
                        }
                    }
                    else
                    {
                        if (isForward)
                        {
                            //如果是正向
                            int indexOfThisMasterControl = _mainWin.MasterControlList.FindIndex(FindMasterControl);
                            for (int i = indexOfThisMasterControl; i >= 0; i--)
                            {
                                if (_mainWin.MasterControlList[i].Is4G)
                                {
                                    int terminal4GNo = _mainWin.MasterControlList[i].TerminalNumber;
                                    for (int j = 0; j < _mainWin.SocketRegister.Count; j++)
                                    {
                                        if (_mainWin.SocketRegister[j] == terminal4GNo)
                                        {
                                            return _mainWin.MasterControlList[i].getTerminalSocket();
                                        }
                                        else if (j == _mainWin.SocketRegister.Count - 1)
                                        {
                                            Find4GErrorMsg = "正向未找到小于该终端号的4G点Socket连接！";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //如果是反向
                            int indexOfThisMasterControl = _mainWin.MasterControlList.FindIndex(FindMasterControl);
                            for (int i = indexOfThisMasterControl; i < _mainWin.MasterControlList.Count; i++)
                            {
                                if (_mainWin.MasterControlList[i].Is4G)
                                {
                                    int terminal4GNo = _mainWin.MasterControlList[i].TerminalNumber;
                                    for (int j = 0; j < _mainWin.SocketRegister.Count; j++)
                                    {
                                        if (_mainWin.SocketRegister[j] == terminal4GNo)
                                        {
                                            return _mainWin.MasterControlList[i].getTerminalSocket();
                                        }
                                        else if (j == _mainWin.SocketRegister.Count - 1)
                                        {
                                            Find4GErrorMsg = "反向未找到大于该终端号的4G点Socket连接！";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Find4GErrorMsg = "主窗口句柄为空！";
                return null;
            }
        }
        private TerminalAndClientUserControl getTerminalSocket()
        {
            if (Terminal != null)
            {
                return this.Terminal;
            }
            return null;
        }
        private bool FindMasterControl(MasterControl mc)
        {
            if (mc.TerminalNumber == this.TerminalNumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void HideContextMenu()
        {
            this.contextMenu.Visibility = Visibility.Hidden;
        }

        public void Online()
        {
            if (_offlineTimer.IsEnabled)
            {
                _offlineTimer.Stop();
                _offlineTimer.Start();
            }
            else
            {
                _offlineTimer.Start();
            }
            this.path4G.Fill = new SolidColorBrush(Colors.Green);
            IsOnline = true;
        }

        public void Offline()
        {
            this.path4G.Fill = new SolidColorBrush(Colors.Red);
            IsOnline = false;
            if (_offlineTimer.IsEnabled)
                _offlineTimer.Stop();
        }

        public void Dispose()
        {
            try
            {
                if (_offlineTimer.IsEnabled)
                    _offlineTimer.Stop();
                //if (Terminal != null)
                //{
                //    Terminal.Dispose();
                //}
            }
            finally
            {
                _offlineTimer = null;
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
