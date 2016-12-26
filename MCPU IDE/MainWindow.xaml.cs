using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

using FastColoredTextBoxNS;

namespace MCPU.IDE
{
    using MCPU.Compiler;
    using MCPU;


    public partial class MainWindow
        : Window
    {
        public FastColoredTextBox fctb => fctb_host.fctb;
        public Processor proc;
        public IntPtr handle;
        public string path;


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
            fctb.CursorChanged += (o, a) => lb_pos.Content = $"{fctb.Cursor.Handle}";
        }

        private void Save()
        {
            if (path == null)
            {
                // select file
                // touch file
            }
            else
            {
                FileInfo nfo = new FileInfo(path);

                if (!nfo.Exists)
                {
                    //error

                    path = null;

                    Save();
                }
                else
                {
                    // save file
                }
            }
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
            Save();


        }

        private void mif_save(object sender, ExecutedRoutedEventArgs e) => Save();

        private void mif_save_as(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void mif_export_html(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void mif_settings(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void mif_exit(object sender, ExecutedRoutedEventArgs e)
        {
            Save();
            Close();
        }

        private void mif_new(object sender, ExecutedRoutedEventArgs e)
        {
            Save();

            fctb.Clear();
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

                TaskDialog.Show(handle, "Compiler error", "Error", $"The code could not be compiled due to the following compiler error:\n'{ex.Message}' on line {ex.LineNr}");
            }
        }

        private void mip_reset(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void mip_start(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void mip_stop(object sender, ExecutedRoutedEventArgs e)
        {

        }

        #endregion
    }

    public static class Commands
    {
        private static RoutedUICommand create(string name, Key key, ModifierKeys mod = ModifierKeys.Control) =>
             new RoutedUICommand(name, name, typeof(Commands), new InputGestureCollection { new KeyGesture(key, mod) });


        public static readonly RoutedUICommand New = create(nameof(New), Key.OemPlus);
        public static readonly RoutedUICommand Open = create(nameof(Open), Key.OemPlus);
        public static readonly RoutedUICommand Save = create(nameof(Save), Key.OemPlus);
        public static readonly RoutedUICommand SaveAs = create(nameof(SaveAs), Key.OemPlus);
        public static readonly RoutedUICommand ExportAsHTML = create(nameof(ExportAsHTML), Key.OemPlus);
        public static readonly RoutedUICommand Preferences = create(nameof(Preferences), Key.OemPlus);
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
    }
}

