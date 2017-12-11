using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for ScrollViewerThumbnail.xaml
    /// </summary>
    public partial class ScrollViewerThumbnail : UserControl, INotifyPropertyChanged
    {
        private int _railNumber = 0;
        private double _userControlActualWidth;
        private Rectangle[] _rails1;
        private Rectangle[] _rails2;
        private double _xPosition = 0;
        private double _borderBoxWidth = 40;
        private double _cvsFollowMouseWidth;
        private double _scrollViewerTotalWidth;
        public delegate void MouseClicked();
        public event MouseClicked MouseClickedEvent;

        /// <summary>
        /// 铁轨被终端分开的段数
        /// </summary>
        public int RailNumber
        {
            get
            {
                return _railNumber;
            }

            set
            {
                _railNumber = value;
            }
        }

        public double XPosition
        {
            get
            {
                return _xPosition;
            }

            set
            {
                if (_xPosition != value)
                {
                    _xPosition = value;
                    OnPropertyChanged("XPosition");
                }
            }
        }

        public double BorderBoxWidth
        {
            get
            {
                return _borderBoxWidth;
            }

            set
            {
                if (_borderBoxWidth != value)
                {
                    _borderBoxWidth = value > CvsFollowMouseWidth ? CvsFollowMouseWidth : value;
                    OnPropertyChanged("BorderBoxWidth");
                }
            }
        }

        public double ScrollViewerTotalWidth
        {
            get
            {
                return _scrollViewerTotalWidth;
            }

            set
            {
                _scrollViewerTotalWidth = value;
            }
        }

        public double CvsFollowMouseWidth
        {
            get
            {
                return _cvsFollowMouseWidth;
            }

            set
            {
                _cvsFollowMouseWidth = value;
            }
        }

        public ScrollViewerThumbnail()
        {
            InitializeComponent();
        }
        public ScrollViewerThumbnail(int railNumber)
        {
            InitializeComponent();
            this._railNumber = railNumber;
            this._userControlActualWidth = this.ActualWidth;
            _rails1 = new Rectangle[railNumber];
            _rails2 = new Rectangle[railNumber];
            for (int i = 0; i < railNumber; i++)
            {
                _rails1[i] = new Rectangle();
                _rails1[i].Fill = new SolidColorBrush(Colors.Red);
                _rails2[i] = new Rectangle();
                _rails2[i].Fill = new SolidColorBrush(Colors.Red);
            }
            railRender();
        }

        private void thumbnailRail_Loaded(object sender, RoutedEventArgs e)
        {
            this._userControlActualWidth = this.ActualWidth;
        }
        private void thumbnailRail_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this._userControlActualWidth = this.ActualWidth;
            this.CvsFollowMouseWidth = this.cvsFollowMouse.ActualWidth;
            BorderBoxWidth = this._userControlActualWidth / ScrollViewerTotalWidth * this.CvsFollowMouseWidth;
            if (_rails1 != null && _rails2 != null)
            {
                railRender();
            }
        }
        private void railRender()
        {
            double width = this._userControlActualWidth / this._railNumber;
            int i = 0;
            this.cvsRail1Container.Children.Clear();
            this.cvsRail2Container.Children.Clear();
            foreach (var item in _rails1)
            {
                item.Height = 3;
                item.Width = width;
                this.cvsRail1Container.Children.Add(item);
                Canvas.SetLeft(item, width * i);
                i++;
            }
            i = 0;
            foreach (var item in _rails2)
            {
                item.Height = 3;
                item.Width = width;
                this.cvsRail2Container.Children.Add(item);
                Canvas.SetLeft(item, width * i);
                i++;
            }
        }

        private void thumbnailRail_MouseEnter(object sender, MouseEventArgs e)
        {
            this.bdrBox.Visibility = Visibility.Visible;
        }

        private void thumbnailRail_MouseLeave(object sender, MouseEventArgs e)
        {
            this.bdrBox.Visibility = Visibility.Hidden;
        }

        private void thumbnailRail_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(this.cvsFollowMouse);
            double bdrBoxWidth = this.bdrBox.ActualWidth;
            CvsFollowMouseWidth = this.cvsFollowMouse.ActualWidth;
            if ((p.X - bdrBoxWidth / 2) < 0)
            {
                XPosition = 0;
            }
            else if (p.X > (CvsFollowMouseWidth - bdrBoxWidth / 2))
            {
                XPosition = CvsFollowMouseWidth - bdrBoxWidth;
            }
            else
            {
                XPosition = p.X - bdrBoxWidth / 2;
            }
        }
        private void thumbnailRail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MouseClickedEvent != null)
            {
                MouseClickedEvent();
            }
        }
        public void Error(int[] errorNo, int whichRail)
        {
            if (whichRail == 1)
            {
                for (int i = 0; i < errorNo.Length; i++)
                {
                    _rails1[errorNo[i]].Fill = new SolidColorBrush(Colors.Red);
                }
            }
            else if (whichRail == 2)
            {
                for (int i = 0; i < errorNo.Length; i++)
                {
                    _rails2[errorNo[i]].Fill = new SolidColorBrush(Colors.Red);
                }
            }
        }

        public void Normal(int[] normalNo, int whichRail)
        {
            if (whichRail == 1)
            {
                for (int i = 0; i < normalNo.Length; i++)
                {
                    _rails1[normalNo[i]].Fill = new SolidColorBrush(Colors.LightGreen);
                }
            }
            else if (whichRail == 2)
            {
                for (int i = 0; i < normalNo.Length; i++)
                {
                    _rails2[normalNo[i]].Fill = new SolidColorBrush(Colors.LightGreen);
                }
            }
        }

        public void Different(int[] differentNo, int whichRail)
        {
            if (whichRail == 1)
            {
                for (int i = 0; i < differentNo.Length; i++)
                {
                    _rails1[differentNo[i]].Fill = new SolidColorBrush(Colors.Orange);
                }
            }
            else if (whichRail == 2)
            {
                for (int i = 0; i < differentNo.Length; i++)
                {
                    _rails2[differentNo[i]].Fill = new SolidColorBrush(Colors.Orange);
                }
            }
        }
        public void Timeout(int[] differentNo, int whichRail)
        {
            if (whichRail == 1)
            {
                for (int i = 0; i < differentNo.Length; i++)
                {
                    _rails1[differentNo[i]].Fill = new SolidColorBrush(Colors.Gray);
                }
            }
            else if (whichRail == 2)
            {
                for (int i = 0; i < differentNo.Length; i++)
                {
                    _rails2[differentNo[i]].Fill = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        public void ContinuousInterference(int[] differentNo, int whichRail)
        {
            if (whichRail == 1)
            {
                for (int i = 0; i < differentNo.Length; i++)
                {
                    _rails1[differentNo[i]].Fill = new SolidColorBrush(Colors.LightBlue);
                }
            }
            else if (whichRail == 2)
            {
                for (int i = 0; i < differentNo.Length; i++)
                {
                    _rails2[differentNo[i]].Fill = new SolidColorBrush(Colors.LightBlue);
                }
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
