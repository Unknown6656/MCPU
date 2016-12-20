using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace MCPU
{
    using Compiler;
    
    public static class Program
    {
        public static void Main(string[] args)
        {
            var proc = new Processor(3);
            var res = MCPUCompiler.Compile(@"
FUNC decr1
    SUB [$0] 1
END FUNC

FUNC add42
    ADD [$0] 0x43
    CALL decr1 $0
END FUNC

.MAIN
    .KERNEL

    JMP topkek

    MOV [0] 0
    MOV [1] 0x315
    MOV [2] -0x43
    CALL add42 0
    CALL add42 1
    CALL add42 2
topekek:
    SYSCALL 1
    .USER
");
            int ln = 0;
            foreach (Instruction ins in res)
                WriteLine($"0x{ln++:x4} >  {ins}");

            proc.OnError += (p, ex) => {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            };
            proc.Process(res);


            ReadKey(true);
        }
    }
}
