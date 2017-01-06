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
        internal Queue<Task> tasks = new Queue<Task>();
        internal MainWindow mwin;
        internal bool active;
        

        ~ProcessorWindow() => active = false;

        public ProcessorWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            
            mwin = mainWindow;
            active = true;

            new Task(delegate
            {
                while (active)
                    if (tasks.Count > 0)
                        tasks.Dequeue().Start();
            }).Start();
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            Proc_InstructionExecuted(mwin.proc, null);
        }

        internal unsafe void Proc_InstructionExecuted(Processor p, Instruction args)
        {
            if (p == null)
                DoUIStuff(delegate
                {
                    lst_io.Items.Clear();
                    lst_call.Items.Clear();
                    lst_instr.Items.Clear();

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
                });
            else
            {
                Instruction[] instr = p.Instructions;
                FunctionCall[] calls = p.CallStack;
                List<IOPortData> _io = new List<IOPortData>();
                List<StackframeData> _stack = new List<StackframeData>();
                List<InstructionData> _instr = new List<InstructionData>();
                StringBuilder sbstack = new StringBuilder();
                StringBuilder sbuser = new StringBuilder();
                int eip = p.IP, num = 0;
                string bp = $"0x{p.StackBaseAddress:x8}";
                string sp = $"0x{p.StackPointerAddress:x8}";
                string cupid = $"0x{p.CPUID:x8}";
                string flags = $"{Convert.ToString((ushort)p.Flags, 2).PadLeft(16, '0')} : {p.Flags}";
                string info = $"{Convert.ToString((ushort)p.InformationFlags, 2).PadLeft(16, '0')} : {p.InformationFlags}";
                string instrc = $"0x{instr.Length:x8}";
                string ip = $"0x{eip:x8}";
                string memsz = $"0x{p.Size:x8}";
                string tick = $"0x{p.Ticks:x8}";
                string instrs = string.Join(" ", from b in Instruction.SerializeMultiple(instr) select b.ToString("x2"));
                
                for (int i = 0; i < p.StackSize * 4; i++)
                    sbstack.Append($"{((byte*)p.StackPointer)[i]:x2} ");

                for (int i = 0, l = p.Size; i < l; i++)
                    sbuser.Append($"{p[i]:x8} ");

                foreach (IOPort port in p.IO)
                    _io.Add(new IOPortData
                    {
                        Direction = port.Direction,
                        Raw = Convert.ToString(port.Raw, 2).PadLeft(8, '0').Insert(4, "."),
                        PortNumber = $"{"global_port".GetStr()} №{++num:D2}",
                        Value = $"{port.Value:D2}",
                    });

                num = 0;

                foreach (FunctionCall call in calls)
                    _stack.Add(new StackframeData
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
                    _instr.Add(new InstructionData
                    {
                        Code = $"0x{i.OPCode.Number:x4}",
                        Line = $"0x{++num:x8}",
                        Token = i.OPCode.Token.ToUpper(),
                        FG = Resources[num == p.IP ? "fg_cinstr" : "fg_instr"] as SolidColorBrush,
                        Arguments = string.Join(", ", from arg in i.Arguments
                                                      select arg.ToShortString()),
                        // Keyword = instr.OPCode.IsKeyword ? new BitmapImage(new Uri("Resources/")) : null,
                    });
                
                DoUIStuff(delegate
                {
                    lst_io.Items.Clear();
                    lst_call.Items.Clear();
                    lst_instr.Items.Clear();

                    tb_bp.Text = bp;
                    tb_sp.Text = sp;
                    tb_cpuid.Text = cupid;
                    tb_flags.Text = flags;
                    tb_info.Text = info;
                    tb_instrc.Text = instrc;
                    tb_ip.Text = ip;
                    tb_memsz.Text = memsz;
                    tb_tick.Text = tick;

                    foreach (IOPortData i in _io)
                        lst_io.Items.Add(i);
                    foreach (StackframeData c in _stack)
                        lst_call.Items.Add(c);
                    foreach (InstructionData i in _instr)
                        lst_instr.Items.Add(i);
                    
                    lst_io.SelectedIndex = -1;
                    lst_instr.SelectedIndex = eip >= lst_instr.Items.Count ? -1 : eip;
                    lst_instr.ScrollIntoView(lst_instr.SelectedItem);
                    lst_call.Items.MoveCurrentToFirst();
                    tb_raw_calls.Text = sbstack.ToString();
                    tb_raw_user.Text = sbuser.ToString();
                    tb_raw_instr.Text = instrs;
                });
            }
        }

        internal void Proc_OnTextOutput(Processor p, string args) => DoUIStuff(delegate
        {
            tb_outp.Inlines.Add(new Run(args)
            {
                Foreground = Brushes.WhiteSmoke,
            });
            sc_outp.ScrollToBottom();
        });

        internal void Proc_ProcessorReset(Processor p)
        {
            Dispatcher.Invoke(() => tb_outp.Inlines.Clear());

            Proc_InstructionExecuted(p, null);
        }

        internal void Proc_OnError(Processor p, Exception ex) => DoUIStuff(delegate
        {
            tb_outp.Inlines.Add(new Run(ex.Message + '\n')
            {
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
            });
            sc_outp.ScrollToBottom();
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

        public void DoUIStuff(Action act)
        {
            if (act != null)
                tasks.Enqueue(new Task(() => Dispatcher.Invoke(act)));
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
