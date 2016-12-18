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


            var instr = new Instruction[]
            {
/* 00 */        (JMP, new IA[] { (6, Label) }),
/* ------------ FUNCTION @ 0x01 ------------ */// FUNCTION TEST
/* 01 */        (KERNEL, new IA[] { 1 }),
/* 02 */        (SYSCALL, new IA[] { (0, Parameter) }),
/* 03 */        (KERNEL, new IA[] { 0 }),
/* 04 */        (RET, null),
/* ------------ END OF FUNCTION ------------ */
/* 05 */        (CALL, new IA[] { (1, Function), 0 }),
/* 06 */        (COPY, new IA[] { (0, Address), (64, Address), 32 }),
/* 07 */        (IO, new IA[] { 13, 1 }),
/* 08 */        (IN, new IA[] { 13, (0x6f, Address) }),
/* 09 */        (CPUID, new IA[] { (0x7f, Address) }),
/* 0a */        (MOV, new IA[] { (0x7c, Address), 0x315 }),
/* 0b */        (MOV, new IA[] { (0x7d, Address), 0x42 }),
/* 0c */        (ADD, new IA[] { (0x7c, Address), (0x7d, Address) }),
/* 0d */        (CALL, new IA[] { (1, Function), 1 }), // DEBUG
            };
            ConsoleExtensions.HexDump(Instruction.SerializeMultiple(instr));
            
            proc.ProcessWithoutReset(instr);

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");
            
            ReadKey(true);
        }
    }
}
