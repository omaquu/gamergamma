using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GamerGamma
{
    public class PointCurveEditorForm : Form
    {
        private GammaService _gamma;
        private Action _onUpdate;
        private PictureBox _canvas;
        private List<Point> _currentPoints;
        private int _draggingIdx = -1;
        private TextBox _txtValues;
        private TextBox _txtGlobal;
        
        // Mode
        private enum Channel { Linked, Red, Green, Blue }
        private Channel _channel = Channel.Linked;
        private bool _splineMode => _gamma.Smooth; 
        private bool _showRef = false;
        private bool _showLive = true;

        private Button btnLink, btnRed, btnGreen, btnBlue, btnResetAll;
        private Label lblCoord;
        private CheckBox chkSmooth;

        public PointCurveEditorForm(GammaService gamma, Action onUpdate)
        {
            _gamma = gamma;
            _onUpdate = onUpdate;
            
            // Initial load
            LoadPoints();

            this.Text = "Points Curve Editor";
            this.Size = new Size(650, 950); // Taller for Global Config
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
            Controls.Add(layout);

            // Channel Selector
            var pnlChan = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            btnLink = CreateModeBtn("Linked", Channel.Linked, Color.FromArgb(60, 60, 60));
            btnRed = CreateModeBtn("Red", Channel.Red, Color.FromArgb(60, 60, 60));
            btnGreen = CreateModeBtn("Green", Channel.Green, Color.FromArgb(60, 60, 60));
            btnBlue = CreateModeBtn("Blue", Channel.Blue, Color.FromArgb(60, 60, 60));
            pnlChan.Controls.AddRange(new Control[] { btnLink, btnRed, btnGreen, btnBlue });
            
            chkSmooth = new CheckBox { Text = "Smooth (Experimental)", AutoSize = true, ForeColor = Color.White, Margin = new Padding(10,5,0,0), Checked = _gamma.Smooth };
            chkSmooth.CheckedChanged += (s,e) => { 
                _gamma.Smooth = chkSmooth.Checked; 
                _gamma.Update();
                _canvas.Invalidate(); 
                _onUpdate?.Invoke();
            };
            pnlChan.Controls.Add(chkSmooth);

            var chkShowRef = new CheckBox { Text = "Show Reference", AutoSize = true, ForeColor = Color.LightGray, Margin = new Padding(10,5,0,0), Checked = false };
            chkShowRef.CheckedChanged += (s,e) => { _showRef = chkShowRef.Checked; _canvas.Invalidate(); };
            pnlChan.Controls.Add(chkShowRef);

            var chkShowLive = new CheckBox { Text = "Show Output", AutoSize = true, ForeColor = Color.Cyan, Margin = new Padding(10,5,0,0), Checked = true };
            chkShowLive.CheckedChanged += (s,e) => { _showLive = chkShowLive.Checked; _canvas.Invalidate(); };
            pnlChan.Controls.Add(chkShowLive);
            
            layout.Controls.Add(pnlChan);

            // Canvas
            var pnlCanvas = new Panel { Width = 600, Height = 600, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(20, 20, 20) };
            _canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20), Cursor = Cursors.Cross };
            pnlCanvas.Controls.Add(_canvas);
            
            _canvas.Paint += Canvas_Paint;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            layout.Controls.Add(pnlCanvas);

            // Coords & Reset
            var pnlInfo = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Width = 600 };
            lblCoord = new Label { Text = "X: - Y: -", AutoSize = true, ForeColor = Color.Yellow, Width = 150, Font = new Font("Consolas", 10) };
            
            var btnReset = new Button { Text = "Reset Channel", BackColor = Color.FromArgb(60, 60, 60), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Width = 120 };
            btnReset.Click += (s, e) => {
                _currentPoints.Clear();
                _currentPoints.Add(new Point(0, 1));
                _currentPoints.Add(new Point(255, 255));
                SavePoints();
                UpdateTxt();
                _gamma.Update();
                _canvas.Invalidate();
                _onUpdate?.Invoke();
            };

            btnResetAll = new Button { Text = "Reset All", BackColor = Color.FromArgb(100, 30, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Width = 100 };
            btnResetAll.Click += (s, e) => {
                _gamma.Reset(); 
                _gamma.PointCurveMaster.Clear();
                _gamma.Smooth = false; // Ensure linear on reset
                chkSmooth.Checked = false;
                _gamma.PointCurveMaster.Add(new Point(0, 1)); 
                _gamma.PointCurveMaster.Add(new Point(255, 255));
                
                LoadPoints();
                UpdateTxt();
                _canvas.Invalidate();
                _onUpdate?.Invoke();
            };
            
            pnlInfo.Controls.Add(lblCoord);
            pnlInfo.Controls.Add(btnReset);
            pnlInfo.Controls.Add(btnResetAll);
            layout.Controls.Add(pnlInfo);

            // Value Box
             layout.Controls.Add(new Label { Text = "Values (Copy/Paste: x,y; ...)", AutoSize = true, ForeColor = Color.Gray });
            _txtValues = new TextBox { Width = 600, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            _txtValues.TextChanged += TxtValues_TextChanged;
            layout.Controls.Add(_txtValues);

            // Global Config Code
            layout.Controls.Add(new Label { Text = "Global Config Code (Copy/Paste All Settings)", AutoSize = true, ForeColor = Color.Cyan, Margin = new Padding(0, 10, 0, 0) });
            _txtGlobal = new TextBox { Width = 600, Height = 50, Multiline = true, BackColor = Color.FromArgb(40, 40, 50), ForeColor = Color.LightGreen, ScrollBars = ScrollBars.Vertical };
            // Load initial
            _txtGlobal.Text = _gamma.GetGlobalConfigString();
            
            var btnApplyGlobal = new Button { Text = "Apply Config Code", Width = 295, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 70, 50), ForeColor = Color.White };
            btnApplyGlobal.Click += (s, e) => {
                _gamma.ApplyGlobalConfigString(_txtGlobal.Text);
                LoadPoints();
                UpdateTxt();
                _gamma.Update();
                _onUpdate?.Invoke();
                MessageBox.Show("Configuration Loaded!");
            };
            
            var btnCopyGlobal = new Button { Text = "Copy Code", Width = 295, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 70), ForeColor = Color.White, Margin = new Padding(10,0,0,0) };
            btnCopyGlobal.Click += (s, e) => {
                if(!string.IsNullOrEmpty(_txtGlobal.Text)) {
                    Clipboard.SetText(_txtGlobal.Text);
                    MessageBox.Show("Code Copied to Clipboard!");
                }
            };
            
            var flowBtns = new FlowLayoutPanel { AutoSize=true, Margin = new Padding(0,5,0,0) };
            flowBtns.Controls.Add(btnApplyGlobal);
            flowBtns.Controls.Add(btnCopyGlobal);

            layout.Controls.Add(_txtGlobal);
            layout.Controls.Add(flowBtns);
            
            // Hook into external updates
             _gamma.OnSettingsChanged += () => {
                if (!this.IsDisposed && this.Visible) {
                    this.BeginInvoke(new Action(() => {
                         _canvas.Invalidate();
                         if (_txtGlobal != null && !_txtGlobal.Focused) {
                             _txtGlobal.Text = _gamma.GetGlobalConfigString();
                         }
                    }));
                }
            };

            UpdateTxt();
            HighlightMode();
            chkSmooth.Checked = _gamma.Smooth;

            // Tips
            layout.Controls.Add(new Label { 
                Text = "TIP: Left Click to Create point and Right Click to delete Point", 
                AutoSize = true, 
                ForeColor = Color.DimGray, 
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Margin = new Padding(0, 10, 0, 0)
            });
        }

        private Button CreateModeBtn(string tx, Channel m, Color c) {
            var b = new Button { Text = tx, FlatStyle = FlatStyle.Flat, ForeColor = c == Color.White ? Color.Black : Color.White, BackColor = c, Width = 100 };
            b.FlatAppearance.BorderColor = c == Color.FromArgb(60,60,60) ? Color.Gray : c; // Default gray for internal logic, but we want colors
            if (m == Channel.Red) b.FlatAppearance.BorderColor = Color.Red;
            if (m == Channel.Green) b.FlatAppearance.BorderColor = Color.Lime;
            if (m == Channel.Blue) b.FlatAppearance.BorderColor = Color.Cyan;
            if (m == Channel.Linked) b.FlatAppearance.BorderColor = Color.White;
            b.FlatAppearance.BorderSize = 1;
            b.Click += (s,e) => { _channel = m; LoadPoints(); HighlightMode(); UpdateTxt(); _canvas.Invalidate(); };
            return b;
        }

        private void HighlightMode() {
            void SetStyle(Button b, bool active) {
                b.BackColor = active ? Color.FromArgb(100, 100, 100) : Color.FromArgb(60, 60, 60);
                b.FlatAppearance.BorderSize = 0;
            }
            SetStyle(btnLink, _channel == Channel.Linked);
            SetStyle(btnRed, _channel == Channel.Red);
            SetStyle(btnGreen, _channel == Channel.Green);
            SetStyle(btnBlue, _channel == Channel.Blue);
        }

        public void LoadPoints() {
            List<Point> src = null;
            if (_channel == Channel.Linked) src = _gamma.PointCurveMaster;
            else if (_channel == Channel.Red) src = _gamma.PointCurveR;
            else if (_channel == Channel.Green) src = _gamma.PointCurveG;
            else if (_channel == Channel.Blue) src = _gamma.PointCurveB;
            
            if (src == null || src.Count < 2) {
                src = new List<Point> { new Point(0,0), new Point(255,255) };
                if (_channel == Channel.Linked) _gamma.PointCurveMaster = src;
                else if (_channel == Channel.Red) _gamma.PointCurveR = src;
                else if (_channel == Channel.Green) _gamma.PointCurveG = src;
                else if (_channel == Channel.Blue) _gamma.PointCurveB = src;
            }
            _currentPoints = src; 
        }

        private void SavePoints() {
            // Sort points to ensure X order is monotonic
            _currentPoints.Sort((a,b) => a.X.CompareTo(b.X));
            
            if (_channel == Channel.Linked) _gamma.PointCurveMaster = _currentPoints;
            // No longer overwriting R/G/B when Linked.
        }

        private void UpdateTxt()
        {
            if (_currentPoints == null) return;
            // Already sorted in SavePoints usually, but let's be safe
            _txtValues.TextChanged -= TxtValues_TextChanged; 
            _txtValues.Text = string.Join("; ", _currentPoints.Select(p => $"{p.X},{p.Y}"));
            _txtValues.TextChanged += TxtValues_TextChanged;
        }

        private void TxtValues_TextChanged(object sender, EventArgs e)
        {
             try {
                 var parts = _txtValues.Text.Split(';');
                 var newPts = new List<Point>();
                 foreach(var part in parts) {
                     var coords = part.Trim().Split(',');
                     if(coords.Length == 2) {
                         if(int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y)) {
                             newPts.Add(new Point(Math.Max(0, Math.Min(255, x)), Math.Max(0, Math.Min(255, y))));
                         }
                     }
                 }
                 if(newPts.Count >= 2) {
                     _currentPoints.Clear();
                     _currentPoints.AddRange(newPts);
                     SavePoints();
                     _canvas.Invalidate();
                     _onUpdate?.Invoke();
                 }
             } catch {}
        }

        private void ResetPoints() {
            // Force clean linear curve
            _currentPoints = new List<Point> { new Point(0, 0), new Point(255, 255) };
            SavePoints();
            UpdateTxt();
            _canvas.Invalidate();
            _onUpdate?.Invoke();
            _gamma.Update(); // Force update
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = _canvas.Width;
            int h = _canvas.Height;
            int stripH = 30; 
            int gridH = h - stripH;

            // Dark Background
            g.Clear(Color.FromArgb(20, 20, 20));

            // Grid
            using (var p = new Pen(Color.FromArgb(50, 50, 50))) {
                for (int i = 0; i <= 4; i++) {
                    int posW = i * (w - 1) / 4;
                    int posH = i * (gridH - 1) / 4;
                    g.DrawLine(p, posW, 0, posW, gridH);
                    g.DrawLine(p, 0, posH, w, posH);
                }
            }
            using (var helpPen = new Pen(Color.FromArgb(40, 40, 40))) {
                g.DrawLine(helpPen, 0, gridH, w, 0); 
            }

            // Axis Labels
            using (var f = new Font("Consolas", 8))
            using (var b = new SolidBrush(Color.Gray)) {
                for (int i = 0; i <= 4; i++) {
                    string s = (i * 64 - (i == 4 ? 1 : 0)).ToString();
                    if (i > 0) g.DrawString(s, f, b, (i * (w - 1) / 4) - 20, gridH - 15);
                    if (i < 4) g.DrawString(((4 - i) * 64).ToString(), f, b, 2, (i * (gridH - 1) / 4) + 2);
                }
            }

            // Ghost Curves (Visible even in Linked mode, with higher opacity)
            if (_showRef) {
                if (_channel != Channel.Red) DrawCurveLine(g, _gamma.PointCurveR, Color.FromArgb(128, 255, 0, 0), w, gridH, _splineMode);
                if (_channel != Channel.Green) DrawCurveLine(g, _gamma.PointCurveG, Color.FromArgb(128, 0, 255, 0), w, gridH, _splineMode);
                if (_channel != Channel.Blue) DrawCurveLine(g, _gamma.PointCurveB, Color.FromArgb(128, 0, 0, 255), w, gridH, _splineMode);
            }

            // Current Color
            Color cColor = _channel == Channel.Red ? Color.Red : _channel == Channel.Green ? Color.Lime : _channel == Channel.Blue ? Color.Cyan : Color.White;

            // 1. Draw Manual Point Curve (Dashed)
            using (var pDashed = new Pen(Color.FromArgb(160, cColor), 1) { DashStyle = DashStyle.Dash }) {
                DrawCurveLine(g, _currentPoints, pDashed, w, gridH, _splineMode);
            }

            // 2. Draw Total Output (Result) Curve (Solid)
            if (_showLive) {
                var (rRamp, gRamp, bRamp) = _gamma.GetRamp();
                if (rRamp != null) {
                    using (var pSolid = new Pen(cColor, 2)) {
                         if (_channel == Channel.Red || _channel == Channel.Linked) DrawRampLine(g, rRamp, pSolid, w, gridH);
                         else if (_channel == Channel.Green) DrawRampLine(g, gRamp, pSolid, w, gridH);
                         else if (_channel == Channel.Blue) DrawRampLine(g, bRamp, pSolid, w, gridH);
                    }
                }
            }

            // Points
            foreach (var p in _currentPoints) {
                float cx = (float)(p.X / 255.0 * (w - 1));
                float cy = (float)(gridH - (p.Y / 255.0 * gridH));
                g.FillEllipse(Brushes.Yellow, cx - 4, cy - 4, 8, 8);
                g.DrawEllipse(Pens.Black, cx - 4, cy - 4, 8, 8);
            }

            // Greyscale Strip
            int stripY = gridH + 5;
            int stripBoxH = stripH - 10;
            int steps = 24;
            float stepW = w / (float)steps;
            for(int i=0; i<steps; i++) {
                int val = i * 255 / (steps-1);
                using(var b = new SolidBrush(Color.FromArgb(val, val, val))) {
                    g.FillRectangle(b, i*stepW, stripY, stepW+1, stripBoxH);
                }
            }
            using (var stripPen = new Pen(Color.FromArgb(60, 60, 60))) {
                g.DrawRectangle(stripPen, 0, stripY, w - 1, stripBoxH - 1);
            }
        }
        
        private void DrawRampLine(Graphics g, ushort[] ramp, Pen p, int w, int h) {
             if (ramp == null) return;
             var pts = new List<PointF>();
             for(int i=0; i<256; i+=2) { // Skip some for perf
                 float x = (float)(i / 255.0 * (w - 1));
                 float y = (float)(h - (ramp[i] / 65535.0 * h));
                 pts.Add(new PointF(x, y));
             }
             if (pts.Count > 1) g.DrawLines(p, pts.ToArray());
        }

        private void DrawCurveLine(Graphics g, List<Point> points, Pen pen, int w, int h, bool spline) {
             if (points == null || points.Count < 2) return;
             var sorted = points.OrderBy(p => p.X).ToList();
             
             if (sorted.Count == 2) {
                 float x1 = (float)(sorted[0].X / 255.0 * (w - 1));
                 float y1 = (float)(h - (sorted[0].Y / 255.0 * h));
                 float x2 = (float)(sorted[1].X / 255.0 * (w - 1));
                 float y2 = (float)(h - (sorted[1].Y / 255.0 * h));
                 g.DrawLine(pen, x1, y1, x2, y2);
                 return;
             }

             var pointsArr = new List<PointF>();
             for(int i=0; i<256; i+=4) { 
                 double val = GammaServiceHelper.Interpolate(sorted, i, spline);
                 float x = (float)(i / 255.0 * (w - 1));
                 float y = (float)(h - (val / 255.0 * h));
                 pointsArr.Add(new PointF(x, y));
             }
             double valEnd = GammaServiceHelper.Interpolate(sorted, 255, spline);
             pointsArr.Add(new PointF(w - 1, (float)(h - (valEnd / 255.0 * h))));

             if(pointsArr.Count > 1) g.DrawLines(pen, pointsArr.ToArray());
        }

        // Overload for color and weight
        private void DrawCurveLine(Graphics g, List<Point> points, Color color, int w, int h, bool spline) {
            using (var p = new Pen(color, 2)) DrawCurveLine(g, points, p, w, h, spline);
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            int w = _canvas.Width;
            int h = _canvas.Height;
            int gridH = h - 30;
            
            // Hit detection
            for(int i=0; i<_currentPoints.Count; i++) {
                float cx = (float)(_currentPoints[i].X / 255.0 * (w - 1));
                float cy = (float)(gridH - (_currentPoints[i].Y / 255.0 * gridH));
                if (Math.Abs(e.X - cx) < 10 && Math.Abs(e.Y - cy) < 10) {
                    if (e.Button == MouseButtons.Right) {
                        if (_currentPoints[i].X == 0 || _currentPoints[i].X == 255) return;
                        _currentPoints.RemoveAt(i);
                        SavePoints(); UpdateTxt(); _canvas.Invalidate(); _onUpdate?.Invoke();
                        _gamma.Update(); 
                        return;
                    }
                    _draggingIdx = i;
                    return;
                }
            }

            // Add new point
            if (e.Button == MouseButtons.Left) {
                 int nx = Clamp((int)(e.X / (float)(w - 1) * 255), 0, 255);
                 int ny = Clamp((int)((gridH - e.Y) / (float)gridH * 255), 0, 255);
                 
                 _currentPoints.Add(new Point(nx, ny));
                 SavePoints(); 
                 UpdateTxt(); 
                 _canvas.Invalidate(); 
                 _onUpdate?.Invoke();
                 _gamma.Update(); 
                 
                 for(int i=0; i<_currentPoints.Count; i++) {
                     if (_currentPoints[i].X == nx && _currentPoints[i].Y == ny) {
                         _draggingIdx = i;
                         break;
                     }
                 }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            int w = _canvas.Width;
            int h = _canvas.Height;
            int gridH = h - 30;
            int nx = Clamp((int)(e.X / (float)(w - 1) * 255), 0, 255);
            int ny = Clamp((int)((gridH - e.Y) / (float)gridH * 255), 0, 255);
            
            lblCoord.Text = $"X: {nx}  Y: {ny}";

            if (_draggingIdx != -1 && _draggingIdx < _currentPoints.Count) {
                 // Prevent modifying X for 0 and 255 points? 
                 // User might want to move Y.
                 // If index 0 (should be X=0), lock X
                 // If last index (should be X=255), lock X
                 // But wait, after sorting index changes easily.
                 // Let's identify by original requirement: Curve MUST start at 0 and end at 255 X-wise.
                 
                 // If it's the point at X=0, keep X=0
                 // If it's the point at X=255, keep X=255
                 // Since we sort, 0 is always first, 255 is always last if they exist.
                 
                 // Simple logic:
                 if (_draggingIdx == 0) nx = 0;
                 if (_draggingIdx == _currentPoints.Count - 1) nx = 255;

                 _currentPoints[_draggingIdx] = new Point(nx, ny);
                 
                 // Allow live sorting for drag?
                 _currentPoints.Sort((a,b) => a.X.CompareTo(b.X));
                 // update draggingIdx to follow the point
                 for(int i=0; i<_currentPoints.Count; i++) {
                     if (_currentPoints[i].X == nx && _currentPoints[i].Y == ny) {
                         _draggingIdx = i;
                         break;
                     }
                 }

                 if (_channel == Channel.Linked) _gamma.PointCurveMaster = _currentPoints;
                 else if (_channel == Channel.Red) _gamma.PointCurveR = _currentPoints;
                 else if (_channel == Channel.Green) _gamma.PointCurveG = _currentPoints;
                 else if (_channel == Channel.Blue) _gamma.PointCurveB = _currentPoints;

                 _canvas.Invalidate();
                 _gamma.Update(); 
                 _onUpdate?.Invoke(); // Fix: Update main preview live while dragging
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            _draggingIdx = -1;
            SavePoints(); // Final sort and save
            UpdateTxt();
            _canvas.Invalidate();
        }

        private int Clamp(int v, int min, int max) {
            return v < min ? min : v > max ? max : v;
        }
    }
}
