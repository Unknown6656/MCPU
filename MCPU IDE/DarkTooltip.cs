using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System;

namespace MCPU.IDE
{
    public class DarkTooltip
        : ToolTip
    {
        public Brush Background { set; get; }
        public Pen Foreground { set; get; }


        public DarkTooltip()
        {
            OwnerDraw = true;
            Draw += new DrawToolTipEventHandler(OnDraw);
            Popup += DarkTooltip_Popup;

            Background = new SolidBrush(Color.FromArgb(255, 30, 30, 30));
            Foreground = Pens.WhiteSmoke;
        }

        private void DarkTooltip_Popup(object sender, PopupEventArgs e) =>
            e.ToolTipSize = new Size(e.ToolTipSize.Width + 10, e.ToolTipSize.Height);

        private void OnDraw(object sender, DrawToolTipEventArgs e)
        {
            using (Graphics g = e.Graphics)
            {
                g.FillRectangle(Background, e.Bounds);
                g.DrawRectangle(Foreground, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
                // g.DrawString(e.ToolTipText, new Font(e.Font, FontStyle.Bold), Brushes.Silver, new PointF(e.Bounds.X + 6, e.Bounds.Y + 6));
                g.DrawString(e.ToolTipText, new Font(e.Font, FontStyle.Bold), Foreground.Brush, new PointF(e.Bounds.X + 5, e.Bounds.Y + 5));
            }
        }
    }
}