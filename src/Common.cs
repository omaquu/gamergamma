using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace GamerGamma
{
    public enum ChannelMode { Linked, Red, Green, Blue }
    public enum GammaApi { GDI, VESA, NvAPI }

    public class ChannelData
    {
        public double Gamma { get; set; } = 1.0;
        public double Brightness { get; set; } = 1.0; 
        public double Contrast { get; set; } = 1.0;   
        public double BlackLevel { get; set; } = 0.0;
        public double BlackFloor { get; set; } = 0.0;
        public double WhiteCeiling { get; set; } = 0.0; 
        public double BlackStab { get; set; } = 0.0;
        public double WhiteStab { get; set; } = 0.0;
        public double MidGamma { get; set; } = 0.0;

        public ChannelData Clone()
        {
            return (ChannelData)MemberwiseClone();
        }
    }

    public class ExtendedColorSettings
    {
        public ChannelData Red { get; set; } = new ChannelData();
        public ChannelData Green { get; set; } = new ChannelData();
        public ChannelData Blue { get; set; } = new ChannelData();
        public double Saturation { get; set; } = 1.0;
        public double Hue { get; set; }
        public double Dithering { get; set; }
        public double Sharpness { get; set; }
        public GammaApi Api { get; set; }
    }

    public class ColorProfile
    {
        public string Name { get; set; }
        public ExtendedColorSettings Settings { get; set; }
        public int Hotkey { get; set; } 
        public int HotkeyModifiers { get; set; }
    }

    public class AppSettings
    {
        public List<ColorProfile> Profiles { get; set; } = new List<ColorProfile>();
        public int SelectedProfileIndex { get; set; } = -1;
        
        public bool MinimizeToTray { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        
        public string SelectedMonitorDeviceName { get; set; }
        public ExtendedColorSettings CurrentSettings { get; set; } = new ExtendedColorSettings();
        public Dictionary<string, ExtendedColorSettings> MonitorSettings { get; set; } = new Dictionary<string, ExtendedColorSettings>();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Red;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Green;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Blue;
    }
}