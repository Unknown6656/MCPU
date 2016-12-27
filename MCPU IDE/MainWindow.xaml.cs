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
using Microsoft.Win32;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

using FastColoredTextBoxNS;

using WinForms = System.Windows.Forms;

namespace MCPU.IDE
{
    using MCPU.Compiler;
    using MCPU.IDE;
    using MCPU;

    public partial class MainWindow
        : Window
    {
        internal FastColoredTextBox fctb => fctb_host.fctb;
        internal Processor proc;
        internal IntPtr handle;
        internal bool changed;
        internal string path;


        public MainWindow() => InitializeComponent();

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
            fctb.SelectionChanged += Fctb_SelectionChanged;
            fctb.OnTextChanged(); // update control after loading

            Fctb_SelectionChanged(null, null);
            global_insert(null, null);

            changed = false;

            mif_new(null, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e) => e.Cancel = !Save();

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
                        if (TaskDialog.Show(handle, "msg_war_unsaved".GetStr(), "msg_war".GetStr(), "msg_wartxt_unsaved".GetStr(), TaskDialogButtons.Yes | TaskDialogButtons.No | TaskDialogButtons.Cancel, TaskDialogIcon.SecurityWarning) == TaskDialogResult.Yes)
                            return true;

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

        #region MENU ITEMS

        private void mie_zoom_in_Click(object sender, ExecutedRoutedEventArgs e) => fctb.Zoom = (int)(fctb.Zoom * 1.2);

        private void mie_zoom_out_Click(object sender, ExecutedRoutedEventArgs e) => fctb.Zoom = (int)(fctb.Zoom / 1.2);

        private void mie_zoom_res_Click(object sender, ExecutedRoutedEventArgs e) => fctb.Zoom = 100;

        private void mie_select_all(object sender, ExecutedRoutedEventArgs e) => fctb.SelectAll();

        private void mie_delete(object sender, ExecutedRoutedEventArgs e) => fctb.ClearSelected();

        private void mie_paste(object sender, ExecutedRoutedEventArgs e) => fctb.Paste();

        private void mie_copy(object sender, ExecutedRoutedEventArgs e) => fctb.Copy();

        private void mie_undo(object sender, ExecutedRoutedEventArgs e) => fctb.Undo();

        private void mie_redo(object sender, ExecutedRoutedEventArgs e) => fctb.Redo();

        private void mie_cut(object sender, ExecutedRoutedEventArgs e) => fctb.Cut();

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

        private void mif_settings(object sender, ExecutedRoutedEventArgs e) => new SettingsWindow().ShowDialog();

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

        private void mic_compile(object sender, ExecutedRoutedEventArgs e)
        {
            Union<MCPUCompilerResult, MCPUCompilerException> res = MCPUCompiler.Compile(fctb.Text);

            if (res.IsA)
            {
                MCPUCompilerResult cmpres = res;
                
                fctb_host.labels = cmpres.Labels;
                fctb_host.functions = cmpres.Functions;
                proc.Instructions = cmpres.Instructions;
            }
            else
            {
                MCPUCompilerException ex = res;

                TaskDialog.Show(handle, "msg_err_compiler".GetStr(), "msg_err".GetStr(), "msg_errtxt_compiler".GetStr(ex.Message, ex.LineNr), TaskDialogButtons.Ok, TaskDialogIcon.SecurityError);
            }
        }

        private void mip_reset(object sender, ExecutedRoutedEventArgs e)
        {
            proc.Reset();

            // TODO ?
        }

        private void mip_start(object sender, ExecutedRoutedEventArgs e)
        {
            proc.Process();

            // SOME ASYNC SHIT HAS TO GO HERE OR EVERYTHING WILL RUN INSIDE THE UI-THREAD --> NOT GOOD !
        }

        private void mip_stop(object sender, ExecutedRoutedEventArgs e)
        {
            proc.Halt();

            // TODO ?
        }

        internal void mih_github(object sender, ExecutedRoutedEventArgs e) => Process.Start(@"https://github.com/Unknown6656/MCPU/").Dispose();

        private void mih_about(object sender, ExecutedRoutedEventArgs e) => new AboutWindow(this).ShowDialog();

        #endregion
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
        public static readonly RoutedUICommand Stop = create(nameof(Stop), Key.F6, ModifierKeys.Shift);
        public static readonly RoutedUICommand Reset = create(nameof(Reset), Key.F6);
        public static readonly RoutedUICommand About = create(nameof(Reset), Key.F1, ModifierKeys.None);
        public static readonly RoutedUICommand GitHub = create(nameof(Reset), Key.F2, ModifierKeys.None);
        public static readonly RoutedUICommand InsertDelete = create(nameof(InsertDelete), Key.Insert, ModifierKeys.None);
    }
}

