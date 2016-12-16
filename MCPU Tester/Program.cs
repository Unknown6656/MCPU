using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    using IA = InstructionArgument;

    using static System.Console;
    using static OPCodes;
    using System.Reflection;

    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            Processor proc = new Processor(128);

            proc.PushCall(new FunctionCall(315, 42, 0x7eadbeef));
            proc.PushCall(new FunctionCall(315, -1, 4, 2, 3, 1, 5));

            var c1 = proc.PopCall();
            var c2 = proc.PopCall();

            int addr = 0;

            foreach (byte b in Encoding.ASCII.GetBytes("hello -- top kek lulz /foo/bar"))
                proc.UserSpace[addr++] = b;

            proc.IO.SetValue(7, 12);

            for (int i = 0; i < 32; i++)
                proc[i] = i | (i << 8) | (i << 16) | (i << 24);

            ConsoleExtensions.HexDump(proc.ToBytes());

            proc.ProcessWithoutReset(
                (COPY, new IA[] { (0, ArgumentType.Address), (64, ArgumentType.Address), 32 }),
                (KERNEL, new IA[] { 1 }),
                (SYSCALL, new IA[] { 0 }),
                (KERNEL, new IA[] { 0 }),
                (IO, new IA[] { 13 }),
                (IN, new IA[] { 13 })
            );

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");
            
            ConsoleExtensions.HexDump(proc.ToBytes());

            ReadKey(true);
        }
    }
}
