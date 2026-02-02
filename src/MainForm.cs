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
        private TrackBar tbGamma, tbBright, tbContrast, tbLum, tbBlackFloor, tbWhiteCeil, tbBlackStab, tbWhiteStab, tbMidGamma, tbHdr, tbDeHaze, tbTemp, tbTint, tbBlackLevel, tbHue, tbDither, tbToneSculpt, tbWhiteLevel, tbSolar, tbInversion, tbClipping, tbMonoStrength;
        private NumericUpDown nudGamma, nudBright, nudContrast, nudLum, nudBlackFloor, nudWhiteCeil, nudBlackStab, nudWhiteStab, nudMidGamma, nudHdr, nudDeHaze, nudTemp, nudTint, nudBlackLevel, nudHue, nudDither, nudToneSculpt, nudWhiteLevel, nudSolar, nudInversion, nudClipping, nudMonoStrength;
        private ComboBox cbMono, cbLuts;
        private TrackBar tbLutStr;
        private NumericUpDown nudLutStr;

        // Vertical RGB Gamma Sliders
        private TrackBar tbR, tbG, tbB;
        private NumericUpDown nudR, nudG, nudB;
        
        // Split Toning
        private Button btnShadow, btnHigh;

        // Tray & UI
        private NotifyIcon trayIcon;
        private CheckBox chkMinimizeToTray;
        private ComboBox cbMonitors, cbProfiles;
        private Label lblMonitorInfo;
        private Panel grpProf;
        private PictureBox previewBox;
        private AppSettings _appSettings;
        private string _settingsPath;
        private Label lblChain;
        private CheckBox chkStartWithWin;
        private CheckBox chkStartMinimized;
        private CheckBox chkTooltips;
        private ToolTip _tip = new ToolTip { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 10000 };
        private bool _ignoreEvents = false;
        private int boxW = 310;
        private int leftW = 190;
        private double _lastMasterGamma = 1.0;
        private double _redOffset = 0.0, _greenOffset = 0.0, _blueOffset = 0.0;

        private CheckBox btnCurves;
        private PointCurveEditorForm _curveEditor;

        public MainForm() { try {
            this.Text = "Gamer Gamma v1.4";
            this.ClientSize = new Size(1240, 680);
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
            var pnlQuickGrid = new TableLayoutPanel { Width = leftW - 20, Height = 150, ColumnCount = 2, RowCount = 4 };
            pnlQuickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlQuickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Button CreateQBtn(string t, Action a, Color? bc = null) {
                var b = new Button { Text = t, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = bc ?? Color.FromArgb(50, 50, 50), ForeColor = Color.White, Margin = new Padding(2) };
                b.Click += (s, e) => { a(); UpdateUIValues(); DrawPreview(); };
                return b;
            }

            // Buttons renaming
            var fBtn = new Font(FontFamily.GenericSansSerif, 7);
            
            // Row 0: omaquu | FPS Gamer
            var btnOmaquu = CreateCtxBtn("omaquu", ChannelMode.Linked, Color.FromArgb(255, 128, 0)); // Orange like coffee button
            btnOmaquu.Click += (s,e) => {
                _gamma.Reset();
                _gamma.TransferMode = TransferMode.BT2020;
                _gamma.Red.BlackStab = _gamma.Green.BlackStab = _gamma.Blue.BlackStab = 0.404;
                _gamma.Red.WhiteStab = _gamma.Green.WhiteStab = _gamma.Blue.WhiteStab = 1.032;
                _gamma.Red.MidGamma = _gamma.Green.MidGamma = _gamma.Blue.MidGamma = 0.07;
                _gamma.DeHaze = 0.1;
                _gamma.ToneSculpt = 0.7;
                UpdateUIValues();
                _gamma.Update();
                DrawPreview();
            };
            btnOmaquu.Font = fBtn;
            btnOmaquu.Width = (leftW - 20) / 2 - 4;
            btnOmaquu.BackColor = Color.FromArgb(255, 128, 0); // Orange
            btnOmaquu.ForeColor = Color.White;
            btnOmaquu.FlatAppearance.BorderSize = 1;
            btnOmaquu.FlatAppearance.BorderColor = Color.White;
            pnlQuickGrid.Controls.Add(btnOmaquu, 0, 0);

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

            // Row 1: De-Haze | BT.709
            pnlQuickGrid.Controls.Add(CreateQBtn("De-Haze", () => { 
                _gamma.Reset(); 
                _gamma.Red.Gamma = _gamma.Green.Gamma = _gamma.Blue.Gamma = 1.10;
                _gamma.Red.BlackStab = _gamma.Green.BlackStab = _gamma.Blue.BlackStab = 0.36;
                _gamma.Red.MidGamma = _gamma.Green.MidGamma = _gamma.Blue.MidGamma = 0.14;
                _gamma.DeHaze = 0.60; 
                _gamma.Temperature = 0.0;
                _gamma.Update(); 
            }, Color.FromArgb(70, 60, 100)), 0, 1);
            
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
            pnlQuickGrid.Controls.Add(btnExt709, 1, 1);

            // Row 2: WARM | COOL (reduced height)
            var btnWarm = CreateQBtn("WARM", ApplyWarmMode, Color.FromArgb(120, 70, 30));
            btnWarm.Height = 25;
            pnlQuickGrid.Controls.Add(btnWarm, 0, 2);
            
            var btnCool = CreateQBtn("COOL", ApplyCoolMode, Color.FromArgb(70, 100, 150));
            btnCool.Height = 25;
            pnlQuickGrid.Controls.Add(btnCool, 1, 2);

            // Row 3: DEFAULT (full width)
            var btnDefault = CreateQBtn("DEFAULT", () => _gamma.Reset(), Color.DarkGreen);
            btnDefault.Height = 25;
            pnlQuickGrid.Controls.Add(btnDefault, 0, 3);
            pnlQuickGrid.SetColumnSpan(btnDefault, 2); // Span 2 columns

            pnlQuickGrid.Height = 130; // Compact height
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
            chkTooltips = new CheckBox { Text = "Tooltips", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            chkTooltips.CheckedChanged += (s, e) => { _tip.Active = chkTooltips.Checked; SaveSettings(); };

            var btnExport = new Button { Text = "Export", Width = 75, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,80), Margin = new Padding(0,5,5,0) };
            var btnImport = new Button { Text = "Import", Width = 75, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,80,50), Margin = new Padding(0,5,0,0) };
            
            grpProf.Controls.Add(cbProfiles);
            var pnlProfBtns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0,5,0,0) };
            pnlProfBtns.Controls.AddRange(new[] { btnSave, btnDel, btnBind });
            grpProf.Controls.Add(pnlProfBtns);
            grpProf.Controls.Add(chkTooltips);
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
            var lblFooter = new Label { Text = "© omaquu 2026 // twitch.tv/omaquu\nGamerGamma v1.4", Width = leftW, Height = 40, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.Gray, Font = new Font("Consolas", 7) };
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
            btnCoffee.Click += (s, e) => { System.Diagnostics.Process.Start("https://ko-fi.com/omaquu/tip"); };
            
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

            // Channel Mode Buttons (Relocated above reset buttons)
            btnLink = CreateCtxBtn("M", ChannelMode.Linked, Color.White);
            btnRed = CreateCtxBtn("R", ChannelMode.Red, Color.Red);
            btnGreen = CreateCtxBtn("G", ChannelMode.Green, Color.Lime);
            btnBlue = CreateCtxBtn("B", ChannelMode.Blue, Color.Cyan);
            foreach (Button b in new[] { btnLink, btnRed, btnGreen, btnBlue }) { b.Width = 20; b.Height = 20; b.Font = new Font(b.Font.FontFamily, 6, FontStyle.Bold); }

            var grpPrimaries = CreateGroup("Primaries", boxW);
            grpPrimaries.Height = 280;
            grpPrimaries.AutoSize = false;
            var pnlMasterSliders = new FlowLayoutPanel { Width = boxW - 20, Height = 225, WrapContents = false, AutoSize = true };
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Master", 0.1, 4.0, 1.0, out tbGamma, out nudGamma, isMaster: true, modeBtn: btnLink, helpText: "Controls the 'weight' of mid-tones; improves image depth."));
            
            lblChain = new Label { Text = _channelMode == ChannelMode.Linked ? "🔒" : "🔓", AutoSize = true, Font = new Font(Font.FontFamily, 12), ForeColor = Color.Yellow, Margin = new Padding(2, 60, 2, 0), Cursor = Cursors.Hand };
            lblChain.Click += (s, e) => {
                SetChannelMode(_channelMode == ChannelMode.Linked ? ChannelMode.Red : ChannelMode.Linked);
            };
            pnlMasterSliders.Controls.Add(lblChain);

            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Red", 0.1, 4.0, 1.0, out tbR, out nudR, "Red", modeBtn: btnRed, helpText: "Adjusts the weight of the red channel."));
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Green", 0.1, 4.0, 1.0, out tbG, out nudG, "Green", modeBtn: btnGreen, helpText: "Adjusts the weight of the green channel."));
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Blue", 0.1, 4.0, 1.0, out tbB, out nudB, "Blue", modeBtn: btnBlue, helpText: "Adjusts the weight of the blue channel."));
            grpPrimaries.Controls.Add(pnlMasterSliders);
            pnlMid.Controls.Add(grpPrimaries);

            var grpColorimetry = CreateGroup("Colorimetry", boxW);
            grpColorimetry.Height = 450;
            grpColorimetry.AutoSize = false;
            grpColorimetry.Controls.Add(CreateSlider("Temperature", -1.0, 1.0, 0.0, out tbTemp, out nudTemp, helpText: "Makes the screen look warmer (orange) or cooler (blue).")); 
            grpColorimetry.Controls.Add(CreateSlider("Tint", -1.0, 1.0, 0.0, out tbTint, out nudTint, helpText: "Adjusts the balance between magenta and green levels."));
            grpColorimetry.Controls.Add(CreateSlider("Hue", 0.0, 359.0, 0.0, out tbHue, out nudHue, helpText: "Shifts all colors around the color wheel."));
            
            // Mono Mode (Moved from Advanced)
            var pnlMono = new FlowLayoutPanel { Width = boxW - 20, Height = 75, FlowDirection = FlowDirection.LeftToRight };
            pnlMono.Controls.Add(new Label { Text = "Mono Mode", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 5, 10, 0) });
            cbMono = new ComboBox { Width = 100, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            cbMono.Items.AddRange(Enum.GetNames(typeof(MonoMode)));
            cbMono.SelectedIndexChanged += (s,e) => {
                _gamma.MonoMode = (MonoMode)Enum.Parse(typeof(MonoMode), cbMono.SelectedItem.ToString());
                _gamma.Update(); DrawPreview(); SaveSettings();
            };
            pnlMono.Controls.Add(cbMono);
            pnlMono.Controls.Add(CreateSlider("Strength", 0.0, 1.0, 0.0, out tbMonoStrength, out nudMonoStrength, helpText: "Controls the intensity of the single-color effect."));
            grpColorimetry.Controls.Add(pnlMono);

            // 1D LUTs
            var pnlLut = new FlowLayoutPanel { Width = boxW - 20, Height = 100, FlowDirection = FlowDirection.LeftToRight };
            cbLuts = new ComboBox { Width = 150, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            cbLuts.Items.Add("None");
            cbLuts.SelectedIndex = 0;
            var btnAddLut = new Button { Text = "+", Width = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkGreen, ForeColor = Color.White };
            btnAddLut.Click += (s,e) => ImportLut();
            _tip.SetToolTip(btnAddLut, "Add your own LUTs (supports .png, .cube, .txt).");
            var btnRemoveLut = new Button { Text = "-", Width = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkRed, ForeColor = Color.White };
            btnRemoveLut.Click += (s,e) => RemoveSelectedLut();
            _tip.SetToolTip(btnRemoveLut, "Remove the selected LUT from the application.");

            pnlLut.Controls.Add(new Label { Text = "1D LUT", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 5, 5, 0) });
            pnlLut.Controls.Add(cbLuts);
            pnlLut.Controls.Add(btnAddLut);
            pnlLut.Controls.Add(btnRemoveLut);
            
            pnlLut.Controls.Add(CreateSlider("Intensity", 0.0, 0.6, 0.01, out tbLutStr, out nudLutStr, step: 0.01, helpText: "Blends the selected 1D LUT effect with the original image."));
            cbLuts.SelectedIndexChanged += (s,e) => {
                if (_ignoreEvents) return;
                _gamma.SelectedLut = cbLuts.SelectedItem?.ToString() == "None" ? "" : cbLuts.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(_gamma.SelectedLut)) { _ignoreEvents = true; nudLutStr.Value = 0.01M; _ignoreEvents = false; }
                _gamma.Update(); DrawPreview(); SaveSettings();
            };
            grpColorimetry.Controls.Add(pnlLut);

            pnlMid.Controls.Add(grpColorimetry);

            // Column 2
            var pnlRight = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlRight, 2, 0);

            var grpDynRange = CreateGroup("Dynamic Range", boxW);
            grpDynRange.Height = 305; // Shaved more as requested
            grpDynRange.AutoSize = false;
            grpDynRange.Controls.Add(CreateSlider("Luminance", -1.0, 1.0, 0.0, out tbLum, out nudLum, helpText: "Controls the overall light output of the screen."));
            grpDynRange.Controls.Add(CreateSlider("Brightness", 0.0, 2.0, 1.0, out tbBright, out nudBright, helpText: "Adjusts the shadow detail level."));
            grpDynRange.Controls.Add(CreateSlider("Contrast", 0.0, 2.0, 1.0, out tbContrast, out nudContrast, helpText: "Strengthens the difference between dark and light parts.")); 
            grpDynRange.Controls.Add(CreateSlider("White Level", 0.0, 2.0, 1.0, out tbWhiteLevel, out nudWhiteLevel, helpText: "Sets the intensity of the brightest whites."));
            // Force Black Level directly below White Level
            grpDynRange.Controls.Add(CreateSlider("Black Level", -1.0, 1.0, 0.0, out tbBlackLevel, out nudBlackLevel, helpText: "Sets the intensity of the deepest blacks."));
            pnlRight.Controls.Add(grpDynRange);

            var grpToneMapping = CreateGroup("Tone Mapping", boxW);
            grpToneMapping.Height = 420;
            grpToneMapping.AutoSize = false;
            grpToneMapping.Controls.Add(CreateSlider("Smart Contrast", -1.0, 1.0, 0.0, out tbHdr, out nudHdr, helpText: "Combines stabilizer and de-haze for a quick image boost."));
            grpToneMapping.Controls.Add(CreateSlider("Mid-Gamma", -1.0, 1.0, 0.0, out tbMidGamma, out nudMidGamma, helpText: "Fine-tunes the brightness of the middle range."));
            grpToneMapping.Controls.Add(CreateSlider("De-Haze", -4.0, 4.0, 0.0, out tbDeHaze, out nudDeHaze, helpText: "Removes the 'cloudy' look from the image."));
            grpToneMapping.Controls.Add(CreateSlider("Black Stabilizer", -2.0, 2.0, 0.0, out tbBlackStab, out nudBlackStab, helpText: "Makes dark areas more visible without washing out.")); 
            grpToneMapping.Controls.Add(CreateSlider("White Stabilizer", -2.0, 2.0, 0.0, out tbWhiteStab, out nudWhiteStab, helpText: "Protects highlights from becoming too bright.")); 
            grpToneMapping.Controls.Add(CreateSlider("Tone Sculpt", -4.0, 4.0, 0.0, out tbToneSculpt, out nudToneSculpt, helpText: "Sculpts the image tones for more or less punch."));
            pnlRight.Controls.Add(grpToneMapping);

            // Column 3: Preview
            var pnlCol3 = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlCol3, 3, 0);

            var grpPreview = new Panel { Width = 350, Height = 440, BackColor = Color.FromArgb(45, 45, 48), Margin = new Padding(5), Padding = new Padding(10) };
            var lblPreviewTitle = new Label { Text = "PREVIEW", Dock = DockStyle.Top, Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold), ForeColor = Color.Gray, Height = 25 };
            
            previewBox = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.StretchImage, Padding = new Padding(0) };
            var previewContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            previewContainer.Controls.Add(previewBox);
            
            grpPreview.Controls.Add(previewContainer);
            grpPreview.Controls.Add(lblPreviewTitle);
            pnlCol3.Controls.Add(grpPreview);

            // COLOR PREVIEW (Relocated from Advanced)
            var grpColPrev = new Panel { Width = 350, Height = 190, BackColor = Color.FromArgb(40,40,40), Margin = new Padding(5, 5, 5, 0) };
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
            chkTooltips.Checked = _appSettings.ShowTooltips;
            _tip.Active = _appSettings.ShowTooltips;

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
          } catch (Exception ex) { MessageBox.Show("Startup Error: " + ex.Message + "\n" + ex.StackTrace); Environment.Exit(1); } }

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

        private Panel CreateVerticalSlider(string label, double min, double max, double def, out TrackBar tb, out NumericUpDown nud, string channelName = null, bool isMaster = false, Button modeBtn = null, string helpText = null)
        {
            var p = new Panel { Size = new Size(58, 215), Margin = new Padding(2) };
            
            void TrySelect() {
                if (label == "Master") SetChannelMode(ChannelMode.Linked);
                else {
                    if (channelName == "Red") SetChannelMode(ChannelMode.Red);
                    else if (channelName == "Green") SetChannelMode(ChannelMode.Green);
                    else if (channelName == "Blue") SetChannelMode(ChannelMode.Blue);
                }
            }
            p.Click += (s,e) => TrySelect(); 
            p.MouseDown += (s,e) => TrySelect();

            if (channelName == "Red") p.BackColor = Color.FromArgb(45, 30, 30);
            else if (channelName == "Green") p.BackColor = Color.FromArgb(30, 45, 30);
            else if (channelName == "Blue") p.BackColor = Color.FromArgb(30, 30, 45);
            else if (isMaster) p.BackColor = Color.FromArgb(40, 40, 40);

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0), Padding = new Padding(0) };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // NUD
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // TrackBar
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // Reset
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // ModeBtn
            p.Controls.Add(tlp);
            tlp.MouseDown += (s,e) => TrySelect();

            nud = new NumericUpDown { Minimum = (decimal)min, Maximum = (decimal)max, Value = (decimal)def, DecimalPlaces = 2, Increment = 0.01M, Dock = DockStyle.Top, Width = 50, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, Font = new Font(Font.FontFamily, 7), Margin = new Padding(4, 2, 4, 2) };
            if (!string.IsNullOrEmpty(helpText)) _tip.SetToolTip(nud, helpText);
            tlp.Controls.Add(nud, 0, 0);

            tb = new TrackBar { Orientation = Orientation.Vertical, Minimum = 0, Maximum = 1000, Value = (int)((def - min) / (max - min) * 1000), TickStyle = TickStyle.Both, TickFrequency = 100, Dock = DockStyle.Fill, Margin = new Padding(0) };
            if (!string.IsNullOrEmpty(helpText)) _tip.SetToolTip(tb, helpText);
            tlp.Controls.Add(tb, 0, 1);

            var btnResetBtn = new Button { Text = "↺", Size = new Size(35, 18), FlatStyle = FlatStyle.Flat, ForeColor = Color.Red, BackColor = Color.FromArgb(40, 40, 40), Font = new Font(Font.FontFamily, 6), Margin = new Padding(12, 2, 0, 2), Dock = DockStyle.None };
            btnResetBtn.FlatAppearance.BorderSize = 0;
            tlp.Controls.Add(btnResetBtn, 0, 2);

            if (modeBtn != null) {
                modeBtn.Dock = DockStyle.None;
                modeBtn.Size = new Size(35, 18);
                modeBtn.Margin = new Padding(12, 1, 0, 0);
                tlp.Controls.Add(modeBtn, 0, 3);
            }

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
                        _gamma.Red.Gamma = Clamp(newVal + _redOffset, min, max);
                        _gamma.Green.Gamma = Clamp(newVal + _greenOffset, min, max);
                        _gamma.Blue.Gamma = Clamp(newVal + _blueOffset, min, max);
                        _gamma.Update();
                        _lastMasterGamma = newVal;
                        UpdateUIValues();
                        DrawPreview();
                    }
                    else if (channelName != null) {
                        double v = (double)cNud.Value;
                        if (channelName == "Red") { _gamma.Red.Gamma = v; _redOffset = v - _lastMasterGamma; }
                        else if (channelName == "Green") { _gamma.Green.Gamma = v; _greenOffset = v - _lastMasterGamma; }
                        else if (channelName == "Blue") { _gamma.Blue.Gamma = v; _blueOffset = v - _lastMasterGamma; }
                        _gamma.Update();
                        UpdateUIValues();
                        DrawPreview();
                    }
                }
            };
            btnResetBtn.Click += (s, e) => { if (isMaster) { _redOffset=_greenOffset=_blueOffset=0; } cNud.Value = (decimal)def; };

            p.Paint += (s, e) => {
                if (!cNud.Enabled) {
                     using (var brush = new SolidBrush(Color.FromArgb(160, 20, 20, 20))) {
                         e.Graphics.FillRectangle(brush, p.ClientRectangle);
                     }
                }
            };

            return p;
        }

        private Panel CreateSlider(string label, double min, double max, double def, out TrackBar tb, out NumericUpDown nud, double step = 0.01, int width = -1, string helpText = null)
        {
            int w = (width > 0) ? width : boxW - 20;
            var p = new Panel { Size = new Size(w, 48), Margin = new Padding(0, 0, 0, 5) };
            var l = new Label { Text = label, Location = new Point(0, 0), AutoSize = true, Font = new Font(Font.FontFamily, 7), ForeColor = Color.Gray };
            var btnReset = new Button { Text = "↺", Size = new Size(22, 22), Location = new Point(w - 23, 20), FlatStyle = FlatStyle.Flat, ForeColor = Color.Red, BackColor = Color.FromArgb(40, 40, 40), Font = new Font(Font.FontFamily, 8) };
            btnReset.FlatAppearance.BorderSize = 0;
            nud = new NumericUpDown { Minimum = (decimal)min, Maximum = (decimal)max, Value = (decimal)def, DecimalPlaces = (step < 0.1 ? 2 : 1), Increment = (decimal)step, Width = 55, Location = new Point(w - 55, 0), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, Font = new Font(Font.FontFamily, 7) };
            tb = new TrackBar { Minimum = 0, Maximum = 1000, Value = (int)((def - min) / (max - min) * 1000), TickStyle = TickStyle.Both, TickFrequency = 100, Width = w - 60, Location = new Point(0, 18), Height = 25 };
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
            if (!string.IsNullOrEmpty(helpText)) {
                _tip.SetToolTip(l, helpText);
                _tip.SetToolTip(tb, helpText);
                _tip.SetToolTip(nud, helpText);
            }
            p.Controls.Add(l); p.Controls.Add(tb); p.Controls.Add(nud); p.Controls.Add(btnReset);
            return p;
        }

        private Panel CreateGroup(string title, int width = 200)
        {
            var p = new FlowLayoutPanel { 
                FlowDirection = FlowDirection.TopDown, 
                WrapContents = false,
                AutoSize = true, 
                AutoSizeMode = AutoSizeMode.GrowAndShrink, 
                Width = width, 
                MinimumSize = new Size(width, 100), 
                MaximumSize = new Size(width, 2000), 
                BackColor = Color.FromArgb(45, 45, 48), 
                Margin = new Padding(5), 
                Padding = new Padding(10) 
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

            if (_channelMode == ChannelMode.Linked) {
                _lastMasterGamma = _gamma.Red.Gamma;
            }
            if (nudGamma != null) SafeSetNud(nudGamma, _lastMasterGamma);

            // Re-sync offsets to maintain "sticky" behavior accurately
            _redOffset = _gamma.Red.Gamma - _lastMasterGamma;
            _greenOffset = _gamma.Green.Gamma - _lastMasterGamma;
            _blueOffset = _gamma.Blue.Gamma - _lastMasterGamma;

            var d = (_channelMode == ChannelMode.Red) ? _gamma.Red :
                       (_channelMode == ChannelMode.Green) ? _gamma.Green :
                       (_channelMode == ChannelMode.Blue) ? _gamma.Blue :
                       _gamma.Red;

            SafeSetNud(nudBright, d.Brightness);
            SafeSetNud(nudContrast, d.Contrast);
            SafeSetNud(nudLum, d.Luminance);
            
            SafeSetNud(nudBlackStab, d.BlackStab);
            SafeSetNud(nudWhiteStab, d.WhiteStab);
            SafeSetNud(nudMidGamma, d.MidGamma);
            SafeSetNud(nudHdr, d.SmartContrast);
            
            SafeSetNud(nudBlackFloor, d.BlackFloor);
            SafeSetNud(nudWhiteCeil, d.WhiteCeiling);
            
            SafeSetNud(nudDeHaze, d.DeHaze);
            SafeSetNud(nudTemp, _gamma.Temperature);
            SafeSetNud(nudTint, _gamma.Tint);
            
            SafeSetNud(nudBlackLevel, d.BlackLevel);
            SafeSetNud(nudWhiteLevel, d.WhiteLevel);
            SafeSetNud(nudHue, _gamma.Hue);
            SafeSetNud(nudDither, d.Dithering);
            SafeSetNud(nudToneSculpt, d.ToneSculpt);
            SafeSetNud(nudSolar, d.Solarization);
            SafeSetNud(nudInversion, d.Inversion);
            SafeSetNud(nudClipping, d.Clipping);
            SafeSetNud(nudMonoStrength, d.MonoStrength);
            
            if (cbMono != null) cbMono.SelectedItem = _gamma.MonoMode.ToString();
            RefreshLutList();
            if (cbLuts != null) cbLuts.SelectedItem = string.IsNullOrEmpty(_gamma.SelectedLut) ? "None" : _gamma.SelectedLut;
            SafeSetNud(nudLutStr, _gamma.LutStrength);
            if (lblChain != null) lblChain.Text = _channelMode == ChannelMode.Linked ? "🔒" : "🔓";
            
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
                
                bool linked = (_channelMode == ChannelMode.Linked);
                bool isR = (_channelMode == ChannelMode.Red);
                bool isG = (_channelMode == ChannelMode.Green);
                bool isB = (_channelMode == ChannelMode.Blue);

                // Disable TrackBar/NUD but keep Parent Panel enabled for click-to-select logic
                if (nudGamma != null) nudGamma.Enabled = tbGamma.Enabled = linked;
                if (nudR != null) nudR.Enabled = tbR.Enabled = isR;
                if (nudG != null) nudG.Enabled = tbG.Enabled = isG;
                if (nudB != null) nudB.Enabled = tbB.Enabled = isB;

                // Force repaint for grey overlay
                if (tbGamma != null) tbGamma.Parent.Invalidate();
                if (tbR != null) tbR.Parent.Invalidate();
                if (tbG != null) tbG.Parent.Invalidate();
                if (tbB != null) tbB.Parent.Invalidate();

                if (lblChain != null) lblChain.ForeColor = linked ? Color.Cyan : Color.Gray;
            }

            void Sty(Button b, bool a) => b.BackColor = a ? Color.FromArgb(80,80,80) : Color.FromArgb(50,50,50);
            if(btnLink != null) Sty(btnLink, _channelMode == ChannelMode.Linked);
            if(btnRed != null) Sty(btnRed, _channelMode == ChannelMode.Red);
            if(btnGreen != null) Sty(btnGreen, _channelMode == ChannelMode.Green);
            if(btnBlue != null) Sty(btnBlue, _channelMode == ChannelMode.Blue);

            if (lblChain != null) {
                lblChain.Text = (_channelMode == ChannelMode.Linked) ? "🔒" : "🔓";
                lblChain.ForeColor = (_channelMode == ChannelMode.Linked) ? Color.Yellow : Color.Gray;
            }

            // Master Slider Lock
            if (tbGamma != null) {
                bool isLinked = _channelMode == ChannelMode.Linked;
                tbGamma.BackColor = isLinked ? Color.FromArgb(28, 28, 28) : Color.FromArgb(40, 40, 40);
            }

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
                       case "Luminance": d.Luminance = v; break;
                       case "De-Haze": d.DeHaze = v; break;
                       case "Tone Sculpt": d.ToneSculpt = v; break;
                       case "Smart Contrast": d.SmartContrast = v; break;
                       case "Dither": d.Dithering = v; break;
                       case "White Level": d.WhiteLevel = v; break;
                       case "Solarization": d.Solarization = v; break;
                       case "Inversion": d.Inversion = v; break;
                       case "Clipping": d.Clipping = v; break;
                       case "Strength": d.MonoStrength = v; break; 
                       case "Intensity": _gamma.LutStrength = v; break;
                   }
             }

             if (label == "Hue") { _gamma.Hue = val; }
             else if (label == "Temperature") { _gamma.Temperature = val; }
             else if (label == "Tint") { _gamma.Tint = val; }
             else if (label == "Intensity") { _gamma.LutStrength = val; } // Reverted to 1:1, Max 0.6
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

        private void RefreshLutList()
        {
            if (cbLuts == null) return;
            string selected = cbLuts.SelectedItem?.ToString();
            cbLuts.Items.Clear();
            cbLuts.Items.Add("None");

            string lutDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LUTS");
            if (!Directory.Exists(lutDir)) return;

            foreach (var file in Directory.GetFiles(lutDir))
            {
                cbLuts.Items.Add(Path.GetFileName(file));
            }

            if (!string.IsNullOrEmpty(selected) && cbLuts.Items.Contains(selected))
                cbLuts.SelectedItem = selected;
            else if (!string.IsNullOrEmpty(_gamma.SelectedLut) && cbLuts.Items.Contains(_gamma.SelectedLut))
                cbLuts.SelectedItem = _gamma.SelectedLut;
            else if (cbLuts.Items.Count > 0)
                cbLuts.SelectedIndex = 0;
        }

        private void RemoveSelectedLut()
        {
            if (cbLuts.SelectedItem == null || cbLuts.SelectedItem.ToString() == "None") return;
            string name = cbLuts.SelectedItem.ToString();
            if (MessageBox.Show($"Delete LUT file: {name}?", "Del", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                try {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Luts", name);
                    if (File.Exists(path)) File.Delete(path);
                    RefreshLutList();
                    cbLuts.SelectedIndex = 0;
                } catch (Exception ex) { MessageBox.Show("Failed to delete: " + ex.Message); }
            }
        }

        private void ImportLut()
        {
            using (var ofd = new OpenFileDialog { Filter = "LUT files|*.cube;*.txt;*.lut|All files|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string lutDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LUTS");
                        if (!Directory.Exists(lutDir)) Directory.CreateDirectory(lutDir);
                        string dest = Path.Combine(lutDir, Path.GetFileName(ofd.FileName));
                        File.Copy(ofd.FileName, dest, true);
                        RefreshLutList();
                        cbLuts.SelectedItem = Path.GetFileName(ofd.FileName);
                    }
                    catch (Exception ex) { MessageBox.Show("Failed to import LUT: " + ex.Message); }
                }
            }
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
                   case "Luminance": return d.Luminance;
                   case "De-Haze": return d.DeHaze;
                   case "Tone Sculpt": return d.ToneSculpt;
                   case "Smart Contrast": return d.SmartContrast;
                   case "Dither": return d.Dithering;
                   case "White Level": return d.WhiteLevel;
                   case "Solarization": return d.Solarization;
                   case "Inversion": return d.Inversion;
                   case "Clipping": return d.Clipping;
                    case "Strength": return d.MonoStrength;
                    case "Intensity": return _gamma.LutStrength;
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
            // Removed premature selection to allow constructor logic to trigger the event.
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
            _appSettings.ShowTooltips = chkTooltips.Checked;
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
            if (this.ClientSize.Width < 1400) {
                this.ClientSize = new Size(1570, 680);
                if (_proPanel == null) {
                     InitProPanel();
                } else {
                     _proPanel.Visible = true;
                     var root = Controls[0] as TableLayoutPanel;
                     if(root != null && root.ColumnStyles.Count > 1) {
                          root.ColumnStyles[root.ColumnStyles.Count-1].Width = 330;
                     }
                }
            } else {
                this.ClientSize = new Size(1240, 680);
                if (_proPanel != null) {
                     _proPanel.Visible = false;
                     var root = Controls[0] as TableLayoutPanel;
                     if(root != null && root.ColumnStyles.Count > 1) {
                          root.ColumnStyles[root.ColumnStyles.Count-1].Width = 0;
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
                  root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
                 
                  var pnlPro = new FlowLayoutPanel { Name="ProPanel", FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, BackColor = Color.FromArgb(35, 30, 30), Padding = new Padding(10), AutoScroll = false, WrapContents = false };
                  _proPanel = pnlPro;
                  
                  pnlPro.Controls.Add(new Label { Text = "ADVANCED", Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold), ForeColor = Color.Red, AutoSize = true, Margin = new Padding(0,0,0,10) });

                  // SIGNAL ENGINEERING
                  var grpSignal = CreateGroup("Signal Engineering", 310);
                  grpSignal.Height = 355; // Increased to fit all sliders comfortably
                  grpSignal.AutoSize = false;
                  grpSignal.Controls.Add(CreateSlider("Black Floor", -1.0, 1.0, 0.0, out tbBlackFloor, out nudBlackFloor, width: 290, helpText: "Sets the minimum brightness level (crushes blacks)."));
                  grpSignal.Controls.Add(CreateSlider("White Ceiling", -1.0, 1.0, 0.0, out tbWhiteCeil, out nudWhiteCeil, width: 290, helpText: "Sets the maximum brightness level (clips whites)."));
                  grpSignal.Controls.Add(CreateSlider("Dither", 0.0, 5.0, 0.0, out tbDither, out nudDither, width: 290, helpText: "Adds subtle noise to prevent brightness banding.")); 
                  
                  // Per-Channel Effects
                  grpSignal.Controls.Add(CreateSlider("Solarization", 0.0, 1.0, 0.0, out tbSolar, out nudSolar, width: 290, helpText: "Creates a surreal effect by inverting certain color levels."));
                  grpSignal.Controls.Add(CreateSlider("Inversion", 0.0, 1.0, 0.0, out tbInversion, out nudInversion, width: 290, helpText: "Flips the colors of the screen (Negative effect)."));
                  grpSignal.Controls.Add(CreateSlider("Clipping", -0.5, 0.5, 0.0, out tbClipping, out nudClipping, width: 290, helpText: "Cuts off intensity values at the extreme ends."));
                  
                  pnlPro.Controls.Add(grpSignal);

                  // SPLIT TONING
                  var grpSplit = CreateGroup("Split Toning", 310);
                  grpSplit.Height = 115; // Shrunk as requested
                  grpSplit.AutoSize = false;
                  
                  Button CreateColorBtn(string name, Func<Color> get, Action<Color> set) {
                      var b = new Button { Text = name, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, BackColor = get(), ForeColor = (get().R+get().G+get().B > 380 ? Color.Black : Color.White), Height = 30, Margin = new Padding(2) };
                      b.Click += (s,e) => {
                          using(var cd = new ColorDialog { Color = get(), FullOpen = true }) {
                              if(cd.ShowDialog() == DialogResult.OK) {
                                  set(cd.Color);
                                  b.BackColor = cd.Color;
                                  b.ForeColor = (cd.Color.R + cd.Color.G + cd.Color.B) > 380 ? Color.Black : Color.White;
                              }
                          }
                      };
                      return b;
                  }
                  
                  var tlpSplit = new TableLayoutPanel { Width = 290, Height = 75, ColumnCount = 2, RowCount = 2, Margin = new Padding(0, 5, 0, 0) };
                  tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                  tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                  tlpSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                  tlpSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

                  btnShadow = CreateColorBtn("Shadow Tint", () => _gamma.ShadowTint, c => _gamma.ShadowTint = c);
                  _tip.SetToolTip(btnShadow, "Select a color to tint the shadows (dark areas).");
                  btnHigh = CreateColorBtn("Highlight Tint", () => _gamma.HighlightTint, c => _gamma.HighlightTint = c);
                  _tip.SetToolTip(btnHigh, "Select a color to tint the highlights (bright areas).");
                  
                  var btnSplitApply = new Button { Text = "Apply", Dock = DockStyle.Fill, Height=30, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkGreen, ForeColor = Color.White, Margin = new Padding(2) };
                  btnSplitApply.Click += (s,e) => { _gamma.Update(); DrawPreview(); SaveSettings(); };
                  var btnSplitReset = new Button { Text = "Reset", Dock = DockStyle.Fill, Height=30, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkRed, ForeColor = Color.White, Margin = new Padding(2) };
                  btnSplitReset.Click += (s,e) => { 
                      _gamma.ShadowTint = Color.Black; _gamma.HighlightTint = Color.White; 
                      btnShadow.BackColor = Color.Black; btnShadow.ForeColor = Color.White;
                      btnHigh.BackColor = Color.White; btnHigh.ForeColor = Color.Black;
                      _gamma.Update(); DrawPreview(); SaveSettings(); 
                  };

                  tlpSplit.Controls.Add(btnShadow, 0, 0);       tlpSplit.Controls.Add(btnSplitApply, 1, 0);
                  tlpSplit.Controls.Add(btnHigh, 0, 1);         tlpSplit.Controls.Add(btnSplitReset, 1, 1);
                  
                  grpSplit.Controls.Add(tlpSplit);
                  pnlPro.Controls.Add(grpSplit);

                  // CURVES
                  var grpCurves = CreateGroup("Point Curves", 310);
                  grpCurves.Height = 120; // 2px more shorter
                  grpCurves.AutoSize = false;
                  btnCurves = new CheckBox { Text = "Open Curve Editor", Appearance = Appearance.Button, Width = 290, Height = 35, BackColor = Color.DarkRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) };
                  btnCurves.FlatAppearance.BorderSize = 1;
                  btnCurves.FlatAppearance.BorderColor = Color.White;
                  btnCurves.CheckedChanged += (s, e) => {
                       if (btnCurves.Checked) {
                           if (_curveEditor == null || _curveEditor.IsDisposed) {
                               _curveEditor = new PointCurveEditorForm(_gamma, () => DrawPreview());
                               _curveEditor.FormClosed += (fs, fe) => { btnCurves.Checked = false; _curveEditor = null; };
                               _curveEditor.Show();
                           } else { _curveEditor.BringToFront(); }
                       } else {
                           if (_curveEditor != null && !_curveEditor.IsDisposed) _curveEditor.Close();
                       }
                  };
                  var lblCurveInfo = new Label { Text = "Advanced manual curve control for professionals.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 7, FontStyle.Italic), AutoSize = false, Width=290, Height=40, Margin = new Padding(0, 10, 0, 0) };
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
                          if (i == 4) pos = w - 1; 
                          g.DrawLine(p, pos, 0, pos, h);
                          
                          int val = i * 255 / 4;
                          g.DrawString(val.ToString(), new Font("Arial", 8), Brushes.Gray, pos - (i==4?25:0), h - 15);
                      }
                      // Horizontal (Y Axis)
                      for(int i=0; i<=4; i++) {
                          int pos = i * h / 4;
                          if (i==4) pos = h - 1; 
                          g.DrawLine(p, 0, pos, w, pos);
                          
                          int val = (4-i) * 255 / 4;
                          g.DrawString(val.ToString(), new Font("Arial", 8), Brushes.Gray, 2, pos == h-1 ? pos-15 : pos);
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

