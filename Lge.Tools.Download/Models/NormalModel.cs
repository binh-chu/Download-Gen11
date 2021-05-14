using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace Lge.Tools.Download
{
    public class NormalModel : INotifyPropertyChanged, Models.ITabModel
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
        public Models.TabType TabId { get { return Models.TabType.Normal; } }

        public void TabActiveChanged(bool aActived)
        {
            if (aActived)
            {
                ImageItem.AllErase = this.AllErase;
                ImageItem.Reset = 1; // 항상 리부팅
                Log.LogLevel = this.LogLevel;
                ImageItem.SkipEraseEfs = this.SkipEraseEfs;
                ImageItem.SelFirehose = this.SelFirehose; // jwoh add GM Signing
                ImageItem.SelBaudrate = this.SelBaudrate; // jwoh add Baudrate
                ImageItem.SelModel = this.SelModel; // jwoh add Model
                ImageItem.ECCCheck = this.ECCCheck;         // jwoh add ECC Check

                if (File.Exists(_configPath))
                    ImageItem.Dir = new FileInfo(this.ConfigPath).DirectoryName;

                StatusMessage = "<Normal Downloading Mode>";

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
            get { return false; } //Properties.Settings.Default.NAllErase; }
            set
            {
                // if (Properties.Settings.Default.NAllErase != value)
                {
                    // Properties.Settings.Default.NAllErase = value;
                    // ImageItem.AllErase = value;
                    
                    // InvokePropertyChanged("AllErase");
                }
            }
        }

        public int LogLevel
        {
            get
            {
                return Properties.Settings.Default.NLogLevel;
            }
            set
            {
                if (Properties.Settings.Default.NLogLevel != value)
                {
                    Properties.Settings.Default.NLogLevel = value;
                    Log.LogLevel = value;
                    
                    InvokePropertyChanged("LogLevel");
                }
            }
        }

        public bool SkipEraseEfs
        {
            get { return false; }//Properties.Settings.Default.NskipEraseEfs; }
            set
            {
                // if (Properties.Settings.Default.NskipEraseEfs != value)
                {
                    // Properties.Settings.Default.NskipEraseEfs = value;
                    // ImageItem.SkipEraseEfs = value;
                }
            }
        }

        public bool UseMicomUpdate
        {
            get { return false; }
            set { }
        }

        public bool UseEfsBackup
        {
            get { return false; }//Properties.Settings.Default.NUseEfsBackup; }
            set
            {
                // if (Properties.Settings.Default.NUseEfsBackup != value)
                {
                    // Properties.Settings.Default.NUseEfsBackup = value;
                    // this.SkipEraseEfs = value;

                    // InvokePropertyChanged("UseEfsBackup");
                    // InvokePropertyChanged("UseUpdateOnly");
                }
            }
        }

        public bool UseUpdateOnly
        {
            get { return !this.UseEfsBackup; }
            set
            {
                // if (this.UseEfsBackup == value)
                {
                    // this.UseEfsBackup = !this.UseEfsBackup;
                }
            }
        }

        bool _diagReqEnagle = false;
        public bool DiagRequestEnable
        {
            get
            {
                return this.VersionInfo.Count > 1 && Main.IsIdle;
            }
            set
            {
                if (this.DiagRequestEnable != _diagReqEnagle)
                {
                    _diagReqEnagle = !_diagReqEnagle;

                    InvokePropertyChanged("DiagRequestEnable");

                }
            }
        }

        public bool IsIdle { get { return this.FbMode == FBMode.None; } }

        public bool CanEfsBackup
        {
            get { return false; }
        }

//        bool _fotaErase = false;
        public bool FotaErase
        {
            get { return false; } // _fotaErase
            set
            {
                // if (_fotaErase != value)
                {
                    // _fotaErase = value;
                    // InvokePropertyChanged(nameof(FotaErase));
                }
            }
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
            get { return 0; }//Properties.Settings.Default.NSelBoard; }
            set
            {
                // if (Properties.Settings.Default.NSelBoard != value)
                {
                    // Properties.Settings.Default.NSelBoard = value;
                    // ImageItem.SelBoard = value;

                    // InvokePropertyChanged("SelBoard");
                }
            }
        }
        // jwoh add User/Factory mode ]
        // jwoh add GM Signing [
        public int SelFirehose
        {
            get { return 0; }//Properties.Settings.Default.NSelFirehose; }
            set
            {
                // if (Properties.Settings.Default.NSelFirehose != value)
                {
                    // Properties.Settings.Default.NSelFirehose = value;
                    // ImageItem.SelFirehose = value;

                    // InvokePropertyChanged("SelFirehose");
                }
            }
        }
        // jwoh add GM Signing ]
        // jwoh add Model [
        public int SelModel
        {
            get
            {
                return Properties.Settings.Default.NSelModel;
            }
            set
            {
                if (Properties.Settings.Default.NSelModel != value)
                {
                    Properties.Settings.Default.NSelModel = value;
                    ImageItem.SelModel = value;

                    InvokePropertyChanged("SelModel");
                }
            }
        }
        // jwoh add Model ]
        // jwoh add Baudrate [
        public int SelBaudrate
        {
            get
            {
                return Properties.Settings.Default.NSelBaudrate;
            }
            set
            {
                if (Properties.Settings.Default.NSelBaudrate != value)
                {
                    Properties.Settings.Default.NSelBaudrate = value;

                    ImageItem.SelBaudrate = value;

                    InvokePropertyChanged("SelBaudrate");
                }
            }
        }
        // jwoh add GM Signing ]
        // jwoh add ECCCheck [
        public bool ECCCheck
        {
            get { return false; }
            set { }
        }
        // jwoh add ECCCheck ]

        public Visibility DonlyVisible { get { return Visibility.Collapsed; } }
        public Visibility NonlyCollapsed { get { return Visibility.Collapsed; } }
        public Visibility MonlyVisible { get { return Visibility.Collapsed; } }
        public Visibility MonlyCollapsed { get { return Visibility.Visible; } }

        #endregion Options
        #endregion ITabModel

        public NormalModel()
        {
        }

        public NormalModel(MainModel aMain)
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
        private string _statusMessage;

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
                this.ErrorMessages = string.Format("[{0}] {1}", aItem.Time.ToString("HH:mm:ss"), aItem.Message);
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
                    {
                        _curPort = new SerialportWatcher.PortInfo();
                        //this.FileProgress = 0;
                        //this.TotalProgress = 0;
                        //this.StatusMessage = "";
                    }

                    InvokePropertyChanged("CurrentPort");
                    InvokePropertyChanged("DiagVisible");
                    InvokePropertyChanged("DeviceImage");

                    this.DownloadCommand.RaiseCanExecuteChanged();
                    this.DumpCommand.RaiseCanExecuteChanged();
                    if (_curPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                    {
                        UpdateVersionInfo(100);
                    }
                    else
                        this.VersionInfo = null;
                }
            }
        }

        private SerialportWatcher.PortInfo _micomPort = new SerialportWatcher.PortInfo();
        public SerialportWatcher.PortInfo MicomPort
        {
            get { return _micomPort; }
            private set
            {
                if (!_micomPort.Equals(value))
                {
                    if (value != null)
                        _micomPort = value;
                    else
                        _micomPort = new SerialportWatcher.PortInfo();

                    InvokePropertyChanged("MicomPort");
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

        public bool DiagportPresented
        {
            get
            {
                if (_curPort != null)
                {
                    return _curPort.Kind == SerialportWatcher.PortKind.Diagnostic;
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

        public Visibility DiagVisible
        {
            get
            {
                return DiagportPresented ? Visibility.Visible : Visibility.Collapsed;
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
                        if (!list.Any(x => x.Name == m.Name)) // jwoh temp FileName->Name
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
                        this.DumpCommand.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        private List<VersionItem> _verInfo = null;
        public List<VersionItem> VersionInfo
        {
            get
            {
                if (_verInfo == null)
                {
                    var vis = new List<VersionItem>();
                    vis.Add(new VersionItem("None", "No Version Information"));

                    // retry read version info
                    if (_versionChecking == false && CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                    {
                        UpdateVersionInfo();
                    }

                    return vis;
                }
                return _verInfo;
            }
            set
            {
                if (_verInfo != value)
                {
                    _verInfo = value;
                    InvokePropertyChanged("VersionInfo");
                    this.DownloadCommand.RaiseCanExecuteChanged();
                    this.DumpCommand.RaiseCanExecuteChanged();

                    DiagRequestEnable = true;
                }
            }
        }

        private bool _versionChecking = false;
        private void UpdateVersionInfo(int aDelayMS = 0)
        {
            if (_curPort.Kind != SerialportWatcher.PortKind.Diagnostic || _versionChecking || this.FbMode != FBMode.None)
                return;

            _versionChecking = true;
            _verInfo = null;

            // 새로 버젼 정보를 읽어온다.
            new Task(() => {
                System.Threading.Thread.Sleep(aDelayMS);
                Log.i("Try request Version info to target through diagnostic port");
                Stopwatch wtime = new Stopwatch();
                wtime.Start();
                const int MAX_TRY_TIME = 35 * 1000;
                while (_versionChecking && wtime.ElapsedMilliseconds < MAX_TRY_TIME)
                {
                    if (_curPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                    {
                        if (ReadVersionInfo())
                        {
                            _versionChecking = false;
                            break;
                        }
                        System.Threading.Thread.Sleep(700);
                    }
                    else
                    {
                        _versionChecking = false;
                        break;
                    }
                }
                if (_versionChecking)
                {
                    StatusMessage = "Modem is not ready. reboot and retry...";
                    Log.e("Modem is not ready. May be need rebooting target !");
                    _versionChecking = false;
                }
            }).Start();
           
        }

        Dictionary<string, string>  _verNames = new Dictionary<string, string>();

        string GetUiVersionName(string aItem)
        {
            if (_verNames.Count == 0)
            {
                _verNames.Add("Compile", "Compile Date");
                _verNames.Add("Release", "Release Date");
                _verNames.Add("Software_Ver", "Software Version");
                _verNames.Add("Firmware_Rev", "Firmware Revision");
                _verNames.Add("HW_Rev", "Hardware Revision");
                _verNames.Add("HW_Area", "HW Area");
                _verNames.Add("MCFG_Ver", "MCFG Version");
            }

            if (_verNames.ContainsKey(aItem))
                return _verNames[aItem];

            return aItem;
        }

        public bool ReadVersionInfo()
        {
            if (_targetIf == null)
            {
                _targetIf = new TargetWrapper();
                _targetIf.AppLogEvent += _targetIf_AppLogEvent;
                if (!_targetIf.Load(Helper.ProtocolDllPath))
                {
                    Log.e("The DiagRequest is interrupted by exceptions at library loading.");
                    return false;
                }
            }
           
            Dictionary<string, string> result;
            if (_targetIf.DiagRequest(string.Format("{0}version{0}{0}", (char)TDiagMethod.Get),
                out result, _curPort.Name) != 0) // success
            {
                if (result.Count > 0) // 읽어 왔으나 타켓이 아직 준비가 안된 상태 일 경우.
                {
                    var verItems = new List<VersionItem>();
                    foreach (var r in result)
                    {
                        verItems.Add(new VersionItem(GetUiVersionName(r.Key), r.Value));
                    }
                    this.VersionInfo = verItems;
                    Log.i("Version Infomation is read from taget.");
                    return true;
                }
            }

            return false;
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
                    //                    item.Use = item.IsExist;
                    //                    item.Erase = item.Use;
                    if (item.FileName == "mdm9640-micom.ubi")
                    {
                        item.Use = true;
                        item.Erase = true;
                    }
                    else
                    {
                        item.Use = false;
                        item.Erase = false;
                    }
                    Log.v("Partition ID={0} Name={1}, FileName={2}", item.Id, item.Name,
                                                item.Use ? item.FileName : "(not exist!)");
                }
                Log.v("Erase: {0}", ImageItem.AllErase ? "All" : "Partial");
                Log.i("Configuration is loaded.");

                InvokePropertyChanged("UsedItems");

                return true;
            }

            Log.i("Configuration loading is failed.");
            return false;
        }

        public IEnumerable SerialNormalPorts
        {
            get
            {
                var list = new ArrayList();
                foreach (var port in SerialportWatcher.GetPorts(SerialportWatcher.PortKind.Serial))
                {
                    list.Add(new NormalPortSelect(this, port));
                }
                return list;
            }
        }

        public Visibility SerialPortListVisible
        {
            get
            {
                if (!this.QdloadportPresented && SerialportWatcher.GetPorts(SerialportWatcher.PortKind.Serial).Count > 1)
                {
                    InvokePropertyChanged("SerialNormalPorts");
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        class NormalPortSelect
        {
            public NormalPortSelect(NormalModel aModel, SerialportWatcher.PortInfo aPort)
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
            public bool CheckedMicom
            {
                get { return _model.MicomPort.Equals(this.Port); }
                set
                {
                    if (value)
                    {
                        _model.MicomPort = this.Port;
                    }
                }
            }
            NormalModel _model;
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

                bool ok = arg.TotalBytes > 0;
                Log.i("All {0} is Completed.", FbMode == FBMode.Download ? "download" : "dump");
                this.TotalStatus = string.Format("{0} is completed {1}.",
                    FbMode == FBMode.Download ? "<download>" : "<dump>",
                    ok ? "successfully" : "but failed");

                if (FbMode != FBMode.Download)
                {
                    Pages.TopMessageBox.ShowMsg(
                        string.Format("A task is completed.\n\n<Result>\n\t{0}", arg.TotalBytes > 0 ? "Success" : "Fail - refer logs"), ok);

                    FbMode = FBMode.None;

                    this.UIThread(delegate
                    {
                        DownloadCommand.RaiseCanExecuteChanged();
                        DumpCommand.RaiseCanExecuteChanged();
                    });
                }
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
                    DiagRequestEnable = true;
                }
            }
        }

        private FBMode _fbMode = FBMode.None;
        TargetWrapper _targetIf = null;

        public TargetWrapper TargetIf { get { return _targetIf; } }

        private void ExecuteDownload(object arg)
        {
            Log.i("------------------- Run Download -------------------");
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
                int useCount = 0;
                _listItems.ForEach(x => {
                    x.Progress = 0;    if (x.Use) useCount++;
                });

                if (useCount == 0)
                {
                    Log.MsgBox("No images will be downloading.");
                    return;
                }
                // set fota erase option
                var fxitem = _listItems.SingleOrDefault(fx => fx.Name == "FOTA_SELFTEST");
                if (fxitem != null)
                    fxitem.Erase = this.FotaErase = false;
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

#if (false)
                // process EFS backup
                if (this.SkipEraseEfs)
                {
                    if (this.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                    {
                        this.TotalStatus = "<EFS backup> begin";
                        Dictionary<string, string> result;
                        int ret = _targetIf.DiagRequest(string.Format("{0}efsbackup{0}{0}", (char)TDiagMethod.Run),
                            out result, CurrentPort.Name);

                        if (ret > 0) // success
                        {
                            Log.i("EFS backup is completed");
                        }
                        else
                        {
                            Log.e("EFS backup is failed.");
                        }
                        this.TotalStatus = "<EFS backup> end " + (ret > 0 ? "successfully." : "failed");
                    }
                    else
                    {
                        Log.i("Current Port is not diagnostic port then skip EFS backup");
                    }
                }
                // process Debug mode changing
                if (this.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                {
                    this.TotalStatus = "<Debug Mode> try changing";
                    if(!ChangeDebugMode(true))
                    {
                        this.TotalStatus = "<Debug Mode> chaing is failed";
                        Log.e("Changing debug mode on is failed");
                        return;
                    }
                    Thread.Sleep(1500);
                }
                else
                {
                    Log.i("Current Port is not diagnostic port then skip Debug Mode changing");
                }
#endif   
                // wait qdloader mode
                if (Pages.WaitPortBox.Wait(Models.TabType.Normal, this) == false)
                    return;

                //  다운로드 작업을 시작한다.
                FbMode = FBMode.Download;
                this.DownloadCommand.RaiseCanExecuteChanged();
                DumpCommand.RaiseCanExecuteChanged();

                Task.Factory.StartNew(RunDownloadTask);
                
            }
            catch (Exception e)
            {
                Log.e("Command (Download) invoke Error, Exception: {1}", e);
                FbMode = FBMode.None;
                this.DownloadCommand.RaiseCanExecuteChanged();
                DumpCommand.RaiseCanExecuteChanged();
            }
        }

        private void RunDownloadTask()
        {
            // Print Download option to log
            Log.i("------------------- Prepare : options list -------------------");
            Log.i("[Option-1] All Erase:{0}, Reboot:{1}, LogLevel:{2}", ImageItem.AllErase, ImageItem.Reset, Log.LogLevel);
            Log.i("[Option-2] Erase except EFS & Backup:{0}", ImageItem.SkipEraseEfs);

            bool success = _targetIf.RundDownloadSync(this.CurrentPort.Name, Helper.TempConfigFile(this)) > 0;

            if (success)
            {
                // After rebooting, wait for a Diagnostic port.
                int intervalTime = 700;
                int totalWaitTime = 1 * 60 * 1000;

                this.TotalStatus = "<Wait_Port> Wait for a diagonstics port";
                this.FileProgress = 0;
                Thread.Sleep(1500);
                this.CurrentPort = null; // reset port and wait for changing.

                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (totalWaitTime > timer.ElapsedMilliseconds)
                {
                    this.FileProgress = (int)(timer.ElapsedMilliseconds / 1000);
                    if (this.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                    {
                        this.TotalStatus = "<Wait_Port> Found a diagonstics port";
                        break;
                    }

                    Thread.Sleep(intervalTime);
                }
                timer.Stop();

                // check versionInfo
                if (this.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic)
                {
                    this.TotalStatus = "<Wait_Port> wait timeout for diag port.";
                    Log.e("After Download, " + this.TotalStatus);
                    success = false;
                }
                else
                {
                    intervalTime = 700;
                    totalWaitTime = 50 * 1000;

                    timer.Restart();

                    this.FileProgress = 0;
                    this.TotalStatus = "<Read_VersionInfo> begin...";
                    success = false;
                    while (totalWaitTime > timer.ElapsedMilliseconds)
                    {
                        if (ReadVersionInfo())
                        {
                            this.TotalStatus = "<Read_VersionInfo>  Received version info.";
                            success = true;
                            break;
                        }
                        else
                        {
                            this.TotalStatus = "<Read_VersionInfo>  Modem is not ready ...";
                        }

                        this.FileProgress += 1;
                        Thread.Sleep(intervalTime);
                    }
                    if (!success)
                        Log.e("<Read_versionInfo> modem is not ready.");
                }

                // change to debug mode.
                if (success)
                {
                    this.TotalStatus = "<Debug Mode On> try changing to debug";
                    
                    if (!ChangeDebugMode(true))
                    {
                        this.TotalStatus = "<Debug Mode On> chaing to debug is failed";
                        Log.e("Changing debug mode On is failed");
                        success = false;
                    }
                    else
                    {
                        Thread.Sleep(1500);
                    }
                }
                if (success)
                {
                    this.TotalStatus = "<All Micom reflashing> try changing to user";

                    if (!MicomUpdateAll())
                    {
                        this.TotalStatus = "<All Micom reflashing> chaing to user is failed";
                        Log.e("All Micom reflashing failed");
                        success = false;
                    }
                    else
                    {
                        Thread.Sleep(1500);
                    }
                }
                // change to user mode.
                if (success)
                {
                    this.TotalStatus = "<Debug Mode Off> try changing to user";

                    if (!ChangeDebugMode(true))
                    {
                        this.TotalStatus = "<Debug Mode Off> chaing to user is failed";
                        Log.e("Changing debug mode Off is failed");
                        success = false;
                    }
                    else
                    {
                        Thread.Sleep(1500);
                    }
                }
            }

            Pages.TopMessageBox.ShowMsg(
                        string.Format("A task is completed.\n\n<Result>\n\t{0}", success ? "Success" : "Fail - refer logs"), success);

            this.TotalStatus = "<DOWNLOAD> Finished - " + (success ? "Success" : "Fail");
            FbMode = FBMode.None;

            this.UIThread(delegate
            {
                DownloadCommand.RaiseCanExecuteChanged();
                DumpCommand.RaiseCanExecuteChanged();
            });

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
                if (Pages.WaitPortBox.Wait(Models.TabType.Normal, this) == false)
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
                else if (this.DiagportPresented)
                    iname = "targeton.png";

                return new Uri(@"pack://application:,,,/Gen11Downloader.v2;component/Resources/" + iname, UriKind.Absolute);
            }
        }

        public void InvokeEfsBackup()
        {
            this.TotalStatus = "<Backup Cal & IMEI>  Invoke";
            Log.i("Start Efs backup through diagnostic port.");

            if (_targetIf != null)
            {
                _targetIf.Dispose();
                _targetIf = null;
            }

            _targetIf = new TargetWrapper();
            _targetIf.AppLogEvent += _targetIf_AppLogEvent;
            if (!_targetIf.Load(Helper.ProtocolDllPath))
            {
                Log.MsgBox("Protocol library loading error\n\t:{0}", Helper.ProtocolDllPath);
                return;
            }

            Dictionary<string, string> result;
            int ret = _targetIf.DiagRequest(string.Format("{0}efsbackup{0}{0}", (char)TDiagMethod.Run),
                out result, CurrentPort.Name);

            if (ret > 0) // success
            {
                this.TotalStatus = "<EFS backup>  Processed successfully.";
                Log.i("EFS is backuped.");
            }
            else
            {
                this.TotalStatus = "<EFS backup> Failed.";
                Log.e("EFS backup is failed.");
            }
            
        }

        private bool ChangeDebugMode(bool aOn)
        {
            int status = 0;

            try
            {
                if (_targetIf == null)
                {
                    _targetIf = new TargetWrapper();
                    _targetIf.AppLogEvent += _targetIf_AppLogEvent;
                    if (!_targetIf.Load(Helper.ProtocolDllPath))
                    {
                        Log.e("The DiagRequest is interrupted by exceptions at library loading.");
                        return false;
                    }
                }
                string cmd = "dbgon";
                if (aOn == false)
                    cmd = "dbgoff";

                var requestString = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}", (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, cmd, (char)TDiagSeperator.Item);

                Dictionary<string, string> result;

                int max_try = 30;

                while (max_try-- > 0)
                {
                    if (_targetIf.DiagRequest(requestString, out result, _curPort.Name) != 0) // success
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)
                        {
                            break;
                        }
                        else
                        {
                            Log.e("Debug Mode change Error - {0}", cmd);
                            return false;
                        }
                    }
                    else
                    {
                        Log.i("Debug mode => {0}, Retry until {1} times", cmd, max_try);
                        System.Threading.Thread.Sleep(700);
                    }
                }
                if (max_try <= 0)
                {
                    Log.e("Error: DebugMode changing request Failed : {0} ", cmd);
                    return false;
                }
                System.Threading.Thread.Sleep(1000);
                max_try = 50;
                while (max_try-- > 0)
                {
                    var checkCmd = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}", (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, "check", (char)TDiagSeperator.Item);

                    if (_targetIf.DiagRequest(checkCmd, out result, _curPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1) // OK
                        {
                            Log.i("Mode is changed to {0}", cmd);
                            return true;
                        }
                        else if (status != 3) // FAIL
                        {
                            Log.i("===== Fail DebugOn Port List ADD {0} =====", _curPort.Name);
                            break;
                        }
                    }
                    else
                    {
                        status = 3;
                    }
                    System.Threading.Thread.Sleep(700);
                }
                if (max_try <= 0)
                {
                    Log.i("debug mode changing is timeout");
                    return false;
                }
            }
            finally
            {
                ;
            }
            return false;
        }

        private bool MicomUpdateAll()
        {
            int status = 0;

            if (_targetIf == null)
            {
                _targetIf = new TargetWrapper();
                _targetIf.AppLogEvent += _targetIf_AppLogEvent;
                if (!_targetIf.Load(Helper.ProtocolDllPath))
                {
                    Log.e("The DiagRequest is interrupted by exceptions at library loading.");
                    return false;
                }
            }
            Stopwatch timer = new Stopwatch();
            const int MAX_TIME_OUT = 70 * 1000;
            const int DELAY_TIME = 700;
            try
            {
                Log.i("Request [Micom Update All]");

                // start first half updating
                Dictionary<string, string> result;
                int max_try = 120;
                while (max_try-- > 0)
                {
                    if (_targetIf.DiagRequest(TargetWrapper.MicomCommand("upall"), out result, _curPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status != 1)
                        {
                            Log.e("MICOM update-2 report Error, step: second half updating (status:{0})", status);
                            return false;
                        }
                        else
                            break;
                    }
                    Thread.Sleep(DELAY_TIME);
                }
                if (max_try <= 0)
                {
                    Log.e("Error: MICOM update-2 Failed - No response. ");
                    return false;
                }

                // check update completion - first half
                timer.Start();
                Log.i("Check result [Update All]");
                while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                {
                    if (_targetIf.DiagRequest(TargetWrapper.MicomCommand("check"), out result, _curPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)
                        {
                            Log.v("Micom udpate-2 is finished successfully.");
                            break;
                        }
                        else if (status != 3) // 진행 중인 아닌 (완료도 아닌) 경우
                        {
                            Log.e("MICOM update-2 report Error, step second half checking");
                            return false;
                        }
                    }
                    else
                    {
                        status = 3;
                    }
                    Thread.Sleep(DELAY_TIME);
                }
                timer.Stop();
                if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                {
                    Log.e("Micom update-2 check timeout ({0} ms)", timer.ElapsedMilliseconds);
                }
            }
            catch (Exception e)
            {
                Log.e("Micom update failed, reason: {0}", e.ToString());
                return false;
            }
            finally
            {
                timer.Reset();
                timer = null;
            }
            return true;
        }

        private bool CanDownload(object arg)
        {
            try
            {
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

    public class VersionItem
    {
        public VersionItem(string aItem, string aInfo)
        {
            this.Item = aItem;
            this.Infomation = aInfo;
        }
        public string Item { get; set; }
        public string Infomation { get; set; }
    }
}
