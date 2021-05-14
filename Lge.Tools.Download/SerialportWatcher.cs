//#define USE_WMI

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Windows;
using System.Windows.Interop;
using System.Threading.Tasks;

namespace Lge.Tools.Download
{
    public delegate void SerialPortsChangedHandler(bool aInserted, SerialportWatcher.PortInfo aChangedPort);

    public sealed class SerialportWatcher
    {
        public const int DBT_DEVICEARRIVAL = 0x8000;    // device is inserted
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;  // device is gone     
        public const int WmDevicechange = 0x0219;           // device change event      
        private const int DbtDevtypDeviceinterface = 5;
        public const int DBT_DEVTYP_PORT = 0x00000003;
        // COM Ports : 86E0D1E0-8089-11D0-9CE4-08003E301F73
        // USB COM Ports : 0x25dbce51, 0x6c8f, 0x4a72, 0x8a,0x6d,0xb5,0x4c,0x2b,0x4f,0xc8,0x35
        // Ports (Serial & Parallel : 4D36E978-E325-11CE-BFC1-08002BE10318
        // USB Device : A5DCBF10-6530-11D2-901F-00C04FB951ED
        private static readonly Guid GuidDevinterfaceUSSerialBDevice = new Guid(0x25dbce51, 0x6c8f, 0x4a72, 0x8a, 0x6d, 0xb5, 0x4c, 0x2b, 0x4f, 0xc8, 0x35);
        private static IntPtr _notifyHandle = IntPtr.Zero;
		private static Window _hookWin;
        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
		public static void InstallWindowHook(Window aWin)
		{
#if !USE_WMI
            _hookWin = aWin;
			HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(aWin).Handle);
			source.AddHook( new HwndSourceHook(WndProc));
#endif
        }

		private static IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{            
            if (msg == WmDevicechange)
            {
                var dbt = wParam.ToInt32();
				if (dbt == DBT_DEVICEREMOVECOMPLETE || dbt == DBT_DEVICEARRIVAL)
				{
					int devType = Marshal.ReadInt32(lParam, 4);
					if (devType == DBT_DEVTYP_PORT)
					{
						var name = Marshal.PtrToStringAuto((IntPtr)((long)lParam + 12));
                        if (dbt == DBT_DEVICEARRIVAL)
                        {
                            Task.Factory.StartNew(
                                new Action<object>((state) =>
                                {
                                    SerialportWatcher.UsbPortChanged((string)state, true);
                                }), name);
                        }
                        else
                        {
                            SerialportWatcher.UsbPortChanged(name, false);
                        }
					}
				}
                handled = true;
            }
            else
                handled = false;

            return IntPtr.Zero;
		}

        public static void RegisterUsbDeviceNotification()
        {
            UsbPortChanged(null);

#if USE_WMI
            try
            {
                _sports = ReadUsbPorts();

                var qin = new WqlEventQuery();
                var qout = new WqlEventQuery();
                ManagementScope scope = new ManagementScope("root\\CIMV2");
                scope.Options.EnablePrivileges = true;

                qin.EventClassName = "__InstanceCreationEvent";
                qout.EventClassName = "__InstanceDeletionEvent";
                qout.WithinInterval = qin.WithinInterval = new TimeSpan(0, 0, 2);
                qout.Condition = qin.Condition = @"TargetInstance ISA 'Win32_PnPEntity'";

                _arrival = new ManagementEventWatcher(qin);
                _removal = new ManagementEventWatcher(qout);

                _arrival.EventArrived += (ao, aargs) => RaisePortsChangedIfNecessary(true);
                _removal.EventArrived += (ro, rargs) => RaisePortsChangedIfNecessary(false);

                // Start listening for events
                _arrival.Start();
                _removal.Start();
            }
            catch (ManagementException err)
            {
                Log.e("WMI wather install error: {0}", err);
            }
#else
            
			if (_hookWin == null)
				return;

            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
            {
                DeviceType = DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = GuidDevinterfaceUSSerialBDevice,
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            _notifyHandle = RegisterDeviceNotification(new WindowInteropHelper(_hookWin).Handle, buffer, 0);
           
		    Marshal.FreeHGlobal(buffer);
#endif
        }
#if USE_WMI
        static ManagementEventWatcher _arrival = null;
        static ManagementEventWatcher _removal = null;
        static Dictionary<string, string> _sports = new Dictionary<string, string>();

        static void RaisePortsChangedIfNecessary(bool aAdded)
        {
            lock (_sports)
            {
                var curPorts = ReadUsbPorts();

                if (aAdded)
                {
                    foreach (var n in curPorts)
                    {
                        if (!_ports.Any(x => x.Caption == n.Key)) // 새로 추가됨
                        {
                            var idx = n.Key.LastIndexOf("(COM");
                            var port = n.Key.Substring(idx + 1).TrimEnd(')', ' ');
                            UsbPortChanged(port, aAdded);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var o in _ports)
                    {
                        if (!curPorts.Any(x => x.Key == o.Caption)) // 제거 됨
                        {
                            UsbPortChanged(o.Name, aAdded);
                            break;
                        }
                    }
                }

                _sports = curPorts;
            }
        }
#endif
        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public static void UnregisterUsbDeviceNotification()
        {
#if USE_WMI
            if (_arrival != null)
            {
                _arrival.Dispose();
                _arrival = null;
            }
            if (_removal != null)
            {
                _removal.Dispose();
                _removal = null;
            }
#else
            if (_notifyHandle != IntPtr.Zero)
            {
                UnregisterDeviceNotification(_notifyHandle);
                _notifyHandle = IntPtr.Zero;
            }
#endif
        }


        public static void UsbPortChanged(string aName, bool aInserted = true, Dictionary<string, string> aPortMap = null)
        {
            // update serial port list'
			PortInfo changedPort = null;
            List<PortInfo> ports = null;
            
            lock (PortInfo.PortKinds)
            {
                try
                {
                    if (aInserted == false)
                    {
                        ports = new List<PortInfo>(_ports);
                        changedPort = ports.FirstOrDefault(x => x.Name == aName);
                        if (changedPort != null)
                        {
                            ports.Remove(changedPort);
                            Log.v("REMOVE port: {0}", changedPort.Caption);
                        }
                    }
                    else
                    {
                        var map = aPortMap;
                        if (map == null)
                            map = ReadUsbPorts();

                        ports = new List<PortInfo>();

                        foreach (var pinfo in map)
                        {
                            var usbPort = PortInfo.Parse(pinfo.Key, pinfo.Value);

                            if (usbPort.Kind == PortKind.Diagnostic || usbPort.Kind == PortKind.QDLoader)
                            {
                                if (usbPort.Name == aName && usbPort.Path == PortInfo.None)
                                {
                                    Log.i("Pending PORT detection:{0},{1}", usbPort.Caption, usbPort.Path);
                                    System.Threading.Thread.Sleep(500);
                                    UsbPortChanged(aName, aInserted, aPortMap);
                                    return;
                                }
                            }

                            if (!ports.Any(x => x.Name == usbPort.Name))
                                ports.Add(usbPort);
                            if (aName == null || aName == usbPort.Name)
                            {
                                changedPort = usbPort;
                                Log.v("ADD port: {0}, {1}", changedPort.Caption, changedPort.Path);
                            }
                        }
                    }

                    _ports = new List<PortInfo>(ports);

                    if (aName != null && changedPort != null && SerialPortsChangedEvent != null)
                        SerialPortsChangedEvent(aInserted, changedPort);
                }
                catch (COMException ce)
                {
                    Log.e("PNP USB device  COM Exception: " + ce.ToString());
                }
                catch (Exception e)
                {
                    Log.e("PNP USB device Exception: " + e.ToString());
                }
            }
            
        }

		public static List<PortInfo> GetPorts(PortKind aKind)
		{
            var list = new List<PortInfo>();
			foreach(var p in _ports)
			{
				if (p.Kind == aKind)
				{
                    list.Add(p);
				}				
			}
            return list;
		}
       
        public static event SerialPortsChangedHandler SerialPortsChangedEvent;

		public static IEnumerable<PortInfo>  Ports
		{
			get
			{
				return _ports;
			}	
		} 
        static List<PortInfo> _ports = new List<PortInfo>();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastDeviceinterface
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            internal Guid ClassGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            internal string dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastDevicePort
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            internal string dbcc_name;
        }

        public enum PortKind
        {
            None,
            Serial,
            QDLoader,
            NMEA,
            Diagnostic,
            MemDump,
        }

        public class PortInfo : IEquatable<PortInfo>
        {
            public static readonly string None = "(none)";

            static public readonly PortInfo[] PortKinds = new PortInfo[] {
                new PortInfo("QDLoader", PortKind.QDLoader),
                new PortInfo("NMEA", PortKind.NMEA),
                new PortInfo("Diagnostics 9025", PortKind.Diagnostic),   // qct usb names
                new PortInfo("LGE Android Platform USB Serial Port", PortKind.Diagnostic), // new lge usb names
                new PortInfo("Diagnostics 9", PortKind.MemDump),  // 나머지 진단포트들은 모두 비상 덤프, 다운로드 등의 모드로...
             };

            static public PortInfo Parse(string aCaption, string aPath)
            {
                int idx = aCaption.LastIndexOf("(COM");
                var name = aCaption.Substring(0, idx);
                var port = aCaption.Substring(idx).Trim('(', ')', ' ');

                var addPort = PortKinds.FirstOrDefault(x => name.IndexOf(x.Caption, StringComparison.OrdinalIgnoreCase) >= 0);
                if (addPort == null)
                    addPort = new PortInfo("", PortKind.Serial);

                var portinfo = new PortInfo(port, aCaption, addPort.Kind, aPath);
                return portinfo;
            }

            public PortInfo()
            {
                this.Name = PortInfo.None;
                this.Caption = "(Not connected)";
                this.Kind = PortKind.None;
                this.Path = PortInfo.None;
            }

            public PortInfo(string aCaption, PortKind aKind)
            {
                this.Name = PortInfo.None;
                this.Caption = aCaption;
                this.Kind = aKind;
                this.Path = PortInfo.None;
            }

            public PortInfo(string aPort, string aCaption, PortKind aKind, string aPath)
            {
                this.Name = aPort;
                this.Caption = aCaption;
                this.Kind = aKind;
                this.Path = aPath;
            }

            public string Path { get; private set; }

            public string Name { get; private set; }
            public string Caption { get; private set; }
            public PortKind Kind { get; private set; }
            public override string ToString()
            {
                return this.Caption;
            }

            
            public bool MatchPath(PortInfo aOther)
            {
                string[] spstring1 = aOther.Path.Split('\0');
                string[] spstring2 = this.Path.Split('\0');

                if (aOther == null ||
                    string.IsNullOrEmpty(spstring2[0]) || string.IsNullOrEmpty(spstring1[0]) ||
                    this.Path == PortInfo.None || aOther.Path == PortInfo.None)
                {
                    return false;
                }

                if (this.Name == aOther.Name || spstring2[0].StartsWith(spstring1[0]) || spstring1[0].StartsWith(spstring2[0]))
                    return true;

                return false;
            }

            public bool Equals(PortInfo other)
            {
                if (other == null)
                    return false;

                return (this.Caption == other.Caption) ;
            }

        }

#region Read USB device information for details

        const Int32 INVALID_HANDLE_VALUE = -1;

        /// <summary>
        /// Flags controlling what is included in the device information set built by SetupDiGetClassDevs
        /// </summary>
        [Flags]
        private enum DIGCF : int
        {
            DIGCF_DEFAULT = 0x00000001,    // only valid with DIGCF_DEVICEINTERFACE
            DIGCF_PRESENT = 0x00000002,
            DIGCF_ALLCLASSES = 0x00000004,
            DIGCF_PROFILE = 0x00000008,
            DIGCF_DEVICEINTERFACE = 0x00000010,
        }


        private enum SPDRP : int
        {
            SPDRP_FRIENDLYNAME = 0x0000000C,
            SPDRP_LOCATION_PATHS = 0x00000023,
        }

        //pack=8 for 64 bit.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SP_DEVINFO_DATA
        {
            public UInt32 cbSize;
            public Guid ClassGuid;
            public UInt32 DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public UInt32 cbSize;
            public Guid interfaceClassGuid;
            public UInt32 flags;
            private IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string devicePath;
        }

        [DllImport(@"setupapi.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern Boolean SetupDiEnumDeviceInterfaces(
               IntPtr hDevInfo,
               IntPtr devInfo,
               ref Guid interfaceClassGuid, //ref
               UInt32 memberIndex,
               ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
            );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern Boolean SetupDiGetDeviceInterfaceDetail(
               IntPtr hDevInfo,
               ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, //ref
               ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
               UInt32 deviceInterfaceDetailDataSize,
               out UInt32 requiredSize,
               ref SP_DEVINFO_DATA deviceInfoData
            );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

        [DllImport("setupapi.dll", CharSet = CharSet.Ansi)]     // 1st form using a ClassGUID
        private static extern IntPtr SetupDiGetClassDevs(
               ref Guid ClassGuid, //ref
               IntPtr Enumerator,
               IntPtr hwndParent,
               UInt32 Flags
            );

        [DllImport("setupapi.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
                IntPtr DeviceInfoSet,
                ref SP_DEVINFO_DATA DeviceInfoData, //ref
                UInt32 Property,
                ref UInt32 PropertyRegDataType,
                byte[] PropertyBuffer,
                UInt32 PropertyBufferSize,
                ref UInt32 RequiredSize
            );

        static private Guid COMPORT = new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73");


        static internal Dictionary<string, string> ReadUsbPorts()
        {
            IntPtr hdevInfo = IntPtr.Zero;
            var devList = new Dictionary<string, string>();

            try
            {
                hdevInfo = SetupDiGetClassDevs(ref COMPORT, IntPtr.Zero, IntPtr.Zero, (int)(DIGCF.DIGCF_PRESENT | DIGCF.DIGCF_DEVICEINTERFACE));
                if (hdevInfo.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    bool ok = true;
                    uint idx = 0;
                    uint regType = 0;
                    while (ok)
                    {
                        SP_DEVICE_INTERFACE_DATA dia = new SP_DEVICE_INTERFACE_DATA();
                        dia.cbSize = (uint)Marshal.SizeOf(dia);
                        ok = false;
                        if (SetupDiEnumDeviceInterfaces(hdevInfo, IntPtr.Zero, ref COMPORT, idx, ref dia))
                        {
                            // build a DevInfo Data structure
                            SP_DEVINFO_DATA da = new SP_DEVINFO_DATA();
                            da.cbSize = (uint)Marshal.SizeOf(da);

                            // build a Device Interface Detail Data structure
                            SP_DEVICE_INTERFACE_DETAIL_DATA didd = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                            didd.cbSize = (uint)(4 + 1); // trust me :)
                            uint detailSize = didd.cbSize + 255;
                            // now we can get some more detailed information
                            uint requiredSize;
                            if (SetupDiGetDeviceInterfaceDetail(hdevInfo, ref dia, ref didd, detailSize, out requiredSize, ref da))
                            {
                                byte[] retBuffer = new byte[265];
                                if (SetupDiGetDeviceRegistryProperty(hdevInfo, ref da, (UInt32)SPDRP.SPDRP_FRIENDLYNAME, ref regType, retBuffer, (uint)retBuffer.Length, ref requiredSize))
                                {
                                    string frendlyName = Encoding.Default.GetString(retBuffer, 0, (int)requiredSize).TrimEnd('\0');
                                    string path = PortInfo.None;
                                    // 일반 USB는 정보를 가져오지 못 한다. QCT는 가져옴.
                                    if (SetupDiGetDeviceRegistryProperty(hdevInfo, ref da, (UInt32)SPDRP.SPDRP_LOCATION_PATHS, ref regType, retBuffer, (uint)retBuffer.Length, ref requiredSize))
                                    {
                                        path = Encoding.ASCII.GetString(retBuffer, 0, (int)requiredSize).TrimEnd('\0');
                                    }
                                    devList[frendlyName] = path;
                                }
                            }
                            ok = true;
                        }
                        idx++;
                    }
                }
            }
            catch (Exception e)
            {
                Log.e("Exception:Enumerate usb port - {0} ", e);
            }
            finally
            {
                if (hdevInfo != IntPtr.Zero && hdevInfo.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    SetupDiDestroyDeviceInfoList(hdevInfo); //Clean up the old structure we no longer need.
                }
            }
            return devList;
        }

#endregion Read USB device information for details
    }

}

