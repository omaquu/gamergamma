using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
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

        // Global Core
        private double _saturation = 1.0; 
        private double _hue = 0.0;        
        private double _luminance = 0.0;
        private double _smartContrast = 0.0;
        public event Action OnSettingsChanged;
        private double _deHaze = 0.0;
        private double _temperature = 0.0;
        private double _tint = 0.0;
        private double _dithering = 0.0;
        private double _sharpness = 0.0;
        private double _bump = 0.0;
        private TransferMode _transferMode = TransferMode.PowerLaw;
        
        // Pro / Nice to Have

        
        // Point Curve - Now Per Channel
        // We need lists for R, G, B.
        // If "Linked" is used, we apply one curve to all? Or just edit all 3?
        // Let's store 3 curves.
        // Point Curve - Now Per Channel + Master
        // We need lists for R, G, B, and Master.
        public List<System.Drawing.Point> PointCurveR { get; set; } = new List<System.Drawing.Point>();
        public List<System.Drawing.Point> PointCurveG { get; set; } = new List<System.Drawing.Point>();
        public List<System.Drawing.Point> PointCurveB { get; set; } = new List<System.Drawing.Point>();
        public List<System.Drawing.Point> PointCurveMaster { get; set; } = new List<System.Drawing.Point>();
        private bool _smooth = true;
        public bool Smooth { 
            get => _smooth; 
            set { 
                _smooth = value; 
                OnPropertyChanged(); 
                Update(); 
                OnSettingsChanged?.Invoke(); 
            } 
        }

        public double Saturation { get => _saturation; set { _saturation = value; OnPropertyChanged(); Update(); } }
        public double Hue { get => _hue; set { _hue = value; OnPropertyChanged(); Update(); } }
        public double Luminance { get => _luminance; set { _luminance = value; OnPropertyChanged(); Update(); } }
        public double SmartContrast { 
            get => _smartContrast; 
            set { 
                _smartContrast = Math.Max(0, Math.Min(1, value)); 
                OnPropertyChanged();
                Update(); 
            } 
        }
        public double DeHaze { get => _deHaze; set { _deHaze = value; OnPropertyChanged(); Update(); } }
        public double Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); Update(); } }
        public double Tint { get => _tint; set { _tint = value; OnPropertyChanged(); Update(); } }
        
        // Removed Exp/Sol/Post
        
        public Color ShadowTint { get; set; } = Color.Black;
        public Color HighlightTint { get; set; } = Color.White;

        public double Dithering { get => _dithering; set { _dithering = value; OnPropertyChanged(); Update(); } }
        public double Sharpness { get => _sharpness; set { _sharpness = value; OnPropertyChanged(); Update(); } }
        public double ToneSculpt { get => _bump; set { _bump = value; OnPropertyChanged(); Update(); } }
        public TransferMode TransferMode { get => _transferMode; set { _transferMode = value; OnPropertyChanged(); Update(); } }

        public GammaService() { Reset(); }

        public void Reset()
        {
            Red = new ChannelData();
            Green = new ChannelData();
            Blue = new ChannelData();
            _saturation = 1.0;
            _hue = 0.0;
            _luminance = 0.0;
            _smartContrast = 0.0;
            _deHaze = 0.0;
            _temperature = 0.0;
            _tint = 0.0;
            _dithering = 0.0;
            _bump = 0.0;
            _transferMode = TransferMode.PowerLaw;
            ShadowTint = Color.FromArgb(0, 0, 0);
            HighlightTint = Color.FromArgb(255, 255, 255);
            PointCurveR = new List<System.Drawing.Point>(); PointCurveR.Add(new System.Drawing.Point(0,1)); PointCurveR.Add(new System.Drawing.Point(255,255));
            PointCurveG = new List<System.Drawing.Point>(); PointCurveG.Add(new System.Drawing.Point(0,1)); PointCurveG.Add(new System.Drawing.Point(255,255));
            PointCurveB = new List<System.Drawing.Point>(); PointCurveB.Add(new System.Drawing.Point(0,1)); PointCurveB.Add(new System.Drawing.Point(255,255));
            PointCurveMaster = new List<System.Drawing.Point>(); PointCurveMaster.Add(new System.Drawing.Point(0,1)); PointCurveMaster.Add(new System.Drawing.Point(255,255));
            Smooth = false; 
            Update();
            OnSettingsChanged?.Invoke();
        }

        public string GetGlobalConfigString()
        {
            try {
                var s = GetCurrentSettings();
                var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                return ser.Serialize(s);
            } catch { return ""; }
        }

        public void ApplyGlobalConfigString(string json)
        {
            try {
                var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                var s = ser.Deserialize<ExtendedColorSettings>(json);
                ApplySettings(s);
            } catch { }
        }

        public void Update()
        {
            if (_api == GammaApi.GDI) ApplyGDI();
            else if (_api == GammaApi.NvAPI) ApplyNvAPI();
            OnSettingsChanged?.Invoke();
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

            // Point Curve Interpolation - Per Channel
            ApplyPointCurve(rRaw, PointCurveR);
            ApplyPointCurve(gRaw, PointCurveG);
            ApplyPointCurve(bRaw, PointCurveB);
            
            // Master Curve (Linked)
            ApplyPointCurve(rRaw, PointCurveMaster);
            ApplyPointCurve(gRaw, PointCurveMaster);
            ApplyPointCurve(bRaw, PointCurveMaster);

            // Global Color Matrix / Saturation / Hue / Tint logic
            if (Math.Abs(_saturation - 1.0) > 0.001 || Math.Abs(_hue) > 0.001 || Math.Abs(_temperature) > 0.001 || 
                Math.Abs(_tint) > 0.001 || ShadowTint.ToArgb() != Color.Black.ToArgb() || HighlightTint.ToArgb() != Color.White.ToArgb())
            {
                var rFull = new ushort[256];
                var gFull = new ushort[256];
                var bFull = new ushort[256];

                // 3.0 Hue (Fake) & Temperature/Tint
                // Since GDI is per-channel, we cannot do real Hue rotation.
                // We simulate it by shifting channel gains.
                
                double rGain = 1.0, gGain = 1.0, bGain = 1.0;

                // Fake Hue: Cycle RGB gains
                if (Math.Abs(_hue) > 0.001) {
                     // Hue 0-360.
                     // Simple 3-phase sine for gains
                     double rad = _hue * Math.PI / 180.0;
                     // Offset phases by 120 deg (2pi/3)
                     rGain += Math.Sin(rad) * 0.2;
                     gGain += Math.Sin(rad + 2.0*Math.PI/3.0) * 0.2;
                     bGain += Math.Sin(rad + 4.0*Math.PI/3.0) * 0.2;
                }
                
                // Temp
                if (Math.Abs(_temperature) > 0.001) {
                     rGain += _temperature * 0.2;
                     bGain -= _temperature * 0.2;
                }
                
                // Tint
                if (Math.Abs(_tint) > 0.001) {
                     gGain += _tint * 0.2;
                }

                for (int i = 0; i < 256; i++)
                {
                    double r = rRaw[i] / 65535.0;
                    double g = gRaw[i] / 65535.0;
                    double b = bRaw[i] / 65535.0;

                    // Apply Gains
                    r *= rGain;
                    g *= gGain;
                    b *= bGain;
                    
                    // Split Toning
                    // Apply Shadow Tint to darks, Highlight Tint to brights
                    if (ShadowTint.ToArgb() != Color.Black.ToArgb() || HighlightTint.ToArgb() != Color.White.ToArgb())
                    {
                         double lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                         
                         // Shadow Tint
                         if (ShadowTint.ToArgb() != Color.Black.ToArgb()) {
                             double sR = ShadowTint.R / 255.0;
                             double sG = ShadowTint.G / 255.0;
                             double sB = ShadowTint.B / 255.0;
                             double shadowFactor = (1.0 - lum) * (1.0 - lum); // Focus on darks
                             
                             r = r * (1.0 - shadowFactor) + sR * shadowFactor * r; // Tinting method? Or addition?
                             // Typically split toning adds color to shadows
                             // Let's try simple addition with decay
                             r += sR * shadowFactor * 0.2; 
                             g += sG * shadowFactor * 0.2;
                             b += sB * shadowFactor * 0.2;
                         }

                         // Highlight Tint
                         if (HighlightTint.ToArgb() != Color.White.ToArgb()) {
                             double hR = HighlightTint.R / 255.0;
                             double hG = HighlightTint.G / 255.0;
                             double hB = HighlightTint.B / 255.0;
                             // We want to TINT towards the color, not just add it (which clips white)
                             // So we interpolate towards the target color at high luminance?
                             double highFactor = lum * lum;
                             
                             r = r * (1.0 - highFactor * 0.3) + hR * highFactor * 0.3;
                             g = g * (1.0 - highFactor * 0.3) + hG * highFactor * 0.3;
                             b = b * (1.0 - highFactor * 0.3) + hB * highFactor * 0.3;
                         }
                    }

                    rFull[i] = (ushort)(Clamp(r, 0, 1) * 65535.0);
                    gFull[i] = (ushort)(Clamp(g, 0, 1) * 65535.0);
                    bFull[i] = (ushort)(Clamp(b, 0, 1) * 65535.0);
                }
                return (rFull, gFull, bFull);
            }
            
            return (rRaw, gRaw, bRaw);
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

                // 6. Gamma / Transfer Function
                if (_transferMode == TransferMode.BT709)
                {
                    // BT.709 OETF
                    // L <= 0.018: V = 4.5 * L
                    // L > 0.018:  V = 1.099 * L^0.45 - 0.099
                    if (val <= 0.018) val = 4.5 * val;
                    else val = 1.099 * Math.Pow(val, 0.45) - 0.099;

                    // Apply Gamma Offset on top
                    if (Math.Abs(ch.Gamma - 1.0) > 0.001)
                    {
                        double g = Math.Max(0.1, ch.Gamma);
                        val = Math.Pow(val, 1.0 / g);
                    }
                }
                else if (_transferMode == TransferMode.BT2020)
                {
                    // BT.2020 OETF (Using user's 10-bit coeffs which match 709 structure approx)
                    // They quoted: alpha ~ 1.099, beta ~ 0.018.
                    // E' = 4.5 * E (E < beta)
                    // E' = alpha * E^0.45 - (alpha - 1) (E >= beta)
                    double alpha = 1.099;
                    double beta = 0.018;
                    if (val < beta) val = 4.5 * val;
                    else val = alpha * Math.Pow(val, 0.45) - (alpha - 1.0);

                    // Apply Gamma Offset on top
                    if (Math.Abs(ch.Gamma - 1.0) > 0.001)
                    {
                        double g = Math.Max(0.1, ch.Gamma);
                        val = Math.Pow(val, 1.0 / g);
                    }
                }
                else
                {
                    // Standard Power Law
                    double g = Math.Max(0.1, ch.Gamma);
                    val = Math.Pow(val, 1.0 / g);
                }

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

                // 2.0 De-Haze (S-Curve)
                // Sigmoid: 1 / (1 + exp(-k * (x - 0.5)))
                if (Math.Abs(_deHaze) > 0.001)
                {
                    double x = val;
                    if (_deHaze > 0)
                    {
                        // S-Curve (Contrast Boost) -> Removes Fog
                        // k usually around 10-20 for strong eq.
                        double k = 5.0 * _deHaze; // _deHaze 0-10 now

                        // Center at 0.5?
                        // Simple S-curve approximation:
                        // val = 0.5 + (val - 0.5) * (1.0 + _deHaze); // Linear contrast
                        // Sigmoidal:
                        // We want 0->0, 1->1.
                        // reuse Contrast function but applied as "DeHaze"
                        double sVal = (1.0 / (1.0 + Math.Exp(-k * (x - 0.5)))) - 0.5; // range approx -0.5 to 0.5
                        // Scale back to 0-1
                        // Need to normalize based on k
                        double min = (1.0 / (1.0 + Math.Exp(-k * (-0.5)))) - 0.5;
                        double max = (1.0 / (1.0 + Math.Exp(-k * (0.5)))) - 0.5;
                        val = (sVal - min) / (max - min);
                    }
                    else
                    {
                        // Negative DeDehaze (Fog)
                        // Use a division formula that decays to gray but never inverts
                        // val = 0.5 + (val - 0.5) / (1.0 + amount)
                        double amount = Math.Abs(_deHaze);
                        val = 0.5 + (val - 0.5) / (1.0 + amount); 
                    }
                }

                // 3.0 Luminance (Gain)
                if (Math.Abs(_luminance) > 0.001)
                {
                     // Simple multiplier, clipped
                     val *= (1.0 + _luminance);
                }

                // 4.0 Smart Contrast (Composite)
                // "combine mid-gamma and De-Haze and black stabilizer"
                if (_smartContrast > 0.001)
                {
                    double h = _smartContrast; // 0-1
                    // 1. Black Stab Effect (Lift Shadows)
                    val = Math.Pow(val, 1.0 - (h * 0.3)); 
                    // 2. Mid-Tone Boost (Gamma)
                    val = Math.Pow(val, 1.0 / (1.0 + h * 0.4));
                    // 3. Constant S-Curve (De-Haze like)
                    val = 0.5 + (val - 0.5) * (1.0 + h * 0.3);
                }

                // 5.0 Exposure (Removed)
                
                // 6.0 Solarization (Removed)
                
                // 7.0 Posterization (Removed)
                
                // 8.0 Tone Sculpt (Ex-Bump) (Inverse S-Curve / Sine Offset)
                if (Math.Abs(_bump) > 0.001)
                {
                    // _bump range is now -4.0 to 4.0.
                    double w = Math.Pow(Math.Abs(2.0 * val - 1.0), 2.0);
                    double offset = Math.Sin(val * Math.PI * 2.0) * (_bump * 0.08) * w; 
                    val += offset;
                }

                // FIXED: Changed Math.Clamp to Clamp
                val = Clamp(val, 0.0, 1.0);

                // DITHERING (Global Strength, Local Random)
                if (_dithering > 0)
                {
                    // Use a unique seed per pixel/channel index effectively?
                    // Or just unique Random instances per channel at least!
                    // See BuildChannelRamp creation of 'rand'.
                    double noise = (rand.NextDouble() - 0.5) * (_dithering * 0.1); // Reduced multiplier
                    val += noise;
                }
                
                // Final Clamp
                val = Clamp(val, 0.0, 1.0);

                curve[i] = (ushort)(val * 65535.0);
            }
            return curve;
        }

        // Fix for Spline Interpolation Safety
        private static double SolveSpline(List<Point> pts, double x) {
             if (pts == null || pts.Count < 2) return x;
             
             // ... existing splice logic ...
             // Let's ensure 'pts' is sorted and unique X if not handled by caller?
             // Caller (Interpolate) does sort.
             
             // Locate segment
             int i = 0;
             for(; i < pts.Count - 1; i++) {
                 if (x >= pts[i].X && x <= pts[i+1].X) break;
             }
             if (i >= pts.Count - 1) i = pts.Count - 2; 

             Point p0 = i > 0 ? pts[i-1] : pts[i];
             Point p1 = pts[i];
             Point p2 = pts[i+1];
             Point p3 = (i + 2 < pts.Count) ? pts[i+2] : pts[i+1];

             double dist = p2.X - p1.X;
             if (dist < 0.001) return p1.Y; // Prevent div/0

             double t = (x - p1.X) / dist;
             
             double t2 = t * t;
             double t3 = t2 * t;

             double v = 0.5 * (
                 (2 * p1.Y) +
                 (-p0.Y + p2.Y) * t +
                 (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                 (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3
             );

             return Math.Max(0, Math.Min(255, v));
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
                Luminance = _luminance,
                SmartContrast = _smartContrast,
                DeHaze = _deHaze,
                Temperature = _temperature,
                Tint = _tint,
                Bump = _bump, // Serialized as Bump still to match Common.cs
                // Removed Exp/Sol/Post
                
                ShadowTint = ShadowTint.ToArgb(),
                HighlightTint = HighlightTint.ToArgb(),
                
                // Copy Curves
                CurvesR = PointCurveR.Select(p => new PointDef { X = p.X, Y = p.Y }).ToList(),
                CurvesG = PointCurveG.Select(p => new PointDef { X = p.X, Y = p.Y }).ToList(),
                CurvesB = PointCurveB.Select(p => new PointDef { X = p.X, Y = p.Y }).ToList(),
                CurvesMaster = PointCurveMaster.Select(p => new PointDef { X = p.X, Y = p.Y }).ToList(),

                Smooth = Smooth,

                Dithering = _dithering,
                Sharpness = _sharpness,
                TransferMode = _transferMode,
                Api = _api
            };
        }

        public void ApplySettings(ExtendedColorSettings s)
        {
            if (s == null) return;
            Red = s.Red ?? new ChannelData();
            Green = s.Green ?? new ChannelData();
            Blue = s.Blue ?? new ChannelData();
            
            _transferMode = s.TransferMode; // Load mode

            Saturation = s.Saturation;
            Hue = s.Hue;
            Luminance = s.Luminance;
            SmartContrast = s.SmartContrast;
            DeHaze = s.DeHaze;
            Temperature = s.Temperature;
            Tint = s.Tint;
            ToneSculpt = s.Bump; // Keep serialized name 'Bump' for compatibility or change it? 
            // User complained about settings not loading/saving correctly before, 
            // but if we change the property in ExtendedColorSettings we break old profiles.
            // Let's keep 'Bump' in JSON for now or update Common to have 'ToneSculpt'.
            // Actually user asked for a rename of the UI functionality mostly.
            // I'll map `ToneSculpt` (API) <-> `s.Bump` (DTO).
            
            try { ShadowTint = Color.FromArgb(s.ShadowTint); } catch { ShadowTint = Color.Black; }
            try { HighlightTint = Color.FromArgb(s.HighlightTint); } catch { HighlightTint = Color.White; }
            
            if (s.CurvesR != null) PointCurveR = s.CurvesR.Select(p => new System.Drawing.Point(p.X, p.Y)).ToList();
            else PointCurveR = new List<System.Drawing.Point>();

            if (s.CurvesG != null) PointCurveG = s.CurvesG.Select(p => new System.Drawing.Point(p.X, p.Y)).ToList();
            else PointCurveG = new List<System.Drawing.Point>();

            if (s.CurvesB != null) PointCurveB = s.CurvesB.Select(p => new System.Drawing.Point(p.X, p.Y)).ToList();
            else PointCurveB = new List<System.Drawing.Point>();

            if (s.CurvesMaster != null) PointCurveMaster = s.CurvesMaster.Select(p => new System.Drawing.Point(p.X, p.Y)).ToList();
            else PointCurveMaster = new List<System.Drawing.Point>();

            Dithering = s.Dithering;
            Sharpness = s.Sharpness;
            Api = s.Api;
            Smooth = s.Smooth;
            Update();
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void ApplyPointCurve(ushort[] channel, List<System.Drawing.Point> points)
        {
            if (points == null || points.Count < 2) return;
            // Apply curve to the channel data
             double[] curveLut = new double[256];
             
             for (int i = 0; i < 256; i++)
             {
                 curveLut[i] = GammaServiceHelper.Interpolate(points, i, Smooth) / 255.0;
             }

             for(int i=0; i<256; i++) {
                  // Map current linear position (0-255) to curve value
                  // Channel values are 0-65535.
                  // We treat the channel value as the input X to the curve?
                  // Yes.
                  int idx = ClampInt((int)(channel[i] / 65535.0 * 255.0), 0, 255);
                  channel[i] = (ushort)(Clamp(curveLut[idx], 0, 1) * 65535.0);
             }
        }
        
        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public List<System.Drawing.Point> GetOETFPoints(TransferMode mode)
        {
            var list = new List<System.Drawing.Point>();
            int steps = 128;
            for (int i = 0; i <= steps; i++)
            {
                double L = (double)i / steps;
                double V = L;

                if (mode == TransferMode.BT709)
                {
                    if (L < 0.018) V = 4.5 * L;
                    else V = 1.099 * Math.Pow(L, 0.45) - 0.099;
                }
                else if (mode == TransferMode.BT2020)
                {
                    double alpha = 1.099; 
                    double beta = 0.018;  
                    if (L < beta) V = 4.5 * L;
                    else V = alpha * Math.Pow(L, 0.45) - (alpha - 1.0);
                }
                
                V = Math.Max(0, Math.Min(1, V));
                list.Add(new System.Drawing.Point((int)(L * 255.0), (int)(V * 255.0)));
            }
            return list;
        }
    }

    public static class GammaServiceHelper {
        public static double Interpolate(IList<Point> points, double x, bool spline) {
            if (points == null || points.Count == 0) return x;
            var sorted = points.OrderBy(p => p.X).ToList();
            
            if (!spline) {
                // Linear
                var p2 = sorted.FirstOrDefault(p => p.X >= x);
                var p1 = sorted.LastOrDefault(p => p.X <= x && p != p2);
                if (p2.IsEmpty) p2 = p1; 
                if (p1.IsEmpty) p1 = p2; // Fallback
                
                if (Math.Abs(p1.X - p2.X) < 0.001) return p1.Y;
                
                double t = (x - p1.X) / (double)(p2.X - p1.X);
                return p1.Y + (p2.Y - p1.Y) * t;
            } else {
                // Catmull-Rom Spline Interpolation for smoothness
                return SolveSpline(sorted, x);
            }
        }

        
        // Simple Catmull-Rom implementation
        private static double SolveSpline(List<System.Drawing.Point> pts, double x) {
             if (pts.Count < 2) return x;
             
             // Find segments
             int i = 0;
             for(; i < pts.Count - 1; i++) {
                 if (x >= pts[i].X && x <= pts[i+1].X) break;
             }
             // Clamp to last segment
             if (i >= pts.Count - 1) i = pts.Count - 2; 

             System.Drawing.Point p0 = i > 0 ? pts[i-1] : pts[i];
             System.Drawing.Point p1 = pts[i];
             System.Drawing.Point p2 = pts[i+1];
             System.Drawing.Point p3 = (i + 2 < pts.Count) ? pts[i+2] : pts[i+1];

             double t = (x - p1.X) / (double)Math.Max(1, p2.X - p1.X);
             
             // Catmull-Rom formula
             double t2 = t * t;
             double t3 = t2 * t;

             double v = 0.5 * (
                 (2 * p1.Y) +
                 (-p0.Y + p2.Y) * t +
                 (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                 (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3
             );

             return Math.Max(0, Math.Min(255, v));
        }
    }
}
