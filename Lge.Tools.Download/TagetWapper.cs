using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Lge.Tools.Download
{

    public delegate void AppLogHandler(LogLevels aLogLevel, string aMsg);
    public delegate void AppProgressHandler(ProgressArgs arg);

    public class TargetWrapper : IDisposable
    {
        // Callback delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void fnLogHandler(int aLogLevel, string aMsg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void fnProgressHandler(int aImageID, UInt64 aSentBytes, UInt64 aTotalBytes, int aExtraInfo);

        // Interface functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int fnGetDllVersion( );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        delegate int fnRunDownload(   string aPortName,
                                   string aXmlPah, 
                                    fnLogHandler aLogHandler, fnProgressHandler aProgressHandler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        delegate int fnRunReadback(string aPortName,
                                   string aXmlPah,
                                    fnLogHandler aLogHandler, fnProgressHandler aProgressHandler);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        delegate int fnDiagRequest( string aReqCommand, StringBuilder aResultBuffer,  int aResultSize,
                                    string aPortName,
                                    fnLogHandler aLogHandler,
                                    int aLogLevel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr fnGetTargetInterface();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct NaviveTarget
        {
            public fnGetDllVersion  GetDllVersion;
            public fnRunDownload    RunDownload;
            public fnRunReadback    RunReadback;
            public fnDiagRequest    DiagRequest;
        }

        public TargetWrapper()
        {
        }


        ~TargetWrapper()
        {
            Dispose(false);
        }

        public event AppLogHandler       AppLogEvent;
        public event AppProgressHandler         AppProgressEvent;
        static public bool IsRunning { get { return _runCount > 0; } }
        static object _runSync = new object();
        static int _runCount = 0;

        public bool Load(string aDllPath)
        {
            try
            {
                this.GetTargetInterface(aDllPath);
                return true;
            }
            catch(Exception e)
            {
                Log.e("Protocol Library Loading Exception({0}): - {1} ", aDllPath, e );
            }
            return false;
        }

        public int GetVersion()
        {
            if (_libHandle == IntPtr.Zero)
                throw new ApplicationException("Not yet loaded library.");

            return _target.GetDllVersion();
        }

        public int RundDownload(string aPortName, string aXmlPah)
        {
            if (_libHandle == IntPtr.Zero)
                throw new ApplicationException("Not yet loaded library.");

            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(x => RunDownloadTask(aPortName, aXmlPah)));

            return 0; 
        }

        public int RundDownloadSync(string aPortName, string aXmlPah)
        {
            if (_libHandle == IntPtr.Zero)
                throw new ApplicationException("Not yet loaded library.");

            return RunDownloadTask(aPortName, aXmlPah);
        }

        public int RundReadback(string aPortName, string aXmlPah)
        {
            if (_libHandle == IntPtr.Zero)
                throw new ApplicationException("Not yet loaded library.");

            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(x => RunReadbackTask(aPortName, aXmlPah)));

            return 0;
        }

        public int DiagRequest(string aReqCommand, out Dictionary<string,string> aResult, string aPortName)
        {
            lock (_runSync)
                _runCount++;

            int ret = 0;
            aResult = null;
            StringBuilder sb = new StringBuilder(1024 * 8);

            if (_target.DiagRequest(aReqCommand, sb, sb.Capacity, aPortName, this.LogCallback, Log.LogLevel) != 0)
            {
                var rItems = new Dictionary<string, string>();
                if (sb.Length > 0)
                {
                    var list = sb.ToString().Split((char)TDiagSeperator.Item);
                    foreach (var line in list)
                    {
                        var pair = line.Split((char)TDiagSeperator.Pair);
                        if (pair != null && pair.Length == 2)
                        {
                            rItems[pair[0]] = pair[1];
                        }
                    }                    
                }
                aResult = rItems;
                ret = 1;
            }

            lock (_runSync)
            {
                _runCount--;
            }
            return ret;
        }

        private int RunDownloadTask(string aPortName, string aXmlPah)
        {
            lock(_runSync)
                _runCount++;

            int ret = _target.RunDownload(aPortName, aXmlPah, this.LogCallback, this.ProgressCallback);

            lock (_runSync)
            {
                _runCount--;
            }
            return ret;
        }

        private int RunReadbackTask(string aPortName, string aXmlPah)
        {
            lock (_runSync)
                _runCount++;

            int ret = _target.RunReadback(aPortName, aXmlPah, this.LogCallback, this.ProgressCallback);

            lock (_runSync)
            {
                _runCount--;
            }
            return ret;
        }

        private void GetTargetInterface(string aDllPath)
        {
            _libHandle = TargetWrapper.LoadLibrary(aDllPath);

            IntPtr hAddr = GetProcAddress(_libHandle, "GetTargetInterface");

            fnGetTargetInterface entry = (fnGetTargetInterface)Marshal.GetDelegateForFunctionPointer(hAddr, typeof(fnGetTargetInterface));
            IntPtr hTarget = entry();
            _target = (NaviveTarget)Marshal.PtrToStructure(hTarget, _target.GetType());

        }

        private void LogCallback(int aLogLevel, string aMsg)
        {
            try
            {
                if (AppLogEvent != null)
                    AppLogEvent((LogLevels)aLogLevel, aMsg);
            }
            catch(Exception e)
            {
                Log.e("LogCalback Exception: " + e.ToString());
            }
        }

        private void ProgressCallback(int aImageID, UInt64 aSentBytes, UInt64 aTotalBytes, int aExtraInfo)
        {
            try
            { 
                if (AppProgressEvent != null)
                    AppProgressEvent(new ProgressArgs( aImageID, aSentBytes, aTotalBytes, aExtraInfo));
            }
            catch (Exception e)
            {
                Log.e("ProgressCallback Exception: " + e.ToString());
            }
        }

        private IntPtr _libHandle = IntPtr.Zero;
        NaviveTarget _target;

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static private extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static private extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static private extern bool FreeLibrary(IntPtr hModule);

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.AppLogEvent = null;
                    this.AppProgressEvent = null;
                }
                if (this._libHandle != IntPtr.Zero)
                {
                    FreeLibrary(_libHandle);
                    _libHandle = IntPtr.Zero;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public static string MicomCommand(string aReqCmd)
        {
            return string.Format("{0}micomup{0}cmd{1}{2}{3}{0}",
                    (char)TDiagMethod.Run, (char)TDiagSeperator.Pair, aReqCmd, (char)TDiagSeperator.Item);
        }
    }

    public class ProgressArgs : EventArgs
    {
        public ProgressArgs(int aImageID, UInt64 aSentBytes, UInt64 aTotalBytes, int aExtraInfo)
        {
            this.ImageID = aImageID;
            this.SentBytes = aSentBytes;
            this.TotalBytes = aTotalBytes;
            this.ExtraInfo = aExtraInfo;
        }

        public int ImageID { get; private set; }
        public ulong SentBytes { get; private set; }
        public ulong TotalBytes { get; private set; }
        public int ExtraInfo { get; private set; }
    }

    public enum TDiagMethod : short
    {
        Unknown = 0,
        Get = (short)'?',
        Set = (short)'=',
        Run = (short)'>'
    }

	public enum TDiagSeperator : short
    {
        Pair = (short)']',
        Item = (short)'\r',        
    }

}
