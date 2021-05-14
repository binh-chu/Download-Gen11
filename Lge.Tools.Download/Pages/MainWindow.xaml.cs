using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms; // jwoh add popup message box

namespace Lge.Tools.Download
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            MainModel.Window = this;

            InitializeComponent();

            tabControl.SelectedIndex = Model.CurrentTabIndex;

            DisplayTitle();
        }

        private void Frame_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var f = sender as Frame;
            (f.Content as Page).DataContext = f.DataContext;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SerialportWatcher.UnregisterUsbDeviceNotification();

            DeleteLogFiles(); // Delete Log files

            // save windows position and size
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WinWidth = this.Width;
                Properties.Settings.Default.WinHeight = this.Height;
                Properties.Settings.Default.WinTop = this.Top;
                Properties.Settings.Default.WinLeft = this.Left;
            }

            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(Properties.Settings.Default.MCP2K_4K =="2K")
                System.Windows.MessageBox.Show("The image is 2K version", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            else if(Properties.Settings.Default.MCP2K_4K == "4K")
                System.Windows.MessageBox.Show("The image is 4K version", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                System.Windows.MessageBox.Show("The image is not 2K nor 4K version", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Install window hook for USB device add/removal notification.
            SerialportWatcher.InstallWindowHook(this);
            SerialportWatcher.RegisterUsbDeviceNotification();

            // restore windows position and size
            if (Properties.Settings.Default.WinWidth > 0)
                this.Width = Properties.Settings.Default.WinWidth;
            if (Properties.Settings.Default.WinHeight > 0)
                this.Height = Properties.Settings.Default.WinHeight;
            if (Properties.Settings.Default.WinTop >= 0)
                this.Top = Properties.Settings.Default.WinTop;
            if (Properties.Settings.Default.WinLeft >= 0)
                this.Left = Properties.Settings.Default.WinLeft;
        }

        public MainModel Model { get { return this.DataContext as MainModel; } }

        private void Info_MouseDown(object sender, MouseButtonEventArgs e)
        {
            new Pages.AppInfoBox().ShowDialog();
        }

        private void Option_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var visible = optionGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            // update datacontext with current tab's page
            if (visible == Visibility.Visible)
            {
                switch (Model.CurrentTabIndex)
                {
                    case (int)Models.TabType.Emergency:
                        optionGrid.DataContext = Model.EModel;
                        break;
                    case (int)Models.TabType.Multi:
                        optionGrid.DataContext = Model.MModel;
                        break;
                    case (int)Models.TabType.Dump:
                        optionGrid.DataContext = Model.DModel;
                        break;
                    case (int)Models.TabType.Normal:
                        optionGrid.DataContext = Model.NModel;
                        break;
                    default:
                        return;
                }
            }
            // show and hide
            optionGrid.Visibility = visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Log.LogPath);
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Model.IsIdle)
            {
                e.Handled = true;
                tabControl.SelectedIndex = Model.CurrentTabIndex;
                return;
            }

            Model.CurrentTabIndex = tabControl.SelectedIndex;
            // update datacontext with current tab's page
            if (optionGrid != null && optionGrid.Visibility == Visibility.Visible)
            {
                switch (Model.CurrentTabIndex)
                {
                    case (int)Models.TabType.Emergency:
                        optionGrid.DataContext = Model.EModel;
                        break;
                    case (int)Models.TabType.Multi:
                        optionGrid.DataContext = Model.MModel;
                        break;
                    case (int)Models.TabType.Dump:
                        optionGrid.DataContext = Model.DModel;
                        break;
                    case (int)Models.TabType.Normal:
                        optionGrid.DataContext = Model.NModel;
                        break;
                    default:
                        return;
                }
            }
        }

        private void InvokeEfsBackup(object sender, RoutedEventArgs e)
        {
            Model.MModel.InvokeEfsBackup();
        }

        private void Hide_devOptions(object sender, RoutedEventArgs e)
        {
            _devOptions.Visibility = Visibility.Hidden;
        }

        // buttons password
        MouseButton [] _btnPass = new MouseButton[] { MouseButton.Left, MouseButton.Right, MouseButton.Right, MouseButton.Left };
        List<MouseButton> _btnInput = new List<MouseButton>();
        int _lastTic = 0;
        const int validTicInterval = 2 * 1000;

        private void DevOptionBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_lastTic != 0 && Environment.TickCount - _lastTic > validTicInterval)
            {
                _btnInput.Clear();
            }

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                case MouseButton.Right:
                    _btnInput.Add(e.ChangedButton);
                    _lastTic = Environment.TickCount;                 
                    break;
                default:
                    _btnInput.Clear();
                    _lastTic = 0;
                    break;
            }

            if (_btnInput.Count == _btnPass.Length)
            {
                bool matched = true;
                for(int i = 0; i <_btnInput.Count; i++)
                {
                    if (_btnInput[i] != _btnPass[i])
                    {
                        matched = false;
                        break;
                    }
                }
                if (matched)
                {
                    _devOptions.Visibility = Visibility.Visible;
                }
                _btnInput.Clear();
                _lastTic = 0;
            }
        }

        private void DisplayTitle()
        {
            var asm = Assembly.GetExecutingAssembly();
            string assemblyVersion = asm.GetName().Version.ToString();

            this.Title = string.Format("Gen11 Downloader - V{0}", assemblyVersion);
        }

        private void DeleteLogFiles()
        {
            if (System.IO.Directory.Exists(Helper.LogDirPath))
            {
                string[] files = System.IO.Directory.GetFiles(Helper.LogDirPath);

                foreach (string sfile in files)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(sfile);
                    if (fi.LastAccessTime < DateTime.Now.AddMonths(-1))
                        fi.Delete();
                }
            }
        }
    }
    // jins.choi
    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        private IntPtr _hwnd;
    }
}
