using Lge.Tools.Download.Models;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Lge.Tools.Download
{
    public class EmergencyModel : INotifyPropertyChanged, Models.ITabModel
    {
        #region INotifyPropertyChanged inteface 
        public event PropertyChangedEventHandler PropertyChanged;

        private void InvokePropertyChanged(String aName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(aName));
            }
        }
        #endregion INotifyPropertyChanged inteface 

        #region ITabModel
        public bool GMSignedEmer { get; set; } // jwoh GM Signed or Non Signed
        public Models.TabType TabId { get { return Models.TabType.Emergency; } }
        
        public void TabActiveChanged(bool aActived)
        {
            if (aActived)
            {                
                ImageItem.AllErase = this.AllErase;
                ImageItem.Reset = this.Reboot ? 3 : 0;
                Log.LogLevel = this.LogLevel;
                ImageItem.SkipEraseEfs = this.SkipEraseEfs;
                ImageItem.SelFirehose = this.SelFirehose;         // jwoh add GM Signing
                ImageItem.SelBaudrate = this.SelBaudrate;         // jwoh add Baudrate
                ImageItem.SelModel = this.SelModel;         // jwoh add Model
                ImageItem.ECCCheck = this.ECCCheck;         // jwoh add ECC Check

                if (File.Exists(_configPath))
                    ImageItem.Dir = new FileInfo(this.ConfigPath).DirectoryName;

                StatusMessage = "<Emergency Downloading Mode>";

                Log.LogEvent += Log_LogEvent;

                SerialportWatcher_SerialPortsChangedEvent(true, null);
                SerialportWatcher.SerialPortsChangedEvent += SerialportWatcher_SerialPortsChangedEvent;
            }
            else
            {
                SerialportWatcher.SerialPortsChangedEvent -= SerialportWatcher_SerialPortsChangedEvent;
                Log.LogEvent -= Log_LogEvent;
            }
        }

        #region Options
        public bool AllErase
        {
            get { return Properties.Settings.Default.EAllErase; }
            set
            {
                if (Properties.Settings.Default.EAllErase != value)
                {
                    Properties.Settings.Default.EAllErase = value;
                    ImageItem.AllErase = value;
                    
                    InvokePropertyChanged("AllErase");
                }
            }
        }

        public bool Reboot
        {
            get { return false; } //Properties.Settings.Default.EReboot; }
            set
            {
               // if (Properties.Settings.Default.EReboot != value)
                {
                //    Properties.Settings.Default.EReboot = value;
                //    ImageItem.Reset = value ? 3 : 0;
                    
                //    InvokePropertyChanged("Reboot");
                }
            }
        }

        public int LogLevel
        {
            get
            {
                return Properties.Settings.Default.ELogLevel;
            }
            set
            {
                if (Properties.Settings.Default.ELogLevel != value)
                {
                    Properties.Settings.Default.ELogLevel = value;
                    Log.LogLevel = value;

                    InvokePropertyChanged("LogLevel");
                }
            }
        }

        public bool SkipEraseEfs
        {
            get { return false; }// Properties.Settings.Default.EskipEraseEfs; }
            set
            {
               // if (Properties.Settings.Default.EskipEraseEfs != value)
                {
               //     Properties.Settings.Default.EskipEraseEfs = value;
               //     ImageItem.SkipEraseEfs = value;
                }
            }
        }

        public bool UseMicomUpdate
        {
            get { return false; }
            set {  }
        }

        public bool UseEfsBackup
        {
            get { return false; }// Properties.Settings.Default.EUseEfsBackup; }
            set
            {
               // if (Properties.Settings.Default.EUseEfsBackup != value)
                {
              //      Properties.Settings.Default.EUseEfsBackup = value;
              //      this.SkipEraseEfs = value;

              //      InvokePropertyChanged("UseEfsBackup");
              //      InvokePropertyChanged("UseUpdateOnly");
                }
            }
        }

        public bool UseUpdateOnly
        {
            get { return !this.UseEfsBackup; }
            set
            {
             //   if (this.UseEfsBackup == value)
                {
             //       this.UseEfsBackup = !this.UseEfsBackup;
                }
            }
        }

        public bool CanEfsBackup
        {
            get { return false; }
        }

        public bool FotaErase
        {
            get { return false; }
            set { }
        }

        // jwoh CCM Only function [
        public bool CCMOnly 
        {
            get { return false; }
            set { }
        }
        // jwoh CCM Only function ]
        // jwoh add User/Factory mode [
        public int SelBoard
        {
            get { return 0; }//Properties.Settings.Default.ESelBoard; }
            set
            {
                // if (Properties.Settings.Default.ESelBoard != value)
                {
                    // Properties.Settings.Default.ESelBoard = value;
                    // ImageItem.SelBoard = value;

                    // InvokePropertyChanged("SelBoard");
                }
            }
        }
        // jwoh add User/Factory mode ]
        // jwoh add GM Signing [
        public int SelFirehose
        {
            get
            {
                return Properties.Settings.Default.ESelFirehose;
            }
            set
            {
                if (Properties.Settings.Default.ESelFirehose != value)
                {
                    Properties.Settings.Default.ESelFirehose = value;

                    ImageItem.SelFirehose = value;

                    InvokePropertyChanged("SelFirehose");
                }
            }
        }
        // jwoh add GM Signing ]
        // jwoh add Model [
        public int SelModel
        {
            get { return 0; }//Properties.Settings.Default.ESelModel; }
            set
            {
                // if (Properties.Settings.Default.ESelModel != value)
                {
                    // Properties.Settings.Default.ESelModel = value;
                    // ImageItem.SelModel = value;

                    // InvokePropertyChanged("SelModel");
                }
            }
        }
        // jwoh add Model ]
        // jwoh add Baudrate [
        public int SelBaudrate
        {
            get
            {
                return Properties.Settings.Default.ESelBaudrate;
            }
            set
            {
                if (Properties.Settings.Default.ESelBaudrate != value)
                {
                    Properties.Settings.Default.ESelBaudrate = value;

                    ImageItem.SelBaudrate = value;

                    InvokePropertyChanged("SelBaudrate");
                }
            }
        }
        // jwoh add Baudrate ]
        // jwoh add ECCCheck [
        public bool ECCCheck
        {
            get { return false; }
            set { }
        }
        // jwoh add ECCCheck ]

        public bool UseModeChange { get; set; }

        public bool DiagRequestEnable { get { return false; } set { } }

        public bool IsIdle { get { return this.FbMode == FBMode.None; } }

        public Visibility DonlyVisible { get { return Visibility.Collapsed; } }
        public Visibility NonlyCollapsed { get { return Visibility.Visible; } } // jwoh add Micom Download only
        public Visibility MonlyVisible { get { return Visibility.Collapsed; } }
        public Visibility MonlyCollapsed { get { return Visibility.Visible; } }

        #endregion Options
        #endregion ITabModel

        public EmergencyModel()
        { 
        }

        public EmergencyModel(MainModel aMain)
        {
            this.Main = aMain;

            if (File.Exists(Helper.TempConfigFile(this)))
                File.Delete(Helper.TempConfigFile(this));

            DownloadCommand = new DelegateCommand(ExecuteDownload, CanDownload);
            DumpCommand = new DelegateCommand(ExecuteDump, CanDownload);

            this.ConfigPath = Properties.Settings.Default.ConfigPath;
        }

        public MainModel Main { get; private set; }

        public DelegateCommand DownloadCommand { get; private set; }
        public DelegateCommand DumpCommand { get; private set; }

        private StringBuilder _errMessages = new StringBuilder();
        public string ErrorMessages
        {
            get { return _errMessages.ToString(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                    _errMessages.Clear();
                else
                    _errMessages.AppendLine(value);

                InvokePropertyChanged("ErrorMessages");
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    InvokePropertyChanged("StatusMessage");
                }
            }
        }

        private string _totalStatus = "";
        public string TotalStatus
        {
            get { return _totalStatus; }
            set
            {
                if (_totalStatus != value)
                {
                    _totalStatus = value;
                    InvokePropertyChanged("TotalStatus");

                }
            }
        }

        private void Log_LogEvent(LogLevels aLevel, LogItem aItem)
        {
            if (aLevel == LogLevels.Error)
            {
                ErrorMessages = string.Format("[{0}] {1}", aItem.Time.ToString("HH:mm:ss"), aItem.Message);
            }
            else if ((int)aLevel >= Log.LogLevel)
            {
                StatusMessage = aItem.Message;
            }
        }

        private int _totalProgress = 0;
        public int TotalProgress
        {
            get { return _totalProgress; }
            set
            {
                if (_totalProgress != value)
                {
                    _totalProgress = value;
                    InvokePropertyChanged("TotalProgress");
                }
            }
        }
        private int _fileProgress = 0;
        public int FileProgress
        {
            get { return _fileProgress; }
            set
            {
                if (_fileProgress != value)
                {
                    _fileProgress = value;
                    InvokePropertyChanged("FileProgress");
                }
            }
        }

        private string _curFile = string.Empty;
        public string CurFileName
        {
            get { return _curFile; }
            set
            {
                if (_curFile != value)
                {
                    _curFile = value;
                    InvokePropertyChanged("CurFileName");
                }
            }
        }

        private SerialportWatcher.PortInfo _curPort = new SerialportWatcher.PortInfo();
        public SerialportWatcher.PortInfo CurrentPort
        {
            get { return _curPort; }
            private set
            {
                if (!_curPort.Equals(value))
                {
                    if (value != null)
                        _curPort = value;
                    else
                        _curPort = new SerialportWatcher.PortInfo();

                    InvokePropertyChanged("CurrentPort");
                    InvokePropertyChanged("DeviceImage");

                    this.DownloadCommand.RaiseCanExecuteChanged();
                    this.DumpCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool DebugportPresented
        {
            get
            {
                if (_curPort != null)
                {
                    return _curPort.Kind == SerialportWatcher.PortKind.Serial;
                }
                return false;
            }
        }

        public bool QdloadportPresented
        {
            get
            {
                if (_curPort != null)
                {
                    return _curPort.Kind == SerialportWatcher.PortKind.QDLoader;
                }
                return false;
            }
        }

        private string _configPath;
        private List<ImageItem> _listItems = new List<ImageItem>();

        public List<ImageItem> Items { get { return _listItems; } }

        public List<ImageItem> UsedItems
        {
            get
            {
                // 중복 아이템은 제외.
                var list = new List<ImageItem>();
                foreach(var m in _listItems)
                {
                    if (m.IsExist && m.Protocol == QProtocol.Firehose)
                    {
                        if (!list.Any(x => x.Name == m.Name)) // jwoh FileName->Name
                            list.Add(m);
                    }
                }
                return list;
            }
        }

        public string ConfigPath
        {
            get { return File.Exists(_configPath) ? _configPath : "( Select images folder contains configuration.xml )"; }
            set
            {
                if (File.Exists(value))
                {
                    if (SetConfigXml(value))
                    {
                        _configPath = value;
                        Properties.Settings.Default.ConfigPath = _configPath;
                        InvokePropertyChanged("ConfigPath");
                        InvokePropertyChanged("Editable");
                        this.DownloadCommand.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        public void SelectConfiguration()
        {
            OpenFileDialog fdlg = new OpenFileDialog()
            {
                FileName = Helper.ConfigXmlFileName,
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = _configPath,
                Title = "Select a configuration.xml file exist in images folder",
                Filter = "Xml file(*.xml)|*.xml|All file(*.*)|*.*",
                Multiselect = false,
            };

            if ((fdlg.ShowDialog() ?? false) == false)
                return;

            try
            {
                this.ConfigPath = fdlg.FileName;
            }
            catch(Exception e)
            {
                Log.e("Failed configuration Loading - {0}", e);
            }

        }

        bool SetConfigXml(string aXmlPath)
        {
            if (File.Exists(aXmlPath))
            {
                var dir = new FileInfo(aXmlPath).DirectoryName;
                _listItems = ImageItem.Load(aXmlPath);

                if (_listItems == null || _listItems.Count == 0)
                {
                    Log.e("Selected '{0}' it will not be processed. Please choose a valid file ({1}).", aXmlPath, Helper.ConfigXmlFileName);
                    return false;
                }

                // configuration.xml을 appdata 영역으로 복사한다. (다중 사용자 접근, 읽기 전용 등 문제가 없도록)
                if (File.Exists(Helper.TempConfigFile(this)))
                    File.Delete(Helper.TempConfigFile(this));

                File.Copy(aXmlPath, Helper.TempConfigFile(this));

                //  없는 파일 파티션은 지우지 않고 파일이 있는 부분만 지운다.
                Log.v("=== Configuration Settings ===");
                ImageItem.Dir = dir;

                foreach (var item in _listItems)
                {
                    item.IsExist = File.Exists(Path.Combine(ImageItem.Dir, item.FileName));
                    item.Use = item.IsExist;
                    item.Erase = item.Use;
                    if (item.Name == "FOTA_SELFTEST")
                    {
                        if (item.FileName.Contains("none_secure.img"))
                        {
                            if (aXmlPath.Contains("s.lge"))
                                this.GMSignedEmer = false;
                            else
                                this.GMSignedEmer = true;
                        }
                        else
                        {
                            this.GMSignedEmer = false;
                        }
                    }
                    Log.v("Partition ID={0} Name={1}, FileName={2}", item.Id, item.Name,
                                                item.Use ? item.FileName : "(not exist!)");
                }
                this.LogLevel = Log.LogLevel;
                Log.v("Erase: {0}", ImageItem.AllErase ? "All" : "Partial");
                Log.i("Configuration is loaded.");

                InvokePropertyChanged("UsedItems");

                return true;
            }

            Log.e("Configuration loading is failed.");
            return false;
        }

        public IEnumerable NormalPorts
        {
            get
            {
                var list = new ArrayList();
                foreach (var port in SerialportWatcher.GetPorts(SerialportWatcher.PortKind.Serial))
                {
                    list.Add( new PortSelect(this, port) );
                }
                return list;
            }
        }

        public Visibility PortListVisible
        {
            get
            {
                if (!this.QdloadportPresented && SerialportWatcher.GetPorts(SerialportWatcher.PortKind.Serial).Count > 1)
                {
                    InvokePropertyChanged("NormalPorts");
                    return Visibility.Visible;
                }
                return  Visibility.Collapsed;
            }
        }

        class PortSelect
        {
            public PortSelect(EmergencyModel aModel, SerialportWatcher.PortInfo aPort)
            {
                _model = aModel;
                Port = aPort;
            }
            public SerialportWatcher.PortInfo Port { get; private set; }
            public bool Checked
            {
                get { return _model.CurrentPort.Equals(this.Port); }
                set
                {
                    if (value)
                    {
                        _model.CurrentPort = this.Port;
                    }
                }
            }
            EmergencyModel _model;
        }

        private void SerialportWatcher_SerialPortsChangedEvent(bool aInserted, SerialportWatcher.PortInfo aChangedPort)
        {
            this.UIThread(() =>
           {
               var qport = SerialportWatcher.GetPorts(SerialportWatcher.PortKind.QDLoader);
               if (qport.Count > 0)
               {
                   this.CurrentPort = qport[0];
                   Log.i("QDLoader port is presented ({0})", this.CurrentPort.Name);
               }
               else
               {
                   var dport = SerialportWatcher.GetPorts(SerialportWatcher.PortKind.Serial);
                   if (dport.Count > 0)
                   {
                       if (this.CurrentPort.Kind != SerialportWatcher.PortKind.Serial)
                           this.CurrentPort = dport[0];
                       else if (aInserted == false && this.CurrentPort.Equals(aChangedPort)) // 제거 시 갱신
                            this.CurrentPort = dport[0];

                       Log.i("Debug port is presented ({0})", this.CurrentPort.Name);
                   }
                   else
                   {
                       if (this.CurrentPort.Kind != SerialportWatcher.PortKind.None)
                           this.CurrentPort = new SerialportWatcher.PortInfo();
                       Log.i("Port is none");
                   }
               }
               InvokePropertyChanged("PortListVisible");
           });
        }

        private void TargetIf_AppProgressEvent(ProgressArgs arg)
        {
            int idx = _listItems.FindIndex(x => x.Id == arg.ImageID && (int)x.Protocol == arg.ExtraInfo);

            if (idx >= 0 && arg.TotalBytes > 0) // 유효한 파일 다운로드 중..
            {
                int progress = (int)(arg.SentBytes * 100 / arg.TotalBytes);

                UpdateProgress(idx, progress);
            }
            else if (arg.ExtraInfo == (int)QProtocol.All)
            {
                if (_targetIf != null)
                {
                    _targetIf.AppProgressEvent -= TargetIf_AppProgressEvent;
                }

                if (FbMode == FBMode.Download && arg.TotalBytes > 0)
                    Main.PrintImagesList(_listItems);

                Log.i("All {0} is Completed.", FbMode == FBMode.Download ? "Download" : "Dump");
                this.TotalStatus = string.Format("{0} is completed {1}.", 
                    FbMode == FBMode.Download ? "<download>" : "<dump>",
                    arg.TotalBytes > 0 ? "successfully" : "but failed");

                FbMode = FBMode.None;
                bool ok = arg.TotalBytes > 0;
                Pages.TopMessageBox.ShowMsg(
                    string.Format("A task is completed.\n\n<Result>\n\t{0}", ok ? "Success" : "Fail - refer logs"), ok);

                this.UIThread(delegate
                {
                    DownloadCommand.RaiseCanExecuteChanged();
                    DumpCommand.RaiseCanExecuteChanged();
                });
            }
        }

        private void UpdateProgress(int aIndex, int aProgress)
        {
            var item = _listItems[aIndex];
            if (item.Progress == aProgress)
                return;

            item.Progress = aProgress;

            int count = 0;
            int progress = 0;
            foreach (var m in _listItems)
            {
                if ((FbMode == FBMode.Download && m.Use)
                    || (FbMode == FBMode.Dump && m.Dump))
                {
                    progress += m.Progress;
                    count++;
                }
            }

            this.CurFileName = _listItems[aIndex].FileName;
            this.FileProgress = aProgress;
            this.TotalProgress = progress / count;
            this.TotalStatus = string.Format("{2}  {0}/{1}", item.Name, item.FileName, 
                FbMode == FBMode.Download ? "<download>" : "<dump>");

            Log.v("Progress updated: {0} => {1}%", _listItems[aIndex], _listItems[aIndex].Progress);
        }

        private bool _dumpEnable = false;
        public bool DumpEnable
        {
            get { return _dumpEnable; }
            set
            {
                if (_dumpEnable != value)
                {
                    _dumpEnable = value;
                    InvokePropertyChanged("DumpEnable");
                    InvokePropertyChanged("ShowDump");
                }
            }
        }

        public Visibility ShowDump
        {
            get
            {
                return _dumpEnable ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public FBMode FbMode
        {
            get { return _fbMode; }
            private set
            {
                if (_fbMode != value)
                {
                    _fbMode = value;
                    Main.IsIdle = _fbMode == FBMode.None;
                    InvokePropertyChanged("IsIdle");
                }
            }
        }

        private FBMode _fbMode = FBMode.None;
        TargetWrapper _targetIf = null;

        public TargetWrapper TargetIf { get { return _targetIf; } }

        private void ExecuteDownload(object arg)
        {
            Log.i("------------------- Run Download -------------------");
            if (this.GMSignedEmer == true && ImageItem.SelFirehose == 0)
            {
                //Log.MsgBox("This is a GM signed image. Please check the firehose file.");
                //return;
            }
            if (this.GMSignedEmer == false && ImageItem.SelFirehose == 1)
            {
                //if (ImageItem.Dir.Contains("s.lge"))
                //    Log.MsgBox("This is a lge signed image. Please check the firehose file.");
                //else
                //    Log.MsgBox("This is a non signed image. Please check the firehose file.");
                //return;
            }
            try
            {
                this.TotalProgress = 0;
                this.FileProgress = 0;
                this.TotalStatus = "";

                string BaudrateInfo = "\nEmergency Download나 Image Dump를 수행할 때에는\n다음과 같이 Baudrate을 설정해야 합니다.\n1. Gen11 TCP19 GA Release Version : 115,200 bps\n2. Others : 921,600 bps\n\nYou must set the baudrate as following\nif you are going to do \"Emergency Download\" or \"Image Dump\"\n1. Gen11 TCP19 GA Release Version : 115,200 bps\n2. Others : 921,600 bps\n";
                string BaudrateCaption = "Baudrate Info";
                MController.InfoPopup(BaudrateInfo, BaudrateCaption); // jwoh Baudrate Info popup

                // dispse target wrapper
                if (_targetIf != null)
                {
                    _targetIf.Dispose();
                    _targetIf = null;
                }

                // reset progress value    
                int useCount = 0;
                _listItems.ForEach(x => {
                    x.Progress = 0;    if (x.Use) useCount++;
                });

                if (useCount == 0)
                {
                    Log.MsgBox("No images will be downloading.");
                    return;
                }
                // save configuration xml for library
                // set fota erase option 
                var fxitem = _listItems.SingleOrDefault( fx => fx.Name == "FOTA_SELFTEST");
                if (fxitem != null)
                    fxitem.Erase = this.FotaErase;
                if (!ImageItem.Save(Helper.TempConfigFile(this), _listItems))
                {
                    Log.MsgBox("Error: saving configuration.xml");
                    return;
                }
                // initialize target wrapper
                _targetIf = new TargetWrapper();
                _targetIf.AppLogEvent += _targetIf_AppLogEvent;
                if (!_targetIf.Load(Helper.ProtocolDllPath))
                {
                    Log.MsgBox("Error: loading protocol library");
                    return;
                }
                _targetIf.AppProgressEvent += TargetIf_AppProgressEvent;
                // wait qdloader mode
                if (Pages.WaitPortBox.Wait(Models.TabType.Emergency, this) == false)
                    return;
                // start download
                FbMode = FBMode.Download;
                this.DownloadCommand.RaiseCanExecuteChanged();

                // Print Download option to log
                Log.i("------------------- Prepare : options list -------------------");
                Log.i("[Option-1] All Erase:{0}, Reboot:{1}, LogLevel:{2}", ImageItem.AllErase, ImageItem.Reset, Log.LogLevel);
                Log.i("[Option-2] Erase except EFS:{0}", ImageItem.SkipEraseEfs);

                System.Threading.Thread.Sleep(3 * 1000); // for micom usb stable control.
                _targetIf.RundDownload(this.CurrentPort.Name, Helper.TempConfigFile(this));

            }
            catch (Exception e)
            {
                Log.e("Command (Download) invoke Error, Exception: {1}", e);
            }            
        }

        private void _targetIf_AppLogEvent(LogLevels aLogLevel, string aMsg)
        {
            if (Main.CurrentTabIndex == (int)this.TabId)
            {
                Log.writeLine(aLogLevel, aMsg);
            }
        }

        private void ExecuteDump(object arg)
        {
            Log.i("------------------- Run Dump -------------------");
            string downDir = ImageItem.Dir;
            try
            {
                this.TotalProgress = 0;
                this.FileProgress = 0;
                this.TotalStatus = "";
                // dispse target wrapper
                if (_targetIf != null)
                {
                    _targetIf.Dispose();
                    _targetIf = null;
                }
                
                // reset progress value    
                _listItems.ForEach(x => {
                    x.Progress = 0;
                    x.Dump = true;
                });

                // input save dir
                var fdlg = new System.Windows.Forms.FolderBrowserDialog()
                {
                    SelectedPath = Properties.Settings.Default.saveDir,
                    ShowNewFolderButton = true,
                };
                if (fdlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                
                Properties.Settings.Default.saveDir = fdlg.SelectedPath;
                ImageItem.Dir = fdlg.SelectedPath;

                // save configuration xml for library
                if (!ImageItem.Save(Helper.TempConfigFile(this), _listItems))
                {
                    Log.MsgBox("Error: saving configuration.xml");
                    return;
                }

                // initialize target wrapper
                _targetIf = new TargetWrapper();
                _targetIf.AppLogEvent += _targetIf_AppLogEvent;
                if (!_targetIf.Load(Helper.ProtocolDllPath))
                {
                    Log.MsgBox("Error: loading protocol library");
                    return;
                }
                _targetIf.AppProgressEvent += TargetIf_AppProgressEvent;
                // wait qdloader mode
                if (Pages.WaitPortBox.Wait(Models.TabType.Emergency, this) == false)
                    return;
                // start download
                FbMode = FBMode.Dump;
                this.DumpCommand.RaiseCanExecuteChanged();

                _targetIf.RundReadback(this.CurrentPort.Name, Helper.TempConfigFile(this));

            }
            catch (Exception e)
            {
                Log.e("Command (Dump) invoke Error, Exception: {1}", e);
            }
            finally
            {
                ImageItem.Dir = downDir;
            }
        }

        public bool Editable { get { return File.Exists(Helper.TempConfigFile(this)); } }

        public Uri DeviceImage
        {
            get
            {
                string iname = "notarget.png";
                if (this.QdloadportPresented)
                    iname = "targetdn.png";
                else if (this.DebugportPresented)
                    iname = "targeton.png";

                return new Uri(@"pack://application:,,,/Gen11Downloader.v2;component/Resources/" + iname, UriKind.Absolute);
            }
        }

        private bool CanDownload(object arg)
        {
            try
            {
                InvokePropertyChanged("Editable");

                if (File.Exists(this.ConfigPath) && FbMode == FBMode.None && (this.DebugportPresented || this.QdloadportPresented))
                    return true;
            }
            catch (Exception e)
            {
                Log.e("Check valid (Download) Error, Exception: {1}",  e);
            }

            return false;
        }

    }
}
