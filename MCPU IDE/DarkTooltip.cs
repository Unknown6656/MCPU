using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System;
using System.Runtime.InteropServices;

namespace MCPU.IDE
{
    public class DarkTooltip
        : ToolTip
    {
        internal static readonly Font title_font = new Font("Segoe UI", 12, FontStyle.Bold, GraphicsUnit.Point);
        internal static readonly Font text_font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        internal int maxwidth = 500;
        private Image img;

        internal Image Icon
        {
            set => img = value;
            get
            {
                Icon fetch()
                {
                    switch (ToolTipIcon)
                    {
                        case ToolTipIcon.Error:
                            return ToolTipIcons.Error;
                        case ToolTipIcon.Info:
                            return ToolTipIcons.Information;
                        case ToolTipIcon.Warning:
                            return ToolTipIcons.Warning;
                        default:
                            return null;
                    }
                }

                return img ?? fetch()?.ToBitmap();
            }
        }
        

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
            int ic = Icon == null ? 0 : 32;

            return new SizeF(Math.Max(sz1.Width, sz2.Width) + 14 + ic, Math.Max(sz1.Height + sz2.Height + 14, 10 + ic));
        }

        private void OnDraw(object sender, DrawToolTipEventArgs e)
        {
            Brush tb = new SolidBrush(ForeColor);

            switch (ToolTipIcon)
            {
                case ToolTipIcon.Error:
                    tb = Brushes.Red;

                    break;
                case ToolTipIcon.Info:
                    tb = Brushes.LightSkyBlue;

                    break;
                case ToolTipIcon.Warning:
                    tb = Brushes.Gold;

                    break;
            }

            using (Graphics g = e.Graphics)
            using (SolidBrush bg = new SolidBrush(BackColor))
            using (Pen fg = new Pen(ForeColor, .5f))
            {
                (string title, string text) = Split(e.ToolTipText);

                int offs = 5;

                g.FillRectangle(bg, e.Bounds);
                g.DrawRectangle(fg, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));

                float h = title == "" ? 0 : g.MeasureString(title, title_font, maxwidth - 6 - e.Bounds.Height).Height;

                if (Icon != null)
                {
                    g.DrawImage(Icon, 5, 5, 32, 32);
                    offs = 42;
                }

                g.DrawString(title, title_font, tb, new RectangleF(e.Bounds.X + offs, e.Bounds.Y + 5, maxwidth, h));
                g.DrawString(text, text_font, fg.Brush, new RectangleF(e.Bounds.X + offs, e.Bounds.Y + 6 + h, maxwidth, short.MaxValue));
            }
        }

        private void OnPopup(object sender, PopupEventArgs e)
        {
            SizeF sz = Render(e.AssociatedControl.CreateGraphics(), GetToolTip(e.AssociatedControl));

            e.ToolTipSize = new Size((int)sz.Width, (int)sz.Height);
        }
    }

    public static class ToolTipIcons
    {
        [DllImport("shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);


        public static Icon Warning { get; } = Extract("user32.dll", 1);
        public static Icon Question { get; } = Extract("user32.dll", 2);
        public static Icon Error { get; } = Extract("user32.dll", 3);
        public static Icon Information { get; } = Extract("user32.dll", 4);
        public static Icon Shield { get; } = Extract("user32.dll", 5);


        public static Icon Extract(string file, int number, bool largeIcon = true)
        {
            IntPtr large;
            IntPtr small;

            ExtractIconEx(file, number, out large, out small, 1);

            try
            {
                return Icon.FromHandle(largeIcon ? large : small);
            }
            catch
            {
                return null;
            }
        }
    }
}
