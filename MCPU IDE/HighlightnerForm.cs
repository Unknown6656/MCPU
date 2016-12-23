using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System;

using FastColoredTextBoxNS;

namespace MCPU.IDE
{
    using MCPU.Compiler;
    using MCPU;

    public partial class HighlightnerForm
        : UserControl
    {
        internal const string REGEX_STOKEN = @"\.(user|inline|kernel|main)\b";
        internal const string REGEX_FUNC = @"^(?:\s*\.inline)?\s*func\s+(?:\w+)";
        internal const string REGEX_END_FUNC = @"^\s*end\s+func\b";
        internal const string REGEX_LABEL_DECL = @"^\s*\w+\:";
        internal const string REGEX_COMMENT = @"\;.*$";

        public static readonly TextStyle style_labels = CreateStyle(0x4EC9B0, FontStyle.Italic);
        public static readonly TextStyle style_float = CreateStyle(0xC27223, FontStyle.Regular);
        public static readonly TextStyle style_int = CreateStyle(0x99A88E, FontStyle.Regular);
        // public static readonly TextStyle style_string = CreateStyle(0xCA, 0x83, 0x64, FontStyle.Regular);
        public static readonly TextStyle style_comments = CreateStyle(0x14D81A, FontStyle.Italic);
        public static readonly TextStyle style_stoken = CreateStyle(0xD574F0, FontStyle.Regular);
        public static readonly TextStyle style_kword = CreateStyle(0x2E80EE, FontStyle.Regular);
        public static readonly Dictionary<TextStyle, string> styles = new Dictionary<TextStyle, string> {
            { style_comments, REGEX_COMMENT },
            { style_stoken, REGEX_STOKEN },
            { style_kword, $@"({REGEX_FUNC}|{REGEX_END_FUNC}|\b((sys)?call|halt|ret|reset)\b)" },
            { style_labels, @"(^(\s|\b)+\w+\:|(?:\bfunc\s+)\w+\s*$)" },
            { style_float, $@"\b({MCPUCompiler.FLOAT_CORE})\b" },
            { style_int, $@"\b({MCPUCompiler.INTEGER_CORE})\b" },
        };
        

        public new event EventHandler<TextChangedEventArgs> TextChanged;


        public HighlightnerForm()
        {
            InitializeComponent();

            Load += HighlightnerForm_Load;
        }

        internal static TextStyle CreateStyle(int rgb, FontStyle f) => new TextStyle(new SolidBrush(Color.FromArgb((int)(0xff000000u | rgb))), null, f);

        private void HighlightnerForm_Load(object sender, EventArgs e)
        {
            fctb.AutoIndent = true;
            fctb.TextChanged += Fctb_TextChanged;
            fctb.AutoIndentNeeded += Fctb_AutoIndentNeeded;
            fctb.Text = new string(' ', fctb.TabLength);
            fctb.Selection = new Range(fctb, fctb.TabLength - 1, 0, fctb.TabLength - 1, 0);
            fctb.Select();
        }

        private void Fctb_AutoIndentNeeded(object sender, AutoIndentEventArgs e)
        {
            if (Regex.IsMatch(e.LineText, REGEX_FUNC) ||
                Regex.IsMatch(e.LineText, REGEX_END_FUNC) ||
                Regex.IsMatch(e.LineText, REGEX_LABEL_DECL))
                e.Shift = -e.TabLength;
            else
                ; // TODO
        }

        private void Fctb_TextChanged(object sender, TextChangedEventArgs e)
        {
            Range rng = (sender as FastColoredTextBox).VisibleRange;

            e.ChangedRange.ClearFoldingMarkers();
            e.ChangedRange.SetFoldingMarkers(REGEX_FUNC, REGEX_END_FUNC);

            rng.ClearStyle(styles.Keys.ToArray());

            foreach (KeyValuePair<TextStyle, string> st in styles)
                rng.SetStyle(st.Key, st.Value, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // GENERATE STYLE FROM LABELS + FUNCTIONS
            // rng.SetStyle(style_labels, @"");

            TextChanged?.Invoke(sender, e);
        }
    }
}
