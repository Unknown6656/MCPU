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
        , IDisposable
    {
        internal const string REGEX_STOKEN = @"\.(user|inline|kernel|main|enable|disable|data)\b";
        internal const string REGEX_FUNC = @"^(?:\s*(\.inline|interrupt))?\s*func\s+(?:\w+)";
        internal const string REGEX_END_FUNC = @"^\s*end\s+func\b";
        internal const string REGEX_LABEL_DECL = @"^\s*\w+\:";
        internal const string REGEX_ADDR = @"(\bk)?\[{1,2}(?:.+)\]{1,2}";
        internal const string REGEX_PARAM = @"\$[0-9]+\b";
        internal const string REGEX_COMMENT = @"\;.*$";
        internal static readonly string REGEX_CONSTANT = $@"\b({string.Join("|", MCPUCompiler.Constants.Keys)})\b";
        internal static readonly string REGEX_TODO = $@"\b{MCPUCompiler.TODO_TOKEN}\b";
        internal static readonly string REGEX_INT = $@"\b({MCPUCompiler.INTEGER_CORE})\b";
        internal static readonly string REGEX_FLOAT = $@"\b({MCPUCompiler.FLOAT_CORE})\b";
        internal static readonly string REGEX_KWORD = $@"({REGEX_FUNC}|{REGEX_END_FUNC}|\=|\b({string.Join("|", MCPUCompiler.ReservedKeywords.Union(MCPUCompiler.Constants.Keys))}|{MCPUCompiler.MAIN_FUNCTION_NAME})\b)";
        internal static readonly string REGEX_INSTR = $@"\b({string.Join("|", from o in OPCodes.CodesByToken select o.Key)})\b";
        internal static readonly string REGEX_OPREF = $@"\<\s*({REGEX_INSTR})\s*\>";

        internal static TextStyle CreateStyle(int rgb, FontStyle f) => new TextStyle(new SolidBrush(Color.FromArgb((int)(0xff000000u | rgb))), null, f);

        public static OptimizableStyle style_opt;
        public static readonly ErrorStyle style_error = new ErrorStyle();
        public static readonly ABKStyle style_abk = new ABKStyle(new SolidBrush(Color.FromArgb(unchecked((int)0xff2E80EE))), FontStyle.Regular);
        public static readonly FadeStyle style_todo = new FadeStyle(Color.GreenYellow, Color.DarkGreen, 1000);
        public static readonly TextStyle style_param = CreateStyle(0x92CAF4, FontStyle.Regular);
        public static readonly TextStyle style_addr = CreateStyle(0xEFF284, FontStyle.Regular);
        public static readonly TextStyle style_labels = CreateStyle(0x4EC9B0, FontStyle.Italic);
        public static readonly TextStyle style_float = CreateStyle(0xB8D7A3, FontStyle.Regular); // 0xC27223
        public static readonly TextStyle style_int = CreateStyle(0x99A88E, FontStyle.Regular);
        // public static readonly TextStyle style_string = CreateStyle(0xD69D85, FontStyle.Regular);
        public static readonly TextStyle style_comments = CreateStyle(0x14D81A, FontStyle.Italic);
        public static readonly TextStyle style_stoken = CreateStyle(0xD574F0, FontStyle.Regular);
        public static readonly TextStyle style_kword = CreateStyle(0x2E80EE, FontStyle.Regular);
        public static readonly TextStyle style_opc = CreateStyle(0xFDEBD0, FontStyle.Regular);
        public static readonly TextStyle style_opcref = CreateStyle(0xCAA310, FontStyle.Regular);
        public static readonly Dictionary<TextStyle, string> styles = new Dictionary<TextStyle, string>
        {
            [style_comments] = REGEX_COMMENT,
            [style_todo] = REGEX_TODO,
            // [style_abk] = @"\babk\b",
            [style_stoken] = REGEX_STOKEN,
            [style_param] = REGEX_PARAM,
            [style_opcref] = REGEX_OPREF,
            [style_kword] = REGEX_KWORD,
            // { style_labels, @"(^(\s|\b)+\w+\:|(?:\bfunc\s+)\w+\s*$)" },
            [style_float] = REGEX_FLOAT,
            [style_int] = REGEX_INT,
            [style_addr] = REGEX_ADDR,
            [style_opc] = REGEX_INSTR,
        };
        private static readonly Dictionary<string, Bitmap> autocomp_images = new Dictionary<string, Bitmap>
        {
            ["opref"] = Properties.Resources.autocomp_instrref,
            ["constant"] = Properties.Resources.autocomp_constant,
            ["opcode"] = Properties.Resources.autocomp_instruction,
            ["address"] = Properties.Resources.autocomp_address,
            ["directive"] = Properties.Resources.autocomp_directive,
            ["function"] = Properties.Resources.autocomp_function,
            ["label"] = Properties.Resources.autocomp_label,
            ["keyword"] = Properties.Resources.autocomp_keyword,
            ["snippet"] = Properties.Resources.autocomp_snippet,
        };

        public new event EventHandler<TextChangedEventArgs> TextChanged;

        internal MCPUFunctionMetadata[] functions = new MCPUFunctionMetadata[0];
        internal MCPULabelMetadata[] labels = new MCPULabelMetadata[0];
        internal AutocompleteItem[] std_autocompitems;
        internal AutocompleteMenu autocomp;
        private MCPUCompilerException err;
        private Timer refr_timer;
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

            style_opt = new OptimizableStyle(fctb.BackColor, .55);

            autocomp = new AutocompleteMenu(fctb)
            {
                AllowTabKey = false,
                ToolTip = new DarkTooltip
                {
                    BackColor = fctb.BackColor,
                    ForeColor = fctb.ForeColor
                },
                BackColor = fctb.BackColor,
                ForeColor = fctb.ForeColor,
                ImageList = new ImageList(),
                AppearInterval = 50,
                MinFragmentLength = 1
            };

            foreach (KeyValuePair<string, Bitmap> kvp in autocomp_images)
                autocomp.ImageList.Images.Add(kvp.Key, kvp.Value);

            std_autocompitems = (from kvp in OPCodes.CodesByToken
                                 where kvp.Value.IsHidden
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
                                    new AutocompleteItem
                                    {
                                        Text = ".enable",
                                        MenuText = ".enable",
                                        ToolTipText = "autocomp_enable".GetStr(),
                                        ImageIndex = GetImageIndex("directive"),
                                    },
                                    new AutocompleteItem
                                    {
                                        Text = ".disable",
                                        MenuText = ".disable",
                                        ToolTipText = "autocomp_disable".GetStr(),
                                        ImageIndex = GetImageIndex("directive"),
                                    },
                                }).ToArray();

            UpdateAutocomplete();

            // autocomp.Items.MinimumSize = new Size(200, 300);
            autocomp.Items.Width = 500;

            refr_timer = new Timer
            {
                Interval = 50
            };
            refr_timer.Tick += (s, a) => fctb.Invalidate();
            refr_timer.Start();
        }

        private Range GetEffectiveLineRange(int line)
        {
            string ln = fctb.Lines[line];
            int start = ln.Length - ln.TrimStart().Length;
            int end = (ln.Contains(MCPUCompiler.COMMENT_START) ? ln.Remove(ln.IndexOf(MCPUCompiler.COMMENT_START)) : ln).Trim().Length;

            return new Range(fctb, start, line, start + end, line);
        }

        private int GetImageIndex(string name) => autocomp.ImageList.Images.IndexOfKey(name);

        private void HighlightnerForm_SizeChanged(object sender, EventArgs e) => docmap.Width = 100;

        private void HighlightnerForm_Load(object sender, EventArgs e) => fctb.OnSyntaxHighlight(new TextChangedEventArgs(fctb.Range));

        private void Fctb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Space | Keys.Control))
            {
                Point cursor = fctb.PlaceToPoint(fctb.Selection.End);

                cursor.Offset(0, fctb.CharHeight);

                // autocomp.Show(fctb, cursor);
                autocomp.Show(true);

                e.Handled = true;
            }
        }

        private void Fctb_ToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            DarkTooltip tooltip = fctb.ToolTip as DarkTooltip;

            e.ToolTipIcon = ToolTipIcon.None;
            tooltip.Icon = null;

            if ((Error != null) && (err_range?.Contains(e.Place) ?? false))
            {
                e.ToolTipText = $"{"global_compiler_error".GetStr()}\n{err.Message}";
                e.ToolTipIcon = ToolTipIcon.Error;
            }
            else if (opt_range?.Any(r => r.Contains(e.Place)) ?? false)
            {
                e.ToolTipText = $"{"global_hint".GetStr()}\n{"tooltip_opt".GetStr()}";
                e.ToolTipIcon = ToolTipIcon.Info;
            }
            else if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                string line = fctb.Lines[e.Place.iLine];
                Match m;

                if (line.Contains(';') && (line.IndexOf(';') < e.Place.iChar))
                    return;
                else
                {
                    bool reg(string pat, bool lln = false) => (m = Regex.Match(lln ? line : e.HoveredWord, pat, RegexOptions.IgnoreCase)).Success;
                    string token = e.HoveredWord.ToLower();

                    if (reg(REGEX_TODO))
                    {
                        e.ToolTipText = "tooltip_todo".GetStr();
                        e.ToolTipIcon = ToolTipIcon.Warning;
                    }
                    else if (reg(REGEX_ADDR))
                    {
                        tooltip.Icon = autocomp_images["address"];
                        e.ToolTipText = "tooltip_addr".GetStr(e.HoveredWord);
                    }
                    else if (reg(REGEX_PARAM))
                    {
                        // tooltip.Icon =;
                        e.ToolTipText = "tooltip_param".GetStr(e.HoveredWord);
                    }
                    else if (reg(REGEX_STOKEN))
                    {
                        tooltip.Icon = autocomp_images["directive"];
                        e.ToolTipText = $"{e.HoveredWord}-token\n << TODO >>";
                    }
                    else if (reg(REGEX_CONSTANT))
                    {
                        string str = MCPUCompiler.Constants[e.HoveredWord.Trim().ToLower()];
                        FloatIntUnion fiu = e.Place.iLine + 1;

                        if (int.TryParse(str, out int ival))
                            fiu = ival;
                        else if (float.TryParse(str, out float fval))
                            fiu = fval;

                        tooltip.Icon = autocomp_images["constant"];
                        e.ToolTipText = "tooltip_constant".GetStr(e.HoveredWord, fiu.I, fiu.F);
                    }
                    else if (reg(REGEX_FLOAT))
                    {
                        tooltip.Icon = autocomp_images["constant"];
                        e.ToolTipText = "tooltip_float".GetStr(e.HoveredWord);
                    }
                    else if (reg(REGEX_INT))
                    {
                        tooltip.Icon = autocomp_images["constant"];
                        e.ToolTipText = "tooltip_int".GetStr(e.HoveredWord);
                    }
                    else if (reg(REGEX_LABEL_DECL, true))
                    {
                        tooltip.Icon = autocomp_images["label"];
                        e.ToolTipText = "tooltip_label".GetStr();
                    }
                    else if (reg(REGEX_FUNC, true))
                    {
                        tooltip.Icon = autocomp_images["function"];
                        e.ToolTipText = "tooltip_func".GetStr();
                    }
                    else if (reg(REGEX_END_FUNC, true))
                    {
                        tooltip.Icon = autocomp_images["function"];
                        e.ToolTipText = "tooltip_endfunc".GetStr();
                    }
                    else if (reg(REGEX_OPREF))
                    {
                        tooltip.Icon = autocomp_images["opref"];
                        e.ToolTipText = ""; /////////////////////////////////////////////// TODO ///////////////////////////////////////////////
                    }
                    else if (!reg(REGEX_INSTR) && reg(REGEX_KWORD))
                    {
                        tooltip.Icon = autocomp_images["keyword"];
                        e.ToolTipText = "tooltip_keyword".GetStr(e.HoveredWord);
                    }
                    else
                    {
                        MCPUFunctionMetadata[] func = functions.Where(_ => _.Name.ToLower() == token.Trim()).ToArray();
                        MCPULabelMetadata[] label = labels.Where(_ => _.Name.ToLower() == token.Trim()).ToArray();

                        if (func.Length > 0)
                        {
                            tooltip.Icon = autocomp_images["function"];
                            e.ToolTipText = "tooltip_funcref".GetStr(token, func[0].DefinedLine + 1);
                        }
                        else if (label.Length > 0)
                        {
                            tooltip.Icon = autocomp_images["label"];
                            e.ToolTipText = "tooltip_labelref".GetStr(token, label[0].DefinedLine + 1);
                        }
                        else if (reg(REGEX_INSTR))
                        {
                            OPCode opc = OPCodes.CodesByToken.FirstOrDefault(_ => _.Key.ToLower() == token.Trim()).Value;

                            tooltip.Icon = autocomp_images["opcode"];
                            e.ToolTipText = opc == null ? "tooltip_token".GetStr(token) : "tooltip_instr".GetStr(opc.Token, opc);
                        }
                        else
                        {
                            e.ToolTipIcon = ToolTipIcon.Warning;
                            e.ToolTipText = "tooltip_unknown".GetStr(e.HoveredWord);
                        }
                    }

                    // TODO
                }
            }
        }

        internal void UpdateAutocomplete()
        {
            autocomp.Items.SetAutocompleteItems(
                (from s in Snippets.Names
                 orderby s ascending
                 select new AutocompleteItem
                 {
                     Text = Snippets.snp[s],
                     MenuText = s.ToLower(),
                     ToolTipText = "autocomp_snippet".GetStr(s),
                     ImageIndex = GetImageIndex("snippet"),
                 })
                    .Concat((from f in functions ?? new MCPUFunctionMetadata[0]
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
                    .Concat(from kvp in MCPUCompiler.Constants
                            select new Func<AutocompleteItem>(delegate
                            {
                                FloatIntUnion fiu = 0;

                                if (int.TryParse(kvp.Value, out int ival))
                                    fiu = ival;
                                else if (float.TryParse(kvp.Value, out float fval))
                                    fiu = fval;

                                return new AutocompleteItem
                                {
                                    Text = kvp.Key,
                                    MenuText = kvp.Key,
                                    ToolTipText = "autocomp_constant".GetStr(kvp.Key, fiu.I, fiu.F),
                                    ImageIndex = GetImageIndex("constant"),
                                };
                            }).Invoke())
                    .Concat(std_autocompitems)
                    .OrderBy(_ => _.MenuText)));
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

        internal static string TrimStart(string target, string trimString)
        {
            string result = target;

            while (result.StartsWith(trimString))
                result = result.Substring(trimString.Length);

            return result;
        }

        internal static string TrimEnd(string target, string trimString)
        {
            string result = target;

            while (result.EndsWith(trimString))
                result = result.Substring(0, result.Length - trimString.Length);

            return result;
        }

        public new void Dispose()
        {
            refr_timer.Stop();
            refr_timer.Dispose();
            refr_timer = null;

            base.Dispose();
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
            : base(new SolidBrush(Color.FromArgb((int)(255 * op), c))) => wls = new WavyLineStyle(255, Color.FromArgb(0x55555555));

        public override void Draw(Graphics gr, Point position, Range range)
        {
            wls.Draw(gr, position, range);

            base.Draw(gr, position, range);
        }
    }

    public sealed class FadeStyle
        : TextStyle
    {
        public Color ForeColor1 { get; }
        public Color ForeColor2 { get; }
        public int Milliseconds { get; }


        public FadeStyle(Color fg1, Color fg2, int ms)
            : base(null, null, FontStyle.Bold)
        {
            ForeColor1 = fg1;
            ForeColor2 = fg2;
            Milliseconds = ms;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            TimeSpan now = DateTime.MinValue - DateTime.Now;
            double step = (Math.Sin((now.TotalMilliseconds % Milliseconds) * Math.PI * 2d / Milliseconds) + 1) / 2;

            int interpolate(int c1, int c2) => (int)(c1 * step + c2 * (1 - step));

            base.ForeBrush = new SolidBrush(Color.FromArgb(
                    interpolate(ForeColor1.A, ForeColor2.A),
                    interpolate(ForeColor1.R, ForeColor2.R),
                    interpolate(ForeColor1.G, ForeColor2.G),
                    interpolate(ForeColor1.B, ForeColor2.B)
                ));
            base.Draw(gr, position, range);
        }
    }

    public sealed class ABKStyle
        : TextStyle
    {
        public ABKStyle(Brush foreBrush, FontStyle fontStyle)
            : base(foreBrush, null, fontStyle)
        {
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            foreach (Place p in range)
            {
                int time = (int)(DateTime.Now.TimeOfDay.TotalMilliseconds / 2);
                int φ1 = (int)(time % 360L);
                int φ2 = (int)((time - (p.iChar - range.Start.iChar) * 20) % 360L) * 2;
                int x = position.X + (p.iChar - range.Start.iChar) * range.tb.CharWidth;
                Range r = range.tb.GetRange(p, new Place(p.iChar + 1, p.iLine));
                Point point = new Point(x, position.Y + 5 + (int)(5 * Math.Sin(Math.PI * φ2 / 180)));

                gr.ResetTransform();
                gr.TranslateTransform(point.X + (range.tb.CharWidth / 2), point.Y + (range.tb.CharHeight / 2));
                gr.RotateTransform(φ1);
                gr.ScaleTransform(0.8f, 0.8f);
                gr.TranslateTransform(-range.tb.CharWidth / 2, -range.tb.CharHeight / 2);

                base.Draw(gr, new Point(0, 0), r);
            }

            gr.ResetTransform();
        }
    }
}
