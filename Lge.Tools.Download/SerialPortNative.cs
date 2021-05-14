using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Lge.Tools.Download
{

    public class SerialPortNative : IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {
            if (_handle != null)
            {
                _handle.Close();
                _handle = null;
            }
        }

        #endregion

        #region Implementation

        private const int DcbFlagAbortOnError = 14;
        private const int CommStateRetries = 10;
        private SafeFileHandle _handle;

        public SerialPortNative(string portName, uint uartbaudrate)
        {
            const int dwFlagsAndAttributes = 0;// 0x40000000;
            const int dwAccess = unchecked((int)0xC0000000);

            if ((portName == null) || !portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid Serial Port", "portName");
            }
            SafeFileHandle hFile = CreateFile(@"\\.\" + portName, dwAccess, 0, IntPtr.Zero, 3, dwFlagsAndAttributes,
                                                IntPtr.Zero);
            if (hFile.IsInvalid)
            {
                WinIoError();
            }
            try
            {
                int fileType = GetFileType(hFile);
                if ((fileType != 2) && (fileType != 0))
                {
                    throw new ArgumentException("Invalid Serial Port", "portName");
                }
                _handle = hFile;
                InitializeDcb(uartbaudrate);
            }
            catch
            {
                hFile.Close();
                _handle = null;
                throw;
            }
        }

        public bool Write(string aValues)
        {
            var bytes = Encoding.ASCII.GetBytes(aValues);

            return Write(bytes);
        }

        public bool Write (byte[] aValues)
        {
            uint written;
            return WriteFile(this._handle, aValues, (uint)aValues.Length, out written, IntPtr.Zero);
        }

        public bool Flush()
        {
            return FlushFileBuffers(this._handle);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int FormatMessage(int dwFlags, HandleRef lpSource, int dwMessageId, int dwLanguageId,
                                                StringBuilder lpBuffer, int nSize, IntPtr arguments);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetCommState(SafeFileHandle hFile, ref Dcb lpDcb);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetCommState(SafeFileHandle hFile, ref Dcb lpDcb);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ClearCommError(SafeFileHandle hFile, ref int lpErrors, ref Comstat lpStat);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode,
                                                        IntPtr securityAttrs, int dwCreationDisposition,
                                                        int dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer,  uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten,  IntPtr aOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(SafeFileHandle hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetFileType(SafeFileHandle hFile);

        private void InitializeDcb(uint baudrate)
        {
            Dcb dcb = new Dcb();
            GetCommStateNative(ref dcb);
            dcb.Flags = 1; // only binary mode
            dcb.BaudRate = baudrate;
            dcb.Parity = 0;
            dcb.StopBits = 0;
            dcb.ByteSize = 8;
            SetCommStateNative(ref dcb);
        }

        private static string GetMessage(int errorCode)
        {
            StringBuilder lpBuffer = new StringBuilder(0x200);
            if (
                FormatMessage(0x3200, new HandleRef(null, IntPtr.Zero), errorCode, 0, lpBuffer, lpBuffer.Capacity,
                                IntPtr.Zero) != 0)
            {
                return lpBuffer.ToString();
            }
            return "Unknown Error";
        }

        private static int MakeHrFromErrorCode(int errorCode)
        {
            return (int)(0x80070000 | (uint)errorCode);
        }

        private static void WinIoError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new IOException(GetMessage(errorCode), MakeHrFromErrorCode(errorCode));
        }

        private void GetCommStateNative(ref Dcb lpDcb)
        {
            int commErrors = 0;
            Comstat comStat = new Comstat();

            for (int i = 0; i < CommStateRetries; i++)
            {
                if (!ClearCommError(_handle, ref commErrors, ref comStat))
                {
                    WinIoError();
                }
                if (GetCommState(_handle, ref lpDcb))
                {
                    break;
                }
                if (i == CommStateRetries - 1)
                {
                    WinIoError();
                }
            }
        }

        private void SetCommStateNative(ref Dcb lpDcb)
        {
            int commErrors = 0;
            Comstat comStat = new Comstat();

            for (int i = 0; i < CommStateRetries; i++)
            {
                if (!ClearCommError(_handle, ref commErrors, ref comStat))
                {
                    WinIoError();
                }
                if (SetCommState(_handle, ref lpDcb))
                {
                    break;
                }
                if (i == CommStateRetries - 1)
                {
                    WinIoError();
                }
            }
        }

        #region Nested type: COMSTAT

        [StructLayout(LayoutKind.Sequential)]
        private struct Comstat
        {
            public readonly uint Flags;
            public readonly uint cbInQue;
            public readonly uint cbOutQue;
        }

        #endregion

        #region Nested type: DCB

        [StructLayout(LayoutKind.Sequential)]
        private struct Dcb
        {
            public readonly uint DCBlength;
            public  uint BaudRate;
            public uint Flags;
            public readonly ushort wReserved;
            public readonly ushort XonLim;
            public readonly ushort XoffLim;
            public byte ByteSize;
            public  byte Parity;
            public  byte StopBits;
            public readonly byte XonChar;
            public readonly byte XoffChar;
            public readonly byte ErrorChar;
            public readonly byte EofChar;
            public readonly byte EvtChar;
            public readonly ushort wReserved1;
        }

        #endregion

        #endregion
    }

}
