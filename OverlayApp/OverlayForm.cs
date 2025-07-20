using System;
using System.Windows.Forms;

namespace OverlayApp
{
    public class OverlayForm : Form
    {
        private readonly Color sliceColor = Color.LightGray;
        private readonly Color sliceBorderColor = Color.DimGray;
        private readonly string[] sliceLabels = new string[]
        {
            "́", "̀", "̉", "̃", "̣", "˘", "ˆ", "̛", "đ"
        };
        // The actual output for each slice (combining characters or special)
        private readonly string[] sliceOutputs = new string[]
        {
            "\u0300", // grave 
            "\u0301", // acute
            "đ",      // special: stroke
            "\u0309", // hook above
            "\u0302", // circumflex
            "\u0306", // breve
            "\u0323", // dot below
            "\u0303", // tilde
            "\u031B", // horn
        };
        private readonly int sliceCount = 9;

        // Static property to store the selected diacritical index
        public static int? SelectedDiacriticalIndex { get; set; }

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            this.Opacity = 0.7;
            this.Width = 150;
            this.Height = 150;
            this.ShowInTaskbar = false;

            // Initial position (will be updated before showing)
            UpdatePositionToMouse();

            // Make the form a donut (ring) shape
            int thickness = 90; // thickness of the ring
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, this.Width, this.Height); // outer
            path.AddEllipse(thickness, thickness, this.Width - 2 * thickness, this.Height - 2 * thickness); // inner (hole)
            path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;
            this.Region = new Region(path);

            this.DoubleBuffered = true;
            this.MouseClick += OverlayForm_MouseClick;
        }

        public void UpdatePositionToMouse()
        {
            var mousePos = Cursor.Position;
            this.Location = new System.Drawing.Point(
                mousePos.X - this.Width / 2,
                mousePos.Y - this.Height / 2
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float anglePerSlice = 360f / sliceCount;
            float startAngle = -90f; // Start at top
            Rectangle outerRect = new Rectangle(0, 0, this.Width, this.Height);
            int thickness = 45;
            Rectangle innerRect = new Rectangle(thickness, thickness, this.Width - 2 * thickness, this.Height - 2 * thickness);
            float outerRadius = this.Width / 2f;
            float innerRadius = outerRadius - thickness;
            float centerX = this.Width / 2f;
            float centerY = this.Height / 2f;
            var font = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
            for (int i = 0; i < sliceCount; i++)
            {
                float sliceStart = startAngle + i * anglePerSlice;
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddArc(outerRect, sliceStart, anglePerSlice);
                    path.AddArc(innerRect, sliceStart + anglePerSlice, -anglePerSlice);
                    path.CloseFigure();
                    using (var brush = new SolidBrush(sliceColor))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(sliceBorderColor, 2))
                    {
                        g.DrawPath(pen, path);
                    }
                }
                // Draw label in the middle of the slice
                float midAngle = sliceStart + anglePerSlice / 2f;
                double rad = midAngle * Math.PI / 180.0;
                float labelRadius = (outerRadius + innerRadius) / 2f;
                float labelX = centerX + (float)(labelRadius * Math.Cos(rad));
                float labelY = centerY + (float)(labelRadius * Math.Sin(rad));
                string label = sliceLabels[i % sliceLabels.Length];
                SizeF textSize = g.MeasureString(label, font);
                g.DrawString(label, font, Brushes.Black, labelX - textSize.Width / 2, labelY - textSize.Height / 2);
            }
            // Draw the inner circle to create the hole (for anti-aliasing)
            using (var brush = new SolidBrush(this.BackColor))
            {
                g.FillEllipse(brush, innerRect);
            }
        }

        private void OverlayForm_MouseClick(object? sender, MouseEventArgs e)
        {
            // Convert mouse position to center-based polar coordinates
            float centerX = this.Width / 2f;
            float centerY = this.Height / 2f;
            float dx = e.X - centerX;
            float dy = centerY - e.Y; // Y axis is inverted
            double angle = (Math.Atan2(dy, dx) * 180 / Math.PI);
            if (angle < 0) angle += 360;
            int slice = (int)(angle / ((360.0 / sliceCount)));
            // Only handle clicks inside the ring
            double dist = Math.Sqrt(dx * dx + dy * dy);
            int thickness = 90;
            double outerRadius = this.Width / 2.0;
            double innerRadius = outerRadius - thickness;
            if (dist <= outerRadius && dist >= innerRadius)
            {
                // Store the selected diacritical index and close the overlay
                SelectedDiacriticalIndex = slice % sliceLabels.Length;
                Console.WriteLine(SelectedDiacriticalIndex);
                this.Close();
            }
        }
    }
}
