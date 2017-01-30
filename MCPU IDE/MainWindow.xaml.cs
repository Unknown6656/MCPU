using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

using MCPU.Compiler;
using MCPU.IDE;
using MCPU;

using FastColoredTextBoxNS;

using WinForms = System.Windows.Forms;

namespace MCPU.IDE
{
    public partial class MainWindow
        : Window
        , ILanguageSensitiveWindow
    {
        internal FastColoredTextBox fctb => fctb_host.fctb;
        internal ProcessorWindow watcher;
        internal readonly Setter st_stat;
        internal Thread bg_comp;
        internal Processor proc;
        internal IntPtr handle;
        internal bool changed;
        internal string path;

        internal MCPUCompilerException Error
        {
            get => fctb_host.Error;
            set
            {
                bool error = value != null;

                Color col = (Color)Application.Current.Resources[error ? "BGERR" : "BG"];

                lb_err.Content = error ? "global_error_in".GetStr("global_abbrv_ln".GetStr(), value.LineNr) : "";

                (Resources["stat_bg"] as SolidColorBrush).Color = col;
                statbar.Background = new SolidColorBrush(col);
                statbar.InvalidateVisual();
            }
        }

        public int[] OptimizableLines
        {
            get => fctb_host.OptimizableLines;
            set
            {
                lb_opt.Content = "global_opt".GetStr((value ?? new int[0]).Length);

                statbar.InvalidateVisual();
            }
        }

        ~MainWindow() => DisposeProcessor();

        public MainWindow()
        {
            InitializeComponent();

            watcher = new ProcessorWindow(this);

            InitProcessor();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            handle = new IWIN32WINDOWCONVERTER(this);
            path = null;

            fctb.ZoomChanged += (o, a) =>
            {
                (int nz, int st) = (0, 0);

                (nz, st) = fctb.Zoom >= 500 ? (500, 1) :
                           fctb.Zoom <= 5 ? (5, -1) : (fctb.Zoom, 0);

                if (fctb.Zoom != nz) // prevent a recursive endless loop
                    fctb.Zoom = nz;

                lb_zoom.Content = $"{nz} %";
                mie_zoom_in.IsEnabled = st != 1;
                mie_zoom_out.IsEnabled = st != -1;
                mie_zoom_res.IsEnabled = nz != 100;
            };
            // fctb.TextChanged += (o, a) => new Task(() => Compile(fctb.Text, true)).Start();
            fctb.SelectionChanged += Fctb_SelectionChanged;
            fctb.OnTextChanged(); // update control after loading
            fctb_host.TextChanged += (o, a) =>
            {
                if (bg_comp?.IsAlive ?? false)
                    bg_comp.Abort();

                bg_comp = new Thread(() =>
                {
                    try
                    {
                        this.Dispatcher.Invoke(() => Compile(fctb.Text, true));
                    }
                    catch
                    {
                    }
                });
                bg_comp.Start();
            };

            Fctb_SelectionChanged(null, null);
            global_insert(null, null);

            changed = false;

            // mif_new(null, null);

            fctb_host.Select();
            fctb_host.Focus();
            fctb.Select();
            fctb.Focus();

            OptimizableLines = null;

            UpdateSnippetMenu();
            Compile(fctb.Text, true);
        }

        private void UpdateSnippetMenu()
        {
            mie_ins_snp.Items.Clear();

            foreach (string snippet in Snippets.Names)
            {
                MenuItem subitem = new MenuItem();

                subitem.Click += (s, a) => insertsnippet(Snippets.snp[snippet]);
                subitem.Header = "mw_edit_insert_p".GetStr(snippet);

                mie_ins_snp.Items.Add(subitem);
            }
        }

        private void insertsnippet(string code)
        {
            Place selend = fctb.Selection.End;

            if (fctb.SelectionLength > 0)
                fctb.Selection = new Range(fctb, selend, selend);

            if (fctb.Lines[selend.iLine].Trim().Length > 0)
            {
                selend = new Place(fctb.Lines[selend.iLine].Length - 1, selend.iLine);

                fctb.Selection = new Range(fctb, selend, selend);
            }

            fctb.InsertText($"\n{code}");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !Save();

            watcher.Close();

            DisposeProcessor();

            Application.Current.Shutdown(0);
        }

        private void global_insert(object sender, ExecutedRoutedEventArgs e)
        {
            lb_ins.Content = (WinForms.Control.IsKeyLocked(WinForms.Keys.Insert) ? "mw_OVR" : "mw_INS").GetStr();

            if (sender != null)
            {
                fctb.ProcessKey(WinForms.Keys.Insert);
                fctb.Invalidate();
            }
        }

        private void Fctb_SelectionChanged(object sender, EventArgs e)
        {
            string ToString(Place p) => $"{"global_abbrv_ln".GetStr()} {p.iLine + 1} {"global_abbrv_ch".GetStr()} {p.iChar + 1}";

            Place st = fctb.Selection.Start;
            Place end = fctb.Selection.End;

            lb_pos.Content = st == end ? ToString(st) : $"{ToString(st)} : {ToString(end)}";
        }

        private bool Open()
        {
            bool __open()
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    DefaultExt = ".mcpu",
                    Filter = "MCPU Assembly files (*.mcpu)|*.mcpu|All files|*.*",
                    CheckFileExists = true,
                };

                if (ofd.ShowDialog(this) ?? false)
                    try
                    {
                        using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (StreamReader sr = new StreamReader(fs))
                            fctb.Text = sr.ReadToEnd();

                        fctb.Invalidate();

                        changed = false;
                    }
                    catch
                    {
                        TaskDialog.Show(handle, "msg_err_filenfound".GetStr(), "msg_err".GetStr(), "msg_errtxt_filenfound".GetStr(path), TaskDialogButtons.Ok, TaskDialogIcon.SecurityError);

                        path = null;

                        return Open();
                    }
                else
                    return false;

                return true;
            }
            bool res = __open();

            if (res)
            {
                changed = false;

                fctb.Selection = new Range(fctb, 0, 0, 0, 0);
            }

            return res;
        }

        private bool Save(bool prompt = true)
        {
            bool __save()
            {
                if (changed)
                {
                    if (prompt)
                    {
                        TaskDialogResult tdr = TaskDialog.Show(handle, "msg_war_unsaved".GetStr(), "msg_war".GetStr(), "msg_wartxt_unsaved".GetStr(), TaskDialogButtons.Yes | TaskDialogButtons.No | TaskDialogButtons.Cancel, TaskDialogIcon.SecurityWarning);

                        if (tdr == TaskDialogResult.No)
                            return true;
                        else if (tdr == TaskDialogResult.Cancel)
                            return false;
                    }

                    if (path == null)
                    {
                        SaveFileDialog sfd = new SaveFileDialog
                        {
                            DefaultExt = ".mcpu",
                            Filter = "MCPU Assembly files (*.mcpu)|*.mcpu|All files|*.*",
                            OverwritePrompt = true,
                        };

                        if (path != null)
                            sfd.FileName = path;

                        if (sfd.ShowDialog(this) ?? false)
                        {
                            path = sfd.FileName;

                            return Save();
                        }
                        else
                            return false;
                    }
                    else
                    {
                        FileInfo nfo = new FileInfo(path);
                        DirectoryInfo dir = nfo.Directory;

                        if (!dir.Exists)
                            dir.Create();

                        try
                        {
                            using (FileStream fs = new FileStream(nfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                            using (StreamWriter wr = new StreamWriter(fs))
                                wr.Write(fctb.Text);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(handle, "msg_err_filesave".GetStr(), "msg_err".GetStr(), "msg_errtxt_filesave".GetStr(path, ex.Message), TaskDialogButtons.Ok, TaskDialogIcon.SecurityError);

                            return false;
                        }
                    }
                }

                return true;
            }
            bool res = __save();
            
            if (res)
                changed = false;

            return res;
        }

        internal void DisposeProcessor()
        {
            if (proc != null)
            {
                proc.Dispose();
                proc.OnError -= watcher.Proc_OnError;
                proc.OnTextOutput -= watcher.Proc_OnTextOutput;
                proc.ProcessorReset -= watcher.Proc_ProcessorReset;
                proc.InstructionExecuted -= watcher.Proc_InstructionExecuted;
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        internal void InitProcessor()
        {
            DisposeProcessor();

            proc = new Processor(Properties.Settings.Default.MemorySize, Properties.Settings.Default.CallStackSize, -1);
            proc.OnError += watcher.Proc_OnError;
            proc.OnTextOutput += watcher.Proc_OnTextOutput;
            proc.ProcessorReset += watcher.Proc_ProcessorReset;
            proc.InstructionExecuted += watcher.Proc_InstructionExecuted;
        }

        private void Compile(string code, bool silent = false)
        {
            Union<MCPUCompilerResult, MCPUCompilerException> res = MCPUCompiler.Compile(code);

            if (res.IsA)
            {
                MCPUCompilerResult cmpres = res;

                fctb_host.labels = cmpres.Labels;
                fctb_host.functions = cmpres.Functions;
                fctb_host.OptimizableLines = cmpres.OptimizedLines;
                fctb_host.UpdateAutocomplete();

                if (!silent)
                {
                    proc.Instructions = cmpres.Instructions;

                    watcher.Proc_InstructionExecuted(proc, null);
                }
            }
            else if (!silent)
            {
                MCPUCompilerException ex = res;

                fctb_host.Error = ex;

                TaskDialog.Show(handle, "msg_err_compiler".GetStr(), "msg_err".GetStr(), "msg_errtxt_compiler".GetStr(ex.Message, ex.LineNr), TaskDialogButtons.Ok, TaskDialogIcon.SecurityError);
            }
        }

        public void OnLanguageChanged(string code)
        {
            Fctb_SelectionChanged(fctb, null);

            fctb_host.Error = fctb_host.Error;

            UpdateSnippetMenu();

            // TODO : refresh other stuff 
        }

        #region MENU ITEMS

        private void mie_zoom_in_Click(object sender, ExecutedRoutedEventArgs e) => fctb.Zoom = (int)(fctb.Zoom * 1.2);

        private void mie_zoom_out_Click(object sender, ExecutedRoutedEventArgs e) => fctb.Zoom = (int)(fctb.Zoom / 1.2);

        private void mie_zoom_res_Click(object sender, ExecutedRoutedEventArgs e) => fctb.Zoom = 100;

        private void mie_select_all(object sender, ExecutedRoutedEventArgs e) => fctb.SelectAll();

        private void mie_delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (fctb.SelectionLength == 0)
                fctb.ProcessKey(WinForms.Keys.Delete);
            else
                fctb.ClearSelected();
        }

        private void mie_paste(object sender, ExecutedRoutedEventArgs e) => fctb.Paste();

        private void mie_copy(object sender, ExecutedRoutedEventArgs e) => fctb.Copy();

        private void mie_undo(object sender, ExecutedRoutedEventArgs e) => fctb.Undo();

        private void mie_redo(object sender, ExecutedRoutedEventArgs e) => fctb.Redo();

        private void mie_cut(object sender, ExecutedRoutedEventArgs e) => fctb.Cut();

        private void mie_search(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.F | WinForms.Keys.Control);

        private void mie_replace(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.H | WinForms.Keys.Control);

        private void mie_bm_create(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.B | WinForms.Keys.Control);

        private void mie_bm_delete(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.B | WinForms.Keys.Control | WinForms.Keys.Shift);

        private void mie_bm_prev(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.N | WinForms.Keys.Control | WinForms.Keys.Shift);

        private void mie_bm_next(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.N | WinForms.Keys.Control);

        private void mie_fold(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.F | WinForms.Keys.Alt);

        private void mie_unfold(object sender, ExecutedRoutedEventArgs e) => fctb.ProcessKey(WinForms.Keys.F | WinForms.Keys.Alt | WinForms.Keys.Shift);

        private void mif_open(object sender, ExecutedRoutedEventArgs e)
        {
            if (Save())
                Open();
        }

        private void mif_save(object sender, ExecutedRoutedEventArgs e) => Save(false);

        private void mif_save_as(object sender, ExecutedRoutedEventArgs e)
        {
            string oldpath = path;

            path = null;

            if (!Save(false))
                path = oldpath;
        }

        private void mif_export_html(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                DefaultExt = ".html",
                Filter = "HTML document files (*.html)|*.html|All files|*.*",
                OverwritePrompt = true,
            };

            if (sfd.ShowDialog(this) ?? false)
                try
                {
                    FileInfo nfo = new FileInfo(sfd.FileName);
                    DirectoryInfo dir = nfo.Directory;

                    if (!dir.Exists)
                        dir.Create();

                    using (FileStream fs = new FileStream(nfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                    using (StreamWriter wr = new StreamWriter(fs))
                        wr.Write(fctb.Html);

                    if (TaskDialog.Show(handle, "msg_suc_html".GetStr(), "msg_suc".GetStr(), "msg_suctxt_html".GetStr(nfo.FullName), TaskDialogButtons.Yes | TaskDialogButtons.No | TaskDialogButtons.Cancel, TaskDialogIcon.SecuritySuccess) == TaskDialogResult.Yes)
                        Process.Start($"\"{nfo.FullName}\"").Dispose();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show(handle, "msg_err_html".GetStr(), "msg_err".GetStr(), "msg_errtxt_html".GetStr(ex.Message), TaskDialogButtons.Ok, TaskDialogIcon.SecurityError);
                }
        }

        private void mif_settings(object sender, ExecutedRoutedEventArgs e) => new SettingsWindow(this).ShowDialog();

        private void mif_exit(object sender, ExecutedRoutedEventArgs e) => Close();

        private void mif_new(object sender, ExecutedRoutedEventArgs e)
        {
            if (Save())
            {
                fctb.Clear();
                fctb.Text = new string(' ', fctb.TabLength);
                fctb.Selection = new Range(fctb, fctb.TabLength - 1, 0, fctb.TabLength - 1, 0);

                path = null;
                changed = false;
            }
        }

        private void mic_compile(object sender, ExecutedRoutedEventArgs e) => Compile(fctb.Text);

        private void mip_reset(object sender, ExecutedRoutedEventArgs e)
        {
            proc.Reset();

            // TODO ?
        }

        private void mip_next(object sender, ExecutedRoutedEventArgs e) => new Task(proc.ProcessNext);

        private void mip_start(object sender, ExecutedRoutedEventArgs e)
        {
            new Task(delegate
            {
                proc.ProcessWithoutReset();

                // SOME DARK SYNCHRONIZATION MAGIC GOES HERE

            }).Start();

            // SOME ASYNC SHIT HAS TO GO HERE OR EVERYTHING WILL RUN INSIDE THE UI-THREAD --> NOT GOOD !
        }

        private void mip_stop(object sender, ExecutedRoutedEventArgs e)
        {
            proc.Halt();

            // TODO ?
        }

        internal void mih_github(object sender, ExecutedRoutedEventArgs e) => Process.Start("github_base_url".GetStr()).Dispose();

        private void mih_about(object sender, ExecutedRoutedEventArgs e) => new AboutWindow(this).ShowDialog();

        private void miw_procnfo(object sender, ExecutedRoutedEventArgs e) => watcher.Show();

        #endregion
    }

    public static class Snippets
    {
        internal static Dictionary<string, string> snp { get; } = new Dictionary<string, string> {
            ["IF"] = $@"
    CMP [{MCPUCompiler.TODO_TOKEN}] 0
    JE _else
    
    ; condition is true

    JMP _endif
_else:

    ; condition is false
    
_endif:",
            ["WHILE"] = $@"
_while:
    CMP [{MCPUCompiler.TODO_TOKEN}] 0
    JE _endwhle
    
    ; while body

    JMP _while
_endwhle:",
            ["FOR"] = $@"
    MOV [100h] [{MCPUCompiler.TODO_TOKEN}] ; start value
    MOV [101h] [{MCPUCompiler.TODO_TOKEN}] ; end value
_for:
    CMP [100h] [101h]
    JL _forend
    INCR [100h]

    ; for body

    JMP _for
_forend:
",
        };

        public static string[] Names => snp.Keys.ToArray();
    }

    public static class Commands
    {
        private static RoutedUICommand create(string name, Key key, ModifierKeys mod = ModifierKeys.Control) =>
             new RoutedUICommand(name, name, typeof(Commands), new InputGestureCollection { new KeyGesture(key, mod) });


        public static readonly RoutedUICommand New = create(nameof(New), Key.N);
        public static readonly RoutedUICommand Open = create(nameof(Open), Key.O);
        public static readonly RoutedUICommand Save = create(nameof(Save), Key.S);
        public static readonly RoutedUICommand SaveAs = create(nameof(SaveAs), Key.S, ModifierKeys.Control | ModifierKeys.Shift);
        public static readonly RoutedUICommand ExportAsHTML = create(nameof(ExportAsHTML), Key.E);
        public static readonly RoutedUICommand Preferences = create(nameof(Preferences), Key.F10, ModifierKeys.None);
        public static readonly RoutedUICommand Exit = create(nameof(Exit), Key.F4, ModifierKeys.Alt);
        public static readonly RoutedUICommand ZoomIn = create(nameof(ZoomIn), Key.OemPlus);
        public static readonly RoutedUICommand ZoomOut = create(nameof(ZoomOut), Key.OemMinus);
        public static readonly RoutedUICommand ZoomReset = create(nameof(ZoomReset), Key.D0);
        public static readonly RoutedUICommand Cut = create(nameof(Cut), Key.X);
        public static readonly RoutedUICommand Copy = create(nameof(Copy), Key.C);
        public static readonly RoutedUICommand Paste = create(nameof(Paste), Key.V);
        public static readonly RoutedUICommand SelectAll = create(nameof(SelectAll), Key.A);
        public static readonly RoutedUICommand Delete = create(nameof(Delete), Key.Delete, ModifierKeys.None);
        public static readonly RoutedUICommand Undo = create(nameof(Undo), Key.Z);
        public static readonly RoutedUICommand Redo = create(nameof(Redo), Key.Y);
        public static readonly RoutedUICommand Compile = create(nameof(Compile), Key.F5, ModifierKeys.None);
        public static readonly RoutedUICommand Start = create(nameof(Start), Key.F6, ModifierKeys.None);
        public static readonly RoutedUICommand Next = create(nameof(Next), Key.F5, ModifierKeys.Shift);
        public static readonly RoutedUICommand Stop = create(nameof(Stop), Key.F6, ModifierKeys.Shift);
        public static readonly RoutedUICommand Reset = create(nameof(Reset), Key.F6);
        public static readonly RoutedUICommand About = create(nameof(Reset), Key.F1, ModifierKeys.None);
        public static readonly RoutedUICommand GitHub = create(nameof(Reset), Key.F2, ModifierKeys.None);
        public static readonly RoutedUICommand InsertDelete = create(nameof(InsertDelete), Key.Insert, ModifierKeys.None);
        public static readonly RoutedUICommand Search = create(nameof(Search), Key.F);
        public static readonly RoutedUICommand Replace = create(nameof(Replace), Key.H);
        public static readonly RoutedUICommand NextBookmark = create(nameof(NextBookmark), Key.N);
        public static readonly RoutedUICommand PreviousBookmark = create(nameof(PreviousBookmark), Key.N, ModifierKeys.Shift | ModifierKeys.Control);
        public static readonly RoutedUICommand CreateBookmark = create(nameof(CreateBookmark), Key.B);
        public static readonly RoutedUICommand DeleteBookmark = create(nameof(DeleteBookmark), Key.B, ModifierKeys.Shift | ModifierKeys.Control);
        public static readonly RoutedUICommand FoldAll = create(nameof(FoldAll), Key.F, ModifierKeys.Alt);
        public static readonly RoutedUICommand UnfoldAll = create(nameof(UnfoldAll), Key.F, ModifierKeys.Alt | ModifierKeys.Shift);
        public static readonly RoutedUICommand ProcessorInfo = create(nameof(ProcessorInfo), Key.F8, ModifierKeys.None);
    }
}
