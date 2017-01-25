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
        internal const string REGEX_ADDR = @"(\bk)?\[{1,2}(?:.+)\]{1,2}";
        internal const string REGEX_PARAM = @"\$[0-9]+\b";
        internal const string REGEX_COMMENT = @"\;.*$";

        internal static TextStyle CreateStyle(int rgb, FontStyle f) => new TextStyle(new SolidBrush(Color.FromArgb((int)(0xff000000u | rgb))), null, f);

        public static OptimizableStyle style_opt;
        public static readonly ErrorStyle style_error = new ErrorStyle();
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
            [style_comments] = REGEX_COMMENT,
            [style_stoken] = REGEX_STOKEN,
            [style_param] = REGEX_PARAM,
            [style_kword] = $@"({REGEX_FUNC}|{REGEX_END_FUNC}|\b({string.Join("|", MCPUCompiler.ReservedKeywords)}|{MCPUCompiler.MAIN_FUNCTION_NAME})\b)",
            // { style_labels, @"(^(\s|\b)+\w+\:|(?:\bfunc\s+)\w+\s*$)" },
            [style_float] = $@"\b({MCPUCompiler.FLOAT_CORE})\b",
            [style_int] = $@"\b({MCPUCompiler.INTEGER_CORE})\b",
            [style_addr] = REGEX_ADDR,
        };
        private static readonly Dictionary<string, Bitmap> autocomp_images = new Dictionary<string, Bitmap>
        {
            ["opcode"] = Properties.Resources.autocomp_instruction,
            ["address"] = Properties.Resources.autocomp_address,
            ["directive"] = Properties.Resources.autocomp_directive,
            ["function"] = Properties.Resources.autocomp_function,
            ["label"] = Properties.Resources.autocomp_label,
            ["keyword"] = Properties.Resources.autocomp_keyword,
        };

        public new event EventHandler<TextChangedEventArgs> TextChanged;

        internal MCPUFunctionMetadata[] functions = new MCPUFunctionMetadata[0];
        internal MCPULabelMetadata[] labels = new MCPULabelMetadata[0];
        internal AutocompleteItem[] std_autocompitems;
        internal AutocompleteMenu autocomp;
        private MCPUCompilerException err;
        private Range err_range;
        private Range[] opt_range;
        private int[] opt_lines;

        public new MainWindow Parent { set; get; }


        internal MCPUCompilerException Error
        {
            get => err;
            set
            {
                fctb.Range.ClearStyle(style_error);

                err_range = null;

                if (value != null)
                {
                    int line = value.LineNr - 1;

                    if (line > 0)
                        (err_range = GetEffectiveLineRange(line)).SetStyle(style_error);
                }

                Parent.Error = err = value;
            }
        }

        internal int[] OptimizableLines
        {
            get => opt_lines;
            set
            {
                fctb.Range.ClearStyle(style_opt);

                value = value ?? new int[0];

                opt_range = (from l in value
                             where l > 0
                             where l < fctb.LinesCount
                             select new Func<Range>(() => {
                                 Range r = GetEffectiveLineRange(l);

                                 r.SetStyle(style_opt);

                                 return r;
                             })()).ToArray();

                Parent.OptimizableLines = opt_lines = value;
            }
        }

        public HighlightnerForm()
            : this(App.Current.MainWindow as MainWindow)
        {
        }

        public HighlightnerForm(MainWindow parent)
        {
            InitializeComponent();

            MinimumSize = new Size(500, 300);
            Parent = parent;

            Load += HighlightnerForm_Load;
            SizeChanged += HighlightnerForm_SizeChanged;

            fctb.AutoIndent = true;
            fctb.KeyDown += Fctb_KeyDown;
            fctb.TextChanged += Fctb_TextChanged;
            fctb.ToolTipNeeded += Fctb_ToolTipNeeded;
            fctb.AutoIndentNeeded += Fctb_AutoIndentNeeded;
            fctb.ToolTip = new DarkTooltip()
            {
                BackColor = fctb.BackColor,
                ForeColor = fctb.ForeColor,
            };
            fctb.ServiceColors.CollapseMarkerBorderColor =
            fctb.ServiceColors.CollapseMarkerForeColor =
            fctb.ServiceColors.ExpandMarkerBorderColor =
            fctb.ServiceColors.ExpandMarkerForeColor =
            fctb.ServiceLinesColor;
            fctb.ServiceColors.CollapseMarkerBackColor =
            fctb.ServiceColors.ExpandMarkerBackColor =
            fctb.BookmarkColor =
            fctb.BackColor;
            fctb.Select();

            style_opt = new OptimizableStyle(fctb.BackColor, .35);

            autocomp = new AutocompleteMenu(fctb);
            autocomp.ToolTip = new DarkTooltip();
            autocomp.ToolTip.BackColor = fctb.BackColor;
            autocomp.ToolTip.ForeColor = fctb.ForeColor;
            autocomp.BackColor = fctb.BackColor;
            autocomp.ForeColor = fctb.ForeColor;
            autocomp.ImageList = new ImageList();
            autocomp.AllowTabKey = true;
            autocomp.AppearInterval = 50;

            foreach (var kvp in autocomp_images)
                autocomp.ImageList.Images.Add(kvp.Key, kvp.Value);

            std_autocompitems = (from kvp in OPCodes.CodesByToken
                                 where kvp.Value != OPCodes.KERNEL
                                 let nv = kvp.Key.Replace("@", "")
                                 let kv = kvp.Value.IsKeyword
                                 select new AutocompleteItem
                                 {
                                     Text = nv,
                                     MenuText = nv,
                                     ToolTipText = "autocomp_instr".GetStr(nv, kvp.Value),
                                     ImageIndex = GetImageIndex(kv ? "keyword" : "opcode"),
                                 })
                                .Concat(new AutocompleteItem[]
                                {
                                    new AutocompleteItem
                                     {
                                         Text = ".main",
                                         MenuText = ".main",
                                         ToolTipText = "autocomp_main".GetStr(),
                                         ImageIndex = GetImageIndex("directive"),
                                     },
                                    new AutocompleteItem
                                     {
                                         Text = ".kernel",
                                         MenuText = ".kernel",
                                         ToolTipText = "autocomp_kernel".GetStr(),
                                         ImageIndex = GetImageIndex("directive"),
                                     },
                                    new AutocompleteItem
                                     {
                                         Text = ".user",
                                         MenuText = ".user",
                                         ToolTipText = "autocomp_user".GetStr(),
                                         ImageIndex = GetImageIndex("directive"),
                                     },
                                }).ToArray();

            UpdateAutocomplete();

            // autocomp.Items.MinimumSize = new Size(200, 300);
            autocomp.Items.Width = 500;
            autocomp.MinFragmentLength = 0;
        }

        private Range GetEffectiveLineRange(int line)
        {
            string ln = fctb.Lines[line];
            int start = ln.Length - ln.TrimStart().Length;
            int end = (ln.Contains(MCPUCompiler.COMMENT_START) ? ln.Remove(ln.IndexOf(MCPUCompiler.COMMENT_START)) : ln).Trim().Length;

            return new Range(fctb, start, line, start + end, line);
        }

        private int GetImageIndex(string name) => autocomp.ImageList.Images.IndexOfKey(name);

        private void HighlightnerForm_SizeChanged(object sender, EventArgs e)
        {
            docmap.Width = 100;
        }

        private void HighlightnerForm_Load(object sender, EventArgs e)
        {
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
            e.ToolTipIcon = ToolTipIcon.None;

            if ((Error != null) && (err_range?.Contains(e.Place) ?? false))
            {
                e.ToolTipText = $"{"global_compiler_error".GetStr()}\n{err.Message}";
                e.ToolTipIcon = ToolTipIcon.Error;
            }
            else if (opt_range?.Any(r => r.Contains(e.Place)) ?? false)
                e.ToolTipText = $"{"global_hint".GetStr()}\n{"global_compiler_opt".GetStr()}";
            else if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                string line = fctb.Lines[e.Place.iLine];
                

                // TODO

                e.ToolTipText = $"{e.HoveredWord}\nThis is the tooltip for '{e.HoveredWord}'";
            }
        }

        internal void UpdateAutocomplete()
        {
            autocomp.Items.SetAutocompleteItems((from f in functions ?? new MCPUFunctionMetadata[0]
                                                 select new AutocompleteItem
                                                 {
                                                     Text = f.Name,
                                                     MenuText = f.Name,
                                                     ToolTipText = "autocomp_func".GetStr(f.Name, f, f.DefinedLine),
                                                     ImageIndex = GetImageIndex("function"),
                                                 })
                                         .Concat(from l in labels ?? new MCPULabelMetadata[0]
                                                 select new AutocompleteItem
                                                 {
                                                     Text = l.Name,
                                                     MenuText = l.Name,
                                                     ToolTipText = "autocomp_label".GetStr(l.Name, l, l.DefinedLine),
                                                     ImageIndex = GetImageIndex("label"),
                                                 })
                                         .Concat(std_autocompitems)
                                         .OrderBy(_ => _.Text));
            autocomp.Items.Invalidate();
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
            Error = null;

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

            docmap.Invalidate();

            if (sender != null)
                Parent.changed = true;
        }
    }

    public sealed class ErrorStyle
        : MarkerStyle
    {
        internal WavyLineStyle wls;


        public ErrorStyle()
            : base(new SolidBrush(Color.FromArgb(0x60ff0000))) => wls = new WavyLineStyle(255, Color.Red);

        public override void Draw(Graphics gr, Point position, Range range)
        {
            base.Draw(gr, position, range);

            wls.Draw(gr, position, range);
        }
    }

    public sealed class OptimizableStyle
        : MarkerStyle
    {
        internal WavyLineStyle wls;


        public OptimizableStyle(Color c, double op)
            : base(new SolidBrush(Color.FromArgb((int)(255 * op), c))) => wls = new WavyLineStyle(255, Color.FromArgb(0x50808080));

        public override void Draw(Graphics gr, Point position, Range range)
        {
            wls.Draw(gr, position, range);

            base.Draw(gr, position, range);
        }
    }
}
