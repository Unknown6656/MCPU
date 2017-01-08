using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

using MCPU.Compiler;
using MCPU;

namespace MCPU.MCPUPP.Compiler
{
    public unsafe class MCPUPPCompiler
        : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public const string FUNCTION_PREFIX = "function__";
        /// <summary>
        /// The name of the main (entry-point) MCPU++ function
        /// </summary>
        public const string MAIN_FUNCTION_NAME = FUNCTION_PREFIX + "mcpuppmain";
        /// <summary>
        /// The address of the MCPU++ field 'CC'
        /// </summary>
        public const int F_CC = 0xf7;
        /// <summary>
        /// The address of the MCPU++ field 'RET'
        /// </summary>
        public const int F_RET = 0xf8;
        /// <summary>
        /// The address of the MCPU++ field 'SYP'
        /// </summary>
        public const int F_SYP = 0xf9;
        /// <summary>
        /// The address of the MCPU++ field 'GOF'
        /// </summary>
        public const int F_GOF = 0xfc;
        /// <summary>
        /// The address of the MCPU++ field 'GSZ'
        /// </summary>
        public const int F_GSZ = 0xfd;
        /// <summary>
        /// The address of the MCPU++ field 'LOF'
        /// </summary>
        public const int F_LOF = 0xfe;
        /// <summary>
        /// The address of the MCPU++ field 'LSZ'
        /// </summary>
        public const int F_LSZ = 0xff;
        /// <summary>
        /// The size of the temporary value section (.temp)
        /// </summary>
        public const int TMP_SZ = 0x100;
        /// <summary>
        /// The processor's minimum userspace size (in 4-byte-blocks)
        /// </summary>
        public const int MIN_PROC_SZ = 0x200; // 2 KB

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

        /// <summary>
        /// Formats the given code snippet's indentation
        /// </summary>
        /// <param name="code">Code snippet</param>
        /// <returns>Formatted code</returns>
        public static string FormatMCPUCode(string code) => string.Join("\n", FormatMCPUCode(code.Split('\n', '\r')));

        /// <summary>
        /// Formats the given code snippet line's indentation
        /// </summary>
        /// <param name="lines">Code lines</param>
        /// <returns>Formatted code lines</returns>
        public static string[] FormatMCPUCode(params string[] lines) => FormatMCPUCode(lines as IEnumerable<string>);

        /// <summary>
        /// Formats the given code snippet line's indentation
        /// </summary>
        /// <param name="lines">Code lines</param>
        /// <returns>Formatted code lines</returns>
        public static string[] FormatMCPUCode(IEnumerable<string> lines) => (from string l in lines
                                                                             let tl = l.Trim()
                                                                             where tl.Length > 0
                                                                             let cnt = tl.Contains(MCPUCompiler.COMMENT_START) ? tl.Remove(tl.IndexOf(MCPUCompiler.COMMENT_START)).Trim() : tl
                                                                             select (cnt.EndsWith(":") ? "" : "    ") + tl).ToArray();
        
         /*
          * TODO : generate callee header and footer:
          * 
          * func __myfunc__
          *     clear [[feh]] [ffh]
          *     
          *     ...
          *     
          *     mov [f8h] <return value>
          *     ret
          * end func
          */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nfo"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        public string GenerateFunctionCall(FunctionCallInformation nfo, params InstructionArgument[] argv)
        {
            string args = string.Join(" ", from arg in argv select arg.ToShortString());
            string call_id = $"_call_{GetUniqueID()}_{nfo.Name}";
            string lab_bef = "before" + call_id;
            string lab_aft = "after" + call_id;

            return nfo.Name == MAIN_FUNCTION_NAME ? $@"
    .main
    .kernel
    clear [0] {TMP_SZ}
    mov [{F_LOF}] {TMP_SZ}
    mov [{F_LSZ}] {nfo.LocalSize}
    mov [{F_GOF}] {GlobalSectionOffset}
    call {MAIN_FUNCTION_NAME} {args}
    halt
" : $@"
    pushf
    mov [{F_CC}] 0
{lab_bef}:
    push [[{F_CC}]]
    incr [{F_CC}]
    cmp [{F_CC}] [{F_SYP}]
    jle {lab_bef}
    push [{F_SYP}]
    push [{F_LOF}]
    push [{F_LSZ}]
    add [{F_LOF}] [{F_LSZ}]
    mov [{F_LSZ}] {nfo.LocalSize}
    clear [0] {F_SYP + 1}
    call {nfo.Name} {args}
    pop [{F_LSZ}]
    pop [{F_LOF}]
    pop [{F_SYP}]
    mov [{F_CC}] [{F_SYP}]
{lab_aft}:
    pop [[{F_SYP}]]
    decr [{F_SYP}]
    cmp [{F_SYP}]
    jpos {lab_aft}
";
        }
    }

    [Serializable]
    /// <summary>
    /// Represents a MCPU++ compiler error
    /// </summary>
    public sealed class MCPUPPCompilerException
        : Exception
    {
        /// <summary>
        /// The error message
        /// </summary>
        public new string Message => base.Message;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="msg">Compiler error message</param>
        public MCPUPPCompilerException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// Represents a basic MCPU++ function call information structure
    /// </summary>
    [Serializable]
    public struct FunctionCallInformation
    {
        /// <summary>
        /// The called function's name
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// The called function's local size
        /// </summary>
        public int LocalSize { set; get; }
    }
}
