using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms; // jwoh add popup message box
using System.Reflection;

namespace Lge.Tools.Download.Models
{
    public delegate bool StateHandler(TargetJob aState, TargetItem aItem);

    public static class MController
    {

        public static void Run(TargetItem aItem)
        {
            var asm = Assembly.GetExecutingAssembly(); // jwoh Download Tool Version
            string assemblyVersion = asm.GetName().Version.ToString(); // jwoh Download Tool Version

            // print item info
            aItem.Print(LogLevels.Info, "Job ID: {0}, starting Port Name:{1}", aItem.Id, aItem.CurrentPort.Caption);

            // jwoh Download Tool Version
            aItem.Print(LogLevels.Info, "======= Downloader Tool Version - {0} ========", assemblyVersion);

            // print jobs list
            aItem.Print(LogLevels.Info, "================ BEGIN Job List ({0}) ================", aItem.Steps.Count);
            int idx = 1;
            foreach(var j in aItem.Steps)
            {
                aItem.Print(LogLevels.Info, "Job: {0}", j, idx++);
            }
            aItem.Print(LogLevels.Info, "================  END Job List ================");

            // 기존 configuration 폴더 제거.
            if (File.Exists(Helper.MultiConfigPath(aItem.Id)))
                File.Delete(Helper.MultiConfigPath(aItem.Id));

            // run task with aItem.
            Task.Factory.StartNew(JobProcess, aItem);
        }

        static bool _micomRetry = false;

        static void JobProcess(object arg)
        {
            var item = arg as TargetItem;
            item.Step = 0;
            TargetJob curJob = TargetJob.None;
            int RetryCnt = 2; // jwoh 
            int QloadWaitCnt = 0; // jwoh reboot EDL 재시도 횟수
            DialogResult obj;
            try
            {
                item.Print(LogLevels.Info, "<START Multi-download jobs.>");
                while(curJob != TargetJob.End)
                {
                    curJob = item.NextJob;

                    Status(item, " Start ");

                    _micomRetry = false;

                    item.FileProgress = 0;
                    item.TotalProgress = 0;

                    if (!_handlers[curJob](curJob, item))
                    {
                        if ((curJob == TargetJob.WaitPort_Qloader) && (QloadWaitCnt < 2)) // jwoh Wait_Qload 시 Nomal boot면 Reboot EDL로 다시 시도 [
                        {
                            curJob = item.PrevJob2;
                            QloadWaitCnt++;
                            Thread.Sleep(15000);
                        } // jwoh Wait_Qload 시 Nomal boot면 Reboot EDL로 다시 시도 ]
                        else if (curJob == TargetJob.Diag_VersionInfo) // jwoh add popup message box for VersionInfo [
                        {
                            item.Print(LogLevels.Info, "======= VersionInfo Popup UX: about to display =======");
                            obj = System.Windows.Forms.MessageBox.Show(new WindowWrapper(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle), "\n사용중인 PC의 문제로 Port를 Open하지 못하여 Download를\n완료할 수 없습니다.\n사용 중인 PC에서 Downloader Tool과 ATT를 제외한 모든\nApplication을 종료시킨 후 Download를 다시 시작해 주십시요\n\nThe Port cannot be opened due to a problem coming from your PC.\nPlease try again after terminating all of the running application on\nyour PC except this Downloader Tool and ATT\n", "Port Open",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                            if (obj == DialogResult.OK)
                            {
                                item.Print(LogLevels.Info, "======= VersionInfo Popup UX: User clicked ‘Yes’ button =======");
                                item.Print(LogLevels.Error, "------> JOB ERROR <------");
                                item.Result = -1;
                                break;
                            }
                        } // jwoh add popup message box for VersionInfo ]
                        else if (curJob == TargetJob.Diag_MicomUpdate1)
                        {
                            while (RetryCnt-- >= 0)
                            {
                                if (MicomModeCheck(item) == 1) // Normal Mode
                                {
                                    item.Print(LogLevels.Info, "======= MicomUpdate1 retry =======");
                                    curJob = item.PrevJob;
                                    break;
                                }
                                else // rr mode
                                {
                                    if (MicomModeChange(item, true))
                                    {
                                        item.Print(LogLevels.Info, "======= MicomUpdate1 retry =======");
                                        curJob = item.PrevJob;
                                        break;
                                    }
                                }
                                Thread.Sleep(2000);
                            }
                            if (RetryCnt < 0)
                            {
                                item.Print(LogLevels.Info, "======= MICOM Update 1 Popup UX: about to display =======");
                                obj = System.Windows.Forms.MessageBox.Show(new WindowWrapper(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle), "\nTCP board 내부에서 오류가 발생하여 MICOM Update 1 진행 중 Download 실패 하였습니다.\nDownloader Tool을 종료한 다음, 다시 실행시킨 후 Download를 진행 해 주십시오.\n\nImage downloading is failed while processing in MICOM Update 1 stage due to TCP board's internal error.\nPlease terminate the Download Tool, then re-execute Downloader Tool for retrying to image download.\n", "GEN11 Downloader Tool - MICOM Update 1 Popup",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                if (obj == DialogResult.OK)
                                {
                                    item.Print(LogLevels.Info, "======= MICOM Update 1 Popup UX: User clicked ‘Yes’ button =======");
                                    item.Print(LogLevels.Error, "------> JOB ERROR <------");
                                    item.Result = -1;
                                    break;
                                }
                            }
                        }
                        else if (curJob == TargetJob.Diag_MicomUpdate2)
                        {
                            while (RetryCnt-- >= 0)
                            {
                                if (MicomModeCheck(item) == 4) // rr Mode
                                {
                                    item.Print(LogLevels.Info, "======= MicomUpdate2 retry =======");
                                    curJob = item.PrevJob;
                                    break;
                                }
                                else // normal mode
                                {
                                    if (MicomModeChange(item, false))
                                    {
                                        item.Print(LogLevels.Info, "======= MicomUpdate2 retry =======");
                                        curJob = item.PrevJob;
                                        break;
                                    }
                                }
                                Thread.Sleep(2000);
                            }
                            if (RetryCnt < 0)
                            {
                                item.Print(LogLevels.Info, "======= MICOM Update 2 Popup UX: about to display =======");
                                obj = System.Windows.Forms.MessageBox.Show(new WindowWrapper(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle), "\nTCP board 내부에서 오류가 발생하여 MICOM Update 2 진행 중 Download 실패 하였습니다.\nDownloader Tool을 종료한 다음, 다시 실행시킨 후 Download를 진행 해 주십시오.\n\nImage downloading is failed while processing in MICOM Update 2 stage due to TCP board's internal error.\nPlease terminate the Download Tool, then re-execute Downloader Tool for retrying to image download.\n", "GEN11 Downloader Tool - MICOM Update 2 Popup",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                if (obj == DialogResult.OK)
                                {
                                    item.Print(LogLevels.Info, "======= MICOM Update 2 Popup UX: User clicked ‘Yes’ button =======");
                                    item.Print(LogLevels.Error, "------> JOB ERROR <------");
                                    item.Result = -1;
                                    break;
                                }
                            }
                        }
                        else if (curJob == TargetJob.Dload_All)
                        {
                            item.Print(LogLevels.Info, "======= Download All Popup UX: about to display =======");
                            obj = System.Windows.Forms.MessageBox.Show(new WindowWrapper(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle), "\nImage 다운로드 중 PC와 TCP board 사이의 연결에 오류가 발생하여 Download 실패 하였습니다.\n다음과 같이 조치 해 주십시오.\n(1) Downloader Tool을 종료 시키십시오.\n(2) TCP board의 전원을 제거 해 주십시오.\n(3) TCP board와 PC간에 연결된 케이블을 모두 제거해 주십시오.\n(4) Downloader Tool을 다시 실행시켜 주십시오.\n(5) TCP board와 PC를 연결 해 주십시오.\n(6) TCP board에 전원을 인가 해 주십시오.\n(7) Emergency Download 모드로 다시 시작 해 주십시오.\n\n(1) Terminate Downloader Tool.\n(2) Remove the power cable from the TCP board.\n(3) Disconnect all of the cables between PC and TCP board.\n(4) Re-execute Downloader Tool.\n(5) Connect the power cable to the TCP board.\n(6) Connect the cables between PC and TCP board.\n(7) Retry the image download using Emergency Download Mode.\n",
                                "GEN11 Downloader Tool - Download All Popup",
                                MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                            if (obj == DialogResult.OK)
                            {
                                item.Print(LogLevels.Info, "======= Download All Popup UX: User clicked ‘Yes’ button =======");
                                item.Print(LogLevels.Error, "------> JOB ERROR <------");
                                item.Result = -1;
                                break;
                            }
                        }
                        else
                        {
                            item.Print(LogLevels.Error, "------> JOB ERROR <------");
                            item.Result = -1;
                            break;
                        }
                    }
                    else
                    {
                        RetryCnt = 2;
                    }
                }
                RetryCnt = 2;
                if (item.Result == 0)
                    item.Result = 1;
            }
            catch(Exception e)
            {
                item.Result = -1;
                item.Print(LogLevels.Error, "Unknown Exception: {0}", e);
            }
            item.Print(LogLevels.Info, "<END of Multi-download jobs.>");

            if (curJob != TargetJob.End || item.Result == -1)
                ShEnd(TargetJob.End, item);

            if (Model.TabId == TabType.Multi)
            {
                (Model as MultiModel).CheckEndOfJobs();

                if ((Model as MultiModel).SelBoard == 1 && (Model as MultiModel).FdebugOn == false)
                {
                    (Model as MultiModel).DLEndpopup = true;

                    obj = System.Windows.Forms.MessageBox.Show(new WindowWrapper(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle), "\n다운로드가 완료되었습니다.\nTCP Board를 분리해 주세요\n*** 주의 : 지금 TCP board를 분리하지 않으면\nTCP board가 정상적으로 동작하지 않게 됩니다.\n\nDownload is completed.\nRemove the power cable from the TCP board\nrignt now.\n*** CAUTION : IF THE POWER CABLE DOES NOT\nDISCONNECT NOW, THE TCP BOARD WOULD\nNOT WORK PROPERTY.\n",
                        "GEN11 Downloader Tool - Download All Popup",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                    if (obj == DialogResult.OK)
                    {
                        (Model as MultiModel).DLEndpopup = false;
                    }
                }
            }
        }

        static bool ShNone(TargetJob aState, TargetItem aItem)
        {
            return true;
        }

        public static MultiModel Model { get; set; }

        static Dictionary<TargetJob, StateHandler> _handlers = new Dictionary<TargetJob, StateHandler>() {
                { TargetJob.None,              ShNone },
                { TargetJob.Ready,             ShReady },
                { TargetJob.TargetPrepare,     ShTargetPrepare },
                { TargetJob.Diag_EFS_Backup,   ShDiagEfsBackup },
                { TargetJob.Diag_EFS_ClearFlag,ShDiagEfsClearFlag },
                { TargetJob.Diag_MicomUpdate1, ShDiagMicomUpdate1 },
                { TargetJob.Diag_MicomUpdate2, ShDiagMicomUpdate2 },
                { TargetJob.Diag_MicomResult,  ShDiagMicomResult},
                { TargetJob.Diag_VersionInfo,  ShDiagVersionInfo },
                { TargetJob.Diag_RebootEDL,    ShReboot },
                { TargetJob.Diag_RebootNormal, ShReboot },
                { TargetJob.Diag_DebugOn,      ShDebugMode },
                { TargetJob.Diag_DebugOff,     ShDebugMode },
                { TargetJob.Dload_Micom,       ShDloadMicom },
                { TargetJob.Dload_All,         ShDloadAll },
                { TargetJob.WaitPort_Diag,     ShWaitPort_Diag }, // jwoh ShWaitPort -> ShWaitPort_Diag
                { TargetJob.WaitPort_Qloader,  ShWaitPort_Qloader }, // jwoh ShWaitPort -> ShWaitPort_Qloader
                { TargetJob.End,               ShEnd },
                { TargetJob.Diag_MicomKeyErase,ShMicomKeyErase}, // jwoh add GB key erase function
                { TargetJob.Diag_MicomKeyEraseCheck,ShMicomKeyEraseCheck}, // jwoh add GB key erase check function
                { TargetJob.Diag_PlatformID,   ShPlatformIdCheck }, // jwoh platfom id check
        };

        static bool ShReady(TargetJob aState, TargetItem aItem)
        {
            aItem.FileProgress = 10;
            // xml 설정 (복사)
            aItem.FileProgress = 20;
            aItem.Initialize();

            aItem.FileProgress = 30;
            File.Copy(Helper.TempConfigFile(aItem.Model), Helper.MultiConfigPath(aItem.Id), true);

            aItem.FileProgress = 60;
            if (!aItem.LoadXml(Helper.MultiConfigPath(aItem.Id)))
                return false;

            aItem.FileProgress = 80;
            // tif 생성, log handler 등록.
            if (!aItem.PrepareTargetWrapper(Helper.ProtocolDllPath))
                return false;

            aItem.FileProgress = 100;

            if (ImageItem.SelBoard == 0) // jwoh add User/Factory mode
            {
                Thread.Sleep(7000);
            }

            return true;
        }
        
        static bool ShTargetPrepare(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                //// set delay for stabilization of USB and modem signals
                //for (int i = 0; i < 10; i++)
                //{
                //    aItem.FileProgress += 10;
                //    Thread.Sleep(600);
                //}
                //aItem.FileProgress = 0;

                // 먼저 마이컴이 동작 가능한 상태인지 상태 체크를 한다.
                int mode = MicomModeCheck(aItem);
                if (mode < 0)
                {
                    aItem.Print(LogLevels.Info, "MICOM is not valid !!! check it and then retry.");
                    return false;
                }

                // micom이 RR mode가 아니면(NORMAL), debug mode를  ON 시킨다.
                if (ImageItem.SelBoard == 0)
                {
                    aItem.FileProgress = 10;

                    if (mode == 1 && !ShDebugMode(TargetJob.Diag_DebugOn, aItem))
                        return false;
                }

                aItem.FileProgress = 20;
                
                // Micom 업데이트 경우 정상 Normal 상태를 확인 후, RR mode이면 Update-1은 생략한다.
                if (aItem.Model.UseMicomUpdate)
                {
                    if (mode == 4) // RR mode 
                    {
                        Status(aItem, "MICOM is [RR] then skip Update-1 and go ahead.");
                        while( aItem.NextJob != TargetJob.Diag_MicomUpdate1)
                        {
                        }
                    }
                }

                aItem.FileProgress = 100;
            }
            return true;
        }

        static bool ShDiagEfsBackup(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Dictionary<string, string> result;
                int max_try = 75;
                while (max_try-- > 0)
                {
                    int ret = aItem.Tif.DiagRequest(string.Format("{0}efsbackup{0}{0}", (char)TDiagMethod.Run),
                    out result, aItem.CurrentPort.Name);

                    if (ret != 0)
                        return true;

                    aItem.FileProgress += 5;
                    Thread.Sleep(800);
                }
                return false;
            }
            return true;
        }
        
        static bool ShDiagEfsClearFlag(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Dictionary<string, string> result;
                string cmd = string.Format("{0}efsrecovery{0}{0}", (char)TDiagMethod.Run);
                int max_try = 120;
                while (max_try-- > 0)
                {
                    int ret = aItem.Tif.DiagRequest(cmd, out result, aItem.CurrentPort.Name);

                    if (ret != 0)
                        return true;

                    aItem.FileProgress += 5;
                    Thread.Sleep(500);
                }
            }
            return false;
        }

        static bool MicomModeChange(TargetItem aItem, bool aRR2Normal)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Stopwatch timer = new Stopwatch();
                const int MAX_TIME_OUT = 60 * 1000; // jwoh 10 -> 60
                const int DELAY_TIME = 1000;
                int status = 0;
                int diagcnt = 0; // jwoh Diag port open 되고 15초 후에 micom mode check
                bool mdmreboot = false; // jwoh mdm reboot후
                try
                {
                    string cmd = aRR2Normal ? "rr2normal" : "normal2rr";
                    Status(aItem, "Micom Mode change - {0}", aRR2Normal ? "RR to Normal" : "Normal to RR");
                    aItem.FileProgress = 10;
                    // start first half updating
                    Dictionary<string, string> result;
                    int max_try = 75;
                    while (max_try-- > 0)
                    {
                        if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand(cmd), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status == 1)
                            {
                                break;
                            }
                            else
                            {
                                aItem.Print(LogLevels.Info, "MICOM Mode change Error - {0}", cmd);
                                return false;
                            }
                        }
                        aItem.FileProgress  += 5;
                        Thread.Sleep(800);
                    }
                    if (max_try <= 0)
                    {
                        aItem.Print(LogLevels.Info, "Error: MICOM Mode change - {0}.", cmd);
                        return false;
                    }

                    // check update completion - first half
                    timer.Start();
                    Status(aItem, "Check mode completion - {0}", cmd);
                    while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                    {
                        if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.None) // jwoh after mdm reboot, waiting port open [
                        {
                            mdmreboot = true;
                        } // jwoh after mdm reboot, waiting port open ]
                        else if ((aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic) && mdmreboot) // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        {
                            if (diagcnt >= 15)
                            {
                                return false;
                            }
                            else
                            {
                                diagcnt++;
                            }
                        } // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        else if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status == 1)
                            {
                                aItem.Print(LogLevels.Verbose, "Micom mode is changed to {0}", cmd);
                                break;
                            }
                            else if (status != 3) // 진행 중인 아닌 (완료도 아닌) 경우
                            {
                                aItem.Print(LogLevels.Info, "MICOM mode changing report Error - {0}", cmd);
                                return false;
                            }
                        }
                        else
                        {
                            status = 3;
                        }
                        Thread.Sleep(DELAY_TIME);
                        aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                    }
                    timer.Stop();
                    if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                    {
                        aItem.Print(LogLevels.Info, "Micom mode check timeout ({0} ms) - {0}", timer.ElapsedMilliseconds, cmd);
                        if (_micomRetry == false)
                        {
                            _micomRetry = true;
                            bool ret = false;
                            if (SendMicomReset(aItem))
                                ret = MicomModeChange(aItem, aRR2Normal);
                            _micomRetry = false;
                            return ret;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    aItem.Print(LogLevels.Info, "Micom update-1 failed, reason: {0}", e.ToString());
                    return false;
                }
                finally
                {
                    timer.Reset();
                    timer = null;
                    if (!mdmreboot)
                    {
                        SendDiagEnd(aItem);
                    }
                }
            }
            return true;
        }

        // 1 = normal, 4 = rr, error = -1(2), (3)-working
        static int MicomModeCheck(TargetItem aItem)
        {
            Stopwatch timer = new Stopwatch();
            const int MAX_TIME_OUT = 60 * 1000; // jwoh 10 -> 60
            const int DELAY_TIME = 1000;
            try
            {
                Status(aItem, "Check MICOM  Mode");
                int startProg = aItem.FileProgress;
                // start first half updating
                Dictionary<string, string> result;
                int max_try = 75;
                int status = 0;
                while (max_try-- > 0)
                {
                    if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("mode"),
                        out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)
                        {
                            break;
                        }
                        else
                        {
                            aItem.Print(LogLevels.Info, "MICOM Mode Check Error, (status:{0})", status);
                            return -1;
                        }
                    }
                    Thread.Sleep(800);
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "Error: MICOM Mode Check Failed - No response. ");
                    return -1;
                }
               
                aItem.FileProgress = 100;
                aItem.TotalProgress = startProg +10;

                int mode = -1;
                // check micom mode completion
                timer.Start();
                while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                {
                    if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"),
                    out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)
                        {
                            aItem.Print(LogLevels.Info, "Micom mode is '[NORMAL]");
                            mode = 1;
                            break;
                        }
                        else if (status == 4)
                        {
                            aItem.Print(LogLevels.Info, "Micom mode is '[RR]");
                            mode = 4;
                            break;
                        }
                        else if (status == 2) // 진행 중인 아닌 (완료도 아닌) 경우 - 타겟의 체커 에러
                        {
                            aItem.Print(LogLevels.Info, "MICOM mode checking is abnormal finished.");
                            break;
                        }
                    }
                    Thread.Sleep(DELAY_TIME);
                    aItem.FileProgress = 10 + (int)((timer.ElapsedMilliseconds / 90) % 100);
                }
                timer.Stop();
                if (mode == -1 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                {
                    aItem.Print(LogLevels.Info, "MICOM Mode checking timeout ({0} ms)", timer.ElapsedMilliseconds);
                    if (_micomRetry == false)
                    {
                        _micomRetry = true;
                        if (SendMicomReset(aItem))
                            mode = MicomModeCheck(aItem);
                        _micomRetry = false;
                    }
                }
                return mode;
            }
            catch (Exception e)
            {
                aItem.Print(LogLevels.Info, "MICOM mode checking occur exception, reason: {0}", e.ToString());
                return -1;
            }
            finally
            {
                timer.Reset();
                timer = null;

                SendDiagEnd(aItem);
            }
            
        }

        static bool SendDiagEnd(TargetItem aItem)
        {
            Stopwatch timer = new Stopwatch();
            try
            {
                aItem.FileProgress = 0;
                Status(aItem, "End Diag command");
                
                // start first half updating
                Dictionary<string, string> result;
                int max_try = 20;
                int status = 0;
                while (max_try-- > 0)
                {
                    if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("endcmd"),
                        out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)
                        {
                            break;
                        }
                        else
                        {
                            aItem.Print(LogLevels.Info, "Error: End Command, (status:{0})", status);
                        }
                    }
                    aItem.FileProgress += 10;
                    Thread.Sleep(500);
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "Error: End Command Failed - No response. ");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                aItem.Print(LogLevels.Info, "Diag end command exception, reason: {0}", e.ToString());
                return false;
            }
            finally
            {
                timer.Reset();
                timer = null;
            }

        }

        static bool SendMicomReset(TargetItem aItem)
        {
            Stopwatch timer = new Stopwatch();
            const int MAX_TIME_OUT = 60 * 1000;
            const int DELAY_TIME = 500;
            try
            {
                aItem.FileProgress = 0;
                Status(aItem, "Invoke MICOM RESET");

                // start first half updating
                Dictionary<string, string> result;
                int max_try = 120;
                int status = 0;
                while (max_try-- > 0)
                {
                    if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("reset"),
                        out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status != 1)
                        {
                            aItem.Print(LogLevels.Info, "Error: MICOM RESET, (status:{0})", status);
                            return false;
                        }
                        else
                            break;
                    }
                    aItem.FileProgress += 10;
                    Thread.Sleep(500);
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "Error: MICOM RESET Failed - No response. ");
                    return false;
                }

                aItem.TotalProgress = 10;

                // check micom mode completion
                bool ret = false;
                timer.Start();
                while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                {
                    if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"),
                    out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1)  // OK
                        {
                            ret = true;
                            Thread.Sleep(500);
                            break;
                        }
                        else if (status == 3) // Pending
                        {
                        }
                        else // 진행 중인 아닌 (완료도 아닌) 경우 - 타겟에서 에러 반환
                        {
                            aItem.Print(LogLevels.Info, "MICOM RESET is failed. status:{0}", status);
                            break;
                        }
                    }
                    else
                    {
                        status = 3;
                    }
                    Thread.Sleep(DELAY_TIME);
                    aItem.FileProgress = 10 + (int)((timer.ElapsedMilliseconds / 90) % 100);
                    aItem.TotalProgress = 10 + (int)((timer.ElapsedMilliseconds * 90) / MAX_TIME_OUT);
                }
                timer.Stop();
                if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                {
                    aItem.Print(LogLevels.Info, "MICOM RESET timeout ({0} ms)", timer.ElapsedMilliseconds);
                }
                return ret;
            }
            catch (Exception e)
            {
                aItem.Print(LogLevels.Info, "MICOM RESET exception, reason: {0}", e.ToString());
                return false;
            }
            finally
            {
                timer.Reset();
                timer = null;

                SendDiagEnd(aItem);
            }

        }

        static bool ShDiagMicomUpdate1(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Stopwatch timer = new Stopwatch();
                const int MAX_TIME_OUT = 120 * 1000;
                const int DELAY_TIME = 1000;
                int status = 0;
                int micommodechangecnt = 0;
                int diagcnt = 0; // jwoh Diag port open 되고 15초 후에 micom mode check
                bool mdmreboot = false; // jwoh mdm reboot후
                try
                {
                    Status(aItem, "Wait [Update-1]");
                    Thread.Sleep(5000);

                    Status(aItem, "Request [Update-1]");
                    aItem.FileProgress = 10;
                    // start first half updating
                    Dictionary<string, string> result;
                    int max_try = 120;
                    while (max_try-- > 0)
                    {
                        if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.None) // jwoh after mdm reboot, waiting port open [
                        {
                            mdmreboot = true;
                        } // jwoh after mdm reboot, waiting port open ]
                        else if ((aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic) && mdmreboot && (diagcnt < 25)) // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        {
                            diagcnt++;
                        } // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        else if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("up1"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status != 1)
                            {
                                aItem.Print(LogLevels.Info, "MICOM update-1 report Error, step: first half updating (status:{0})", status);
                                return false;
                            }
                            else
                                break;
                        }
                        aItem.FileProgress = (aItem.FileProgress + 5) % 100;
                        Thread.Sleep(500);                       
                    }
                    diagcnt = 0;
                    mdmreboot = false;

                    if (max_try <= 0)
                    {
                        aItem.Print(LogLevels.Info, "Error: MICOM update-1 Failed - No response. ");
                        return false;
                    }


                    aItem.FileProgress = 100;
                    aItem.TotalProgress = 5;

                    // check update completion - first half
                    timer.Start();
                    Status(aItem, "Check result [Update-1]");
                    while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                    {
                        if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.None) // jwoh after mdm reboot, waiting port open [
                        {
                            mdmreboot = true;
                        } // jwoh after mdm reboot, waiting port open ]
                        else if ((aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic) && mdmreboot) // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        {
                            if (diagcnt >= 15)
                            {
                                return false;
                            }
                            else
                            {
                                diagcnt++;
                            }
                        } // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        else if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status == 1)
                            {
                                aItem.Print(LogLevels.Verbose, "Micom udpate-1 is finished successfully.");
                                break;
                            }
                            else if (status != 3) // 진행 중인 아닌 (완료도 아닌) 경우
                            {
                                aItem.Print(LogLevels.Info, "MICOM update-1 report Error, step first half checking");
                                return false;
                            }
                        }
                        else
                        {
                            status = 3;
                        }
                        aItem.TotalProgress = 5 + (int)(timer.ElapsedMilliseconds * 93 / MAX_TIME_OUT);
                        Thread.Sleep(DELAY_TIME);
                        aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                    }
                    timer.Stop();
                    if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                    {
                        aItem.Print(LogLevels.Info, "Micom update-1 check timeout ({0} ms)", timer.ElapsedMilliseconds);
                        if (_micomRetry == false)
                        {
                            _micomRetry = true;
                            bool ret = false;
                            if (SendMicomReset(aItem))
                                ret = ShDiagMicomUpdate1(aState, aItem);
                            _micomRetry = false;
                            return ret;
                        }
                    }
                    aItem.TotalProgress = 98;

                    SendDiagEnd(aItem);

                    while (micommodechangecnt < 3)
                    {
                        if (MicomModeChange(aItem, false))
                        {
                            break;
                        }
                        else
                        {
                            if (MicomModeCheck(aItem) == 4)
                            {
                                break;
                            }
                        }
                        micommodechangecnt++;
                    }
                    if (micommodechangecnt >= 3)
                        return false;

                    aItem.TotalProgress = 100;

                    return true;
                }
                catch (Exception e)
                {
                    aItem.Print(LogLevels.Info, "Micom update-1 failed, reason: {0}", e.ToString());
                    return false;
                }
                finally
                {
                    timer.Reset();
                    timer = null;       
                    
                    if ((aItem.TotalProgress != 100) && (mdmreboot == false))
                        SendDiagEnd(aItem);
                }
            }
            return true;
        }

        static bool ShDiagMicomUpdate2(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Stopwatch timer = new Stopwatch();
                const int MAX_TIME_OUT = 120 * 1000;
                const int DELAY_TIME = 1000;
                int status = 0;
                int micommodechangecnt = 0;
                int diagcnt = 0; // jwoh Diag port open 되고 15초 후에 micom mode check
                bool mdmreboot = false; // jwoh mdm reboot후
                try
                {
                    Status(aItem, "Wait [Update-2]");
                    Thread.Sleep(5000); // jwoh delay 15-> 5

                    Status(aItem, "Request [Update-2]");
                    // start first half updating
                    Dictionary<string, string> result;
                    int max_try = 120;
                    while (max_try-- > 0)
                    {
                        if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.None) // jwoh after mdm reboot, waiting port open [
                        {
                            mdmreboot = true;
                        } // jwoh after mdm reboot, waiting port open ]
                        else if ((aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic) && mdmreboot && (diagcnt < 25)) // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        {
                            diagcnt++;
                        } // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        else if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("up2"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status != 1)
                            {
                                aItem.Print(LogLevels.Info, "MICOM update-2 report Error, step: second half updating (status:{0})", status);
                                return false;
                            }
                            else
                                break;
                        }
                        aItem.FileProgress = (aItem.FileProgress + 5) % 100;
                        Thread.Sleep(500);
                    }
                    diagcnt = 0;
                    mdmreboot = false;

                    if (max_try <= 0)
                    {
                        aItem.Print(LogLevels.Info, "Error: MICOM update-2 Failed - No response. ");
                        return false;
                    }
                   
                    aItem.FileProgress = 100;
                    aItem.TotalProgress = 5;

                    // check update completion - first half
                    timer.Start();
                    Status(aItem, "Check result [Update-2]");
                    while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                    {
                        if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.None) // jwoh after mdm reboot, waiting port open [
                        {
                            mdmreboot = true;
                        } // jwoh after mdm reboot, waiting port open ]
                        else if ((aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic) && mdmreboot) // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        {
                            if (diagcnt >= 15)
                            {
                                return false;
                            }
                            else
                            {
                                diagcnt++;
                            }
                        } // jwoh mdm reboot 후 다시 diag port 잡았을때 [
                        else if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status == 1)
                            {
                                aItem.Print(LogLevels.Verbose, "Micom udpate-2 is finished successfully.");
                                break;
                            }
                            else if (status != 3) // 진행 중인 아닌 (완료도 아닌) 경우
                            {
                                aItem.Print(LogLevels.Info, "MICOM update-2 report Error, step second half checking");
                                return false;
                            }
                        }
                        else
                        {
                            status = 3;
                        }
                        aItem.TotalProgress = 5 + (int)(timer.ElapsedMilliseconds * 93 / MAX_TIME_OUT);
                        Thread.Sleep(DELAY_TIME);
                        aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                    }
                    timer.Stop();
                    if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                    {
                        aItem.Print(LogLevels.Info, "Micom update-2 check timeout ({0} ms)", timer.ElapsedMilliseconds);
                        if (_micomRetry == false)
                        {
                            _micomRetry = true;
                            bool ret = false;
                            if (SendMicomReset(aItem))
                                ret = ShDiagMicomUpdate2(aState, aItem);
                            _micomRetry = false;
                            return ret;
                        }
                    }
                    aItem.TotalProgress = 98;

                    SendDiagEnd(aItem);

                    if (!ShDebugMode(TargetJob.Diag_DebugOff, aItem)) // changed job list
                        return false;

                    while (micommodechangecnt < 3)
                    {
                        if (MicomModeChange(aItem, true))
                        {
                            break;
                        }
                        else
                        {
                            if (MicomModeCheck(aItem) == 1)
                            {
                                break;
                            }
                        }
                        micommodechangecnt++;
                    }

                    if (micommodechangecnt >= 3)
                        return false;

                    aItem.TotalProgress = 100;

                    return true;
                }
                catch (Exception e)
                {
                    aItem.Print(LogLevels.Info, "Micom update failed, reason: {0}", e.ToString());
                    return false;
                }
                finally
                {
                    timer.Reset();
                    timer = null;

                    if ((aItem.TotalProgress != 100) && (mdmreboot == false))
                        SendDiagEnd(aItem);
                }
            }
            return true;
        }

        static bool ShDiagMicomResult(TargetJob aState, TargetItem aItem)
        {
            int portwaitcnt = 0;

            while (aItem.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic) // jwoh waiting diag port [ 
            {
                aItem.Print(LogLevels.Info, "Wait port - Diagnostic");
                if (portwaitcnt == 10)
                {
                    break;
                }
                portwaitcnt++;
                Thread.Sleep(1000);
            } // jwoh waiting diag port ]

            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                try
                {
                    // check
                    Dictionary<string, string> result;
                    int max_try = 120;
                    int status;
                    while (max_try-- > 0)
                    {
                        if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("result"),
                            out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status != 1)
                            {
                                aItem.Print(LogLevels.Info, "MICOM updating result report Error, step: final result checking (status:{0})", status);
                                return false;
                            }
                            else
                                break;
                        }
                        aItem.FileProgress += 5;
                        Thread.Sleep(500);
                    }
                    if (max_try <= 0)
                    {
                        aItem.Print(LogLevels.Info, "Error: MICOM Result Failed - No response. ");
                        if (_micomRetry == false)
                        {
                            _micomRetry = true;
                            bool ret = false;
                            if (SendMicomReset(aItem))
                                ret = ShDiagMicomResult(aState, aItem);
                            _micomRetry = false;
                            return ret;
                        }
                    }

                    aItem.FileProgress = 100;
                    aItem.TotalProgress = 100;
                }
                finally
                {
                    SendDiagEnd(aItem);
                }
            }
            return true;
        }

        static bool ShDiagVersionInfo(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic)
                return true;

            const int intervalTime = 500;
            const int totalWaitTime = 180 * 1000;

            Stopwatch timer = new Stopwatch();
            int cnt = 0;
            try
            {
                timer.Start();

                while (totalWaitTime > timer.ElapsedMilliseconds)
                {
                    cnt += 10;
                    Dictionary<string, string> result;
                    if (aItem.Tif.DiagRequest(string.Format("{0}version{0}{0}", (char)TDiagMethod.Get),
                       out result, aItem.CurrentPort.Name) != 0)
                    {
                        if (result.Count > 0)
                        {
                            return true;
                        }

                        Status(aItem, "Modem is not ready ...");
                    }
                    else
                    {
                        Status(aItem, "Yet, no response");
                        Thread.Sleep(1000);
                    }

                    aItem.FileProgress = cnt % 100;
                    Thread.Sleep(intervalTime);

                    aItem.TotalProgress = (int)(100 * timer.ElapsedMilliseconds / totalWaitTime);
                }
                return false;
            }
            finally
            {
                timer.Reset();
                timer = null;
            }
        }

        static bool ShReboot(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic)
                return true;

            string cmd = "ereboot";
            if (aState == TargetJob.Diag_RebootNormal)
                cmd = "nreboot";

            Dictionary<string, string> result;
            int max_try = 120;
            while (max_try-- > 0)
            {
                if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                {
                    if (aItem.Tif.DiagRequest(string.Format("{0}{1}{0}{0}", (char)TDiagMethod.Run, cmd),
                        out result, aItem.CurrentPort.Name) != 0)
                        break;
                }
                aItem.FileProgress += 5;
                Thread.Sleep(500);
            }
            if (max_try <= 0)
            {
                aItem.Print(LogLevels.Info, "Reboot request Failed through diagonostic port");
                return false;
            }

            //  포트가 제거될때까지 대기.(노멀 리부팅은 시간이 오래 걸리므로 리부팅 처리 확인 후, 다음 단계 진행)
            if (aState == TargetJob.Diag_RebootNormal)
            {
                aItem.TotalProgress = 25;

                max_try = 75;
                string curPortName = aItem.CurrentPort.Name;
                while (max_try-- > 0)
                {
                    SerialportWatcher.UsbPortChanged(null);
                    if (!SerialportWatcher.Ports.Any(x => x.Name == curPortName))
                        break;

                    Thread.Sleep(800);
                    aItem.FileProgress = (aItem.FileProgress + 10) % 100;
                    aItem.TotalProgress = 25 + (75 - max_try);
                }
            }

            return true;
        }
        
        static bool ShDebugMode(TargetJob aState, TargetItem aItem)
        {
            int portwaitcnt = 0;
            bool skipendcmd = false;
            int status = 0;

            while (aItem.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic) // jwoh waiting diag port [
            {
                aItem.Print(LogLevels.Info, "Wait port - Diagnostic");
                if (portwaitcnt == 10)
                {
                    break;
                }
                portwaitcnt++;
                Thread.Sleep(1000);
            } // jwoh waiting diag port ]

            try
            {
                string cmd = "dbgon";
                if (aState == TargetJob.Diag_DebugOff)
                    cmd = "dbgoff";

                Status(aItem, "Debug mode => {0}", cmd);
                var requestString = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}", (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, cmd, (char)TDiagSeperator.Item);

                Dictionary<string, string> result;
                int max_try = 60;
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
                        aItem.FileProgress += 5;
                        Status(aItem, "Debug mode => {0}, Retry until {1} times", cmd, max_try);
                        aItem.Print(LogLevels.Info, "DebugMode changing request Failed through diagonostic port : {0}", cmd); // jwoh change log level - error->info
                        Thread.Sleep(1000);
                    }
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "Error: DebugMode changing request Failed : {0} ", cmd);
                    skipendcmd = true; // jwoh endcmd를 실행할 경우 true로 return 될 가능성 있음
                    return false;
                }
                System.Threading.Thread.Sleep(1000); // jwoh

                max_try = 75;
                aItem.FileProgress = 0;
                while (max_try-- > 0)
                {
                    var checkCmd = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}",
                        (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, "check", (char)TDiagSeperator.Item);

                    if (aItem.Tif.DiagRequest(checkCmd, out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 1) // OK
                        {
                            Status(aItem, "Mode is changed to {0}", cmd);
                            return true;
                        }
                        else if (status != 3) // FAIL
                        {
                            aItem.Print(LogLevels.Info, "debug mode changing occur errors");
                            skipendcmd = true; // jwoh endcmd를 실행할 경우 true로 return 될 가능성 있음
                            break;
                        }
                    }
                    else
                    {
                        status = 3;
                    }
                    aItem.FileProgress += 5;
                    System.Threading.Thread.Sleep(800);
                }

                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "debug mode changing is timeout");
                    if (_micomRetry == false)
                    {
                        _micomRetry = true;
                        bool ret = false;
                        if (SendMicomReset(aItem))
                            ret = ShDebugMode(aState, aItem);
                        _micomRetry = false;
                        return ret;
                    }
                }
            }
            finally
            {
                if (!skipendcmd)
                    SendDiagEnd(aItem);
            }

            return false;
        }

        static bool ShDloadMicom(TargetJob aState, TargetItem aItem)
        {
            List<ImageItem> orgList = aItem.Items;
            try
            {
                // install prog event
                aItem.InstallProgressHandler(true);
                aItem.Items = ImageItem.Load(aItem.ConfigPath);
                if (aItem.Items == null)
                {
                    aItem.Print(LogLevels.Info, "MICOM image - Load config xml Error (Path:{0}.", aItem.ConfigPath);
                    return false;
                }

                if (!aItem.Items.Any(x => x.Name.StartsWith("MICOM")))
                {
                    aItem.Print(LogLevels.Info, "MICOM partition not found in configuration xml, Path:{0}.", aItem.ConfigPath);
                    return false;
                }
                
                lock (_so)
                {
                    // 옵션 설정
                    ImageItem.Reset = 3; // 항상 리부팅.
                    ImageItem.AllErase = false;

                    foreach (var m in aItem.Items)
                    {
                        if (m.Name.StartsWith("MICOM"))
                        {
                             m.IsExist = File.Exists(Path.Combine(ImageItem.Dir, m.FileName));
                            if (!m.IsExist)
                            {
                                aItem.Print(LogLevels.Info, "{0} image - {1}, Existance:{2}", m.Name, m.FileName, m.IsExist);
                            }
                            m.Use = m.Erase = m.IsExist;
                        }
                        else
                        {
                            m.Use = m.Erase = false;
                        }
                    }
                    if (!aItem.Items.Any(x => x.Use))
                    {
                        aItem.Print(LogLevels.Info, "MICOM iamges are not found");
                        return false;
                    }
                    // ImageItem 저장
                    if (!ImageItem.Save(aItem.ConfigPath, aItem.Items))
                    {
                        aItem.Print(LogLevels.Info, "configuration xml saving error");
                        return false;
                    }
                }
                // 파일 사용 여부 설정 (select micom only)
                aItem.Print(LogLevels.Info, "====== Download Images infomation ======");
                aItem.Print(LogLevels.Info, "Option : AllErase={0}, SkipEraseEFS:{1}, Reboot:{2}, LogLevel:{3}",
                    ImageItem.AllErase, ImageItem.SkipEraseEfs, ImageItem.Reset, Log.LogLevel);
                var mi = aItem.Items.Find(x => x.Name == "MICOM");
                aItem.Print(LogLevels.Info, "Name:{0} File:{1}, File Existance:{2}", mi.Name, mi.FileName, mi.IsExist);

                // 다운로드 
                if (aItem.Tif.RundDownloadSync(aItem.CurrentPort.Name, aItem.ConfigPath) == 0)
                {
                    Status(aItem, "Downloading failed");
                    return false;
                }
            }
            finally
            {
                // uninstall prog event
                aItem.InstallProgressHandler(false);
                // restore options
                lock (_so)
                {
                    ImageItem.AllErase = Properties.Settings.Default.MAllErase;
                }
                aItem.Items = orgList;// 복구
            }
            return true;
        }

        static bool ShDloadAll(TargetJob aState, TargetItem aItem)
        {
            try
            {
                // install prog event
                aItem.InstallProgressHandler(true);

                lock (_so)
                {
                    // 옵션 설정
                    ImageItem.Reset = 3; // 항상 리부팅.

                    // ImageItem 저장
                    // set fota erase option 
                    var fxitem = aItem.Items.SingleOrDefault(fx => fx.Name == "FOTA_SELFTEST");
                    if (fxitem != null)
                        fxitem.Erase = aItem.Model.FotaErase;
                    if (!ImageItem.Save(aItem.ConfigPath, aItem.Items))
                    {
                        aItem.Print(LogLevels.Info, "configuration xml saving error");
                        return false;
                    }
                    fxitem.Erase = false;
                }
                // 파일 사용 여부 설정
                aItem.Print(LogLevels.Info, "====== Download Images infomation ======");
                aItem.Print(LogLevels.Info, "Option : AllErase={0}, SkipEraseEFS:{1}, Reboot:{2}, LogLevel:{3}",
                    ImageItem.AllErase, ImageItem.SkipEraseEfs, ImageItem.Reset, Log.LogLevel);
                foreach (var m in aItem.Items)
                {
                    if (m.Use)
                        aItem.Print(LogLevels.Info, "Name:{0} File:{1}, Existance:{2}", m.Name, m.FileName, m.IsExist);
                }

                // 다운로드
                if (aItem.Tif.RundDownloadSync(aItem.CurrentPort.Name, aItem.ConfigPath) == 0)
                {
                    Status(aItem, "Downloading failed");
                    return false;
                }
            }
            finally
            {
                // uninstall prog event
                aItem.InstallProgressHandler(false);
                // restore options
                lock (_so)
                {
                }
            }
            return true;
        }

        static bool ShWaitPort_Diag(TargetJob aState, TargetItem aItem)
        {
            const int intervalTime = 500;
            int totalWaitTime = 60 * 1000;

            bool result = false;

            SerialportWatcher.PortKind kind = SerialportWatcher.PortKind.None;
            if (aState == TargetJob.WaitPort_Diag)
                kind = SerialportWatcher.PortKind.Diagnostic;

            if (kind == SerialportWatcher.PortKind.None)
            {
                return true;
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (totalWaitTime > timer.ElapsedMilliseconds)
            {
                aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                if (aItem.CurrentPort.Kind == kind)
                {
                    Status(aItem, "Port:{0} is found ({1}).", kind, aItem.CurrentPort.Name);
                    aItem.TotalProgress = 100;
                    result = true;
                    break;
                }

                Thread.Sleep(intervalTime);
                aItem.TotalProgress = (int)(100 * timer.ElapsedMilliseconds / totalWaitTime);
            }
            timer.Reset();
            timer = null;

            if (result == false && ReloadPorts(aItem, kind))
            {
                Status(aItem, "Port::{0} is found ({1}).", kind, aItem.CurrentPort.Name);
                aItem.TotalProgress = 100;
                result = true;
            }

            if (!result)
                Status(aItem, "Port:{0}/{1} is not found ERROR.", kind, aItem.CurrentPort.Name);

            return result;
        }

        static bool ShWaitPort_Qloader(TargetJob aState, TargetItem aItem)
        {
            const int intervalTime = 500;
            int totalWaitTime = 60 * 1000;
            int portnoncnt = 0;

            bool result = false;

            SerialportWatcher.PortKind kind = SerialportWatcher.PortKind.None;
            if (aState == TargetJob.WaitPort_Qloader)
                kind = SerialportWatcher.PortKind.QDLoader;

            if (kind == SerialportWatcher.PortKind.None)
            {
                return true;
            }
            
            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (totalWaitTime > timer.ElapsedMilliseconds)
            {
                aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                if (aItem.CurrentPort.Kind == kind)
                {
                    Status(aItem, "Port:{0} is found ({1}).", kind, aItem.CurrentPort.Name);
                    aItem.TotalProgress = 100;
                    result = true;
                    break;
                }
                else if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
                {
                    if (portnoncnt != 0 || timer.ElapsedMilliseconds >= 59000)
                    {
                        result = false;
                        break;
                    }
                }
                else if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.None)
                {
                    portnoncnt++;
                }

                Thread.Sleep(intervalTime);
                aItem.TotalProgress = (int)(100 * timer.ElapsedMilliseconds / totalWaitTime);
            }
            timer.Reset();
            timer = null;

            if (result == false && ReloadPorts(aItem, kind))
            {
                Status(aItem, "Port::{0} is found ({1}).", kind, aItem.CurrentPort.Name);
                aItem.TotalProgress = 100;
                result = true;
            }

            if (!result)
                Status(aItem, "Port:{0}/{1} is not found ERROR.", kind, aItem.CurrentPort.Name);

            return result;
        }

        static bool ShEnd(TargetJob aState, TargetItem aItem)
        {
            Status(aItem, "Completed - {0}.", aItem.Result >= 0 ? "SUCCESS" : "FAIL");

            if ((Model.TabId == TabType.Multi) && (ImageItem.SelBoard == 1)) // jwoh add User/Factory mode [
                (Model as MultiModel).RemoveDebugPortList(aItem); // jwoh add User/Factory mode ]

            aItem.ReleaseTargetWrapper();

            return true;
        }

        // jwoh add GB key erase function [
        static bool ShMicomKeyErase(TargetJob aState, TargetItem aItem) 
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Stopwatch timer = new Stopwatch();
                const int MAX_TIME_OUT = 10 * 1000;
                const int DELAY_TIME = 1000;
                int status = 0;
                try
                {
                    Thread.Sleep(7000);
                    Status(aItem, "Micom Master key / Unlock Key erase");
                    aItem.FileProgress = 10;
                    // start first half updating
                    Dictionary<string, string> result;
                    int max_try = 20;
                    while (max_try-- > 0)
                    {
                        if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("mkeyerase"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status != 1)
                            {
                                aItem.Print(LogLevels.Info, "Error: MICOM Key Erase , (status:{0})", status);
                                return false;
                            }
                            break;
                        }
                        aItem.FileProgress += 5;
                        Thread.Sleep(800);
                    }
                    if (max_try <= 0)
                    {
                        aItem.Print(LogLevels.Info, "Error: MICOM Key Erase - No response.");
                        return false;
                    }

                    timer.Start();
                    Status(aItem, "Completion Key erase cmd ");
                    while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                    {
                        if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"),
                        out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status == 1)
                            {
                                aItem.Print(LogLevels.Verbose, "Micom Master key / Unlock key Erase OK!");
                                break;
                            }
                            else if (status != 3) // 진행 중인 아닌 (완료도 아닌) 경우
                            {
                                aItem.Print(LogLevels.Info, "MICOM Key Erase is abnormal finished.");
                                return false;
                            }
                        }
                        Thread.Sleep(DELAY_TIME);
                        aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                    }
                    timer.Stop();
                    if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                    {
                        aItem.Print(LogLevels.Info, "Micom Key Erase timeout ({0} ms)", timer.ElapsedMilliseconds);
                        if (_micomRetry == false)
                        {
                            _micomRetry = true;
                            bool ret = false;
                            if (SendMicomReset(aItem))
                                ret = ShMicomKeyErase(aState, aItem);
                            _micomRetry = false;
                            return ret;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    aItem.Print(LogLevels.Info, "Micom Key Erase failed, reason: {0}", e.ToString());
                    return false;
                }
                finally
                {
                    timer.Reset();
                    timer = null;

                    SendDiagEnd(aItem);
                }
            }
            return true;
        }

        static bool ShMicomKeyEraseCheck(TargetJob aState, TargetItem aItem)
        {
            if (aItem.CurrentPort.Kind == SerialportWatcher.PortKind.Diagnostic)
            {
                Stopwatch timer = new Stopwatch();
                const int MAX_TIME_OUT = 10 * 1000;
                const int DELAY_TIME = 1000;
                int status = 0;
                try
                {
                    Status(aItem, "Micom Master key / Unlock Key erase check");
                    aItem.FileProgress = 10;
                    // start first half updating
                    Dictionary<string, string> result;
                    int max_try = 20;
                    while (max_try-- > 0)
                    {
                        if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("mkeyerasecheck"), out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status != 1)
                            {
                                aItem.Print(LogLevels.Info, "Error: MICOM Key Erase Check, (status:{0})", status);
                                return false;
                            }
                            break;
                        }
                        aItem.FileProgress += 5;
                        Thread.Sleep(800);
                    }
                    if (max_try <= 0)
                    {
                        aItem.Print(LogLevels.Info, "Error: MICOM Key Erase Check - No response.");
                        return false;
                    }

                    timer.Start();
                    Status(aItem, "Completion Key erase check cmd ");
                    while (timer.ElapsedMilliseconds < MAX_TIME_OUT)
                    {
                        if (aItem.Tif.DiagRequest(TargetWrapper.MicomCommand("check"),
                        out result, aItem.CurrentPort.Name) != 0)
                        {
                            status = Convert.ToInt32(result["status"]);
                            if (status == 1)
                            {
                                aItem.Print(LogLevels.Verbose, "Micom Master key / Unlock key Erase Check OK!");
                                break;
                            }
                            else if (status != 3) // 진행 중인 아닌 (완료도 아닌) 경우
                            {
                                aItem.Print(LogLevels.Info, "MICOM Key Erase Check is abnormal finished.");
                                return false;
                            }
                        }
                        Thread.Sleep(DELAY_TIME);
                        aItem.FileProgress = (int)((timer.ElapsedMilliseconds / 100) % 100);
                    }
                    timer.Stop();
                    if (status == 3 && timer.ElapsedMilliseconds >= MAX_TIME_OUT)
                    {
                        aItem.Print(LogLevels.Info, "Micom Key Erase check timeout ({0} ms)", timer.ElapsedMilliseconds);
                        if (_micomRetry == false)
                        {
                            _micomRetry = true;
                            bool ret = false;
                            if (SendMicomReset(aItem))
                                ret = ShMicomKeyEraseCheck(aState, aItem);
                            _micomRetry = false;
                            return ret;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    aItem.Print(LogLevels.Info, "Micom Key Erase check failed, reason: {0}", e.ToString());
                    return false;
                }
                finally
                {
                    timer.Reset();
                    timer = null;

                    SendDiagEnd(aItem);
                }
            }
            return true;
        }
        // jwoh add GB key erase function ]

        static bool ShPlatformIdCheck(TargetJob aState, TargetItem aItem)
        {
            int portwaitcnt = 0;
            int status = 0;

            while (aItem.CurrentPort.Kind != SerialportWatcher.PortKind.Diagnostic) // jwoh waiting diag port [
            {
                aItem.Print(LogLevels.Info, "Wait port - Diagnostic");
                if (portwaitcnt == 60)
                {
                    break;
                }
                portwaitcnt++;
                Thread.Sleep(1000);
            } // jwoh waiting diag port ]

            try
            {
                string cmd = "platformid";

                var requestString = string.Format("{0}chmode{0}cmd{1}{2}{3}{0}", (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, cmd, (char)TDiagSeperator.Item);

                Dictionary<string, string> result;
                int max_try = 120;
                while (max_try-- > 0)
                {
                    if (aItem.Tif.DiagRequest(requestString, out result, aItem.CurrentPort.Name) != 0)
                    {
                        status = Convert.ToInt32(result["status"]);
                        if (status == 0)
                        {
                            aItem.Print(LogLevels.Verbose, "Valid Platform ID.");
                            break;
                        }
                        else
                        {
                            aItem.Print(LogLevels.Verbose, "Invalid Platform ID.");
                        }
                    }
                    else
                    {
                        aItem.Print(LogLevels.Info, "Platform ID Failed through diagonostic port");
                    }
                    // error
                    aItem.FileProgress += 5;
                    Thread.Sleep(1000);
                }
                if (max_try <= 0)
                {
                    aItem.Print(LogLevels.Info, "Error: Platform ID request Failed : {0} ", cmd);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            finally
            {
                SendDiagEnd(aItem);
            }
        }

        public static void Status(TargetItem aItem, string aFormat, params object[] args)
        {
            var msg = string.Format(aFormat, args);

            aItem.StatusText = string.Format("<{0}> {1}", aItem.CurrentJob, msg);
            aItem.Print(LogLevels.Info, aItem.StatusText);
        }

        static bool ReloadPorts(TargetItem aItem, SerialportWatcher.PortKind aKind)
        {
            // 포트를 새로 가져온다.
            var map = SerialportWatcher.ReadUsbPorts();
            SerialportWatcher.UsbPortChanged(null, true, map);

            // 현재 포트와 매치되는 것이 있는지 확인한다.
            foreach (var p in SerialportWatcher.Ports)
            {
                if (aItem.MatchPort(p))
                {
                    aItem.CurrentPort = p;

                    if (aItem.CurrentPort.Kind == aKind)
                        return true;
                }
            }

            // 포트는 인식 되었으나 경로 정보를 아직 못 가지고 온 경우이고 타겟이 하나이면 인식으로 처리한다.
            var mmodel = aItem.Model as MultiModel;
            if (mmodel != null && mmodel.TargetItems.Count == 1)
            {
                foreach(var pi in map)
                {
                    var pinfo = SerialportWatcher.PortInfo.Parse(pi.Key, pi.Value);
                    if (pinfo.Kind == aKind)
                    {
                        aItem.Print(LogLevels.Info, "Path Mismatched: Current:{0}={1}, Detected:{2}={3}",
                        aItem.CurrentPort.Name, aItem.CurrentPort.Path, pinfo.Name, pinfo.Path);
                        aItem.CurrentPort = pinfo;
                        return true;
                    }
                }
            }

            return false;
        }

        public static DialogResult InfoPopup(string sText, string sCaption)
        {
            DialogResult obj;

            obj = System.Windows.Forms.MessageBox.Show(new WindowWrapper(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle), sText, sCaption,
                MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

            return obj;
        }

        static object _so = new object();
    }

    public enum TargetJob : int
    {
        None = 0,
        Ready,
        TargetPrepare,
        Diag_EFS_Backup,
        Diag_EFS_ClearFlag,
        Diag_MicomUpdate1,
        Diag_MicomUpdate2,
        Diag_MicomResult,
        Diag_VersionInfo,
        Diag_RebootEDL,
        Diag_RebootNormal,
        Diag_DebugOn,
        Diag_DebugOff,
        Dload_Micom,
        Dload_All,
        WaitPort_Diag,
        WaitPort_Qloader,
        End,
        Diag_MicomKeyErase, // jwoh add GB key erase function
        Diag_MicomKeyEraseCheck, // jwoh add GB key erase check function
        Diag_PlatformID, // jwoh platfom id check
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
    ////////////
}
