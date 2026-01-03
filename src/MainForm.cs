using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GamerGamma
{
    public class MainForm : Form
    {
        private GammaService _gamma;
        private ChannelMode _channelMode = ChannelMode.Linked;

        // Context Buttons
        private Button btnLink, btnRed, btnGreen, btnBlue;
        
        // Removed tbSat/nudSat. Added tbTemp/nudTemp etc.
        private TrackBar tbGamma, tbBright, tbContrast, tbLum, tbBlackFloor, tbWhiteCeil, tbBlackStab, tbWhiteStab, tbMidGamma, tbHdr, tbDeHaze, tbTemp, tbTint, tbBlackLevel, tbHue, tbDither, tbBump;
        private NumericUpDown nudGamma, nudBright, nudContrast, nudLum, nudBlackFloor, nudWhiteCeil, nudBlackStab, nudWhiteStab, nudMidGamma, nudHdr, nudDeHaze, nudTemp, nudTint, nudBlackLevel, nudHue, nudDither, nudBump;

        // Vertical RGB Gamma Sliders
        private TrackBar tbR, tbG, tbB;
        private NumericUpDown nudR, nudG, nudB;
        
        // Split Toning
        private Button btnShadow, btnHigh;

        // Tray & UI
        private NotifyIcon trayIcon;
        private CheckBox chkMinimizeToTray;
        private ComboBox cbMonitors, cbProfiles;
        private Label lblMonitorInfo, lblChain;
        private Panel grpProf;
        private PictureBox previewBox;
        private AppSettings _appSettings;
        private string _settingsPath;
        private CheckBox chkStartWithWin;
        private CheckBox chkStartMinimized;
        private bool _ignoreEvents = false;
        private int boxW = 310;
        private int leftW = 190;
        private double _lastMasterGamma = 1.0;

        private CheckBox btnCurves;
        private PointCurveEditorForm _curveEditor;

        public MainForm()
        {
            this.Text = "Gamer Gamma v1.3";
            this.Size = new Size(1250, 660);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = false;
            
            try { this.Icon = CreateLightbulbIcon(); } catch {}

            _gamma = new GammaService();
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            _ignoreEvents = true;

            InitTray();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Padding = new Padding(0) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, leftW + 20)); // Col 0
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, boxW + 20));  // Col 1
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, boxW + 20));  // Col 2
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));        // Col 3 (Preview)
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Main
            Controls.Add(root);

            // Column 0
            var pnlLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoScroll = false, WrapContents = false };
            root.Controls.Add(pnlLeft, 0, 0);

            // Monitor Info
            var grpMon = CreateGroup("Monitor Info", leftW);
            cbMonitors = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = leftW - 20, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            lblMonitorInfo = new Label { Text = "Resolution info...", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 5, 0, 0) };
            grpMon.Controls.Add(cbMonitors);
            grpMon.Controls.Add(lblMonitorInfo);
            pnlLeft.Controls.Add(grpMon);

            // Quick Settings
            var grpQuick = CreateGroup("Quick Settings", leftW);
            var pnlQuickGrid = new TableLayoutPanel { Width = leftW - 20, Height = 125, ColumnCount = 2, RowCount = 3 };
            pnlQuickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlQuickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Button CreateQBtn(string t, Action a, Color? bc = null) {
                var b = new Button { Text = t, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = bc ?? Color.FromArgb(50, 50, 50), ForeColor = Color.White, Margin = new Padding(2) };
                b.Click += (s, e) => { a(); UpdateUIValues(); DrawPreview(); };
                return b;
            }

            // Buttons renaming as requested
            var fBtn = new Font(FontFamily.GenericSansSerif, 7);
            
            var btnExt709 = CreateCtxBtn("BT.709", ChannelMode.Linked, Color.Yellow); 
            btnExt709.Click += (s,e) => { 
                _gamma.Reset(); 
                // Mode remains PowerLaw (Standard), we apply the curve visually
                _gamma.PointCurveMaster = _gamma.GetOETFPoints(TransferMode.BT709);
                if (_curveEditor != null && !_curveEditor.IsDisposed) _curveEditor.LoadPoints();
                _gamma.Update(); 
                UpdateUIValues(); 
                DrawPreview();
            };
            btnExt709.Font = fBtn;
            btnExt709.Width = (leftW - 20) / 2 - 4; 
            pnlQuickGrid.Controls.Add(btnExt709, 0, 0);

            var btnFps = CreateCtxBtn("FPS Gamer", ChannelMode.Linked, Color.HotPink);
            btnFps.Click += (s,e) => {
                 _gamma.Reset();
                 // BT2020 + BlackStab 0.0 + WhiteStab 0.8 + MidGamma 0.1 + Master 1.0
                 _gamma.TransferMode = TransferMode.BT2020;
                 _gamma.Red.Gamma = _gamma.Green.Gamma = _gamma.Blue.Gamma = 1.0;
                 _gamma.Red.BlackStab = _gamma.Green.BlackStab = _gamma.Blue.BlackStab = 0.0;
                 _gamma.Red.WhiteStab = _gamma.Green.WhiteStab = _gamma.Blue.WhiteStab = 0.8;
                 _gamma.Red.MidGamma = _gamma.Green.MidGamma = _gamma.Blue.MidGamma = 0.1;
                 UpdateUIValues();
                 _gamma.Update();
                 DrawPreview(); 
            };
            btnFps.Font = fBtn;
            btnFps.Width = (leftW - 20) / 2 - 4; 
            pnlQuickGrid.Controls.Add(btnFps, 1, 0);

            pnlQuickGrid.Controls.Add(CreateQBtn("De-Haze", () => { 
                _gamma.Reset(); 
                _gamma.Red.Gamma = _gamma.Green.Gamma = _gamma.Blue.Gamma = 1.10;
                _gamma.Red.BlackStab = _gamma.Green.BlackStab = _gamma.Blue.BlackStab = 0.36;
                _gamma.Red.MidGamma = _gamma.Green.MidGamma = _gamma.Blue.MidGamma = 0.14;
                _gamma.DeHaze = 0.60; 
                _gamma.Temperature = 0.0;
                // Add any other specific settings? These match the request.
                _gamma.Update(); 
            }, Color.FromArgb(70, 60, 100)), 0, 1);
            pnlQuickGrid.Controls.Add(CreateQBtn("DEFAULT", () => _gamma.Reset(), Color.DarkGreen), 1, 1);
            pnlQuickGrid.Controls.Add(CreateQBtn("WARM", ApplyWarmMode, Color.FromArgb(120, 70, 30)), 0, 2);
            pnlQuickGrid.Controls.Add(CreateQBtn("COOL", ApplyCoolMode, Color.FromArgb(70, 100, 150)), 1, 2);

            // Removed duplicate FPS Gamer button

            grpQuick.Controls.Add(pnlQuickGrid);
            pnlLeft.Controls.Add(grpQuick);

            // Profiles
            grpProf = CreateGroup("Profiles", leftW); 
            grpProf.Height = 280; 
            cbProfiles = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = leftW - 20, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnSave = new Button { Text = "Save", Width = 50, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,50), Margin = new Padding(0,5,5,0) };
            var btnDel = new Button { Text = "Del", Width = 45, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,50), Margin = new Padding(0,5,5,0) };
            var btnBind = new Button { Text = "Bind", Width = 50, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,50), Margin = new Padding(0,5,0,0) };
            chkStartWithWin = new CheckBox { Text = "Start with Windows", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            chkStartWithWin.CheckedChanged += (s, e) => { SetStartWithWindows(chkStartWithWin.Checked); SaveSettings(); };
            chkMinimizeToTray = new CheckBox { Text = "Minimize to Tray", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
            chkMinimizeToTray.CheckedChanged += (s, e) => SaveSettings();
            chkStartMinimized = new CheckBox { Text = "Start Minimized", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
            chkStartMinimized.CheckedChanged += (s, e) => SaveSettings();

            var btnExport = new Button { Text = "Export", Width = 75, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,80), Margin = new Padding(0,5,5,0) };
            var btnImport = new Button { Text = "Import", Width = 75, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,80,50), Margin = new Padding(0,5,0,0) };
            
            grpProf.Controls.Add(cbProfiles);
            var pnlProfBtns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0,5,0,0) };
            pnlProfBtns.Controls.AddRange(new[] { btnSave, btnDel, btnBind });
            grpProf.Controls.Add(pnlProfBtns);
            grpProf.Controls.Add(chkStartWithWin);
            grpProf.Controls.Add(chkMinimizeToTray);
            grpProf.Controls.Add(chkStartMinimized);
            var pnlIOBtns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0,5,0,0) };
            pnlIOBtns.Controls.AddRange(new[] { btnExport, btnImport });
            grpProf.Controls.Add(pnlIOBtns);

            // Expand Button (Moved Inside Profiles)
            var btnExpand = new Button { Text = "Advanced >>", Width = leftW - 20, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 40, 40), Margin = new Padding(0, 10, 0, 0) };
            btnExpand.Click += (s, e) => ToggleProView();
            grpProf.Controls.Add(btnExpand);

            pnlLeft.Controls.Add(grpProf);

            // Relocated Footer Info (Into Left Panel bottom)
            var pnlIntegratedFooter = new FlowLayoutPanel { Width = leftW, Height = 120, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 5, 0, 0) };
            var lblFooter = new Label { Text = "© omaxtr 2026 // twitch.tv/omaxtr\nGamerGamma v1.3", Width = leftW, Height = 40, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.Gray, Font = new Font("Consolas", 7) };
            var btnCoffee = new Button { 
                Text = "☕ Buy me a Coffee", 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(255, 128, 0), 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Width = 140,
                Height = 25,
                Margin = new Padding((leftW - 140) / 2, 0, 0, 0)
            };
            btnCoffee.FlatAppearance.BorderSize = 0;
            btnCoffee.Click += (s, e) => { System.Diagnostics.Process.Start("https://ko-fi.com/omaxtr/tip"); };
            
            pnlIntegratedFooter.Controls.Add(lblFooter);
            pnlIntegratedFooter.Controls.Add(btnCoffee);
            pnlLeft.Controls.Add(pnlIntegratedFooter);

            btnSave.Click += BtnSaveProfile_Click;
            btnDel.Click += BtnRemoveProfile_Click;
            btnBind.Click += BtnHotkeyBind_Click;
            btnExport.Click += BtnExport_Click;
            btnImport.Click += BtnImport_Click;

            // Column 1
            var pnlMid = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlMid, 1, 0);

            var grpMaster = CreateGroup("Master Channels", boxW);
            var pnlMasterSliders = new FlowLayoutPanel { Width = boxW - 20, Height = 210, FlowDirection = FlowDirection.LeftToRight };
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Master", 0.1, 4.0, 1.0, out tbGamma, out nudGamma, isMaster: true));
            lblChain = new Label { Text = "🔗", AutoSize = true, Font = new Font(Font.FontFamily, 12), ForeColor = Color.Gray, Margin = new Padding(0, 80, 0, 0) };
            pnlMasterSliders.Controls.Add(lblChain);
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Red", 0.1, 4.0, 1.0, out tbR, out nudR, "Red"));
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Green", 0.1, 4.0, 1.0, out tbG, out nudG, "Green"));
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Blue", 0.1, 4.0, 1.0, out tbB, out nudB, "Blue"));
            grpMaster.Controls.Add(pnlMasterSliders);

            // Channel Mode Buttons
            var pnlModeBtns = new FlowLayoutPanel { Width = boxW - 20, Height = 35, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 10, 0, 0), WrapContents = false };
            btnLink = CreateCtxBtn("Linked", ChannelMode.Linked, Color.White);
            btnRed = CreateCtxBtn("Red", ChannelMode.Red, Color.Red);
            btnGreen = CreateCtxBtn("Green", ChannelMode.Green, Color.Lime);
            btnBlue = CreateCtxBtn("Blue", ChannelMode.Blue, Color.Cyan);
            foreach (Button b in new[] { btnLink, btnRed, btnGreen, btnBlue }) { b.Width = (boxW - 40) / 4; b.Height = 25; b.Font = new Font(b.Font.FontFamily, 7, FontStyle.Bold); }
            pnlModeBtns.Controls.AddRange(new[] { btnLink, btnRed, btnGreen, btnBlue });
            grpMaster.Controls.Add(pnlModeBtns);
            pnlMid.Controls.Add(grpMaster);

            var grpColorDim = CreateGroup("Color Dimension", boxW);
            grpColorDim.Controls.Add(CreateSlider("Luminance", -1.0, 1.0, 0.0, out tbLum, out nudLum));
            grpColorDim.Controls.Add(CreateSlider("Hue", 0.0, 359.0, 0.0, out tbHue, out nudHue)); // 0..359
            grpColorDim.Controls.Add(CreateSlider("Temperature", -1.0, 1.0, 0.0, out tbTemp, out nudTemp)); 
            grpColorDim.Controls.Add(CreateSlider("Tint", -1.0, 1.0, 0.0, out tbTint, out nudTint));
            pnlMid.Controls.Add(grpColorDim);

            // Column 2
            var pnlRight = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlRight, 2, 0);

            var grpLevels = CreateGroup("Levels", boxW);
            grpLevels.Controls.Add(CreateSlider("Brightness", 0.0, 2.0, 1.0, out tbBright, out nudBright));
            grpLevels.Controls.Add(CreateSlider("Contrast", 0.0, 2.0, 1.0, out tbContrast, out nudContrast)); 
            grpLevels.Controls.Add(CreateSlider("Black Level", -1.0, 1.0, 0.0, out tbBlackLevel, out nudBlackLevel));
            pnlRight.Controls.Add(grpLevels);

            var grpStab = CreateGroup("Stabilizers", boxW);
            grpStab.Controls.Add(CreateSlider("Black Stabilizer", -2.0, 2.0, 0.0, out tbBlackStab, out nudBlackStab)); 
            grpStab.Controls.Add(CreateSlider("White Stabilizer", -2.0, 2.0, 0.0, out tbWhiteStab, out nudWhiteStab)); 
            grpStab.Controls.Add(CreateSlider("Mid-Gamma", -1.0, 1.0, 0.0, out tbMidGamma, out nudMidGamma));
            grpStab.Controls.Add(CreateSlider("Black Floor", -1.0, 1.0, 0.0, out tbBlackFloor, out nudBlackFloor));
            grpStab.Controls.Add(CreateSlider("White Ceiling", -1.0, 1.0, 0.0, out tbWhiteCeil, out nudWhiteCeil));
            pnlRight.Controls.Add(grpStab);

            // Column 3: Preview
            var pnlCol3 = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlCol3, 3, 0);

            var grpPreview = new Panel { Width = 350, Height = 400, BackColor = Color.FromArgb(45, 45, 48), Margin = new Padding(5), Padding = new Padding(10) };
            var lblPreviewTitle = new Label { Text = "PREVIEW", Dock = DockStyle.Top, Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold), ForeColor = Color.Gray, Height = 25 };
            
            previewBox = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.StretchImage, Padding = new Padding(0) };
            var previewContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            previewContainer.Controls.Add(previewBox);
            
            grpPreview.Controls.Add(previewContainer);
            grpPreview.Controls.Add(lblPreviewTitle);
            pnlCol3.Controls.Add(grpPreview);

            // grpExtras REMOVED - Controls moved elsewhere
            
            // COLOR PREVIEW (Relocated from Advanced)
            var grpColPrev = new Panel { Width = 350, Height = 150, BackColor = Color.FromArgb(40,40,40), Margin = new Padding(5, 10, 5, 0) };
            var lblCP = new Label { Text = "COLOR PREVIEW", Dock = DockStyle.Top, Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold), ForeColor = Color.Gray, Height = 20 };
            var pbox = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Black };
            pbox.Paint += (s, e) => {
                var g = e.Graphics;
                int w = pbox.Width;
                int h = pbox.Height;
                if (w <= 0 || h <= 0) return;

                // 1. SMPTE Bars (40%)
                Color[] bars = new Color[] { Color.White, Color.Yellow, Color.Cyan, Color.Lime, Color.Magenta, Color.Red, Color.Blue };
                int barsH = (int)(h * 0.4);
                for (int i = 0; i < 7; i++) {
                    int x1 = i * w / 7;
                    int x2 = (i + 1) * w / 7;
                    using (var b = new SolidBrush(bars[i])) g.FillRectangle(b, x1, 0, x2 - x1, barsH);
                }

                int stripH = (int)(h * 0.2);

                // 2. Hue Spectrum (20%)
                using (var lb = new LinearGradientBrush(new Rectangle(0, barsH, w, stripH), Color.Red, Color.Red, LinearGradientMode.Horizontal)) {
                    ColorBlend cb = new ColorBlend();
                    cb.Positions = new[] { 0f, 0.16f, 0.33f, 0.5f, 0.66f, 0.83f, 1f };
                    cb.Colors = new[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red };
                    lb.InterpolationColors = cb;
                    g.FillRectangle(lb, 0, barsH, w, stripH);
                }

                // 3. Smooth Grayscale Gradient (20%)
                using (var lb = new LinearGradientBrush(new Rectangle(0, barsH + stripH, w, stripH), Color.Black, Color.White, LinearGradientMode.Horizontal)) {
                    g.FillRectangle(lb, 0, barsH + stripH, w, stripH);
                }

                // 4. Stepped Grayscale (20%) - 20 Steps (Reversed: White to Black)
                int steps = 20;
                for (int i = 0; i < steps; i++) {
                    int x1 = i * w / steps;
                    int x2 = (i + 1) * w / steps;
                    int gray = 255 - (int)(i * 255.0 / (steps - 1)); // 255 down to 0
                    using (var b = new SolidBrush(Color.FromArgb(gray, gray, gray))) {
                        g.FillRectangle(b, x1, barsH + 2 * stripH, x2 - x1, h - (barsH + 2 * stripH));
                    }
                }
            };
            grpColPrev.Controls.Add(pbox);
            grpColPrev.Controls.Add(lblCP);
            pnlCol3.Controls.Add(grpColPrev);
            // HDR Toning removed from here
            // Exp/Sol/Post REMOVED

            this.FormClosing += (s, e) => { trayIcon.Visible = false; };
            
            this.Resize += (s, e) => { 
                if (this.WindowState == FormWindowState.Minimized && chkMinimizeToTray.Checked) { 
                    this.ShowInTaskbar = false; 
                    this.Hide(); 
                    trayIcon.Visible = true; 
                } 
            };

            LoadMonitors();
            LoadProfiles();
            
            _ignoreEvents = true; 
            if (_appSettings.CurrentSettings != null) {
                _gamma.ApplySettings(_appSettings.CurrentSettings);
            }

            if (!string.IsNullOrEmpty(_appSettings.SelectedMonitorDeviceName)) {
                for (int i = 0; i < cbMonitors.Items.Count; i++) {
                    if (cbMonitors.Items[i].ToString().Contains(_appSettings.SelectedMonitorDeviceName)) {
                        cbMonitors.SelectedIndex = i;
                        break;
                    }
                }
            } else if (cbMonitors.Items.Count > 0) {
                cbMonitors.SelectedIndex = 0;
            }

            chkMinimizeToTray.Checked = _appSettings.MinimizeToTray;
            chkStartWithWin.Checked = GetStartWithWindows();
            chkStartMinimized.Checked = _appSettings.StartMinimized;

            UpdateUIValues();
            DrawPreview();
            _ignoreEvents = false;

            if (_appSettings.StartMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                if (_appSettings.MinimizeToTray)
                {
                    this.ShowInTaskbar = false; 
                    this.Hide();
                    trayIcon.Visible = true;
                }
            }
        }

        private void SetStartWithWindows(bool start)
        {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (start) key.SetValue("GamerGamma", Application.ExecutablePath);
                    else key.DeleteValue("GamerGamma", false);
                }
            } catch {}
        }

        private bool GetStartWithWindows()
        {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false)) {
                    return key.GetValue("GamerGamma") != null;
                }
            } catch { return false; }
        }

        private void InitTray()
        {
            trayIcon = new NotifyIcon { Text = "Gamer Gamma", Visible = false };
            try { trayIcon.Icon = CreateLightbulbIcon(); } catch {}
            var menu = new ContextMenuStrip();
            Action openAction = () => { 
                this.ShowInTaskbar = true; 
                this.Show(); 
                this.WindowState = FormWindowState.Normal; 
            };
            menu.Items.Add("Open", null, (s, e) => openAction());
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, e) => { trayIcon.Visible = false; Application.Exit(); });
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (s, e) => openAction();
        }

        private Icon CreateLightbulbIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.Yellow, 8, 4, 16, 20); // Bulb
                g.FillRectangle(Brushes.Gray, 10, 22, 12, 6);   // Base
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private Panel CreateVerticalSlider(string label, double min, double max, double def, out TrackBar tb, out NumericUpDown nud, string channelName = null, bool isMaster = false)
        {
            var p = new Panel { Size = new Size(55, 200), Margin = new Padding(2) };
            var l = new Label { Text = label, Dock = DockStyle.Bottom, Height = 15, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 7), ForeColor = Color.Gray };
            var btnReset = new Button { Text = "↺", Size = new Size(20, 20), Dock = DockStyle.Bottom, FlatStyle = FlatStyle.Flat, ForeColor = Color.Red, BackColor = Color.FromArgb(40, 40, 40), Font = new Font(Font.FontFamily, 6) };
            btnReset.FlatAppearance.BorderSize = 0;

            nud = new NumericUpDown { Minimum = (decimal)min, Maximum = (decimal)max, Value = (decimal)def, DecimalPlaces = 2, Increment = 0.01M, Dock = DockStyle.Top, Width = 45, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, Font = new Font(Font.FontFamily, 7) };
            tb = new TrackBar { Orientation = Orientation.Vertical, Minimum = 0, Maximum = 1000, Value = (int)((def - min) / (max - min) * 1000), TickStyle = TickStyle.Both, TickFrequency = 250, Dock = DockStyle.Fill };

            var cTb = tb; var cNud = nud;
            cTb.Scroll += (s, e) => {
                double v = min + (cTb.Value / 1000.0) * (max - min);
                cNud.Value = (decimal)v;
            };
            cNud.ValueChanged += (s, e) => {
                int ival = (int)(((double)cNud.Value - min) / (max - min) * 1000);
                cTb.Value = (int)Clamp(ival, 0, 1000);
                
                if (!_ignoreEvents) {
                    if (isMaster) {
                        double newVal = (double)cNud.Value;
                        double delta = newVal - _lastMasterGamma;
                        if (_channelMode == ChannelMode.Linked) {
                            _gamma.Red.Gamma += delta;
                            _gamma.Green.Gamma += delta;
                            _gamma.Blue.Gamma += delta;
                        } else {
                            if (_channelMode == ChannelMode.Red) _gamma.Red.Gamma = newVal;
                            else if (_channelMode == ChannelMode.Green) _gamma.Green.Gamma = newVal;
                            else if (_channelMode == ChannelMode.Blue) _gamma.Blue.Gamma = newVal;
                        }
                        _gamma.Update();
                        _lastMasterGamma = newVal;
                        UpdateUIValues();
                        DrawPreview();
                    }
                    else if (channelName != null) ApplyChannelGamma(channelName, (double)cNud.Value);
                }
            };
            btnReset.Click += (s, e) => cNud.Value = (decimal)def;
            p.Controls.Add(tb); p.Controls.Add(nud); p.Controls.Add(btnReset); p.Controls.Add(l);
            return p;
        }

        private Panel CreateSlider(string label, double min, double max, double def, out TrackBar tb, out NumericUpDown nud, double step = 0.01)
        {
            int w = boxW - 20;
            var p = new Panel { Size = new Size(w, 45), Margin = new Padding(0, 0, 0, 5) };
            var l = new Label { Text = label, Location = new Point(0, 0), AutoSize = true, Font = new Font(Font.FontFamily, 7), ForeColor = Color.Gray };
            var btnReset = new Button { Text = "↺", Size = new Size(22, 22), Location = new Point(w - 25, 18), FlatStyle = FlatStyle.Flat, ForeColor = Color.Red, BackColor = Color.FromArgb(40, 40, 40), Font = new Font(Font.FontFamily, 8) };
            btnReset.FlatAppearance.BorderSize = 0;
            nud = new NumericUpDown { Minimum = (decimal)min, Maximum = (decimal)max, Value = (decimal)def, DecimalPlaces = 2, Increment = (decimal)step, Width = 55, Location = new Point(w - 60, 0), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, Font = new Font(Font.FontFamily, 7) };
            tb = new TrackBar { Minimum = 0, Maximum = 1000, Value = (int)((def - min) / (max - min) * 1000), TickStyle = TickStyle.Both, TickFrequency = 250, Width = w - 65, Location = new Point(0, 15), Height = 25 };
            var ct = tb; var cn = nud;
            ct.Scroll += (s, e) => {
                double v = min + (ct.Value / 1000.0) * (max - min);
                cn.Value = (decimal)v;
            };
            cn.ValueChanged += (s, e) => {
                ct.Value = (int)(((double)cn.Value - min) / (max - min) * 1000);
                if (!_ignoreEvents) ApplyValue(label, (double)cn.Value);
            };
            btnReset.Click += (s, e) => cn.Value = (decimal)def;
            p.Controls.Add(l); p.Controls.Add(tb); p.Controls.Add(nud); p.Controls.Add(btnReset);
            return p;
        }

        private Panel CreateGroup(string title, int width = 200)
        {
            var p = new FlowLayoutPanel { 
                FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Width = width, MinimumSize = new Size(width, 100), MaximumSize = new Size(width, 2000), BackColor = Color.FromArgb(45, 45, 48), Margin = new Padding(5), Padding = new Padding(10) 
            };
            p.Controls.Add(new Label { Text = title.ToUpper(), Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0,0,0,10) });
            return p;
        }
        
        private void SetChannelMode(ChannelMode mode)
        {
            _channelMode = mode;
            UpdateUIValues();
        }

        private void UpdateUIValues()
        {
            if (_gamma == null) return;
            _ignoreEvents = true;

            var d = (_channelMode == ChannelMode.Red) ? _gamma.Red :
                       (_channelMode == ChannelMode.Green) ? _gamma.Green :
                       (_channelMode == ChannelMode.Blue) ? _gamma.Blue :
                       _gamma.Red; 

            SafeSetNud(nudGamma, d.Gamma); 
            _lastMasterGamma = (double)nudGamma.Value;
            
            SafeSetNud(nudBright, d.Brightness);
            SafeSetNud(nudContrast, d.Contrast);
            SafeSetNud(nudLum, _gamma.Luminance);
            
            SafeSetNud(nudBlackStab, d.BlackStab);
            SafeSetNud(nudWhiteStab, d.WhiteStab);
            SafeSetNud(nudMidGamma, d.MidGamma);
            SafeSetNud(nudHdr, _gamma.SmartContrast);
            
            SafeSetNud(nudBlackFloor, d.BlackFloor);
            SafeSetNud(nudWhiteCeil, d.WhiteCeiling);
            
            SafeSetNud(nudDeHaze, _gamma.DeHaze);
            SafeSetNud(nudTemp, _gamma.Temperature);
            SafeSetNud(nudTint, _gamma.Tint);
            
            SafeSetNud(nudBlackLevel, d.BlackLevel);
            SafeSetNud(nudHue, _gamma.Hue);
            SafeSetNud(nudDither, _gamma.Dithering);
            SafeSetNud(nudBump, _gamma.ToneSculpt);
            
            // Split Toning Sync
            if (btnShadow != null) {
                btnShadow.BackColor = _gamma.ShadowTint;
                btnShadow.ForeColor = (_gamma.ShadowTint.R + _gamma.ShadowTint.G + _gamma.ShadowTint.B > 380) ? Color.Black : Color.White;
            }
            if (btnHigh != null) {
                btnHigh.BackColor = _gamma.HighlightTint;
                btnHigh.ForeColor = (_gamma.HighlightTint.R + _gamma.HighlightTint.G + _gamma.HighlightTint.B > 380) ? Color.Black : Color.White;
            }
            
            if (nudR != null) {
                SafeSetNud(nudR, _gamma.Red.Gamma);
                SafeSetNud(nudG, _gamma.Green.Gamma);
                SafeSetNud(nudB, _gamma.Blue.Gamma);
                bool link = _channelMode == ChannelMode.Linked;
                tbR.Enabled = nudR.Enabled = !link && (_channelMode == ChannelMode.Red);
                tbG.Enabled = nudG.Enabled = !link && (_channelMode == ChannelMode.Green);
                tbB.Enabled = nudB.Enabled = !link && (_channelMode == ChannelMode.Blue);
                if (lblChain != null) lblChain.ForeColor = link ? Color.Cyan : Color.Gray;
            }

            void Sty(Button b, bool a) => b.BackColor = a ? Color.FromArgb(80,80,80) : Color.FromArgb(50,50,50);
            if(btnLink != null) Sty(btnLink, _channelMode == ChannelMode.Linked);
            if(btnRed != null) Sty(btnRed, _channelMode == ChannelMode.Red);
            if(btnGreen != null) Sty(btnGreen, _channelMode == ChannelMode.Green);
            if(btnBlue != null) Sty(btnBlue, _channelMode == ChannelMode.Blue);

            if (btnLink != null) btnLink.BackColor = (_channelMode == ChannelMode.Linked) ? Color.FromArgb(80, 80, 80) : Color.FromArgb(50, 50, 50);
            if (lblChain != null) lblChain.Visible = (_channelMode == ChannelMode.Linked);

            _ignoreEvents = false;
        }

        private void SafeSetNud(NumericUpDown nud, double val)
        {
            if (nud == null) return;
            decimal dVal = (decimal)Clamp(val, (double)nud.Minimum, (double)nud.Maximum);
            if (nud.Value != dVal) nud.Value = dVal;
        }


        private Button CreateCtxBtn(string t, ChannelMode m, Color c) {
            var b = new Button { Text=t, Width=80, Height=30, FlatStyle=FlatStyle.Flat, BackColor=Color.FromArgb(50,50,50), ForeColor=c };
            b.FlatAppearance.BorderColor = c;
            b.FlatAppearance.BorderSize = 1;
            b.Click += (s,e) => SetChannelMode(m);
            return b;
        }

        private void BtnHotkeyBind_Click(object sender, EventArgs e)
        {
            if (cbProfiles.SelectedIndex < 0) return;
            var prof = _appSettings.Profiles[cbProfiles.SelectedIndex];

            using (var f = new Form { Text = "Press any Key + Modifiers", Size = new Size(300, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog })
            {
                var lbl = new Label { Text = "Press keys (e.g. Ctrl + Alt + G)\nEsc to clear", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                f.Controls.Add(lbl);
                f.KeyPreview = true;
                f.KeyDown += (sf, ef) => {
                    if (ef.KeyCode == Keys.Escape) { prof.Hotkey = 0; prof.HotkeyModifiers = 0; f.DialogResult = DialogResult.OK; }
                    else if (ef.KeyCode != Keys.ControlKey && ef.KeyCode != Keys.ShiftKey && ef.KeyCode != Keys.Menu) {
                        prof.Hotkey = (int)ef.KeyCode;
                        int mods = 0;
                        if (ef.Control) mods |= 0x0002;
                        if (ef.Alt) mods |= 0x0001;
                        if (ef.Shift) mods |= 0x0004;
                        prof.HotkeyModifiers = mods;
                        f.DialogResult = DialogResult.OK;
                    }
                };
                if (f.ShowDialog() == DialogResult.OK) {
                    SaveSettings();
                    LoadProfiles();
                    RegisterHotkeys();
                }
            }
        }

        private void RegisterHotkeys()
        {
            for (int i = 0; i < _appSettings.Profiles.Count; i++) {
                GamerGammaApi.UnregisterHotKey(this.Handle, i);
                if (_appSettings.Profiles[i].Hotkey > 0) {
                    GamerGammaApi.RegisterHotKey(this.Handle, i, _appSettings.Profiles[i].HotkeyModifiers, _appSettings.Profiles[i].Hotkey);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) { // WM_HOTKEY
                int id = m.WParam.ToInt32();
                if (id >= 0 && id < _appSettings.Profiles.Count) {
                    _ignoreEvents = true;
                    cbProfiles.SelectedIndex = id;
                    _ignoreEvents = false;
                    _gamma.ApplySettings(_appSettings.Profiles[id].Settings);
                    UpdateUIValues();
                    DrawPreview();
                    SaveSettings();
                }
            }
            base.WndProc(ref m);
        }

        private void SetGamma(double g)
        {
            _gamma.Red.Gamma = g;
            _gamma.Green.Gamma = g;
            _gamma.Blue.Gamma = g;
            _gamma.Update();
            UpdateUIValues();
        }

        private void ApplyChannelGamma(string channel, double val)
        {
            double oldVal = (channel == "Red") ? _gamma.Red.Gamma : (channel == "Green") ? _gamma.Green.Gamma : _gamma.Blue.Gamma;
            if (_channelMode == ChannelMode.Linked)
            {
                double diff = val - oldVal;
                _gamma.Red.Gamma += diff;
                _gamma.Green.Gamma += diff;
                _gamma.Blue.Gamma += diff;
            }
            else
            {
                if (channel == "Red") _gamma.Red.Gamma = val;
                if (channel == "Green") _gamma.Green.Gamma = val;
                if (channel == "Blue") _gamma.Blue.Gamma = val;
            }
            _gamma.Update();
            DrawPreview();
        }

        private void ApplyValue(string label, double val)
        {
             void UpdateChannel(ChannelData d, double v, string l) {
                  switch(l) {
                      case "Gamma": d.Gamma = v; break;
                      case "Brightness": d.Brightness = v; break;
                       case "Contrast": d.Contrast = v; break;
                       case "White Ceiling": d.WhiteCeiling = v; break;
                       case "Mid-Gamma": d.MidGamma = v; break;
                       case "Black Level": d.BlackLevel = v; break;
                       case "Black Floor": d.BlackFloor = v; break;
                       case "Black Stabilizer": d.BlackStab = v; break;
                       case "White Stabilizer": d.WhiteStab = v; break;
                  }
             }

             if (label == "Hue") { _gamma.Hue = val; }
             else if (label == "Luminance") { _gamma.Luminance = val; }
             else if (label == "Smart Contrast") { _gamma.SmartContrast = val; } // Fixed label
             else if (label == "De-Haze") { _gamma.DeHaze = val; }
             else if (label == "Temperature") { _gamma.Temperature = val; }
             else if (label == "Tint") { _gamma.Tint = val; }
             else if (label == "Dither") { _gamma.Dithering = val; }
             else if (label == "Tone Sculpt") { _gamma.ToneSculpt = val; } // Fixed property name
             else if (_channelMode == ChannelMode.Linked) {
                 double oldVal = GetChannelValue(_gamma.Red, label);
                 double diff = val - oldVal;
                 UpdateChannel(_gamma.Green, GetChannelValue(_gamma.Green, label) + diff, label);
                 UpdateChannel(_gamma.Blue, GetChannelValue(_gamma.Blue, label) + diff, label);
                 UpdateChannel(_gamma.Red, val, label);
             } else {
                 var d = (_channelMode == ChannelMode.Red) ? _gamma.Red : (_channelMode == ChannelMode.Green) ? _gamma.Green : (_channelMode == ChannelMode.Blue) ? _gamma.Blue : _gamma.Red;
                 UpdateChannel(d, val, label);
             }
             _gamma.Update();
             UpdateUIValues();
             DrawPreview();
             SaveSettings();
        }

        private double GetChannelValue(ChannelData d, string label) {
             switch(label) {
                  case "Gamma": return d.Gamma;
                  case "Brightness": return d.Brightness;
                   case "Contrast": return d.Contrast;
                   case "White Ceiling": return d.WhiteCeiling;
                   case "Mid-Gamma": return d.MidGamma;
                   case "Black Level": return d.BlackLevel;
                   case "Black Floor": return d.BlackFloor;
                   case "Black Stabilizer": return d.BlackStab;
                   case "White Stabilizer": return d.WhiteStab;
              }
             return 0;
        }

        private ComboBox cmbMonitors;
        // private Label lblMonitorInfo; // This is already declared at the top.
        
        private void CreateGroup_Monitor(FlowLayoutPanel parent)
        {
            var g = CreateGroup("Monitor Selection", leftW);
            parent.Controls.Add(g);
            cmbMonitors = new ComboBox { Width = 280, BackColor = Color.FromArgb(50,50,50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            
            // Populate
            RefreshMonitors();
            
            g.Controls.Add(cmbMonitors);
            
            lblMonitorInfo = new Label { Text = "Detecting...", AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) };
            g.Controls.Add(lblMonitorInfo);
            
            // Trigger detection
            try {
                var info = GamerGammaApi.GetMonitors();
                if (info.Count > 0) {
                     // Try to find the primary or just show the first one
                     var prim = info.Find(x => x.IsPrimary) ?? info[0];
                     lblMonitorInfo.Text = "Monitor: " + prim.DeviceString;
                }
                else lblMonitorInfo.Text = "Monitor: Standard Display";
            } catch { lblMonitorInfo.Text = ""; }
        }

        private void RefreshMonitors() {
             cmbMonitors.Items.Clear();
             foreach (var screen in Screen.AllScreens) {
                 string name = screen.DeviceName.Replace(@"\\.\DISPLAY", "Display ");
                 if (screen.Primary) name += " (Primary)";
                 cmbMonitors.Items.Add(name);
             }
             if (cmbMonitors.Items.Count > 0) cmbMonitors.SelectedIndex = 0;
        }

        private void LoadMonitors()
        {
            cbMonitors.Items.Clear();
            var mons = GamerGammaApi.GetMonitors();
            int i=1; 
            foreach(var m in mons) {
                if (string.IsNullOrWhiteSpace(m.DeviceString) || m.DeviceString.Contains("Generic")) m.DeviceString = $"Monitor {i++}";
                cbMonitors.Items.Add(m);
            }
            if(cbMonitors.Items.Count > 0) cbMonitors.SelectedIndex = 0;
            cbMonitors.SelectedIndexChanged += (s,e) => {
                 if(cbMonitors.SelectedItem is MonitorInfo mi) { 
                     if (!string.IsNullOrEmpty(_gamma.TargetDisplay) && !_ignoreEvents) {
                         _appSettings.MonitorSettings[_gamma.TargetDisplay] = _gamma.GetCurrentSettings();
                     }
                     _gamma.TargetDisplay = mi.DeviceName; 
                     lblMonitorInfo.Text = $"{mi.Width}x{mi.Height}@{mi.Frequency}Hz";
                     if (_appSettings.MonitorSettings.ContainsKey(mi.DeviceName)) {
                         _gamma.ApplySettings(_appSettings.MonitorSettings[mi.DeviceName]);
                     } else {
                         _gamma.Reset();
                     }
                     UpdateUIValues();
                     DrawPreview();
                     SaveSettings();
                 }
            };
        }
        
        private void LoadProfiles()
        {
            var serializer = new JavaScriptSerializer();
            if (File.Exists(_settingsPath)) {
                try {
                    string json = File.ReadAllText(_settingsPath);
                    if (json.TrimStart().StartsWith("[")) {
                        var legacy = serializer.Deserialize<List<ColorProfile>>(json);
                        _appSettings = new AppSettings { Profiles = legacy };
                    } else {
                        _appSettings = serializer.Deserialize<AppSettings>(json);
                    }
                } catch { _appSettings = new AppSettings(); }
            } else {
                string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
                if (File.Exists(oldPath)) {
                    try {
                        var legacy = serializer.Deserialize<List<ColorProfile>>(File.ReadAllText(oldPath));
                        _appSettings = new AppSettings { Profiles = legacy };
                    } catch { _appSettings = new AppSettings(); }
                } else {
                    _appSettings = new AppSettings();
                }
            }

            cbProfiles.Items.Clear();
            foreach (var p in _appSettings.Profiles) {
                string hkText = "";
                if (p.Hotkey > 0) {
                    hkText = " (";
                    if ((p.HotkeyModifiers & 0x0002) != 0) hkText += "Ctrl+";
                    if ((p.HotkeyModifiers & 0x0001) != 0) hkText += "Alt+";
                    if ((p.HotkeyModifiers & 0x0004) != 0) hkText += "Shift+";
                    hkText += ((Keys)p.Hotkey).ToString() + ")";
                }
                cbProfiles.Items.Add(p.Name + hkText);
            }
            if (_appSettings.SelectedProfileIndex >= 0 && _appSettings.SelectedProfileIndex < _appSettings.Profiles.Count)
                cbProfiles.SelectedIndex = _appSettings.SelectedProfileIndex;
            else if (cbProfiles.Items.Count > 0) cbProfiles.SelectedIndex = 0;
     
             cbProfiles.SelectedIndexChanged += (s, e) => {
                 if (cbProfiles.SelectedIndex >= 0 && cbProfiles.SelectedIndex < _appSettings.Profiles.Count)
                 {
                     _appSettings.SelectedProfileIndex = cbProfiles.SelectedIndex;
                     _gamma.ApplySettings(_appSettings.Profiles[cbProfiles.SelectedIndex].Settings);
                     UpdateUIValues();
                     DrawPreview();
                     SaveSettings();
                 }
             };
             RegisterHotkeys();
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
             var name = Prompt.ShowDialog("Profile Name", "Save");
             if(!string.IsNullOrWhiteSpace(name)) {
                 _appSettings.Profiles.Add(new ColorProfile { Name=name, Settings=_gamma.GetCurrentSettings() });
                 SaveSettings();
                 cbProfiles.Items.Add(name);
                 cbProfiles.SelectedIndex = cbProfiles.Items.Count - 1; 
             }
        }
        
        private void SaveSettings() 
        {
            if (_ignoreEvents) return;
            _appSettings.CurrentSettings = _gamma.GetCurrentSettings();
            _appSettings.MinimizeToTray = chkMinimizeToTray.Checked;
            _appSettings.StartMinimized = chkStartMinimized.Checked;
            _appSettings.SelectedProfileIndex = cbProfiles.SelectedIndex;
            
            if (!string.IsNullOrEmpty(_gamma.TargetDisplay)) {
                _appSettings.MonitorSettings[_gamma.TargetDisplay] = _gamma.GetCurrentSettings();
            }
            
            if (cbMonitors.SelectedIndex >= 0) {
                var txt = cbMonitors.Items[cbMonitors.SelectedIndex].ToString();
                var start = txt.IndexOf("(");
                var end = txt.IndexOf(")");
                if (start >= 0 && end > start) {
                    _appSettings.SelectedMonitorDeviceName = txt.Substring(start + 1, end - start - 1);
                }
            }
            
            var serializer = new JavaScriptSerializer();
            File.WriteAllText(_settingsPath, serializer.Serialize(_appSettings));
        }

        private void ApplyWarmMode() {
            _gamma.Reset();
            _gamma.Red.Gamma = 0.85;
            _gamma.Green.Gamma = 0.81;
            _gamma.Blue.Gamma = 0.77; 
            _lastMasterGamma = 0.72;
            _gamma.Update();
            UpdateUIValues();
            DrawPreview();
            SaveSettings();
        }

        private void ApplyCoolMode() {
            _gamma.Reset();
            _gamma.Red.Gamma = 0.77;
            _gamma.Green.Gamma = 0.81;
            _gamma.Blue.Gamma = 0.84;
            _lastMasterGamma = 0.72;
            _gamma.Update();
            UpdateUIValues();
            DrawPreview();
            SaveSettings();
        }

        private void ToggleProView()
        {
            if (this.Width < 1500) {
                this.Width = 1620;
                if (_proPanel == null) {
                     InitProPanel();
                } else {
                     _proPanel.Visible = true;
                     // Restore column width
                     var root = Controls[0] as TableLayoutPanel;
                     if(root != null && root.ColumnStyles.Count > 1) {
                         root.ColumnStyles[root.ColumnStyles.Count-1].Width = 350;
                     }
                }
            } else {
                this.Width = 1250;
                this.Height = 660;
                if (_proPanel != null) {
                     _proPanel.Visible = false;
                     // Collapse column width to 0 to prevent "blank part" overlay
                     var root = Controls[0] as TableLayoutPanel;
                     if(root != null && root.ColumnStyles.Count > 1) {
                         root.ColumnStyles[root.ColumnStyles.Count-1].Width = 0; // Hide column
                     }
                }
            }
        }
        
        private Control _proPanel;

        private void InitProPanel()
        {
             var root = Controls[0] as TableLayoutPanel;
             if(root != null) {
                 if (_proPanel != null) return; 

                 // Expand column
                 root.ColumnCount++;
                 root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
                 
                  var pnlPro = new FlowLayoutPanel { Name="ProPanel", FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, BackColor = Color.FromArgb(35, 30, 30), Padding = new Padding(10), AutoScroll = false };
                  _proPanel = pnlPro;
                  
                  pnlPro.Controls.Add(new Label { Text = "ADVANCED", Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold), ForeColor = Color.Red, AutoSize = true, Margin = new Padding(0,0,0,10) });

                 // EXTENDED CONTROLS
                 var grpNice = CreateGroup("Extended Controls", 330);
                 
                 // Dither
                  grpNice.Controls.Add(CreateSlider("Dither", 0.0, 5.0, 0.0, out tbDither, out nudDither)); 
                  tbDither.Scroll += (s,e) => ApplyValue("Dither", (double)nudDither.Value);
                  nudDither.ValueChanged += (s,e) => ApplyValue("Dither", (double)nudDither.Value);
                  
                  // De-Haze (Moved from main columns)
                  grpNice.Controls.Add(CreateSlider("De-Haze", -10.0, 10.0, 0.0, out tbDeHaze, out nudDeHaze));
                  tbDeHaze.Scroll += (s,e) => ApplyValue("De-Haze", (double)nudDeHaze.Value);
                  nudDeHaze.ValueChanged += (s,e) => ApplyValue("De-Haze", (double)nudDeHaze.Value);
                 
                 // Smart Contrast (was HDR Toning)
                 // REMOVED DUPLICATE HERE
                 
                 // Tone Sculpt (was Bump)
                 // Fix: Ensure range -4.0 to 4.0 works with CreateSlider.
                 // CreateSlider likely assumes 0-1 or similar if not robust?
                 // Let's assume CreateSlider handles it, but verify step.
                 grpNice.Controls.Add(CreateSlider("Tone Sculpt", -4.0, 4.0, 0.0, out tbBump, out nudBump, 0.1));
                 tbBump.Scroll += (s,e) => ApplyValue("Bump", (double)nudBump.Value); 
                 nudBump.ValueChanged += (s,e) => ApplyValue("Bump", (double)nudBump.Value);
                 
                 // Smart Contrast (Single Instance)
                 grpNice.Controls.Add(CreateSlider("Smart Contrast", 0.0, 1.0, 0.0, out tbHdr, out nudHdr));
                 tbHdr.Scroll += (s,e) => ApplyValue("Smart Contrast", (double)nudHdr.Value);
                 nudHdr.ValueChanged += (s,e) => ApplyValue("Smart Contrast", (double)nudHdr.Value);
                 
                 pnlPro.Controls.Add(grpNice);

                 // SPLIT TONING
                 var grpSplit = CreateGroup("Split Toning", 330);
                 grpSplit.Height = 160; // Increased height for Apply/Reset buttons
                 
                 Button CreateColorBtn(string name, Func<Color> get, Action<Color> set) {
                     var b = new Button { Text = name, FlatStyle = FlatStyle.Flat, Width = 300, BackColor = get(), ForeColor = (get().R+get().G+get().B > 380 ? Color.Black : Color.White), Height = 30, Margin = new Padding(0,0,0,5) };
                     b.Click += (s,e) => {
                         using(var cd = new ColorDialog { Color = get(), FullOpen = true }) {
                             if(cd.ShowDialog() == DialogResult.OK) {
                                 set(cd.Color);
                                 b.BackColor = cd.Color;
                                 b.ForeColor = (cd.Color.R + cd.Color.G + cd.Color.B) > 380 ? Color.Black : Color.White;
                                 // Don't update live yet? User asked for Apply button.
                                 // But visual feedback on button is good.
                             }
                         }
                     };
                     return b;
                 }
                 
                 btnShadow = CreateColorBtn("Shadow Tint", () => _gamma.ShadowTint, c => _gamma.ShadowTint = c);
                 btnHigh = CreateColorBtn("Highlight Tint", () => _gamma.HighlightTint, c => _gamma.HighlightTint = c);
                 
                 grpSplit.Controls.Add(btnShadow);
                 grpSplit.Controls.Add(btnHigh);

                 var pnlSplitBtns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0,5,0,0) };
                 var btnSplitApply = new Button { Text = "Apply", Width = 145, Height=30, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkGreen, ForeColor = Color.White, Margin = new Padding(0,0,10,0) };
                 btnSplitApply.Click += (s,e) => { _gamma.Update(); DrawPreview(); SaveSettings(); };
                 
                 var btnSplitReset = new Button { Text = "Reset", Width = 145, Height=30, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkRed, ForeColor = Color.White };
                 btnSplitReset.Click += (s,e) => { 
                     _gamma.ShadowTint = Color.Black; 
                     _gamma.HighlightTint = Color.White; 
                     btnShadow.BackColor = Color.Black; btnShadow.ForeColor = Color.White;
                     btnHigh.BackColor = Color.White; btnHigh.ForeColor = Color.Black;
                     _gamma.Update(); 
                     SaveSettings(); 
                 };

                 pnlSplitBtns.Controls.Add(btnSplitApply);
                 pnlSplitBtns.Controls.Add(btnSplitReset);
                 grpSplit.Controls.Add(pnlSplitBtns);

                  pnlPro.Controls.Add(grpSplit);
                  
                  // CURVE EDITOR SECTION
                  var grpCurves = CreateGroup("Point Curves", 330);
                  grpCurves.Height = 100;
                  
                  btnCurves = new CheckBox { Text = "Open Curve Editor", Appearance = Appearance.Button, Width = 300, Height = 35, BackColor = Color.DarkRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) };
                  btnCurves.FlatAppearance.BorderSize = 1;
                  btnCurves.FlatAppearance.BorderColor = Color.White;
                  btnCurves.CheckedChanged += (s, e) => {
                       if (btnCurves.Checked) {
                           if (_curveEditor == null || _curveEditor.IsDisposed) {
                               _curveEditor = new PointCurveEditorForm(_gamma, () => DrawPreview());
                               _curveEditor.FormClosed += (fs, fe) => { btnCurves.Checked = false; _curveEditor = null; };
                               _curveEditor.Show();
                           } else {
                               _curveEditor.BringToFront();
                           }
                       } else {
                           if (_curveEditor != null && !_curveEditor.IsDisposed) {
                               _curveEditor.Close();
                           }
                       }
                  };
                  
                  var lblCurveInfo = new Label { Text = "Advanced manual curve control for professionals.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 8, FontStyle.Italic), AutoSize = true, Margin = new Padding(5, 5, 0, 0) };
                  
                  grpCurves.Controls.Add(btnCurves);
                  grpCurves.Controls.Add(lblCurveInfo);
                  pnlPro.Controls.Add(grpCurves);

                  root.Controls.Add(pnlPro, 4, 0); 
              }
         } 

        private void DrawPreview()
        {
            if (previewBox == null) return;
            int w = previewBox.Width;
            int h = previewBox.Height;
            if (w <= 0 || h <= 0) return;

            if (previewBox.Image == null || previewBox.Image.Width != w || previewBox.Image.Height != h)
                previewBox.Image = new Bitmap(w, h);
            
            using (var g = Graphics.FromImage(previewBox.Image)) {
                 g.Clear(Color.Black);
                 // Draw Grid (4x4 segments)
                 using (var p = new Pen(Color.FromArgb(40, 40, 40), 1)) {
                     // Vertical (X Axis)
                     for (int i = 0; i <= 4; i++) {
                         int pos = i * w / 4;
                         if (i == 4) pos = w - 1; // Ensure last line is at the edge
                         g.DrawLine(p, pos, 0, pos, h);
                         
                         string s = i.ToString();
                         g.DrawString(s, new Font("Arial", 8), Brushes.Gray, pos - (i==4?12:0), h - 15);
                     }
                     // Horizontal (Y Axis)
                     for(int i=0; i<=4; i++) {
                         int pos = i * h / 4;
                         if (i==4) pos = h - 1; // Ensure last line is at the edge
                         g.DrawLine(p, 0, pos, w, pos);
                         
                         string s = (4-i).ToString(); 
                         g.DrawString(s, new Font("Arial", 8), Brushes.Gray, 2, pos == h-1 ? pos-15 : pos);
                     }
                 }

                 g.DrawRectangle(Pens.Gray, 0, 0, w - 1, h - 1);
                 var (rRamp, gRamp, bRamp) = _gamma.GetRamp();
                 
                 using (var bmp = new Bitmap(w, h)) {
                     // Need to clear bmp? defaults to transparent (0,0,0,0) which is good.
                     
                     for (int i = 0; i < 256; i++) {
                         int x = (int)(i * (w - 1) / 255.0);
                         if (x < 0 || x >= w) continue;

                         int ry = h - 1 - (int)(rRamp[i] * (h - 1) / 65535.0);
                         int gy = h - 1 - (int)(gRamp[i] * (h - 1) / 65535.0);
                         int by = h - 1 - (int)(bRamp[i] * (h - 1) / 65535.0);

                         if (ry >= 0 && ry < h) bmp.SetPixel(x, ry, Color.Red);
                         if (gy >= 0 && gy < h) bmp.SetPixel(x, gy, Color.Lime);
                         if (by >= 0 && by < h) bmp.SetPixel(x, by, Color.Cyan);
                     }
                     g.DrawImage(bmp, 0, 0, w, h); // Stretch draw? No, 1:1 if sizes match.
                 }
            }
            previewBox.Invalidate();
        }

        private void BtnRemoveProfile_Click(object sender, EventArgs e) {
            if (cbProfiles.SelectedIndex >= 0 && cbProfiles.SelectedIndex < _appSettings.Profiles.Count) {
                var p = _appSettings.Profiles[cbProfiles.SelectedIndex];
                if (MessageBox.Show($"Delete {p.Name}?", "Del", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    _appSettings.Profiles.Remove(p); 
                    SaveSettings(); 
                    LoadProfiles();
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "GamerGamma_Settings.json" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try {
                    var serializer = new JavaScriptSerializer();
                    File.WriteAllText(sfd.FileName, serializer.Serialize(_appSettings));
                    MessageBox.Show("Settings Exported!");
                } catch (Exception ex) { MessageBox.Show("Export failed: " + ex.Message); }
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var serializer = new JavaScriptSerializer();
                    string json = File.ReadAllText(ofd.FileName);
                    if (json.TrimStart().StartsWith("[")) {
                        var legacy = serializer.Deserialize<List<ColorProfile>>(json);
                        _appSettings.Profiles.AddRange(legacy);
                    } else {
                        var imported = serializer.Deserialize<AppSettings>(json);
                        if (imported.Profiles != null) _appSettings.Profiles.AddRange(imported.Profiles);
                    }
                    SaveSettings();
                    LoadProfiles();
                    MessageBox.Show("Import successful!");
                }
                catch (Exception ex) { MessageBox.Show("Import failed: " + ex.Message); }
            }
        }




        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = caption, StartPosition = FormStartPosition.CenterScreen, BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Button confirmation = new Button() { Text = "Ok", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80,80,80) };
            prompt.Controls.Add(textBox); prompt.Controls.Add(confirmation); prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}