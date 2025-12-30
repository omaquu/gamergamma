using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace GamerGamma
{
    public class MainForm : Form
    {
        private GammaService _gamma;
        private ChannelMode _channelMode = ChannelMode.Linked;

        // Context Buttons
        private Button btnLink, btnRed, btnGreen, btnBlue;
        
        private TrackBar tbGamma, tbBright, tbContrast, tbSat, tbBlackStab, tbWhite, tbShadow, tbMid, tbHigh, tbDark, tbHue, tbDither;
        private NumericUpDown nudGamma, nudBright, nudContrast, nudSat, nudBlackStab, nudWhite, nudShadow, nudMid, nudHigh, nudDark, nudHue, nudDither;

        // Vertical RGB Gamma Sliders
        private TrackBar tbR, tbG, tbB;
        private NumericUpDown nudR, nudG, nudB;

        // Tray & UI
        private NotifyIcon trayIcon;
        private CheckBox chkMinimizeToTray;
        private ComboBox cbMonitors, cbProfiles;
        private Label lblMonitorInfo, lblChain;
        private Panel grpProf;
        private PictureBox previewBox;
        private List<ColorProfile> _profiles;
        private string _profilesPath;
        private bool _ignoreEvents = false;
        private int boxW = 280;
        private double _lastMasterGamma = 1.0;

        public MainForm()
        {
            this.Text = "Gamer Gamma v1.0 Beta";
            this.Size = new Size(1250, 680); // Shrink vertically
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = false;
            
            try { this.Icon = CreateLightbulbIcon(); } catch {}

            _gamma = new GammaService();
            _profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
            _ignoreEvents = true;

            InitTray();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300)); // Col 0: Left Stack
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310)); // Col 1: Channels/Levels (matched boxW)
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310)); // Col 2: Stabilizers/Extras
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Col 3: Preview
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Footer
            Controls.Add(root);

            // Column 0: Monitor -> Quick -> Profiles
            var pnlLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoScroll = false, WrapContents = false };
            root.Controls.Add(pnlLeft, 0, 0);

            // Monitor Info
            var grpMon = CreateGroup("Monitor Info", boxW);
            cbMonitors = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = boxW - 30, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            lblMonitorInfo = new Label { Text = "Resolution info...", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 5, 0, 0) };
            grpMon.Controls.Add(cbMonitors);
            grpMon.Controls.Add(lblMonitorInfo);
            pnlLeft.Controls.Add(grpMon);

            // Quick Settings
            var grpQuick = CreateGroup("Quick Settings", boxW);
            var pnlQuickGrid = new TableLayoutPanel { Width = boxW - 30, Height = 120, ColumnCount = 2, RowCount = 3 };
            pnlQuickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlQuickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Button CreateQBtn(string t, Action a, Color? bc = null) {
                var b = new Button { Text = t, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = bc ?? Color.FromArgb(50, 50, 50), ForeColor = Color.White, Margin = new Padding(2) };
                b.Click += (s, e) => { a(); UpdateUIValues(); DrawPreview(); };
                return b;
            }

            pnlQuickGrid.Controls.Add(CreateQBtn("1.8", () => SetGamma(1.8)), 0, 0);
            pnlQuickGrid.Controls.Add(CreateQBtn("2.2", () => SetGamma(2.2)), 1, 0);
            pnlQuickGrid.Controls.Add(CreateQBtn("2.4", () => SetGamma(2.4)), 0, 1);
            pnlQuickGrid.Controls.Add(CreateQBtn("DEFAULT", () => _gamma.Reset(), Color.DarkGreen), 1, 1);
            pnlQuickGrid.Controls.Add(CreateQBtn("WARM", ApplyWarmMode, Color.FromArgb(120, 70, 30)), 0, 2);
            pnlQuickGrid.Controls.Add(CreateQBtn("COOL", ApplyCoolMode, Color.FromArgb(70, 100, 150)), 1, 2);

            grpQuick.Controls.Add(pnlQuickGrid);
            pnlLeft.Controls.Add(grpQuick);

            // Profiles
            grpProf = CreateGroup("Profiles", boxW + 40); // Match width
            grpProf.Height = 310; 
            cbProfiles = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = boxW + 10, BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnSave = new Button { Text = "Save", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,50), Margin = new Padding(0,5,5,0) };
            var btnDel = new Button { Text = "Delete", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,50), Margin = new Padding(0,5,5,0) };
            var btnBind = new Button { Text = "Set Hotkey", Width = 100, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,50), Margin = new Padding(0,5,0,0) };
            chkMinimizeToTray = new CheckBox { Text = "Minimize to Tray", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            var btnExport = new Button { Text = "Export Settings", Width = 120, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,50,80), Margin = new Padding(0,5,5,0) };
            var btnImport = new Button { Text = "Import Settings", Width = 120, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50,80,50), Margin = new Padding(0,5,0,0) };
            
            btnSave.Click += BtnSaveProfile_Click;
            btnDel.Click += BtnRemoveProfile_Click;
            btnBind.Click += BtnHotkeyBind_Click;
            btnExport.Click += BtnExport_Click;
            btnImport.Click += BtnImport_Click;

            grpProf.Controls.Add(cbProfiles);
            var pnlProfBtns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0,5,0,0) };
            pnlProfBtns.Controls.AddRange(new[] { btnSave, btnDel, btnBind });
            grpProf.Controls.Add(pnlProfBtns);
            grpProf.Controls.Add(chkMinimizeToTray);
            var pnlIOBtns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0,5,0,0) };
            pnlIOBtns.Controls.AddRange(new[] { btnExport, btnImport });
            grpProf.Controls.Add(pnlIOBtns);

            pnlLeft.Controls.Add(grpProf);

            // Column 1: Master Channels & Levels
            var pnlMid = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlMid, 1, 0);

            var grpMaster = CreateGroup("Master Channels", boxW + 40);
            var pnlMasterSliders = new FlowLayoutPanel { Width = boxW + 20, Height = 210, FlowDirection = FlowDirection.LeftToRight };
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Master", 0.1, 4.0, 1.0, out tbGamma, out nudGamma, isMaster: true));
            lblChain = new Label { Text = "🔗", AutoSize = true, Font = new Font(Font.FontFamily, 12), ForeColor = Color.Gray, Margin = new Padding(0, 80, 0, 0) };
            pnlMasterSliders.Controls.Add(lblChain);
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Red", 0.1, 4.0, 1.0, out tbR, out nudR, "Red"));
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Green", 0.1, 4.0, 1.0, out tbG, out nudG, "Green"));
            pnlMasterSliders.Controls.Add(CreateVerticalSlider("Blue", 0.1, 4.0, 1.0, out tbB, out nudB, "Blue"));
            grpMaster.Controls.Add(pnlMasterSliders);

            // Channel Mode Buttons INSIDE Master Channels box
            var pnlModeBtns = new FlowLayoutPanel { Width = boxW + 30, Height = 35, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 10, 0, 0), WrapContents = false };
            btnLink = CreateCtxBtn("Linked", ChannelMode.Linked, Color.White);
            btnRed = CreateCtxBtn("Red", ChannelMode.Red, Color.Red);
            btnGreen = CreateCtxBtn("Green", ChannelMode.Green, Color.Lime);
            btnBlue = CreateCtxBtn("Blue", ChannelMode.Blue, Color.Cyan);
            // Sizing for mode buttons to fit
            foreach (Button b in new[] { btnLink, btnRed, btnGreen, btnBlue }) { b.Width = 62; b.Height = 25; b.Font = new Font(b.Font.FontFamily, 7, FontStyle.Bold); }
            pnlModeBtns.Controls.AddRange(new[] { btnLink, btnRed, btnGreen, btnBlue });
            grpMaster.Controls.Add(pnlModeBtns);
            pnlMid.Controls.Add(grpMaster);

            var grpLevels = CreateGroup("Levels", boxW + 40);
            grpLevels.Controls.Add(CreateSlider("Brightness", 0.0, 2.0, 1.0, out tbBright, out nudBright));
            grpLevels.Controls.Add(CreateSlider("Contrast", 0.0, 2.0, 0.5, out tbContrast, out nudContrast)); // Reset 0.5
            grpLevels.Controls.Add(CreateSlider("Saturation", 0.0, 3.0, 1.0, out tbSat, out nudSat));
            pnlMid.Controls.Add(grpLevels);

            // Column 2: Stabilizers & Extras
            var pnlRight = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, WrapContents = false };
            root.Controls.Add(pnlRight, 2, 0);

            var grpStab = CreateGroup("Stabilizers", boxW + 40);
            grpStab.Controls.Add(CreateSlider("Black Stabilizer", 0.0, 1.0, 0.0, out tbBlackStab, out nudBlackStab));
            grpStab.Controls.Add(CreateSlider("White Stabilizer", 0.0, 1.0, 0.0, out tbWhite, out nudWhite));
            grpStab.Controls.Add(CreateSlider("Shadow", 0.0, 2.0, 0.0, out tbShadow, out nudShadow));
            grpStab.Controls.Add(CreateSlider("Mid-Tone", 0.1, 4.0, 0.5, out tbMid, out nudMid)); // Reset 0.5
            grpStab.Controls.Add(CreateSlider("Highlight", 0.0, 2.0, 0.0, out tbHigh, out nudHigh));
            pnlRight.Controls.Add(grpStab);

            var grpExtras = CreateGroup("Extras", boxW + 40);
            grpExtras.Controls.Add(CreateSlider("Darkness", 0.0, 1.0, 0.0, out tbDark, out nudDark));
            grpExtras.Controls.Add(CreateSlider("Hue", -1.0, 1.0, 0.0, out tbHue, out nudHue));
            grpExtras.Controls.Add(CreateSlider("Dither", 0.0, 1.0, 0.0, out tbDither, out nudDither));
            pnlRight.Controls.Add(grpExtras);

            // Column 3: Preview
            var pnlPreviewRoot = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var pnlPreviewInner = new Panel { Width = 300, Height = 480, BackColor = Color.Black, Margin = new Padding(0) }; // Height align
            previewBox = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.Normal };
            pnlPreviewInner.Controls.Add(previewBox);
            pnlPreviewRoot.Controls.Add(pnlPreviewInner);
            root.Controls.Add(pnlPreviewRoot, 3, 0);

            // Footer
            var lblFooter = new Label { Text = "© omaxtr 2025 / twitch.tv/omaxtr / Gamer Gamma v1.0 Beta", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray, Font = new Font("Consolas", 8) };
            root.Controls.Add(lblFooter, 0, 1);
            root.SetColumnSpan(lblFooter, 4);

            this.FormClosing += (s, e) => { trayIcon.Visible = false; };
            this.Resize += (s, e) => { if (this.WindowState == FormWindowState.Minimized && chkMinimizeToTray.Checked) { this.Hide(); trayIcon.Visible = true; } };

            LoadMonitors();
            LoadProfiles();
            UpdateUIValues();
            _ignoreEvents = false;
            DrawPreview();
        }

        private void InitTray()
        {
            trayIcon = new NotifyIcon { Text = "Gamer Gamma", Visible = false };
            try { trayIcon.Icon = CreateLightbulbIcon(); } catch {}
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, e) => { trayIcon.Visible = false; Application.Exit(); });
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
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
            
            // Reset Button ↺
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
                cTb.Value = Math.Clamp(ival, 0, 1000);
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
            var p = new Panel { Size = new Size(boxW - 25, 45), Margin = new Padding(0, 0, 0, 5) };
            var l = new Label { Text = label, Location = new Point(0, 0), AutoSize = true, Font = new Font(Font.FontFamily, 7), ForeColor = Color.Gray };
            
            // Reset Button ↺
            var btnReset = new Button { Text = "↺", Size = new Size(22, 22), Location = new Point(boxW - 55, 18), FlatStyle = FlatStyle.Flat, ForeColor = Color.Red, BackColor = Color.FromArgb(40, 40, 40), Font = new Font(Font.FontFamily, 8) };
            btnReset.FlatAppearance.BorderSize = 0;

            nud = new NumericUpDown { Minimum = (decimal)min, Maximum = (decimal)max, Value = (decimal)def, DecimalPlaces = 2, Width = 45, Location = new Point(boxW - 65, 0), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, Font = new Font(Font.FontFamily, 7) };
            tb = new TrackBar { Minimum = 0, Maximum = 1000, Value = (int)((def - min) / (max - min) * 1000), TickStyle = TickStyle.Both, TickFrequency = 250, Width = boxW - 80, Location = new Point(0, 15), Height = 25 };

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
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Width = width, BackColor = Color.FromArgb(45, 45, 48), Margin = new Padding(5), Padding = new Padding(10) };
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

            nudGamma.Value = (decimal)_gamma.Red.Gamma; // Master ref Red
            _lastMasterGamma = (double)nudGamma.Value;
            
            nudBright.Value = (decimal)d.Brightness;
            nudContrast.Value = (decimal)d.Contrast;
            nudSat.Value = (decimal)_gamma.Saturation;
            nudBlackStab.Value = (decimal)d.BlackStab;
            nudWhite.Value = (decimal)d.WhiteStab;
            nudShadow.Value = (decimal)d.ShadowStab;
            nudMid.Value = (decimal)d.MidTone;
            nudHigh.Value = (decimal)d.HighlightStab;
            nudDark.Value = (decimal)d.BlackLevel;
            nudHue.Value = (decimal)_gamma.Hue;
            nudDither.Value = (decimal)_gamma.Dithering;
            

            if (nudR != null) {
                nudR.Value = (decimal)_gamma.Red.Gamma;
                nudG.Value = (decimal)_gamma.Green.Gamma;
                nudB.Value = (decimal)_gamma.Blue.Gamma;
                bool link = _channelMode == ChannelMode.Linked;
                
                // Grey out RGB sliders when linked
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

            // Highlight Linked button properly
            if (btnLink != null) {
                btnLink.BackColor = (_channelMode == ChannelMode.Linked) ? Color.FromArgb(80, 80, 80) : Color.FromArgb(50, 50, 50);
            }

            // Hide/Show 🔗 icon
            if (lblChain != null) lblChain.Visible = (_channelMode == ChannelMode.Linked);

            _ignoreEvents = false;
        }


        private Button CreateCtxBtn(string t, ChannelMode m, Color c) {
            var b = new Button { Text=t, Width=80, Height=30, FlatStyle=FlatStyle.Flat, BackColor=Color.FromArgb(50,50,50), ForeColor=c };
            b.Click += (s,e) => SetChannelMode(m);
            return b;
        }

        private void BtnHotkeyBind_Click(object sender, EventArgs e)
        {
            if (cbProfiles.SelectedIndex < 0) return;
            var prof = _profiles[cbProfiles.SelectedIndex];

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
                    SaveProfiles();
                    LoadProfiles();
                    RegisterHotkeys();
                }
            }
        }

        private void RegisterHotkeys()
        {
            for (int i = 0; i < _profiles.Count; i++) {
                GamerGammaApi.UnregisterHotKey(this.Handle, i);
                if (_profiles[i].Hotkey > 0) {
                    GamerGammaApi.RegisterHotKey(this.Handle, i, _profiles[i].HotkeyModifiers, _profiles[i].Hotkey);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) { // WM_HOTKEY
                int id = m.WParam.ToInt32();
                if (id >= 0 && id < _profiles.Count) {
                    _gamma.ApplySettings(_profiles[id].Settings);
                    UpdateUIValues();
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
                       case "White Stabilizer": d.WhiteStab = v; break;
                       case "Mid-Tone": d.MidTone = v; break;
                       case "Darkness": d.BlackLevel = v; break;
                       case "Black Stabilizer": d.BlackStab = v; break;
                       case "Shadow": d.ShadowStab = v; break;
                       case "Highlight": d.HighlightStab = v; break;
                  }
             }

             if (label == "Saturation") { _gamma.Saturation = val; }
             else if (label == "Hue") { _gamma.Hue = val; }
             else if (label == "Dither") { _gamma.Dithering = val; }
             else if (_channelMode == ChannelMode.Linked) {
                 double oldVal = _gamma.Red.Gamma; // Red as reference
                 switch(label) {
                      case "Gamma": oldVal = _gamma.Red.Gamma; break;
                       case "Brightness": oldVal = _gamma.Red.Brightness; break;
                       case "Contrast": oldVal = _gamma.Red.Contrast; break;
                       case "Saturation": oldVal = _gamma.Saturation; break;
                       case "White Stabilizer": oldVal = _gamma.Red.WhiteStab; break;
                       case "Mid-Tone": oldVal = _gamma.Red.MidTone; break;
                       case "Black Stabilizer": oldVal = _gamma.Red.BlackStab; break;
                       case "Shadow": oldVal = _gamma.Red.ShadowStab; break;
                       case "Highlight": oldVal = _gamma.Red.HighlightStab; break;
                       case "Darkness": oldVal = _gamma.Red.BlackLevel; break;
                  }
                 double diff = val - oldVal;
                 UpdateChannel(_gamma.Green, GetChannelValue(_gamma.Green, label) + diff, label);
                 UpdateChannel(_gamma.Blue, GetChannelValue(_gamma.Blue, label) + diff, label);
                 UpdateChannel(_gamma.Red, val, label);
             } else {
                 var d = (_channelMode == ChannelMode.Red) ? _gamma.Red : (_channelMode == ChannelMode.Green) ? _gamma.Green : (_channelMode == ChannelMode.Blue) ? _gamma.Blue : _gamma.Red;
                 UpdateChannel(d, val, label);
             }
             _gamma.Update();
             DrawPreview();
        }

        private double GetChannelValue(ChannelData d, string label) {
             switch(label) {
                  case "Gamma": return d.Gamma;
                  case "Brightness": return d.Brightness;
                   case "Contrast": return d.Contrast;
                   case "White Stabilizer": return d.WhiteStab;
                   case "Mid-Tone": return d.MidTone;
                   case "Darkness": return d.BlackLevel;
                   case "Black Stabilizer": return d.BlackStab;
                   case "Shadow": return d.ShadowStab;
                   case "Highlight": return d.HighlightStab;
              }
             return 0;
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
                 if(cbMonitors.SelectedItem is MonitorInfo mi) { _gamma.TargetDisplay=mi.DeviceName; lblMonitorInfo.Text=$"{mi.Width}x{mi.Height}@{mi.Frequency}Hz"; }
            };
        }
        
        private void LoadProfiles()
        {
            if (File.Exists(_profilesPath)) _profiles = JsonSerializer.Deserialize<List<ColorProfile>>(File.ReadAllText(_profilesPath));
            else _profiles = new List<ColorProfile>();

            cbProfiles.Items.Clear();
            foreach (var p in _profiles) {
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
            if (cbProfiles.Items.Count > 0) cbProfiles.SelectedIndex = 0;
     
             cbProfiles.SelectedIndexChanged += (s, e) => {
                 if (cbProfiles.SelectedIndex >= 0 && cbProfiles.SelectedIndex < _profiles.Count)
                 {
                     _gamma.ApplySettings(_profiles[cbProfiles.SelectedIndex].Settings);
                     UpdateUIValues();
                     DrawPreview();
                 }
             };
             RegisterHotkeys();
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
             var name = Prompt.ShowDialog("Profile Name", "Save");
             if(!string.IsNullOrWhiteSpace(name)) {
                 _profiles.Add(new ColorProfile { Name=name, Settings=_gamma.GetCurrentSettings() });
                 SaveProfiles();
                 cbProfiles.Items.Add(name);
                 cbProfiles.SelectedIndex = cbProfiles.Items.Count - 1; 
             }
        }
        
        private void SaveProfiles() => File.WriteAllText(_profilesPath, System.Text.Json.JsonSerializer.Serialize(_profiles));

        private void ApplyWarmMode() {
            // WARM: reset defaults -> R=1.0, G=0.9, B=0.8
            _gamma.Reset();
            _gamma.Red.Gamma = 1.0;
            _gamma.Green.Gamma = 0.9;
            _gamma.Blue.Gamma = 0.8; 
            _gamma.Update();
            UpdateUIValues();
        }

        private void ApplyCoolMode() {
            // COOL: reset defaults -> Light blue tint
            _gamma.Reset();
            _gamma.Red.Gamma = 0.8;
            _gamma.Green.Gamma = 0.9;
            _gamma.Blue.Gamma = 1.2;
            _gamma.Update();
            UpdateUIValues();
        }

        private void BtnRemoveProfile_Click(object sender, EventArgs e) {
            if (cbProfiles.SelectedIndex >= 0) {
                var p = _profiles[cbProfiles.SelectedIndex];
                if (MessageBox.Show($"Delete {p.Name}?", "Del", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    _profiles.Remove(p); SaveProfiles(); LoadProfiles();
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON Files|*.json", Title = "Export Settings" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(_gamma.GetCurrentSettings()));
                MessageBox.Show("Settings Exported!");
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON Files|*.json", Title = "Import Settings" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try {
                    var settings = JsonSerializer.Deserialize<ExtendedColorSettings>(File.ReadAllText(ofd.FileName));
                    _gamma.ApplySettings(settings);
                    UpdateUIValues();
                    DrawPreview();
                    MessageBox.Show("Settings Imported!");
                } catch { MessageBox.Show("Invalid settings file."); }
            }
        }

        private void DrawPreview()
        {
            int w = previewBox.Width;
            int h = previewBox.Height;
            if (w <= 0 || h <= 0) return;

            if (previewBox.Image == null || previewBox.Image.Width != w || previewBox.Image.Height != h)
                previewBox.Image = new Bitmap(w, h);
            
            using (var g = Graphics.FromImage(previewBox.Image)) {
                 g.Clear(Color.Black);
                 g.DrawRectangle(Pens.Gray, 0, 0, w - 1, h - 1);
                 var (rRamp, gRamp, bRamp) = _gamma.GetRamp();
                 
                 using (var bmp = new Bitmap(w, h)) {
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
                     g.DrawImage(bmp, 0, 0, w, h); // Stretch draw
                 }
            }
            previewBox.Invalidate();
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
