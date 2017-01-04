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
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace MCPU.IDE
{
    public partial class ProcessorWindow
        : Window
        , ILanguageSensitiveWindow
    {
        internal MainWindow mwin;


        public ProcessorWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            mwin = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            Proc_InstructionExecuted(mwin.proc, OPCodes.NOP);
        }

        internal void Proc_InstructionExecuted(Processor p, Instruction args)
        {
            Dispatcher.Invoke(delegate
            {
                lst_io.Items.Clear();

                int num = 0;

                foreach (IOPort port in p.IO)
                    lst_io.Items.Add(new IOPortData
                    {
                        Direction = port.Direction,
                        Raw = Convert.ToString(port.Raw, 2).Insert(4, "."),
                        PortNumber = $"{"global_port".GetStr()} №{++num:D2}",
                        Value = $"{port.Value:D2}", 
                    });
            });
        }

        internal void Proc_ProcessorReset(Processor p)
        {

        }

        internal void Proc_OnError(Processor p, Exception args)
        {

        }

        public void OnLanguageChanged(string code)
        {

        }
    }

    public class IOPortData
    {
        public string Raw { set; get; }
        public string Value { set; get; }
        public string PortNumber { set; get; }
        public IODirection Direction { set; get; }
    }
}
