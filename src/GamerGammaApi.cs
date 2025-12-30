using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace GamerGamma
{
    public class MonitorInfo
    {
        public string DeviceName { get; set; }
        public string DeviceString { get; set; }
        public string DeviceID { get; set; }
        public string DeviceKey { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Frequency { get; set; }
        public bool IsPrimary { get; set; }

        public override string ToString()
        {
            return $"{DeviceString} ({Width}x{Height} @ {Frequency}Hz)";
        }
    }

    public static class GamerGammaApi
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [DllImport("gdi32.dll")]
        public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);
        
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            public const int CCHDEVICENAME = 32;
            public const int CCHFORMNAME = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTtoption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }
        
        [Flags]
        public enum DisplayDeviceStateFlags : int
        {
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            PrimaryDevice = 0x4,
            MirroringDriver = 0x8,
            VGACompatible = 0x10,
            Removable = 0x20,
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        // VCP codes
        private const byte VCP_BRIGHTNESS = 0x10;
        private const byte VCP_CONTRAST = 0x12;

        public static List<MonitorInfo> GetMonitors()
        {
            var list = new List<MonitorInfo>();
            var d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);

            for (uint i = 0; EnumDisplayDevices(null, i, ref d, 0); i++)
            {
                if ((d.StateFlags & (int)DisplayDeviceStateFlags.AttachedToDesktop) == (int)DisplayDeviceStateFlags.AttachedToDesktop)
                {
                    var mon = new MonitorInfo
                    {
                        DeviceName = d.DeviceName,
                        DeviceString = d.DeviceString,
                        DeviceID = d.DeviceID,
                        DeviceKey = d.DeviceKey, // Added DeviceKey
                        IsPrimary = (d.StateFlags & (int)DisplayDeviceStateFlags.PrimaryDevice) != 0
                    };
                    
                    // Drill down to get the actual Monitor Name (not just Adapter)
                    var monDev = new DISPLAY_DEVICE();
                    monDev.cb = Marshal.SizeOf(monDev);
                    // Pass EDD_GET_DEVICE_INTERFACE_NAME (0x00000001) if available? 
                    // EnumDisplayDevices 2nd param: iDevNum. When d.DeviceName is passed as lpDevice, use 0 to get the first monitor on that adapter.
                    if (EnumDisplayDevices(d.DeviceName, 0, ref monDev, 0x00000001)) // 0x1 = EDD_GET_DEVICE_INTERFACE_NAME
                    {
                        if (!string.IsNullOrEmpty(monDev.DeviceString))
                             mon.DeviceString = monDev.DeviceString;
                    }

                    DEVMODE dm = new DEVMODE();
                    dm.dmSize = (short)Marshal.SizeOf(dm);
                    if (EnumDisplaySettings(d.DeviceName, -1, ref dm))
                    {
                        mon.Width = dm.dmPelsWidth;
                        mon.Height = dm.dmPelsHeight;
                        mon.Frequency = dm.dmDisplayFrequency;
                    }

                    list.Add(mon);
                }
                d.cb = Marshal.SizeOf(d);
            }
            return list;
        }

        public static void SetGamma(string deviceName, RAMP ramp)
        {
            // Use CreateDC to get a handle for the specific monitor
            IntPtr hdc = CreateDC(null, deviceName, null, IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                SetDeviceGammaRamp(hdc, ref ramp);
                DeleteDC(hdc);
            }
        }
        
        public static void OpenWindowsDisplaySettings()
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:display") { UseShellExecute = true }); } catch {}
        }
    }
}
