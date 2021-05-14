using Lge.Tools.Download.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Timers; // jwoh add User/Factory mode [
using System.Xml; // jwoh Vesion info

namespace Lge.Tools.Download
{
    public class MultiModel : INotifyPropertyChanged, Models.ITabModel
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
        public Models.TabType TabId { get { return Models.TabType.Multi; } }
        public bool DLEndpopup { get; set; } // jwoh add User/Factory mode 
        public bool FdebugOn { get; set; } // jwoh add debugon in factory mode
        public int CheckBuildVersion { get; set; } // jwoh Vesion info
        public bool GMSignedMulti { get; set; } // jwoh GM Signed or Non Signed

        public void TabActiveChanged(bool aActived)
        {
            if (aActived)
            {
                ImageItem.AllErase = this.AllErase;
                ImageItem.Reset = 3;
                Log.LogLevel = this.LogLevel;
                ImageItem.SkipEraseEfs = this.SkipEraseEfs;
                ImageItem.SelBoard = this.SelBoard; // jwoh add User/Factory mode 
                ImageItem.SelFirehose = this.SelFirehose; // jwoh add GM Signing
                ImageItem.SelModel = this.SelModel; // jwoh add Model
                ImageItem.ECCCheck = this.ECCCheck;         // jwoh add ECC Check
                ImageItem.MCP2K = this.MCP2K;
                ImageItem.MCP4K = this.MCP4K;
                if (File.Exists(_configPath))
                    ImageItem.Dir = new FileInfo(this.ConfigPath).DirectoryName;

                Log.LogEvent += Log_LogEvent;

                this.TargetItems.Clear();
                _id = 1;
                SerialportWatcher_SerialPortsChangedEvent(true, null);
                SerialportWatcher.SerialPortsChangedEvent += SerialportWatcher_SerialPortsChangedEvent;

                InvokePropertyChanged("StatusMessage");
            }
            else
            {
                Log.LogEvent -= Log_LogEvent;
                SerialportWatcher.SerialPortsChangedEvent -= SerialportWatcher_SerialPortsChangedEvent;
            }
        }

        public void CheckEndOfJobs()
        {
            this.UIThread(delegate {
                if (this.TargetItems.All(x => x.Result != 0))
                {
                    int ok = this.TargetItems.Sum(x => x.Result == 1  ? 1 : 0);
                    int fail = this.TargetItems.Count - ok;

                    string msg = string.Format("Total Target Tasks: {0}\n\n\tSuccess:{1}\n\tFail: {2}", this.TargetItems.Count, ok, fail);

                    Log.i("*********************** END  Multi download ***********************");
                    Pages.TopMessageBox.ShowMsg(msg, fail == 0);

                    this.FbMode = FBMode.None;
                    // clear all items.
                    // 현재는 그대로 두고 사용자가 제거 할 경우만 제거하자.
                    _id = 1;
                    // register again items.
                }
            });
            
        }

        string _configPath;
        public string ConfigPath
        {
            get { return File.Exists(_configPath) ? _configPath : "( Select images folder contains configuration.xml )"; }
            set
            {
                if (File.Exists(value))
                {
                    if (LoadConfigXml(value))
                    {
                        _configPath = value;
                        Properties.Settings.Default.ConfigPath = _configPath;
                        InvokePropertyChanged("ConfigPath");
                        InvokePropertyChanged("Editable");
                        UpdateJobList();
                        this.DownloadCommand.RaiseCanExecuteChanged();
                    }
                }
                LoadBuildInfoXml(); // jwoh Vesion info
            }
        }

        private void Log_LogEvent(LogLevels aLevel, LogItem aItem)
        {
            if (aLevel == LogLevels.Error)
            {
                this.ErrorMessages = string.Format("[{0}] {1}", aItem.Time.ToString("HH:mm:ss"), aItem.Message);
            }
        }

        List<string> ChangedDebugPort = new List<string>(); // jwoh add User/Factory mode 

        private int _id = 0;

        Timer SendDebugtimer = new System.Timers.Timer(); // jwoh add User/Factory mode 

        private void SerialportWatcher_SerialPortsChangedEvent(bool aInserted, SerialportWatcher.PortInfo aChangedPort)
        {
            this.UIThread(() =>
           {
               if (this.FbMode == FBMode.None)
               {
                   if (aInserted)
                   {
                       if (aChangedPort != null)
                       {
                           if (aChangedPort.Kind == SerialportWatcher.PortKind.Diagnostic
                                   || aChangedPort.Kind == SerialportWatcher.PortKind.QDLoader)
                           {
                               var titem = new TargetItem(_id, this);
                               titem.CurrentPort = aChangedPort;
                               this.TargetItems.Add(titem);
                               Log.i("ADD Target Items1: ID:{0}, Port:{1}", _id, titem.CurrentPort.Caption);
                               UpdateJobList(titem);
                               _id++;
                               // jwoh add User/Factory mode [
                               if ((this.SelBoard == 1) && (this.DLEndpopup == false) && (this.IsIdle))
                               {
                                   SendDebugtimer.Interval = 800;
                                   SendDebugtimer.Elapsed += new ElapsedEventHandler(timer_SendDebugon);
                                   SendDebugtimer.Start();
                               } // jwoh add User/Factory mode ]
                           }
                       }
                       else
                       {
                           foreach (var port in SerialportWatcher.Ports)
                           {
                               if (port.Kind == SerialportWatcher.PortKind.Diagnostic
                                   || port.Kind == SerialportWatcher.PortKind.QDLoader)
                               {
                                   var titem = new TargetItem(_id, this);
                                   titem.CurrentPort = port;
                                   this.TargetItems.Add(titem);
                                   Log.i("ADD Target Items2: ID:{0}, Port:{1}", _id, titem.CurrentPort.Caption);
                                   _id++;
                                   // jwoh add User/Factory mode [
                                   if ((this.SelBoard == 1) && (this.DLEndpopup == false) && (this.IsIdle))
                                   {
                                       if (this.FdebugOn)
                                       {
                                           titem.FileProgress = 100;
                                       }
                                       SendDebugtimer.Interval = 800;
                                       SendDebugtimer.Elapsed += new ElapsedEventHandler(timer_SendDebugon);
                                       SendDebugtimer.Start();
                                   } // jwoh add User/Factory mode [
                               }
                           }
                           UpdateJobList();
                       }
                       this.DownloadCommand.RaiseCanExecuteChanged();
                       InvokePropertyChanged("CanEfsBackup");
                   }
                   else // removed
                    {
                       if (aChangedPort != null)
                       {
                           var item = this.TargetItems.FirstOrDefault(x => x.MatchPort(aChangedPort));
                           if (item != null && item.Id >= 0) // valid
                            {
                               this.TargetItems.Remove(item);
                               // jwoh add User/Factory mode [
                               if ((this.SelBoard == 1) && (this.ChangedDebugPort.Count > 0))
                               {
                                   item.Print(LogLevels.Error, "===== DebugOff Port List Remove {0} =====", item.CurrentPort.Name);
                                   this.ChangedDebugPort.RemoveAll(ditem => ditem == item.CurrentPort.Name);
                                   if (this.ChangedDebugPort.Count == 0)
                                   {
                                       this.FdebugOn = false;
                                   }
                               } // jwoh add User/Factory mode [
                               Log.i("REMOVE Target Items: ID:{0}, Port:{1}", item.Id, item.CurrentPort.Caption);
                               _id--; // jwoh 
                               this.DownloadCommand.RaiseCanExecuteChanged();
                           }
                       }
                   }
               }
               else // busy state
                {
                   if (aChangedPort != null)
                   {
                       lock (TargetItem._so)
                       {
                           Log.i("{0} Port:{1}", aInserted ? "ADD1" : "REMOVE1", aChangedPort.Caption);
                       }
                       foreach (var ti in this.TargetItems)
                           ti.UpdatedTargetPort(aChangedPort, aInserted);
                   }
               }
           });         
            
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
                var strXml = File.ReadAllText(this.ConfigPath);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(strXml);
                XmlNodeList fhProgram = xml.SelectNodes("/configuration/downloader/firehose/images/image/program");
                string str_DownloadMode = null;
                foreach (XmlNode xnp in fhProgram)
                {

                    str_DownloadMode = xnp.Attributes["SECTOR_SIZE_IN_BYTES"].Value ;
                    if(str_DownloadMode != "4096" && str_DownloadMode != "2048")
                    {                     
                        break;
                    }
                }

                if (str_DownloadMode == "4096")
                {
                    Properties.Settings.Default.MCP2K_4K = "4K";
                    MCP2K = false;
                    MCP4K = true;
                    MessageBox.Show("The image is 2K version", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (str_DownloadMode == "2048")
                {
                    Properties.Settings.Default.MCP2K_4K = "2K";
                    MCP2K = true;
                    MCP4K = false;
                    MessageBox.Show("The image is 2K version", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Properties.Settings.Default.MCP2K_4K = null;
                    MCP2K = false;
                    MCP4K = false;
                    if (this.MCP2K == this.MCP4K) // tab_num is the number of tab downloader
                        MessageBox.Show("The image is not 2K nor 4K version", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                }

                ImageItem.MCP2K = this.MCP2K;
                ImageItem.MCP4K = this.MCP4K;
            }
            catch (Exception e)
            {
                Log.e("Failed configuration Loading - {0}", e);
            }

        }

        void UpdateJobList(TargetItem aItem = null)
        {
            if (this.FbMode != FBMode.None)
                return;

            var jlist = new List<TargetJob>();
            bool existDown = this.Items.Any(x => x.Use && x.Protocol == QProtocol.Firehose);

            if (this.CCMOnly)
            {
                jlist.Add(TargetJob.Ready);
                jlist.Add(TargetJob.Diag_VersionInfo);
                if (this.SkipEraseEfs)
                {
                    jlist.Add(TargetJob.Diag_EFS_Backup);
                }
                if (existDown)
                {
                    jlist.Add(TargetJob.Diag_RebootEDL);
                    jlist.Add(TargetJob.WaitPort_Qloader);
                    jlist.Add(TargetJob.Dload_All);
                }
                jlist.Add(TargetJob.WaitPort_Diag);
                jlist.Add(TargetJob.Diag_VersionInfo);
                if (this.SkipEraseEfs)
                {
                    jlist.Add(TargetJob.Diag_EFS_ClearFlag);
                }
            }
            else
            {
                jlist.Add(TargetJob.Ready);
                jlist.Add(TargetJob.Diag_VersionInfo);
                jlist.Add(TargetJob.TargetPrepare);

                if (this.SkipEraseEfs)
                {
                    jlist.Add(TargetJob.Diag_EFS_Backup);
                }
                if (existDown)
                {
                    jlist.Add(TargetJob.Diag_RebootEDL);
                    jlist.Add(TargetJob.WaitPort_Qloader);
                }
                if (this.UseMicomUpdate)
                {
                    if (!existDown)
                    {
                        jlist.Add(TargetJob.Diag_RebootEDL);
                        jlist.Add(TargetJob.WaitPort_Qloader);
                    }
                    jlist.Add(TargetJob.Dload_Micom);
                    jlist.Add(TargetJob.WaitPort_Diag);

                    jlist.Add(TargetJob.Diag_VersionInfo);
                    jlist.Add(TargetJob.Diag_MicomUpdate1);
                    if (existDown)
                    {
                        jlist.Add(TargetJob.Diag_RebootEDL);
                        jlist.Add(TargetJob.WaitPort_Qloader);
                    }
                }
                if (existDown)
                {
                    jlist.Add(TargetJob.Dload_All);
                }
                else
                {
                    jlist.Add(TargetJob.Diag_RebootNormal);
                }

                jlist.Add(TargetJob.WaitPort_Diag);
                jlist.Add(TargetJob.Diag_VersionInfo);

                if (this.UseMicomUpdate)
                {
                    jlist.Add(TargetJob.Diag_MicomUpdate2);
                    jlist.Add(TargetJob.Diag_MicomResult);
                    if (this.SkipEraseEfs) // jwoh changed job list
                    {
                        jlist.Add(TargetJob.Diag_EFS_ClearFlag);
                    }
                    if (this.SelModel != 0) // jwoh add GB key erase function [
                    {
                        jlist.Add(TargetJob.Diag_MicomKeyErase);
                        if ((CheckBuildVersion & 0x40) != 0)
                        {
                            jlist.Add(TargetJob.Diag_MicomKeyEraseCheck);
                        }
                    } // jwoh add GB key erase function ]
                    jlist.Add(TargetJob.Diag_RebootNormal);
                    jlist.Add(TargetJob.WaitPort_Diag);
                    jlist.Add(TargetJob.Diag_VersionInfo);
//                    jlist.Add(TargetJob.Diag_PlatformID);
                }
                else
                {
                    if (this.SkipEraseEfs)
                    {
                        jlist.Add(TargetJob.Diag_EFS_ClearFlag);
                    }
                    if (this.UseModeChange)
                    {
                        jlist.Add(TargetJob.Diag_DebugOff);
                    }
                }
            }
            jlist.Add(TargetJob.End);

            if (aItem == null)
            {
                foreach (var titem in this.TargetItems)
                {
                    titem.Steps = jlist;
                    titem.Step = 0;
                }
            }
            else
            {
                aItem.Steps = jlist;
                aItem.Step = 0;
            }
        }

        public enum TargetVersion : int
        {
            None = 0,
            TARGET_DAILY = 0x01,
            TARGET_RELEASE = 0x02,
            TARGET_TCP19 = 0x10,
            TARGET_TCP20 = 0x20,
            TARGET_TCP21 = 0x40,
            TARGET_VCP20 = 0x80,
            TARGET_UN = 0x100,
            TARGET_ERA = 0x200,
            TARGET_GA = 0x1000,
            TARGET_GB = 0x2000,
            TARGET_GX = 0x4000,
        }

        bool LoadBuildInfoXml()
        {
            string VersionInfoFullPath;

            CheckBuildVersion = 0;
            if (ImageItem.Dir == null)
            {
                return false;
            }

            System.IO.DirectoryInfo dInfo = new System.IO.DirectoryInfo(ImageItem.Dir);
            System.IO.FileInfo[] fInfo = dInfo.GetFiles("*.buildinfo.xml");

            if (fInfo.Length == 0)
            {
                return false;
            }

            if (fInfo[0].Name.ToString().IndexOf("daily.") != -1)
            {
                CheckBuildVersion |= (int)TargetVersion.TARGET_DAILY;
            }
            if (fInfo[0].Name.ToString().IndexOf("release.") != -1)
            {
                CheckBuildVersion |= (int)TargetVersion.TARGET_RELEASE;
            }

            VersionInfoFullPath = string.Format("{0}\\{1}", ImageItem.Dir, fInfo[0].Name.ToString());

            var strXml = File.ReadAllText(VersionInfoFullPath, Encoding.GetEncoding("UTF-8"));

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(strXml);
            XmlNode BuildVer = xml.SelectSingleNode("/manifest/buildversion");

            if (BuildVer != null)
            {
                var target_VersionInfo = BuildVer.InnerText;
                string sVerNum;

                if (target_VersionInfo.IndexOf(".tcp19") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_TCP19;
                }
                if (target_VersionInfo.IndexOf(".tcp20") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_TCP20;
                }
                if (target_VersionInfo.IndexOf(".tcp21") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_TCP21;
                }
                if (target_VersionInfo.IndexOf(".vcp20") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_VCP20;
                }
                if (target_VersionInfo.IndexOf(".un") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_UN;
                }
                if (target_VersionInfo.IndexOf(".era") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_ERA;
                }
                if (target_VersionInfo.IndexOf(".ga") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_GA;
                }
                if (target_VersionInfo.IndexOf(".gb") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_GB;
                }
                if (target_VersionInfo.IndexOf(".gx") != -1)
                {
                    CheckBuildVersion |= (int)TargetVersion.TARGET_GX;
                }

                sVerNum = target_VersionInfo.Substring(0, 4);
            }
            return true;
        }

        bool LoadConfigXml(string aXmlPath)
        {
            if (File.Exists(aXmlPath))
            {
                var dir = new FileInfo(aXmlPath).DirectoryName;
                var items = ImageItem.Load(aXmlPath);

                if (items == null || items.Count == 0)
                {
                    Log.e("Selected '{0}' it will not be processed. Please choose a valid file ({1}).", aXmlPath, Helper.ConfigXmlFileName);
                    return false;
                }
                _listItems = items;

                // configuration.xml을 appdata 영역으로 복사한다. (다중 사용자 접근, 읽기 전용 등 문제가 없도록)
                if (File.Exists(Helper.TempConfigFile(this)))
                    File.Delete(Helper.TempConfigFile(this));
                File.Copy(aXmlPath, Helper.TempConfigFile(this));

                // multi 모드용 각 타겟용 복사된 환경 설정 파일들 제거.
                if (Directory.Exists(Helper.MultiConfigDir))
                    Directory.Delete(Helper.MultiConfigDir, true);

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
                            if(aXmlPath.Contains("s.lge"))
                                this.GMSignedMulti = false;
                            else
                                this.GMSignedMulti = true;
                        }
                        else
                        {
                            this.GMSignedMulti = false;
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

        private List<ImageItem> _listItems = new List<ImageItem>();

        public List<ImageItem> Items { get { return _listItems; } }

        public List<ImageItem> UsedItems
        {
            get
            {
                // 중복 아이템은 제외.
                var list = new List<ImageItem>();
                foreach (var m in _listItems)
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

        #region Options
        public bool AllErase
        {
            get { return Properties.Settings.Default.MAllErase; }
            set
            {
                if (Properties.Settings.Default.MAllErase != value)
                {
                    Properties.Settings.Default.MAllErase = value;
                    ImageItem.AllErase = value;
                    
                    InvokePropertyChanged("AllErase");
                }
            }
        }

        public int LogLevel
        {
            get
            {
                return Properties.Settings.Default.MLogLevel;
            }
            set
            {
                if (Properties.Settings.Default.MLogLevel != value)
                {
                    Properties.Settings.Default.MLogLevel = value;

                    Log.LogLevel = value;

                    InvokePropertyChanged("LogLevel");
                }
            }
        }

        public bool SkipEraseEfs
        {
            get { return this.UseEfsBackup; }// Properties.Settings.Default.MskipEraseEfs; }
            set
            {
                ImageItem.SkipEraseEfs = value;
                if (UseEfsBackup != value)//Properties.Settings.Default.MskipEraseEfs != value)
                {
                    //     Properties.Settings.Default.MskipEraseEfs = value;
                    UseEfsBackup = value;
                }
            }
        }

        public bool UseUpdateOnly
        {
            get { return !this.UseEfsBackup; }
            set
            {
                if (this.UseEfsBackup == value)
                {
                    this.UseEfsBackup = !this.UseEfsBackup;
                }
            }
        }

        public bool UseMicomUpdate
        {
            get { return Properties.Settings.Default.MUseMicomUpdate; }
            set
            {
                if (Properties.Settings.Default.MUseMicomUpdate != value)
                {
                    Properties.Settings.Default.MUseMicomUpdate = value;

                    if (value == true) // jwoh CCM Only function [
                    {
                        this.CCMOnly = false;
                    } // jwoh CCM Only function ]

                    InvokePropertyChanged("UseMicomUpdate");

                    UpdateJobList();
                }
            }
        }

        public bool UseEfsBackup
        {
            get { return Properties.Settings.Default.MUseEfsBackup; }
            set
            {
                if (Properties.Settings.Default.MUseEfsBackup != value)
                {
                    Properties.Settings.Default.MUseEfsBackup = value;

                    this.SkipEraseEfs = value;

                    InvokePropertyChanged("UseEfsBackup");
                    InvokePropertyChanged("UseUpdateOnly");

                    UpdateJobList();
                }
            }
        }

        public bool UseModeChange
        {
            get
            {
                return true;// Properties.Settings.Default.MUseModeChange;
            }
            set
            {
                if (Properties.Settings.Default.MUseModeChange != value)
                {
                    Properties.Settings.Default.MUseModeChange = value;

                    InvokePropertyChanged("UseModeChange");

                    UpdateJobList();
                }
            }
        }

        public bool IsIdle { get { return this.FbMode == FBMode.None; } }

        public bool CanEfsBackup
        {
            get { return this.TargetItems.Count > 0 && this.FbMode == FBMode.None; }
        }

        public bool FotaErase
        {
            get { return false; }
            set { }
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
                if (value != _MCP2K)
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

        // jwoh CCM Only function [
        bool _CCMOnly = false; 
        public bool CCMOnly
        {
            get { return _CCMOnly; }
            set
            {
                if (_CCMOnly != value)
                {
                    _CCMOnly = value;
                    ImageItem.CCMOnly = value;

                    if (value == true)
                    {
                        Properties.Settings.Default.MUseMicomUpdate = false;
                        InvokePropertyChanged("UseMicomUpdate");
                    }
                    InvokePropertyChanged(nameof(CCMOnly));

                    UpdateJobList();
                }
            }
        } 
        // jwoh CCM Only function ]
        // jwoh add User/Factory mode [
        public int SelBoard
        {
            get
            {
                return Properties.Settings.Default.MSelBoard;
            }
            set
            {
                if (Properties.Settings.Default.MSelBoard != value)
                {
                    Properties.Settings.Default.MSelBoard = value;

                    ImageItem.SelBoard = value;

                    InvokePropertyChanged("SelBoard");
                }
            }
        }
        // jwoh add User/Factory mode ]
        // jwoh add GM Signing ]
        public int SelFirehose
        {
            get
            {
                return Properties.Settings.Default.MSelFirehose;
            }
            set
            {
                if (Properties.Settings.Default.MSelFirehose != value)
                {
                    Properties.Settings.Default.MSelFirehose = value;

                    ImageItem.SelFirehose = value;

                    InvokePropertyChanged("SelFirehose");
                }
            }
        }
        // jwoh add GM Signing ]
        // jwoh add Model [
        public int SelModel
        {
            get
            {
                return Properties.Settings.Default.MSelModel;
            }
            set
            {
                if (Properties.Settings.Default.MSelModel != value)
                {
                    Properties.Settings.Default.MSelModel = value;
                    ImageItem.SelModel = value;
                    InvokePropertyChanged("SelModel");
                    UpdateJobList();
                    Log.i("Selected Patform ID {0}", value);
                }
            }
        }
        // jwoh add Model ]
        // jwoh add Baudrate ]
        public int SelBaudrate
        {
            get
            {
                return Properties.Settings.Default.MSelBaudrate;
            }
            set
            {
                if (Properties.Settings.Default.MSelBaudrate != value)
                {
                    Properties.Settings.Default.MSelBaudrate = value;

                    ImageItem.SelBaudrate = value;

                    InvokePropertyChanged("SelBaudrate");
                }
            }
        }
        // jwoh add GM Baudrate ]
        // jwoh add ECCCheck [
        public bool ECCCheck
        {
            get { return false; }
            set { }
        }
        // jwoh add ECCCheck ]

        public bool DiagRequestEnable { get { return false; } set { } }

        public Visibility DonlyVisible { get { return Visibility.Collapsed; } }
        public Visibility NonlyCollapsed { get { return Visibility.Visible; } } // jwoh add Micom Download only
        public Visibility MonlyVisible { get { return Visibility.Visible; } }
        public Visibility MonlyCollapsed { get { return Visibility.Collapsed; } }
        #endregion Options
        #endregion ITabModel

        bool _donwloadFinishedOnce = true;
        public Visibility ShowRefreshButton
        {
            get { return _donwloadFinishedOnce ? Visibility.Visible : Visibility.Hidden; }
        }

        public MultiModel()
        {
           
        }

        public MultiModel(MainModel aMain)
        {
            this.Main = aMain;

            if (File.Exists(Helper.TempConfigFile(this)))
                File.Delete(Helper.TempConfigFile(this));

            DownloadCommand = new DelegateCommand(ExecuteDownload, CanDownload);

            this.ConfigPath = Properties.Settings.Default.ConfigPath;

            MController.Model = this;
            
        }

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

        public FBMode FbMode
        {
            get { return _fbMode; }
            private set
            {
                if (_fbMode != value)
                {
                    if (_fbMode == FBMode.Download)
                    {
                        _donwloadFinishedOnce = true;
                        InvokePropertyChanged("ShowRefreshButton");
                    }
                    _fbMode = value;
                    Main.IsIdle = _fbMode == FBMode.None;
                    this.DownloadCommand.RaiseCanExecuteChanged();
                    InvokePropertyChanged("CanEfsBackup");
                    InvokePropertyChanged("EditableItems");
                    InvokePropertyChanged("IsIdle"); 
                }
            }
        }

        public bool DumpEnable
        {
            get { return false; }
            set { }
        }

        public  void ChangedItems()
        {
            UpdateJobList();
        }

        private FBMode _fbMode = FBMode.None;

        public MainModel Main { get; private set; }

        public DelegateCommand DownloadCommand { get; private set; }

        private ObservableCollection<TargetItem> _targetItems = new ObservableCollection<TargetItem>();
        public ObservableCollection<TargetItem> TargetItems
        {
            get { return _targetItems; }
        }

        public bool Editable { get { return File.Exists(Helper.TempConfigFile(this)) && this.FbMode == FBMode.None; } }
        public bool EditableItems { get { return this.FbMode == FBMode.None; } }

        private void ExecuteDownload(object arg)
        {
            try
            {
                if (this.TargetItems.Count == 0)
                    return;
                                
                if (!File.Exists(Helper.TempConfigFile(this)))
                {
                    Log.MsgBox("First select a valid configuration file.");
                    return;
                }
                if (this.GMSignedMulti == true && ImageItem.SelFirehose == 0)
                {
                    //Log.MsgBox("This is a GM signed image. Please check the firehose file.");
                    //return;
                }
                if (this.GMSignedMulti == false && ImageItem.SelFirehose == 1)
                {
                    //if(ImageItem.Dir.Contains("s.lge"))
                    //    Log.MsgBox("This is a lge signed image. Please check the firehose file.");
                    //else
                    //    Log.MsgBox("This is a non signed image. Please check the firehose file.");
                    //return;
                }

               FbMode = FBMode.Download;
                this.DownloadCommand.RaiseCanExecuteChanged();

                // 전체 정보 출력 및 각 아이템 실행.
                Log.i("*********************** START  Multi download ***********************");
                Log.i("------------------- Prepare : options list -------------------");
                Log.i("Total Download target count:{0}", this.TargetItems.Count);
                Log.i("[Option-1] AllErase:{0}, Reboot:{1}, LogLevel:{2}", ImageItem.AllErase, ImageItem.Reset, Log.LogLevel);
                Log.i("[Option-2] Erase except EFS & Backup:{0}, Use Micom Update:{1}", ImageItem.SkipEraseEfs, this.UseMicomUpdate);

                ImageItem.Save( Helper.TempConfigFile(this), this.Items);

                foreach (var titem in this.TargetItems)
                {
                    // 실행.
                    MController.Run(titem);
                }

            }
            catch (Exception e)
            {
                Log.e("Command (Download) invoke Error, Exception: {1}", e);
            }

        }

        int _jobCount = 0;
        public void InvokeEfsBackup()
        {
            // start
            Log.i("Start Backup Cal & IMEI.");

            this.FbMode = FBMode.Backup;
            _jobCount = 0;

            foreach (var m in this.TargetItems)
            {
                _jobCount++;
                Task.Factory.StartNew(x =>
                {
                    // prepare
                    TargetItem tm = x as TargetItem;
                    tm.Initialize();

                    // check diag port
                    tm.StatusText = "<Backup Cal & IMEI> check port";
                    if (tm.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic)
                    {
                        tm.StatusText = "<Backup Cal & IMEI> Error - No diagnostic port";
                        Log.e("{0}/{1} {2}", tm.Id, tm.CurrentPort.ToString(), tm.StatusText);
                        return;
                    }

                    // load protocol lib.
                    tm.StatusText = "<Backup Cal & IMEI> Load protocol lib.";
                    if (!tm.PrepareTargetWrapper(Helper.ProtocolDllPath))
                    {
                        tm.StatusText = "<Backup Cal & IMEI> Error - Load protocol lib.";
                        Log.e("{0}/{1} {2}", tm.Id, tm.CurrentPort.ToString(), tm.StatusText);
                        return;
                    }
                    
                    try
                    {
                        // read version info
                        tm.StatusText = "<Backup Cal & IMEI> Check Version is valid.";
                        const int intervalTime = 200;
                        const int totalWaitTime = 30 * 1000;
                        Stopwatch timer = new Stopwatch();
                        int cnt = 0;
                        // invoke backup efs
                        timer.Restart();
                        while (totalWaitTime > timer.ElapsedMilliseconds)
                        {
                            cnt += 10;
                            Dictionary<string, string> result;
                            if (tm.Tif.DiagRequest(string.Format("{0}version{0}{0}", (char)TDiagMethod.Get),
                               out result, tm.CurrentPort.Name) != 0)
                            {
                                if (result.Count > 0)
                                {
                                    timer.Reset();
                                    break;
                                }

                                tm.StatusText = @"<Backup Cal & IMEI> Modem is not ready ...";
                            }
                            else
                            {
                                tm.StatusText = @"<Backup Cal & IMEI> Yet, no response";
                            }

                            tm.FileProgress = cnt % 100;
                            System.Threading.Thread.Sleep(intervalTime);

                            tm.TotalProgress = (int)(100 * timer.ElapsedMilliseconds / totalWaitTime);
                        }
                        if (timer.ElapsedMilliseconds >= totalWaitTime)
                        {
                            tm.StatusText = "<Backup Cal & IMEI> Error - Version check is invalid.";
                            Log.e("{0}/{1} {2}", tm.Id, tm.CurrentPort.ToString(), tm.StatusText);
                            return;
                        }

                        // end
                        tm.StatusText = "<Backup Cal & IMEI> Run Backup command";
                        Dictionary<string, string> res;
                        cnt = tm.Tif.DiagRequest(string.Format("{0}efsbackup{0}{0}", (char)TDiagMethod.Run),
                        out res, tm.CurrentPort.Name);

                        if(cnt == 0)
                        {
                            tm.StatusText = "<Backup Cal & IMEI> Error - Backup command return error.";
                            Log.e("{0}/{1} {2}", tm.Id, tm.CurrentPort.ToString(), tm.StatusText);
                            return;
                        }
                        tm.StatusText = "<Backup Cal & IMEI> Backup is finished successfully.";
                    }
                    catch (Exception e)
                    {
                        tm.StatusText = "<Backup Cal & IMEI> Exception:" + e.ToString();
                        Log.e("{0}/{1} {2}", tm.Id, tm.CurrentPort.ToString(), tm.StatusText);
                    }
                    finally
                    {
                        tm.ReleaseTargetWrapper();

                        _jobCount--;
                        if (_jobCount == 0 && this.FbMode != FBMode.None)
                        {
                            this.FbMode = FBMode.None;
                        }
                    }
                }, m);
            }

        }
               
        private bool CanDownload(object arg)
        {
            try
            {
                if (File.Exists(this.ConfigPath) && FbMode == FBMode.None && this.TargetItems.Count > 0)
                {
                    if ((this.SelBoard == 1) && (this.FdebugOn == false))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.e("Check valid (Download) Error, Exception: {1}", e);
            }

            return false;
        }

        // jwoh add User/Factory mode [
        public void RemoveDebugPortList(TargetItem tItem)
        {
            if (this.ChangedDebugPort.Count > 0)
            {
                tItem.Print(LogLevels.Error, "===== DebugOff Port List Remove {0} =====", tItem.CurrentPort.Name);
                this.ChangedDebugPort.RemoveAll(ditem => ditem == tItem.CurrentPort.Name);
                if (this.ChangedDebugPort.Count == 0)
                {
                    this.FdebugOn = false;
                }
            }
        }

        private bool SendDebugOn(object arg)
        {
            var aItem = arg as TargetItem;
            int status = 0;

            try
            {
                string cmd = "dbgon";

                var requestString = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}", (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, cmd, (char)TDiagSeperator.Item);

                Dictionary<string, string> result;

                int max_try = 60;

                if (aItem.PrepareTargetWrapper(Helper.ProtocolDllPath))
                    aItem.Print(LogLevels.Info, "PrepareTargetWrapper");
                while (max_try-- > 0)
                {
                    if (aItem.Tif.DiagRequest(requestString, out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)
                        {
                            break;
                        }
                        else
                        {
                            aItem.Print(LogLevels.Info, "Debug Mode change Error - {0}", cmd);
                            return false;
                        }
                    }
                    else
                    {
                        // error
                        aItem.Print(LogLevels.Info, "Debug mode => {0}, Retry until {1} times", cmd, max_try);
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "Error: DebugMode changing request Failed : {0} ", cmd);
                    return false;
                }
                System.Threading.Thread.Sleep(1000);
                max_try = 75;
                while (max_try-- > 0)
                {
                    var checkCmd = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}", (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, "check", (char)TDiagSeperator.Item);

                    if (aItem.Tif.DiagRequest(checkCmd, out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1) // OK
                        {
                            aItem.Print(LogLevels.Error, "===== Success DebugOn Port List ADD {0} =====", aItem.CurrentPort.Name);
                            this.ChangedDebugPort.Add(aItem.CurrentPort.Name);
                            this.FdebugOn = true;
                            this.DownloadCommand.RaiseCanExecuteChanged();
                            (arg as TargetItem).FileProgress = 100;
                            return true;
                        }
                        else if (status != 3) // FAIL
                        {
                            aItem.Print(LogLevels.Info, "===== Fail DebugOn Port List ADD {0} =====", aItem.CurrentPort.Name);
                            break;
                        }
                    }
                    else
                    {
                        status = 3;
                    }
                    System.Threading.Thread.Sleep(800);
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "debug mode changing is timeout");
                    return false;
                }
            }
            finally
            {
                ;
            }
            return false;
        }

        void timer_SendDebugon(object sender, ElapsedEventArgs e)
        {
            SendDebugtimer.Stop();
            SendDebugtimer.Elapsed -= new ElapsedEventHandler(timer_SendDebugon);

            Log.i("SendDebugtimer Stop");

            this.UIThread(() =>
            {
                bool NewPort;

                foreach (var titem in this.TargetItems)
                {
                    NewPort = true;

                    if (this.ChangedDebugPort.Count == 0)
                    {
                        Task.Factory.StartNew(SendDebugOn, titem);
                    }
                    else
                    {
                        for (int i = 0; i < this.ChangedDebugPort.Count; i++)
                        {
                            if (this.ChangedDebugPort[i].Equals(titem.CurrentPort.Name, StringComparison.Ordinal))
                            {
                                NewPort = false;
                                break;
                            }
                        }
                        if (NewPort)
                        {
                            Task.Factory.StartNew(SendDebugOn, titem);
                        }
                    }
                }
            });
        }
        // jwoh add User/Factory mode [
    }
}
