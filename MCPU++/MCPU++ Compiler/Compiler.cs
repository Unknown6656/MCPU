using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MCPU.Compiler;
using MCPU;

namespace MCPU.MCPUPP.Compiler
{
    public static class Compiler
    {

        public static Instruction[] GenerateFunctionCall(FunctionCallInformation nfo)
        {

        }
    }

    public struct FunctionCallInformation
    {
        public string Name { set; get; }
        public int LocalCount { set; get; }
    }
}
