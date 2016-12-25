﻿using System.Text.RegularExpressions;
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
        internal const string REGEX_ADDR = @"(\bk)?\[{1,2}(?:.+)\]{1,2}";
        internal const string REGEX_PARAM = @"\$[0-9]+\b";
        internal const string REGEX_COMMENT = @"\;.*$";

        internal static TextStyle CreateStyle(int rgb, FontStyle f) => new TextStyle(new SolidBrush(Color.FromArgb((int)(0xff000000u | rgb))), null, f);

        public static readonly TextStyle style_param = CreateStyle(0x92CAF4, FontStyle.Regular);
        public static readonly TextStyle style_addr = CreateStyle(0xEFF284, FontStyle.Regular);
        public static readonly TextStyle style_labels = CreateStyle(0x4EC9B0, FontStyle.Italic);
        public static readonly TextStyle style_float = CreateStyle(0xB8D7A3, FontStyle.Regular); // 0xC27223
        public static readonly TextStyle style_int = CreateStyle(0x99A88E, FontStyle.Regular);
        // public static readonly TextStyle style_string = CreateStyle(0xD69D85, FontStyle.Regular);
        public static readonly TextStyle style_comments = CreateStyle(0x14D81A, FontStyle.Italic);
        public static readonly TextStyle style_stoken = CreateStyle(0xD574F0, FontStyle.Regular);
        public static readonly TextStyle style_kword = CreateStyle(0x2E80EE, FontStyle.Regular);
        public static readonly Dictionary<TextStyle, string> styles = new Dictionary<TextStyle, string>
        {
            { style_comments, REGEX_COMMENT },
            { style_stoken, REGEX_STOKEN },
            { style_param, REGEX_PARAM },
            { style_kword, $@"({REGEX_FUNC}|{REGEX_END_FUNC}|\b((sys)?call|halt|ret|reset)\b)" },
            // { style_labels, @"(^(\s|\b)+\w+\:|(?:\bfunc\s+)\w+\s*$)" },
            { style_float, $@"\b({MCPUCompiler.FLOAT_CORE})\b" },
            { style_int, $@"\b({MCPUCompiler.INTEGER_CORE})\b" },
            { style_addr, REGEX_ADDR },
        };
        internal static readonly Dictionary<string, Bitmap> autocomp_images = new Dictionary<string, Bitmap>
        {
            { "opcode", new Bitmap(1, 1) },
        };

        public new event EventHandler<TextChangedEventArgs> TextChanged;

        internal List<string> functions = new List<string>();
        internal List<string> labels = new List<string>();
        internal AutocompleteItem[] std_autocompitems;
        internal AutocompleteMenu autocomp;


        public HighlightnerForm()
        {
            InitializeComponent();

            Load += HighlightnerForm_Load;
            SizeChanged += HighlightnerForm_SizeChanged;
        }

        private void HighlightnerForm_SizeChanged(object sender, EventArgs e)
        {
            docmap.Width = 100;
        }

        private void HighlightnerForm_Load(object sender, EventArgs e)
        {
            MinimumSize = new Size(500, 300);
            
            fctb.AutoIndent = true;
            fctb.KeyDown += Fctb_KeyDown;
            fctb.TextChanged += Fctb_TextChanged;
            fctb.ToolTipNeeded += Fctb_ToolTipNeeded;
            fctb.AutoIndentNeeded += Fctb_AutoIndentNeeded;
            // fctb.Text = new string(' ', fctb.TabLength);
            fctb.Selection = new Range(fctb, fctb.TabLength - 1, 0, fctb.TabLength - 1, 0);
            fctb.ToolTip = new DarkTooltip();
            fctb.ToolTip.BackColor = fctb.BackColor;
            fctb.ToolTip.ForeColor = fctb.ForeColor;
            fctb.Select();

            autocomp = new AutocompleteMenu(fctb);
            autocomp.ToolTip = new DarkTooltip();
            autocomp.ToolTip.BackColor = fctb.BackColor;
            autocomp.ToolTip.ForeColor = fctb.ForeColor;
            autocomp.BackColor = fctb.BackColor;
            autocomp.ForeColor = fctb.ForeColor;
            autocomp.Opening += Autocomp_Opening;
            autocomp.ImageList = new ImageList();
            autocomp.AllowTabKey = true;
            autocomp.AppearInterval = 100;
            autocomp.MinFragmentLength = 1;
            autocomp.SearchPattern = ".";
            
            foreach (var kvp in autocomp_images)
                autocomp.ImageList.Images.Add(kvp.Key, kvp.Value);

            std_autocompitems = (from kvp in OPCodes.CodesByToken
                                 where kvp.Value != OPCodes.KERNEL
                                 let nv = kvp.Key.Replace("@", "")
                                 select new AutocompleteItem
                                 {
                                     Text = nv,
                                     MenuText = nv,
                                     ToolTipTitle = $"Instruction '{nv}'",
                                     ToolTipText = $"The instrucion {kvp.Value}",
                                     ImageIndex = autocomp.ImageList.Images.IndexOfKey("opcode"),
                                 }).ToArray();

            autocomp.Items.SetAutocompleteItems(std_autocompitems);
            // autocomp.Items.MinimumSize = new Size(200, 300);
            autocomp.Items.Width = 500;

            fctb.OnSyntaxHighlight(new TextChangedEventArgs(fctb.Range));
        }

        private void Fctb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Space | Keys.Control))
            {
                Point cursor = fctb.PlaceToPoint(fctb.Selection.End);

                cursor.Offset(0, fctb.CharHeight);

                autocomp.Show(fctb, cursor);

                e.Handled = true;
            }
        }

        private void Fctb_ToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                e.ToolTipTitle = e.HoveredWord;
                
                // TODO

                e.ToolTipText = "This is tooltip for '" + e.HoveredWord + "'";
            }
        }

        private void Autocomp_Opening(object sender, CancelEventArgs e) =>
            autocomp.Items.SetAutocompleteItems(std_autocompitems.Concat(from f in functions
                                                                         select new AutocompleteItem
                                                                         {
                                                                             // TODO
                                                                         })
                                                                 .Concat(from l in labels
                                                                         select new AutocompleteItem
                                                                         {
                                                                             // TODO
                                                                         }));

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
            Range rng = fctb.Range;

            e.ChangedRange.ClearFoldingMarkers();
            e.ChangedRange.SetFoldingMarkers(REGEX_FUNC, REGEX_END_FUNC);

            rng.ClearStyle(styles.Keys.ToArray());
            rng.ClearStyle(style_labels);

            foreach (KeyValuePair<TextStyle, string> st in styles)
                rng.SetStyle(st.Key, st.Value, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Range found in fctb.GetRanges(@"^(\s|\b)+(?<range>\w+)\:", RegexOptions.IgnoreCase | RegexOptions.Multiline)
                            .Concat(fctb.GetRanges(@"(?:\bfunc\s+)(?<range>\w+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)))
            {
                found.ClearStyle(style_kword);
                rng.SetStyle(style_labels, $@"\b{found.Text}\b");
            }
            
            TextChanged?.Invoke(sender, e);

            docmap.NeedRepaint();
        }
    }
}
