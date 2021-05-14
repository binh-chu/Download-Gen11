using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lge.Tools.Download
{
    public delegate void LogHandler(LogLevels aLevel, LogItem aItem);
    public static class Log
    {
        public static void RegisterTargetEvent(TargetWrapper aTif, bool aInstall = true)
        {
            if (aInstall)
                aTif.AppLogEvent += TargetWrapper_AppLogEvent;
            else
                aTif.AppLogEvent -= TargetWrapper_AppLogEvent;
        }

        private static void TargetWrapper_AppLogEvent(LogLevels aLogLevel, string aMsg)
        {
            if (LogLevel >= (int)aLogLevel)
                writeLine(aLogLevel, aMsg);
        }

        public static void i(string aMsg, params object[] args)
        {
            if (LogLevel >= (int)LogLevels.Info)
                writeLine(LogLevels.Info, aMsg, args);
        }

        public static void v(string aMsg, params object[] args)
        {
            if (LogLevel >= (int)LogLevels.Verbose)
                writeLine(LogLevels.Verbose, aMsg, args);
        }

        public static void e(string aMsg, params object[] args)
        {
            if (LogLevel >= (int)LogLevels.Error)
                writeLine(LogLevels.Error, aMsg, args);
        }

        public static void a(string aMsg, params object[] args)
        {
            writeLine(LogLevels.None, aMsg, args);
        }

        private static int _logFailCount = 0;
        public static void writeLine(LogLevels aLevel, string aMsg, params object[] args)
        {
            try
            {
                var msg = string.Format(aMsg, args);
                var log = new LogItem(DateTime.Now, msg);

                Log.Save(log);

                if (LogEvent != null)
                {
                    LogEvent(aLevel, log);
                }
            }
            catch(Exception e)
            {
                _logFailCount++;
                if (_logFailCount < 3)
                    Log.MsgBox("Log write error: check log file - {0} \n\nException:{1}", Log.LogPath, e);

            }
        }

        public static void Save(LogItem aItem)
        {
            try
            {
                if (_logPath == null)
                    _logPath = LogPath;

                lock (_logPath)
                {
                    using (var fsave = new System.IO.StreamWriter(LogPath, true, Encoding.Default))
                    {
                        fsave.WriteLine(string.Format("[{0}] {1}", aItem.Time.ToString("yyyy-MM-dd HH:mm:ss"), aItem.Message));
                    }
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        public static string LogPath
        {
            get
            {
                if (_logPath == null)
                    _logPath = Helper.LogFile;
                return _logPath;
            }
        }

        public static event LogHandler LogEvent;
        private static string _logPath = null;

        public static int LogLevel { get; set; }

        public static void MsgBox(string aMsgFormat, params object[] args)
        {
            var msg = string.Format(aMsgFormat, args);
            Log.v(msg);

            Extension.UIThread(delegate
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, 
                    msg, "Gen11 Tools"
                    ,System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information
                    ,System.Windows.MessageBoxResult.OK, System.Windows.MessageBoxOptions.None);
            });
        }
    }


    public class LogItem
    {
        public LogItem(DateTime aTime, string aMsg)
        {
            this.Time = aTime;
            this.Message = aMsg;
        }

        public LogItem(string aMsg)
        {
            this.Time = DateTime.Now;
            this.Message = aMsg;
        }

        public string Message { get; private set; }
        public DateTime Time { get; private set; }

        public override string ToString()
        {
            return string.Format("Log: {0} {1}", Time.ToString("HH:mm:ss"), Message);
        }
    }

    public enum LogLevels : int
    {
        None = 0,
        Error = 1,
        Info = 2,
        Verbose = 3,
    }
}
