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
    using MCPU.Compiler;

    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            Processor proc = new Processor(128);

            proc.IO.SetValue(7, 12);
            proc.IO.SetValue(13, 5);
            proc.OnError += (p, ex) => {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            };

            for (int i = 0; i < 32; i++)
                proc[i] = i | (i << 8) | (i << 16) | (i << 24);

            int addr = 44;
            foreach (byte b in Encoding.ASCII.GetBytes("hello! top kek lulz /foo/bar/"))
                proc.UserSpace[addr++] = b;
            
            var instr = MCPUCompiler.Compile(@"
func test
    .kernel
    syscall $0
    .user
end func

    .main
    call test 0
    copy [0] [64] 32
    io 13 1
    in 13 [0x6f]
    cpuid [0x7f]
    mov [7ch] 315
    mov [7dh] 42
    add [7ch] [7dh]
    call test 1
").AsA.Instructions;
            ConsoleExtensions.HexDump(Instruction.SerializeMultiple(instr));
            
            proc.ProcessWithoutReset(instr);

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");
            
            ReadKey(true);
        }
    }
}
