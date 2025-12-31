using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GamerGamma
{
    public class GammaService : INotifyPropertyChanged
    {
        // GammaApi moved to Common.cs

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        private GammaApi _api = GammaApi.GDI;
        public GammaApi Api { get => _api; set { _api = value; OnPropertyChanged(); Update(); } }

        private string _targetDisplay;
        public string TargetDisplay { get => _targetDisplay; set { _targetDisplay = value; OnPropertyChanged(); Update(); } }

        // Channels
        public ChannelData Red { get; set; } = new ChannelData();
        public ChannelData Green { get; set; } = new ChannelData();
        public ChannelData Blue { get; set; } = new ChannelData();

        // Global
        private double _saturation = 1.0; 
        private double _hue = 0.0;        
        private double _dithering = 0.0;
        private double _sharpness = 0.0;

        public double Saturation { get => _saturation; set { _saturation = value; OnPropertyChanged(); Update(); } }
        public double Hue { get => _hue; set { _hue = value; OnPropertyChanged(); Update(); } }
        public double Dithering { get => _dithering; set { _dithering = value; OnPropertyChanged(); Update(); } }
        public double Sharpness { get => _sharpness; set { _sharpness = value; OnPropertyChanged(); Update(); } }

        public GammaService() { Reset(); }

        public void Reset()
        {
            Red = new ChannelData();
            Green = new ChannelData();
            Blue = new ChannelData();
            _saturation = 1.0;
            _hue = 0.0;
            _dithering = 0.0;
            _sharpness = 0.0;
            Update();
        }

        public void Update()
        {
            if (_api == GammaApi.GDI) ApplyGDI();
            else if (_api == GammaApi.NvAPI) ApplyNvAPI();
        }

        private void ApplyGDI()
        {
            var ramp = BuildRamp();
            try
            {
                GamerGammaApi.SetGamma(_targetDisplay, ramp);
            }
            catch { }
        }

        private void ApplyNvAPI()
        {
            // Placeholder
        }

        public (ushort[] R, ushort[] G, ushort[] B) GetRamp()
        {
            var rRaw = BuildChannelRamp(Red, 0);
            var gRaw = BuildChannelRamp(Green, 1);
            var bRaw = BuildChannelRamp(Blue, 2);

            if (Math.Abs(_saturation - 1.0) < 0.001)
                return (rRaw, gRaw, bRaw);

            var rFull = new ushort[256];
            var gFull = new ushort[256];
            var bFull = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                double r = rRaw[i] / 65535.0;
                double g = gRaw[i] / 65535.0;
                double b = bRaw[i] / 65535.0;
                double l = 0.299 * r + 0.587 * g + 0.114 * b;

                r = l + (r - l) * _saturation;
                g = l + (g - l) * _saturation;
                b = l + (b - l) * _saturation;

                // FIXED: Changed Math.Clamp to Clamp
                rFull[i] = (ushort)(Clamp(r, 0, 1) * 65535.0);
                gFull[i] = (ushort)(Clamp(g, 0, 1) * 65535.0);
                bFull[i] = (ushort)(Clamp(b, 0, 1) * 65535.0);
            }
            return (rFull, gFull, bFull);
        }

        private RAMP BuildRamp()
        {
            var (r, g, b) = GetRamp();
            var ramp = new RAMP();
            ramp.Red = r;
            ramp.Green = g;
            ramp.Blue = b;
            return ramp;
        }

        private ushort[] BuildChannelRamp(ChannelData ch, int channelIdx)
        {
            var curve = new ushort[256];
            Random rand = new Random();

            for (int i = 0; i < 256; i++)
            {
                double val = i / 255.0;

                // 1. Black Level
                val = ch.BlackLevel + val * (1.0 - ch.BlackLevel);

                // 2. Black Stabilizer (Lift Shadows - LG Style)
                if (Math.Abs(ch.BlackStab) > 0.001)
                {
                    if (ch.BlackStab > 0)
                    {
                         // Lift shadows (bow upwards)
                         val = Math.Pow(val, Math.Max(0.1, 1.0 - (ch.BlackStab * 0.6)));
                    }
                    else
                    {
                         // Crush shadows (bow downwards aggressively)
                         val = Math.Pow(val, 1.0 + (Math.Abs(ch.BlackStab) * 2.5));
                    }
                }

                // 3. Contrast (Pivot 0.5)
                double cMult = ch.Contrast; // 1.0 is neutral
                val = 0.5 + (val - 0.5) * cMult;
                
                // 4. Black Floor (Hard cut)
                if (Math.Abs(ch.BlackFloor) > 0.001)
                {
                    if (ch.BlackFloor > 0)
                    {
                        double floor = ch.BlackFloor * 0.5;
                        if (val < floor) val = floor;
                    }
                    else
                    {
                        // Negative: Deepen blacks by applying an offset
                        double crush = Math.Abs(ch.BlackFloor) * 0.2;
                        val = Math.Max(0, val - crush);
                    }
                }

                // 5. White Stabilizer (Smooth Highlights)
                if (Math.Abs(ch.WhiteStab) > 0.001)
                {
                    double inv = 1.0 - val;
                    double hFactor = 1.0 - (ch.WhiteStab * 0.4);
                    inv = Math.Pow(inv, Math.Max(0.1, hFactor));
                    val = 1.0 - inv;
                }

                // 6. White Ceiling (Hard Ceiling cut)
                if (Math.Abs(ch.WhiteCeiling) > 0.001)
                {
                    if (ch.WhiteCeiling > 0)
                    {
                        double ceil = 1.0 - (ch.WhiteCeiling * 0.5);
                        if (val > ceil) val = ceil;
                    }
                    else
                    {
                        // Negative: Boost highlights
                        double boost = Math.Abs(ch.WhiteCeiling) * 0.2;
                        val = Math.Min(1.0, val + boost);
                    }
                }

                // FIXED: Changed Math.Clamp to Clamp
                val = Clamp(val, 0.0, 1.0);

                // 6. Gamma
                double g = Math.Max(0.1, ch.Gamma);
                val = Math.Pow(val, 1.0 / g);

                // 7. Mid-Gamma (0.0 is neutral, effective 1.0)
                if (Math.Abs(ch.MidGamma) > 0.01)
                {
                    double mt = 1.0 + ch.MidGamma; 
                    val = Math.Pow(val, 1.0 / Math.Max(0.1, mt));
                }

                // 8. Brightness (Offset)
                // 1.0 neutral.
                double bOffset = ch.Brightness - 1.0; 
                val += bOffset;

                // 9. Dithering (Global)
                if (_dithering > 0)
                {
                    val += (rand.NextDouble() - 0.5) * (_dithering * 0.15); 
                }

                // 10. Generic Hue (Fake Tint for GDI)
                // Hue > 0: Boost Green/Blue (Cooler), Reduce Red
                // Hue < 0: Boost Red (Warmer), Reduce Green/Blue
                if (Math.Abs(_hue) > 0.001)
                {
                    double h = _hue * 0.2; // Strength factor
                    if (channelIdx == 0) // Red
                    {
                         val -= h; 
                    }
                    else // Green, Blue
                    {
                        val += h;
                    }
                }

                // FIXED: Changed Math.Clamp to Clamp
                val = Clamp(val, 0.0, 1.0);

                curve[i] = (ushort)(val * 65535.0);
            }
            return curve;
        }
        
        public ExtendedColorSettings GetCurrentSettings()
        {
            return new ExtendedColorSettings
            {
                Red = Red.Clone(),
                Green = Green.Clone(),
                Blue = Blue.Clone(),
                Saturation = _saturation,
                Hue = _hue,
                Dithering = _dithering,
                Sharpness = _sharpness,
                Api = _api
            };
        }

        public void ApplySettings(ExtendedColorSettings s)
        {
            if (s == null) return;
            Red = s.Red ?? new ChannelData();
            Green = s.Green ?? new ChannelData();
            Blue = s.Blue ?? new ChannelData();
            Saturation = s.Saturation;
            Hue = s.Hue;
            Dithering = s.Dithering;
            Sharpness = s.Sharpness;
            Api = s.Api;
            Update();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }
}