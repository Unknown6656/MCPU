using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System;

namespace MCPU.IDE
{
    public class DarkTooltip
        : ToolTip
    {
        internal static readonly Font title_font = new Font("Segoe UI", 12, FontStyle.Bold, GraphicsUnit.Point);
        internal static readonly Font text_font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        internal int maxwidth = 500;


        public DarkTooltip()
            : base()
        {
            OwnerDraw = true;
            IsBalloon = false;
            Draw += OnDraw;
            Popup += OnPopup;

            BackColor = Color.FromArgb(255, 30, 30, 30);
            ForeColor = Color.WhiteSmoke;
        }
        
        private (string, string) Split(string text)
        {
            string title = text.Contains('\n') ? text.Remove(text.IndexOf('\n')) : "";

            text = text.Remove(0, title.Length);

            return (title.Trim(), text.Trim());
        }

        private SizeF Render(Graphics g, string text)
        {
            string title;

            (title, text) = Split(text);

            SizeF sz1 = title == "" ? SizeF.Empty : g.MeasureString(title, title_font, maxwidth);
            SizeF sz2 = g.MeasureString(text, text_font, maxwidth);

            return new SizeF(Math.Max(sz1.Width, sz2.Width) + 14, sz1.Height + sz2.Height + 14);
        }

        private void OnDraw(object sender, DrawToolTipEventArgs e)
        {
            using (Graphics g = e.Graphics)
            using (SolidBrush bg = new SolidBrush(BackColor))
            using (Pen fg = new Pen(ForeColor, .5f))
            {
                (string title, string text) = Split(e.ToolTipText);

                g.FillRectangle(bg, e.Bounds);
                g.DrawRectangle(fg, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));

                float h = title == "" ? 0 : g.MeasureString(title, title_font, maxwidth).Height;

                g.DrawString(title, title_font, fg.Brush, new RectangleF(e.Bounds.X + 5, e.Bounds.Y + 5, maxwidth, h));
                g.DrawString(text, text_font, fg.Brush, new RectangleF(e.Bounds.X + 5, e.Bounds.Y + 6 + h, maxwidth, short.MaxValue));
            }
        }
        
        private void OnPopup(object sender, PopupEventArgs e)
        {
            SizeF sz = Render(e.AssociatedControl.CreateGraphics(), GetToolTip(e.AssociatedControl));

            e.ToolTipSize = new Size((int)sz.Width, (int)sz.Height);
        }
    }
}
