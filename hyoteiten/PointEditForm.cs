using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace hyoteiten
{
    public partial class PointEditForm : Form
    {
        public List<PointResult> OuterPoints { get; private set; }
        public List<PointResult> InnerPoints { get; private set; }
        public List<PointResult> CheckPoints { get; private set; }

        private PointResult selectedPoint = null;
        private bool isDragging = false;

        private enum EditMode { SelectMove, AddNH, AddKS }
        private EditMode currentMode = EditMode.SelectMove;

        // 指定距離（半径 m）
        private double outerCoverageDist = 90.0;
        private double innerCoverageDist = 190.0;

        // ドラッグ移動中の表示倍率・原点ズレ防止用（固定表示範囲）
        private double boundsMinY, boundsMaxY, boundsMinX, boundsMaxX;

        public PointEditForm(List<PointResult> outer, List<PointResult> inner, List<PointResult> check, double outerDist = 90.0, double innerDist = 190.0)
        {
            InitializeComponent();

            this.outerCoverageDist = outerDist;
            this.innerCoverageDist = innerDist;

            OuterPoints = outer.Select(p => new PointResult(p.Name, p.Y, p.X, p.Z, p.Type)).ToList();
            InnerPoints = inner.Select(p => new PointResult(p.Name, p.Y, p.X, p.Z, p.Type)).ToList();
            CheckPoints = check.Select(p => new PointResult(p.Name, p.Y, p.X, p.Z, p.Type)).ToList();

            // 初期表示座標範囲の計算
            UpdateWorldBounds();

            this.Resize += (s, e) => { canvasPanel?.Invalidate(); };
            EnableDoubleBuffer(canvasPanel);

            canvasPanel.Paint += CanvasPanel_Paint;
            canvasPanel.MouseDown += CanvasPanel_MouseDown;
            canvasPanel.MouseMove += CanvasPanel_MouseMove;
            canvasPanel.MouseUp += CanvasPanel_MouseUp;

            radMove.CheckedChanged += (s, e) => { if (radMove.Checked) currentMode = EditMode.SelectMove; };
            radAddNH.CheckedChanged += (s, e) => { if (radAddNH.Checked) currentMode = EditMode.AddNH; };
            radAddKS.CheckedChanged += (s, e) => { if (radAddKS.Checked) currentMode = EditMode.AddKS; };

            btnDelete.Click += BtnDelete_Click;
            btnOk.Click += (s, e) => { this.DialogResult = DialogResult.OK; };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; };
        }

        private void EnableDoubleBuffer(Control control)
        {
            PropertyInfo property = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            property?.SetValue(control, true, null);
        }

        private void UpdateWorldBounds()
        {
            var all = OuterPoints.Concat(InnerPoints).Concat(CheckPoints).ToList();
            if (all.Count == 0)
            {
                boundsMinY = 0; boundsMaxY = 100;
                boundsMinX = 0; boundsMaxX = 100;
                return;
            }

            boundsMinY = all.Min(p => p.Y);
            boundsMaxY = all.Max(p => p.Y);
            boundsMinX = all.Min(p => p.X);
            boundsMaxX = all.Max(p => p.X);
        }

        private PointF WorldToScreen(double y, double x)
        {
            double margin = 60;
            double scaleX = (canvasPanel.Width - margin * 2) / Math.Max(1.0, boundsMaxX - boundsMinX);
            double scaleY = (canvasPanel.Height - margin * 2) / Math.Max(1.0, boundsMaxY - boundsMinY);
            double scale = Math.Min(scaleX, scaleY);

            float sx = (float)(margin + (x - boundsMinX) * scale);
            float sy = (float)(canvasPanel.Height - margin - (y - boundsMinY) * scale);

            return new PointF(sx, sy);
        }

        private Tuple<double, double> ScreenToWorld(float sx, float sy)
        {
            double margin = 60;
            double scaleX = (canvasPanel.Width - margin * 2) / Math.Max(1.0, boundsMaxX - boundsMinX);
            double scaleY = (canvasPanel.Height - margin * 2) / Math.Max(1.0, boundsMaxY - boundsMinY);
            double scale = Math.Min(scaleX, scaleY);

            double x = boundsMinX + (sx - margin) / scale;
            double y = boundsMinY + (canvasPanel.Height - margin - sy) / scale;

            return Tuple.Create(y, x);
        }

        private float GetCurrentScale()
        {
            double margin = 60;
            double scaleX = (canvasPanel.Width - margin * 2) / Math.Max(1.0, boundsMaxX - boundsMinX);
            double scaleY = (canvasPanel.Height - margin * 2) / Math.Max(1.0, boundsMaxY - boundsMinY);
            return (float)Math.Min(scaleX, scaleY);
        }

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            float currentScale = GetCurrentScale();

            // 1. 外部評定点 (GH) の指定距離円（設定値 outerCoverageDist そのままを半径として使用）
            float outerRadiusPixel = (float)(outerCoverageDist * currentScale);
            using (Brush ghBrush = new SolidBrush(Color.FromArgb(20, 255, 0, 0)))
            using (Pen ghPen = new Pen(Color.FromArgb(140, 255, 100, 100), 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                foreach (var gh in OuterPoints)
                {
                    PointF sp = WorldToScreen(gh.Y, gh.X);
                    g.FillEllipse(ghBrush, sp.X - outerRadiusPixel, sp.Y - outerRadiusPixel, outerRadiusPixel * 2, outerRadiusPixel * 2);
                    g.DrawEllipse(ghPen, sp.X - outerRadiusPixel, sp.Y - outerRadiusPixel, outerRadiusPixel * 2, outerRadiusPixel * 2);
                }
            }

            // 2. 内部評定点 (NH) の指定距離円（設定値 innerCoverageDist そのままを半径として使用）
            float innerRadiusPixel = (float)(innerCoverageDist * currentScale);
            using (Brush nhBrush = new SolidBrush(Color.FromArgb(30, 0, 255, 0)))
            using (Pen nhPen = new Pen(Color.FromArgb(180, 0, 255, 100), 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                foreach (var nh in InnerPoints)
                {
                    PointF sp = WorldToScreen(nh.Y, nh.X);
                    g.FillEllipse(nhBrush, sp.X - innerRadiusPixel, sp.Y - innerRadiusPixel, innerRadiusPixel * 2, innerRadiusPixel * 2);
                    g.DrawEllipse(nhPen, sp.X - innerRadiusPixel, sp.Y - innerRadiusPixel, innerRadiusPixel * 2, innerRadiusPixel * 2);
                }
            }

            // 3. 外部評定点 (GH) の外周ポリゴン（黄色ライン）
            if (OuterPoints.Count >= 3)
            {
                PointF[] outerPolyPts = OuterPoints.Select(p => WorldToScreen(p.Y, p.X)).ToArray();
                using (Pen penOuter = new Pen(Color.Yellow, 2f))
                {
                    g.DrawPolygon(penOuter, outerPolyPts);
                }
            }

            // 4. 各点（GH, NH, KS）の描画ローカル関数
            Action<PointResult, Color, int> drawPt = (pt, color, radius) =>
            {
                PointF sp = WorldToScreen(pt.Y, pt.X);
                bool isSelected = (pt == selectedPoint);

                using (Brush brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, sp.X - radius, sp.Y - radius, radius * 2, radius * 2);
                }

                if (isSelected)
                {
                    using (Pen penSel = new Pen(Color.White, 3f))
                    {
                        g.DrawEllipse(penSel, sp.X - radius - 3, sp.Y - radius - 3, (radius + 3) * 2, (radius + 3) * 2);
                    }
                }

                using (Font f = new Font("メイリオ", 9F, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString(pt.Name, f, textBrush, sp.X + radius + 2, sp.Y - radius);
                }
            };

            foreach (var p in OuterPoints) drawPt(p, Color.Red, 7);
            foreach (var p in InnerPoints) drawPt(p, Color.Lime, 7);
            foreach (var p in CheckPoints) drawPt(p, Color.DeepSkyBlue, 7);
        }

        private void CanvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            var allEditable = InnerPoints.Concat(CheckPoints).Concat(OuterPoints).ToList();
            selectedPoint = null;

            foreach (var pt in allEditable)
            {
                PointF sp = WorldToScreen(pt.Y, pt.X);
                double dist = Math.Sqrt(Math.Pow(sp.X - e.X, 2) + Math.Pow(sp.Y - e.Y, 2));
                if (dist <= 15)
                {
                    selectedPoint = pt;
                    break;
                }
            }

            if (currentMode == EditMode.SelectMove && selectedPoint != null)
            {
                isDragging = true;
            }
            else if (currentMode == EditMode.AddNH)
            {
                var world = ScreenToWorld(e.X, e.Y);
                int nextNo = GetNextPointNumber("NH", InnerPoints);
                var newNH = new PointResult($"NH{nextNo}", world.Item1, world.Item2, 0.0, PointType.InnerControl);
                InnerPoints.Add(newNH);
                selectedPoint = newNH;
            }
            else if (currentMode == EditMode.AddKS)
            {
                var world = ScreenToWorld(e.X, e.Y);
                int nextNo = GetNextPointNumber("KS", CheckPoints);
                var newKS = new PointResult($"KS{nextNo}", world.Item1, world.Item2, 0.0, PointType.CheckPoint);
                CheckPoints.Add(newKS);
                selectedPoint = newKS;
            }

            canvasPanel.Invalidate();
        }

        private void CanvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedPoint != null)
            {
                var world = ScreenToWorld(e.X, e.Y);
                selectedPoint.Y = world.Item1;
                selectedPoint.X = world.Item2;
                canvasPanel.Invalidate();
            }
        }

        private void CanvasPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedPoint == null) return;

            if (selectedPoint.Type == PointType.OuterControl)
            {
                MessageBox.Show("外部評定点 (GH) は削除できません。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedPoint.Type == PointType.InnerControl) InnerPoints.Remove(selectedPoint);
            if (selectedPoint.Type == PointType.CheckPoint) CheckPoints.Remove(selectedPoint);

            selectedPoint = null;
            canvasPanel.Invalidate();
        }

        private int GetNextPointNumber(string prefix, List<PointResult> points)
        {
            int max = 0;
            Regex regex = new Regex(@"^" + prefix + @"(\d+)$");

            foreach (var p in points)
            {
                var match = regex.Match(p.Name);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return max + 1;
        }
    }
}