using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MCPU.Compiler;
using MCPU;

namespace MCPU.MCPUPP.Compiler
{
    public unsafe class MCPUPPCompiler
        : IDisposable
    {
        public const string FUNCTION_PREFIX = "function__";
        public const string MAIN_FUNCTION_NAME = FUNCTION_PREFIX + "mcpuppmain";
        public const int MIN_PROC_SZ = 0x200;

        private int idc = 0;

        /// <summary>
        /// The MCPU Processor, on which the current compiler instance relies
        /// </summary>
        public Processor Processor { private set; get; }
        /// <summary>
        /// The size of the memory segment reserved for global variables
        /// </summary>
        public int GlobalSectionOffset { private set; get; }


        /// <summary>
        /// Destructs the current MCPU++ Compiler instance
        /// </summary>
        ~MCPUPPCompiler() => Dispose();

        /// <summary>
        /// Disposes the current compiler instance and relases all resources
        /// </summary>
        public void Dispose() => Processor = null;

        /// <summary>
        /// Creates a new MCPU++ Compiler instance based on the given processor
        /// </summary>
        /// <param name="p">MCPU Processor</param>
        public MCPUPPCompiler(Processor p)
        {
            if (p.Size <= MIN_PROC_SZ)
                throw new MCPUPPCompilerException($"The minimum processor userspace memory segment size must be {MIN_PROC_SZ * 4} bytes, to compile and execute a MCPU++-application");

            Processor = p;
            Processor.OnDisposed += _ => Dispose();
        }

        /// <summary>
        /// Returns a unique ID, which can be used for labels etc.
        /// </summary>
        /// <returns>Unique ID</returns>
        public string GetUniqueID() => $"{new Guid():N}_{idc++:x}";

        public string GenerateFunctionCall(FunctionCallInformation nfo, params InstructionArgument[] argv)
        {
            string args = string.Join(" ", from arg in argv select arg.ToShortString());
            string call_id = $"_call_{GetUniqueID()}_{nfo.Name}";
            string lab_bef = "before" + call_id;
            string lab_aft = "after" + call_id;

            return nfo.Name == MAIN_FUNCTION_NAME ? $@"
    .main
    .kernel
    clear [0] 100h
    mov [feh] 100h
    mov [ffh] {nfo.LocalSize:x}h
    mov [fch] {GlobalSectionOffset:x}h
    call {MAIN_FUNCTION_NAME} {args}
    halt
" : $@"
    pushf
    mov [f7h] 0
{lab_bef}:
    push [[f7h]]
    incr [f7h]
    cmp [f7h] [f9h]
    jle {lab_bef}
    
";
        }
    }

    public struct FunctionCallInformation
    {
        public string Name { set; get; }
        public int LocalSize { set; get; }
    }
}
