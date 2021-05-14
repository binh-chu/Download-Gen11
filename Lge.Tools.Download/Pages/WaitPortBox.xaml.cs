using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lge.Tools.Download.Pages
{
    /// <summary>
    /// WaitPortBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WaitPortBox : Window
    {
        private WaitPortBox(Models.TabType aInvokeType, Object arg)
        {
            this._invokeType = aInvokeType;
            this._arg = arg;

            InitializeComponent();

            SerialportWatcher.SerialPortsChangedEvent += SerialportWatcher_SerialPortsChangedEvent;
            this.Closing += (s, e) =>
            {
                _running = false;
                SerialportWatcher.SerialPortsChangedEvent -= SerialportWatcher_SerialPortsChangedEvent;
            };
        }

        private object _arg;

        private void SerialportWatcher_SerialPortsChangedEvent(bool aInserted, SerialportWatcher.PortInfo aChangedPort)
        {
            if (aInserted && aChangedPort.Kind == SerialportWatcher.PortKind.QDLoader)
            {
                this.UIThread(delegate
                {
                    this.DialogResult = true;
                    this.Close();
                });
            }
            
        }

        protected override void OnInitialized(EventArgs e)
        {
            this.Owner = Application.Current.MainWindow;
            base.OnInitialized(e);

            // run wait task
            if (_invokeType == Models.TabType.Emergency)
            {
                // debug포트롤 'D' 문자를 전송, SBL1에서 수신 시 QDLoader모드로 전환됨.
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(this.TryEDownloadMode), _arg);
            }
            else if (_invokeType == Models.TabType.Dump)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(this.TryDumpMode), _arg);
            }
            else if (_invokeType == Models.TabType.Normal)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(this.TryMicomDownloadMode), _arg);
            }
            else
            {
                // 그냥 사용자가 끌때 까지 대기.
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var child = Owner.Content as FrameworkElement;

            Point pos = new Point(0, 0);
            pos = child.PointToScreen(pos);

            var h = child.ActualHeight;
            var w = child.ActualWidth;

            this.Height = h;
            this.Width = w;

            var ch = this._mid.ActualHeight;
            this._top.Height = (h - ch) / 2;

            this.Top = pos.Y;//this.Owner.Top + pos.Y;
            this.Left = pos.X;//this.Owner.Left + pos.X;

        }

        
        private Models.TabType _invokeType;

        public static bool Wait(Models.TabType aInvokeType, object arg)
        {
            bool result = false;
            try
            {
                // 벌써 감지 됨.
                if (SerialportWatcher.GetPorts(SerialportWatcher.PortKind.QDLoader).Count > 0)
                    return true;

                WaitPortBox box = new WaitPortBox(aInvokeType, arg);

                result = box.ShowDialog() ?? false;
            }
            catch (Exception e)
            {
                Log.e("Exception at wait port windows, {0}", e);
            }

            return result;
        }

        private bool _running = false;

        // try enter emergency download mode
        private void TryEDownloadMode(object arg)
        {
            uint uartbaudrate = 0;
            var sendBytes = Encoding.ASCII.GetBytes("ddd");
            _running = true;
            Log.i("WaitPort Thread Started.");

            var model = arg as EmergencyModel;

            while (_running)
            {
                try
                {
                    if (model.CurrentPort.Kind == SerialportWatcher.PortKind.QDLoader)
                        break;

                    if (model.CurrentPort.Kind != SerialportWatcher.PortKind.Serial)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    string portName = model.CurrentPort.Name;

                    if (string.IsNullOrEmpty(portName))
                    {
                        continue;
                    }
                    if (model.SelBaudrate == 9)
                        uartbaudrate = 921600;
                    else if (model.SelBaudrate == 8)
                        uartbaudrate = 460800;
                    else if (model.SelBaudrate == 7)
                        uartbaudrate = 230400;
                    else if (model.SelBaudrate == 6)
                        uartbaudrate = 115200;
                    else if (model.SelBaudrate == 5)
                        uartbaudrate = 57600;
                    else if (model.SelBaudrate == 4)
                        uartbaudrate = 38400;
                    else if (model.SelBaudrate == 3)
                        uartbaudrate = 19200;
                    else if (model.SelBaudrate == 2)
                        uartbaudrate = 14400;
                    else if (model.SelBaudrate == 1)
                        uartbaudrate = 9600;
                    else if (model.SelBaudrate == 0)
                        uartbaudrate = 7200;

                    using (var port = new SerialPortNative(portName, uartbaudrate))
                    {
                        // send
                        while (_running)
                        {
                            if (!port.Write(sendBytes))
                                break;

                            //port.Flush();
                            System.Threading.Thread.Sleep(100);
                        }
                    }

                }
                catch (Exception e)
                {
                    Log.e("Exception: {0}, It can't open a port., {0}", model.CurrentPort.Name, e);
                    System.Threading.Thread.Sleep(600);
                }
            }
            _running = false;
            Log.i("WaitPort Thread Exit");
        }

        private void TryMicomDownloadMode(object arg)
        {
            uint uartbaudrate = 0;
            var sendBytes = Encoding.ASCII.GetBytes("ddd");
            _running = true;
            Log.i("WaitPort Thread Started.");

            var model = arg as NormalModel;

            while (_running)
            {
                try
                {
                    if (model.CurrentPort.Kind == SerialportWatcher.PortKind.QDLoader)
                        break;

                    if (model.CurrentPort.Kind != SerialportWatcher.PortKind.Serial || model.MicomPort.Kind != SerialportWatcher.PortKind.Serial)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    string portNameMdm = model.CurrentPort.Name;
                    string portNameMicom = model.MicomPort.Name;

                    if (string.IsNullOrEmpty(portNameMdm) || string.IsNullOrEmpty(portNameMicom))
                    {
                        continue;
                    }
                    if (model.SelBaudrate == 9)
                        uartbaudrate = 921600;
                    else if (model.SelBaudrate == 8)
                        uartbaudrate = 460800;
                    else if (model.SelBaudrate == 7)
                        uartbaudrate = 230400;
                    else if (model.SelBaudrate == 6)
                        uartbaudrate = 115200;
                    else if (model.SelBaudrate == 5)
                        uartbaudrate = 57600;
                    else if (model.SelBaudrate == 4)
                        uartbaudrate = 38400;
                    else if (model.SelBaudrate == 3)
                        uartbaudrate = 19200;
                    else if (model.SelBaudrate == 2)
                        uartbaudrate = 14400;
                    else if (model.SelBaudrate == 1)
                        uartbaudrate = 9600;
                    else if (model.SelBaudrate == 0)
                        uartbaudrate = 7200;

                    using (var mdmport = new SerialPortNative(portNameMdm, uartbaudrate))
                    {
                        var micomport = new SerialPortNative(portNameMicom, uartbaudrate);
                        // send
                        while (_running)
                        {
                            if (!micomport.Write(sendBytes))
                                break;

                            if (!mdmport.Write(sendBytes))
                                break;

                            //port.Flush();
                            System.Threading.Thread.Sleep(3);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.e("Exception: {0}, It can't open a port., {0}", model.CurrentPort.Name, e);
                    System.Threading.Thread.Sleep(600);
                }
            }
            _running = false;
            Log.i("WaitPort Thread Exit");
        }

        private void TryDumpMode(object arg)
        {
            uint uartbaudrate = 0;
            var sendBytes = Encoding.ASCII.GetBytes("ddd");
            _running = true;
            Log.i("WaitPort Thread Started.");

            var model = arg as DumpModel;

            while (_running)
            {
                try
                {
                    if (model.CurrentPort.Kind == SerialportWatcher.PortKind.QDLoader)
                        break;

                    if (model.CurrentPort.Kind != SerialportWatcher.PortKind.Serial)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    string portName = model.CurrentPort.Name;

                    if (string.IsNullOrEmpty(portName))
                    {
                        continue;
                    }
                    if (model.SelBaudrate == 9)
                        uartbaudrate = 921600;
                    else if (model.SelBaudrate == 8)
                        uartbaudrate = 460800;
                    else if (model.SelBaudrate == 7)
                        uartbaudrate = 230400;
                    else if (model.SelBaudrate == 6)
                        uartbaudrate = 115200;
                    else if (model.SelBaudrate == 5)
                        uartbaudrate = 57600;
                    else if (model.SelBaudrate == 4)
                        uartbaudrate = 38400;
                    else if (model.SelBaudrate == 3)
                        uartbaudrate = 19200;
                    else if (model.SelBaudrate == 2)
                        uartbaudrate = 14400;
                    else if (model.SelBaudrate == 1)
                        uartbaudrate = 9600;
                    else if (model.SelBaudrate == 0)
                        uartbaudrate = 7200;

                    using (var port = new SerialPortNative(portName, uartbaudrate))
                    {
                        // send
                        while (_running)
                        {
                            if (!port.Write(sendBytes))
                                break;

                            //port.Flush();
                            System.Threading.Thread.Sleep(100);
                        }
                    }

                }
                catch (Exception e)
                {
                    Log.e("Exception: {0}, It can't open a port., {0}", model.CurrentPort.Name, e);
                    System.Threading.Thread.Sleep(600);
                }
            }
            _running = false;
            Log.i("WaitPort Thread Exit");
        }

        // try enter normal download mode
        //private void TryNDownloadMode(object arg)
        //{
        //    _running = true;
        //    Log.i("WaitPort Thread Started.");

        //    var model = arg as NormalModel;

        //    // request reboot edl packet
        //    while (_running)
        //    {
        //        try
        //        {
        //            if (model.VersionInfo.Count > 1) // 유효한 버젼 정보를 받았을 경우..
        //            {
        //                Dictionary<string, string> result;
        //                if (model.TargetIf.DiagRequest(string.Format("{0}ereboot{0}{0}", (char)TDiagMethod.Run),
        //                    out result, model.CurrentPort.Name) > 0)
        //                {
        //                    break;
        //                }
        //            }
        //            System.Threading.Thread.Sleep(200);
        //        }
        //        catch (Exception e)
        //        {
        //            Log.e("Exception: Diagnostic request for reboot edl, {0}", e);
        //            System.Threading.Thread.Sleep(600);
        //        }
        //    }

        //    // wait qdloader port
        //    while(_running)
        //    {
        //        // add some codes...
        //        //...

        //        System.Threading.Thread.Sleep(100);
        //    }

        //    _running = false;
        //    Log.i("WaitPort Thread Exit");
        //}
    }
}
