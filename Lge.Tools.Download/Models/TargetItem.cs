using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Lge.Tools.Download.Models
{
    public class TargetItem : INotifyPropertyChanged, IDisposable
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

        public TargetItem()
        {
            this.Id = -1;
            this.Model = null;
        }

        public TargetItem(int aId, ITabModel aModel)
        {
            this.Id = aId;
            this.Model = aModel;

            this.Initialize();
        }

        int _id = -1;
        public int Id
        {
            get { return _id; }
            private set
            {
                if (_id != value)
                {
                    _id = value;
                    InvokePropertyChanged("Id");
                }
            }
        }

        public Uri DeviceImage
        {
            get
            {
                string iname = "notarget.png";
                if (this.CurrentPort.Kind == SerialportWatcher.PortKind.QDLoader)
                    iname = "targetdn.png";
                else if (this.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                    iname = "targeton.png";

                return new Uri(@"pack://application:,,,/Gen11Downloader.v2;component/Resources/" + iname, UriKind.Absolute);
            }
        }

        int _totalProgress = 0;
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

        int _fileProgress = 0;
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

        ImageItem _curImage = null;
        public ImageItem CurrentImage
        {
            get { return _curImage; }
            set
            {
                if (_curImage != value)
                {
                    _curImage = value;
                }
            }
        }

        public TargetWrapper Tif
        {
            get { return _tif; }
        }

        public ITabModel Model { get; private set; }

        TargetWrapper _tif = null;

        SerialportWatcher.PortInfo _qlloadPort = new SerialportWatcher.PortInfo();
        SerialportWatcher.PortInfo _diagPort = new SerialportWatcher.PortInfo();
        SerialportWatcher.PortInfo _curPort = new SerialportWatcher.PortInfo();

        public SerialportWatcher.PortInfo CurrentPort
        {
            get { return _curPort; }
            set
            {
                if (!_curPort.Equals(value))
                {
                    if (value == null)
                        _curPort = new SerialportWatcher.PortInfo();
                    else
                    {
                        _curPort = value;
                        if (_curPort.Kind == SerialportWatcher.PortKind.QDLoader)
                            _qlloadPort = _curPort;
                        else if (_curPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                            _diagPort = _curPort;
                    }
                    this.Print(LogLevels.Info, "UpdatePort: {0}/{1}, Path:{2}", _curPort.Kind, _curPort.Name, _curPort.Path);

                    InvokePropertyChanged("CurrentPort");
                    InvokePropertyChanged("DeviceImage");
                }
            }
        }

        string _configPath = "";
        public string ConfigPath
        {
            get
            {
                if (string.IsNullOrEmpty(_configPath))
                    _configPath = Helper.MultiConfigPath(this.Id);

                return _configPath;
            }
            set
            {
                _configPath = value;
            }
        }


        private List<TargetJob> _steps = new List<TargetJob>();
        public List<TargetJob> Steps
        {
            get { return _steps; }
            set
            {
                if (_steps != value)
                {
                    _steps = value;
                    InvokePropertyChanged("Steps");
                }
            }
        }

        public TargetJob PrevJob2 // jwoh After reboot EDL, Qload open fail [
        {
            get
            {
                this.Step = this.Step - 2;
                return CurrentJob;
            }
        } // jwoh After reboot EDL, Qload open fail ]

        public TargetJob PrevJob // jwoh After reboot EDL, Qload open fail [
        {
            get
            {
                this.Step = this.Step - 1;
                return CurrentJob;
            }
        } // jwoh After reboot EDL, Qload open fail ]

        public TargetJob NextJob
        {
            get
            {
                this.Step = this.Step + 1;
                return CurrentJob;
            }
        }

        public TargetJob CurrentJob
        {
            get
            {
                if (_step == 0)
                    return TargetJob.None;

                if (_step <= _steps.Count)
                    return Steps[_step - 1];

                return TargetJob.End;
            }
        }

        private int _step = 0;
        public int Step
        {
            get { return _step; }
            set
            {
                if (_step != value)
                {
                    if (value == 0)
                        _step = value;
                    if (value > 0 && _step <= _steps.Count)
                        _step = value;
                    InvokePropertyChanged("Step");
                }
            }
        }

        private string _statusText = "";
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    InvokePropertyChanged("StatusText");
                }
            }
        }

        private int _result = 0;
        public int Result
        {
            get { return _result; }
            set
            {
                if (_result != value)
                {
                    _result = value;
                    InvokePropertyChanged("Result");
                }
            }
        }

        private object _tag = new object();
        public object Tag
        {
            get { return _tag; }
            set
            {
                if (_tag != value)
                {
                    _tag = value;
                    InvokePropertyChanged("Tag");
                }
            }
        }

        TargetJob _state = TargetJob.None;

        public TargetJob State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    // 상태에 따른 표시 문자열을 갱신한다.(혹은 표시 유무에 대한 컨트롤 제어)
                }
            }
        }

        public bool MatchPort(SerialportWatcher.PortInfo aPort)
        {
            if ( (aPort.Kind == SerialportWatcher.PortKind.Diagnostic || aPort.Kind == SerialportWatcher.PortKind.QDLoader) 
                 && (aPort.MatchPath(this._qlloadPort) || aPort.MatchPath(this._diagPort))
               )
            {                
                return true;
            }
            return false;
        }

        public bool LoadXml(string aXmlPath)
        {
            if (!File.Exists(aXmlPath))
            {
                MController.Status(this, "config xml not found:{0}", aXmlPath);
                return false;
            }

            _listItems = ImageItem.Load(aXmlPath);
            
            if (_listItems == null)
            {
                MController.Status(this, "config xml Loading failed:{0}", aXmlPath);
                return false;
            }

            foreach (var item in _listItems)
            {
                item.IsExist = File.Exists(Path.Combine(ImageItem.Dir, item.FileName));
                item.Erase = item.Use;
                Log.v("Partition ID={0} Name={1}, FileName={2}, Use={3}", item.Id, item.Name,
                                            item.Use ? item.FileName : "(not exist!)", item.Use);
            }

            this.ConfigPath = aXmlPath;


            return true;
        }

        public void Initialize()
        {
            this.Result = 0;

            _listItems = null;
            if (_tif != null)
            {
                _tif.Dispose();
                _tif = null;
            }
        }

        public bool PrepareTargetWrapper(string aDllPath)
        {
            if (_tif != null)
            {
                _tif.Dispose();
                _tif = null;
            }

            _tif = new TargetWrapper();
            if (!_tif.Load(aDllPath))
            {
                MController.Status(this, "Load Protocol library failed :{0}", aDllPath);
                return false;
            }

            _tif.AppLogEvent += _tif_AppLogEvent;

            return true;
        }

        public void ReleaseTargetWrapper()
        {
            if (_tif != null)
            {
                _tif.AppLogEvent -= _tif_AppLogEvent;

                _tif.Dispose();
                _tif = null;
            }
        }

        private void _tif_AppLogEvent(LogLevels aLogLevel, string aMsg)
        {
            if ((int)aLogLevel <= Log.LogLevel)
                this.Print(aLogLevel, aMsg);
        }
        

        public void UpdatedTargetPort(SerialportWatcher.PortInfo aChangedPort, bool aInserted)
        {
            if (aInserted)
            {
                if(MatchPort(aChangedPort))
                    this.CurrentPort = aChangedPort;
                else // 좀 더 정밀 검사.. 전체에 대해서.
                {
                    foreach(var port in SerialportWatcher.Ports)
                    {
                        if (MatchPort(port))
                        {
                            this.CurrentPort = port;
                            break;
                        }
                    }
                }
            }
            else // remove
            {
                if (!SerialportWatcher.Ports.Any( x => x.Name == _curPort.Name))
                {
                    CurrentPort = null;
                }
            }
        }

        public void Print(LogLevels aLevel, string aFormat, params object[] args)
        {
            var msg = string.Format(aFormat, args);

            var log = string.Format("{1}@{0}.{3} {2}", this.Id, this.CurrentPort.Name, msg, this.CurrentJob);

            if (aLevel == LogLevels.Error)
            {
                //MController.Status(this, "Error !");
            }

            lock (_so)
            {
                Log.writeLine(aLevel, log);
            }
        }
        internal static object _so = new object(); // for log sync

        public void InstallProgressHandler(bool aInstall = true)
        {
            if (aInstall)
                this.Tif.AppProgressEvent += Tif_AppProgressEvent;
            else
                this.Tif.AppProgressEvent -= Tif_AppProgressEvent;
        }

        string _lastName = "";
        private void Tif_AppProgressEvent(ProgressArgs arg)
        {
            int idx = _listItems.FindIndex(x => x.Id == arg.ImageID && (int)x.Protocol == arg.ExtraInfo);
            if (idx >= 0 && arg.TotalBytes > 0) // 유효한 파일 다운로드 중..
            {
                var item = _listItems[idx];
                int progress = (int)(arg.SentBytes * 100 / arg.TotalBytes);

                if (item.Progress == progress)
                    return;

                item.Progress = progress;

                int count = 0;
                int total = 0;
                foreach (var m in _listItems)
                {
                    if (m.Use)
                    {
                        total += m.Progress;
                        count++;
                    }
                }

                this.FileProgress = progress;
                this.TotalProgress = count > 0 ? total / count : 0;
                if (_lastName != item.Name)
                {
                    _lastName = item.Name;
                    MController.Status(this, "{0}/{1}", item.Name, item.FileName);
                }
            }
            else if (arg.ExtraInfo == (int)QProtocol.All)
            {
                this.TotalProgress = 100;

                Print(LogLevels.Info, "====== Downloaded Images  ======");
                foreach (var m in _listItems)
                {
                    if (m.Use)
                    {
                        Print(LogLevels.Info, "Name:{0} File:{1}", m.Name, m.FileName);
                    }
                }
                _lastName = "";
            }
        }

        private List<ImageItem> _listItems = new List<ImageItem>();
        public List<ImageItem> Items { get { return _listItems; } set { _listItems = value; } }


        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_tif != null)
                    {
                        _tif.Dispose();
                        _tif = null;
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
    
}
