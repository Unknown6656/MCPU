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
                Instruction[] instr = p.Instructions;

                tb_bp.Text = $"0x{p.StackBaseAddress:x8}";
                tb_sp.Text = $"0x{p.StackPointerAddress:x8}";
                tb_cpuid.Text = $"0x{p.CPUID:x8}";
                tb_flags.Text = $"{Convert.ToString((ushort)p.Flags, 2).PadLeft(16, '0')} : {p.Flags}";
                tb_info.Text = $"{Convert.ToString((ushort)p.InformationFlags, 2).PadLeft(16, '0')} : {p.InformationFlags}";
                tb_instrc.Text = $"0x{instr.Length:x8}";
                tb_ip.Text = $"0x{p.IP:x8}";
                tb_memsz.Text = $"0x{p.Size:x8}";
                tb_tick.Text = $"0x{p.Ticks:x8}";

                lst_io.Items.Clear();
                lst_call.Items.Clear();

                int num = 0;

                foreach (IOPort port in p.IO)
                    lst_io.Items.Add(new IOPortData
                    {
                        Direction = port.Direction,
                        Raw = Convert.ToString(port.Raw, 2).PadLeft(8, '0').Insert(4, "."),
                        PortNumber = $"{"global_port".GetStr()} №{++num:D2}",
                        Value = $"{port.Value:D2}", 
                    });

                num = 0;

                foreach (FunctionCall call in p.CallStack)
                    lst_io.Items.Add(new StackframeData
                    {
                        Parameters = string.Join(", ", from i in call.Arguments select $"0x{i:x8}"),
                        SavedFlags = Convert.ToString((ushort)call.SavedFlags, 2).PadLeft(16, '0'),
                        ReturnAddress = $"0x{call.ReturnAddress:x8}",
                        Size = $"{call.Size} {"global_bytes".GetStr()}",
                    });
            });
        }

        internal void Proc_ProcessorReset(Processor p)
        {
            Proc_InstructionExecuted(p, OPCodes.NOP);
        }

        internal void Proc_OnError(Processor p, Exception args)
        {

        }

        public void OnLanguageChanged(string code)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (sender == this)
            {
                e.Cancel = true;

                Hide();
            }
        }
    }

    public class IOPortData
    {
        public string Raw { set; get; }
        public string Value { set; get; }
        public string PortNumber { set; get; }
        public IODirection Direction { set; get; }
    }

    public class StackframeData
    {
        public string ReturnAddress { set; get; }
        public string Parameters { set; get; }
        public string SavedFlags { set; get; }
        public string Size { set; get; }
    }
}
