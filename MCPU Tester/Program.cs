using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    using IA = InstructionArgument;

    using static System.Console;
    using static ArgumentType;
    using static OPCodes;

    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            Processor proc = new Processor(128);

            proc.IO.SetValue(7, 12);
            proc.IO.SetValue(13, 5);

            for (int i = 0; i < 32; i++)
                proc[i] = i | (i << 8) | (i << 16) | (i << 24);

            int addr = 44;
            foreach (byte b in Encoding.ASCII.GetBytes("hello! top kek lulz /foo/bar/"))
                proc.UserSpace[addr++] = b;

            // ConsoleExtensions.HexDump(proc.ToBytes());

            proc.ProcessWithoutReset(
/* 00 */        (JMP, new IA[] { (5, Label) }),
/* ------------ FUNCTION @ 0x01 ------------ */
/* 01 */        (KERNEL, new IA[] { 1 }),
/* 02 */        (SYSCALL, new IA[] { 0 }),
/* 03 */        (KERNEL, new IA[] { 0 }),
/* 04 */        (RET, null),
/* ------------ END OF FUNCTION ------------ */
/* 05 */        (CALL, new IA[] { (1, Function) }),
/* 06 */        (COPY, new IA[] { (0, Address), (64, Address), 32 }),
/* 07 */        (IO, new IA[] { 13, 1 }),
/* 08 */        (IN, new IA[] { 13, (0x6f, Address) }),
/* 09 */        (CPUID, new IA[] { (0x7f, Address) }),
/* 0a */        (CALL, new IA[] { (1, Function) }),
/* 0b */        (CALL, new IA[] { (1, Function) })
            );

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");
            
            ConsoleExtensions.HexDump(proc.ToBytes());

            ReadKey(true);
        }
    }
}
