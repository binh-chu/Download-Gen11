using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Lge.Tools.Download
{
    public class MainModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void InvokePropertyChanged(String aName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(aName));
            } 
        }

        public MainModel()
        {
            ModelName = "MainModel";

            SerialportWatcher.UsbPortChanged(null);

            EModel = new EmergencyModel(this);
            MModel = new MultiModel(this);
            DModel = new DumpModel(this);
            NModel = new NormalModel(this); // jwoh add Micom Download only

            this._currentTabIndex = Properties.Settings.Default.TabIndex;
            this.AllErase = Properties.Settings.Default.AllErase;
            this.LogLevel = Properties.Settings.Default.LogLevel;
            this.SelBoard = Properties.Settings.Default.SelBoard; // jwoh add User/Factory mode
            this.SelFirehose = Properties.Settings.Default.SelFirehose; // jwoh add GM Signing
            this.SelBaudrate = Properties.Settings.Default.SelBaudrate; // jwoh add baudrate
            this.SelModel = Properties.Settings.Default.SelModel; // jwoh add model
            this.MCP2K = (Properties.Settings.Default.MCP2K_4K == "2K") ? true : false; 
            this.MCP4K = (Properties.Settings.Default.MCP2K_4K == "4K") ? true : false;
            ModelActivateChange(-1, this.CurrentTabIndex);            
        }

  

        public string ModelName { get; set; }

        public EmergencyModel EModel    { get; private set; }
        public MultiModel MModel        { get; private set; }
        public DumpModel DModel { get; private set; }
        public NormalModel NModel { get; private set; } // jwoh add Micom Download only

        #region Options
        public bool AllErase
        {
            get { return ImageItem.AllErase; }
            set
            {
                if (ImageItem.AllErase != value)
                {
                    ImageItem.AllErase = value;
                    Properties.Settings.Default.AllErase = value;
                    InvokePropertyChanged("AllErase");
                }
            }
        }

        public string[] Loglevels
        {
            get
            {
                return Enum.GetNames(typeof(LogLevels));
            }
        }

        public int LogLevel
        {
            get
            {
                return Log.LogLevel;
            }
            set
            {
                if (Log.LogLevel != value)
                {
                    Log.LogLevel = value;
                    Properties.Settings.Default.LogLevel = value;

                    InvokePropertyChanged("LogLevel");
                }
            }
        }

        public int CurrentTabIndex
        {
            get
            {
                return _currentTabIndex;
            }
            set
            {
                if (IsIdle && _currentTabIndex != value)
                {
                    ModelActivateChange(_currentTabIndex, value);

                    InvokePropertyChanged("CurrentTabIndex");
                }
            }
        }

        // jwoh add User/Factory mode [
        public enum SelBoards : int
        {
            User = 0,
            Factory = 1,
        }
        public string[] Selboards
        {
            get
            {
                return Enum.GetNames(typeof(SelBoards));
            }
        }

        public int SelBoard
        {
            get
            {
                return ImageItem.SelBoard;
            }
            set
            {
                if (ImageItem.SelBoard != value)
                {
                    ImageItem.SelBoard = value;
                    Properties.Settings.Default.SelBoard = value;

                    InvokePropertyChanged("SelBoard");
                }
            }
        }
        // jwoh add User/Factory mode ]
        // jwoh add GM Signing ]
        public enum SelFirehoses : int
        {
            LGE = 0,
            GM = 1,
        }
        public string[] Selfirehoses
        {
            get
            {
                return Enum.GetNames(typeof(SelFirehoses));
            }
        }

        public int SelFirehose
        {
            get
            {
                return ImageItem.SelFirehose;
            }
            set
            {
                if (ImageItem.SelFirehose != value)
                {
                    ImageItem.SelFirehose = value;
                    Properties.Settings.Default.SelFirehose = value;

                    InvokePropertyChanged("SelFirehose");
                }
            }
        }
        // jwoh add GM Signing ]
        // jwoh add Model [
        public enum SelModels : int
        {
            GLOBAL_A = 0,
            GLOBAL_B = 1,
            GEM = 2,
        }
        public string[] Selmodels
        {
            get
            {
                return Enum.GetNames(typeof(SelModels));
            }
        }

        public int SelModel
        {
            get
            {
                return ImageItem.SelModel;
            }
            set
            {
                if (ImageItem.SelModel != value)
                {
                    ImageItem.SelModel = value;
                    Properties.Settings.Default.SelModel = value;

                    InvokePropertyChanged("SelModel");
                }
            }
        }
        private bool _MCP2K;
        public bool MCP2K
        {
            get
            {
                return (Properties.Settings.Default.MCP2K_4K == "2K") ? true : false;           
            }
            set
            {
                if(value != _MCP2K)
                {
                    _MCP2K = value;
                    InvokePropertyChanged("MCP2K");
                }
            }
        }

        private bool _MCP4K;
        public bool MCP4K
        {
            get
            {
                return (Properties.Settings.Default.MCP2K_4K == "4K") ? true : false;
            }
            set
            {
                if (value != _MCP4K)
                {
                    _MCP4K = value;
                    InvokePropertyChanged("MCP4K");
                }
            }
        }

        // jwoh add Model ]
        // jwoh add Baudrate ]
        public enum SelBaudrates : int
        {
            Baudrate_7200 = 0,
            Baudrate_9600 = 1,
            Baudrate_14400 = 2,
            Baudrate_19200 = 3,
            Baudrate_38400 = 4,
            Baudrate_57600 = 5,
            Baudrate_115200 = 6,
            Baudrate_230400 = 7,
            Baudrate_460800 = 8,
            Baudrate_921600 = 9,
        }
        public string[] Selbaudrates
        {
            get
            {
                return Enum.GetNames(typeof(SelBaudrates));
            }
        }

        public int SelBaudrate
        {
            get
            {
                return ImageItem.SelBaudrate;
            }
            set
            {
                if (ImageItem.SelBaudrate != value)
                {
                    ImageItem.SelBaudrate = value;
                    Properties.Settings.Default.SelBaudrate = value;

                    InvokePropertyChanged("SelBaudrate");
                }
            }
        }
        // jwoh add Baudrate ]

        #endregion Options

        int _currentTabIndex = -1;
        
        public static MainWindow Window { get; internal set; }

        bool _isIdle = true;
        public bool IsIdle
        {
            get { return _isIdle; }
            set
            {
                if (_isIdle != value)
                {
                    _isIdle = value;
                    InvokePropertyChanged("IsIdle");
                }
            }
        }

        public void PrintImagesList(IEnumerable<ImageItem> aItems)
        {
            Log.a("============= Downloaded Files =============");
            foreach(var m in aItems)
            {
                if (m.Use)
                    Log.a("\t{0}/{1}", m.Name, m.FileName);
            }
            Log.a("============= End of Files =============");
        }

        private void ModelActivateChange(int aOldIndex, int aNewIndex)
        {
            _currentTabIndex = aNewIndex;
            Properties.Settings.Default.TabIndex = _currentTabIndex;

            var tabs = new Models.ITabModel[] { MModel, EModel, DModel, NModel }; // jwoh add Micom Download only

            // inactivate
            foreach (var tabModel in tabs)
            {
                if ((int)tabModel.TabId == aOldIndex)
                {
                    tabModel.TabActiveChanged(false);
                }
            }

            // activate
            foreach (var tabModel in tabs)
            {
                if ((int)tabModel.TabId == aNewIndex)
                {
                    tabModel.TabActiveChanged(true);
                }
            }
        }
    }

    public enum FBMode
    {
        None,
        Download,
        Dump,
        Micom,
        Backup,
    }

}
