using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCPU
{
    using Compiler;
    
    public static class Program
    {
        public static void Main(string[] args)
        {
            var res = MCPUCompiler.Precompile(@"
FUNC myfunc
    ADD [$0] 42
END FUNC
    MOV [0] 0
    MOV [1] 315
    MOV [2] -43
    CALL myfunc 0
    CALL myfunc 1
    CALL myfunc 2
    KERNEL 1
    SYSCALL 1
    KERNEL 0
");
        }
    }
}
