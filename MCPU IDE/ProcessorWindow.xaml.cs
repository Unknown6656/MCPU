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


            Proc_InstructionExecuted(mwin.proc, null);
        }
        
        internal unsafe void Proc_InstructionExecuted(Processor p, Instruction args) => Dispatcher.Invoke(delegate
        {
            lst_io.Items.Clear();
            lst_call.Items.Clear();
            lst_instr.Items.Clear();

            if (p == null)
            {
                tb_bp.Text =
                tb_sp.Text =
                tb_ip.Text =
                tb_memsz.Text =
                tb_cpuid.Text =
                tb_instrc.Text =
                tb_tick.Text = "0x--------";
                tb_flags.Text =
                tb_info.Text = "---------------- : --------";
                tb_raw_calls.Text =
                tb_raw_user.Text =
                tb_raw_instr.Text = "--------";
            }
            else
            {
                Instruction[] instr = p.Instructions;
                FunctionCall[] calls = p.CallStack;

                tb_bp.Text = $"0x{p.StackBaseAddress:x8}";
                tb_sp.Text = $"0x{p.StackPointerAddress:x8}";
                tb_cpuid.Text = $"0x{p.CPUID:x8}";
                tb_flags.Text = $"{Convert.ToString((ushort)p.Flags, 2).PadLeft(16, '0')} : {p.Flags}";
                tb_info.Text = $"{Convert.ToString((ushort)p.InformationFlags, 2).PadLeft(16, '0')} : {p.InformationFlags}";
                tb_instrc.Text = $"0x{instr.Length:x8}";
                tb_ip.Text = $"0x{p.IP:x8}";
                tb_memsz.Text = $"0x{p.Size:x8}";
                tb_tick.Text = $"0x{p.Ticks:x8}";

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

                foreach (FunctionCall call in calls)
                    lst_call.Items.Add(new StackframeData
                    {
                        Parameters = $" - {string.Join(", ", from i in call.Arguments select $"{i:x8}h")}",
                        ParameterCount = call.Arguments.Length,
                        SavedFlags = $"{Convert.ToString((ushort)call.SavedFlags, 2).PadLeft(16, '0')} ({call.SavedFlags})",
                        ReturnAddress = $"{call.ReturnAddress:x8}h",
                        ReturnInstruction = (call.ReturnAddress < 0) && (call.ReturnAddress >= instr.Length) ? "--------(-) []" : instr[call.ReturnAddress].ToString(),
                        Size = $"{call.Size:x8}h",
                        FG = Resources[num++ == 0 ? "fg_cinstr" : "fg_instr"] as SolidColorBrush,
                    });

                num = -1;

                foreach (Instruction i in instr)
                    lst_instr.Items.Add(new InstructionData
                    {
                        Code = $"0x{i.OPCode.Number:x4}",
                        Line = $"0x{++num:x8}",
                        Token = i.OPCode.Token.ToUpper(),
                        FG = Resources[num == p.IP ? "fg_cinstr" : "fg_instr"] as SolidColorBrush,
                        Arguments = string.Join(", ", from arg in i.Arguments
                                                      select arg.ToShortString()),
                        // Keyword = instr.OPCode.IsKeyword ? new BitmapImage(new Uri("Resources/")) : null,
                    });

                lst_io.SelectedIndex = -1;
                lst_instr.SelectedIndex = p.IP >= lst_instr.Items.Count ? -1 : p.IP;
                lst_instr.ScrollIntoView(lst_instr.SelectedItem);
                lst_call.Items.MoveCurrentToFirst();

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < p.StackSize * 4; i++)
                    sb.Append($"{((byte*)p.StackPointer)[i]:x2} ");

                tb_raw_calls.Text = sb.ToString();

                sb.Clear();

                for (int i = 0, l = p.Size; i < l; i++)
                    sb.Append($"{p[i]:x8} ");

                tb_raw_user.Text = sb.ToString();
                tb_raw_instr.Text = string.Join(" ", from b in Instruction.SerializeMultiple(instr) select b.ToString("x2"));
            }
        });

        internal void Proc_OnTextOutput(Processor p, string args) => Dispatcher.Invoke(delegate
        {
            tb_outp.Inlines.Add(new Run(args)
            {
                Foreground = Brushes.WhiteSmoke,
            });
        });

        internal void Proc_ProcessorReset(Processor p)
        {
            Dispatcher.Invoke(() => tb_outp.Inlines.Clear());

            Proc_InstructionExecuted(p, null);
        }

        internal void Proc_OnError(Processor p, Exception ex) => Dispatcher.Invoke(delegate
        {
            tb_outp.Inlines.Add(new Run(ex.Message)
            {
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
            });
        });

        public void OnLanguageChanged(string code) => Proc_InstructionExecuted(mwin.proc, null);

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
        public string Size { set; get; }
        public string SavedFlags { set; get; }
        public string Parameters { set; get; }
        public int ParameterCount { set; get; }
        public string ReturnAddress { set; get; }
        public string ReturnInstruction { set; get; }
        public SolidColorBrush FG { set; get; }
    }

    public class InstructionData
    {
        public string Code { set; get; }
        public string Line { set; get; }
        public string Token { set; get; }
        public string Arguments { set; get; }
        public BitmapImage Elevated { set; get; }
        public BitmapImage IPHandling { set; get; }
        public BitmapImage Keyword { set; get; }
        public SolidColorBrush FG { set; get; }
    }
}
