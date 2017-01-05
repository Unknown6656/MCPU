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
            Processor proc = new Processor(64, 64, -559038737);
            
            proc.OnError += (p, ex) => {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            };
            
            var res = MCPUCompiler.Compile(@"
func f1
    call f2
end func

func f2
    .kernel
    jmp dump
    nop
    nop
    nop
    nop
dump:
    syscall 1
end func

    .main
    call f1
");
            var instr = res.AsA.Instructions;

            foreach (var i in instr)
                WriteLine(i);

            ConsoleExtensions.HexDump(Instruction.SerializeMultiple(instr));

            proc.ProcessWithoutReset(instr);

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");
            
            ReadKey(true);
        }
    }
}
