using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    /// <summary>
    /// Contains all known OP codes
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode, Serializable, GeneratedCode("Visual Studio T4 Template Generator", "15.0")]
	public static class OPCodes
	{
        /// <summary>
        /// Represents the OP code "NOP" (0x00000000)
        /// </summary>
        public static nop NOP { get; }           = new nop();          // [mcpu.corelib] MCPU.NOP
        /// <summary>
        /// Represents the OP code "HALT" (0x00000001)
        /// </summary>
        public static halt HALT { get; }         = new halt();         // [mcpu.corelib] MCPU.HALT
        /// <summary>
        /// Represents the OP code "JMP" (0x00000002)
        /// </summary>
        public static jmp JMP { get; }           = new jmp();          // [mcpu.corelib] MCPU.JMP
        /// <summary>
        /// Represents the OP code "JMPREL" (0x00000003)
        /// </summary>
        public static jmprel JMPREL { get; }     = new jmprel();       // [mcpu.corelib] MCPU.JMPREL
        /// <summary>
        /// Represents the OP code "ABK" (0x00000004)
        /// </summary>
        public static abk ABK { get; }           = new abk();          // [mcpu.corelib] MCPU.ABK
        /// <summary>
        /// Represents the OP code "SYSCALL" (0x00000005)
        /// </summary>
        public static syscall SYSCALL { get; }   = new syscall();      // [mcpu.corelib] MCPU.SYSCALL
        /// <summary>
        /// Represents the OP code "CALL" (0x00000006)
        /// </summary>
        public static call CALL { get; }         = new call();         // [mcpu.corelib] MCPU.CALL
        /// <summary>
        /// Represents the OP code "RET" (0x00000007)
        /// </summary>
        public static ret RET { get; }           = new ret();          // [mcpu.corelib] MCPU.RET
        /// <summary>
        /// Represents the OP code "COPY" (0x00000008)
        /// </summary>
        public static copy COPY { get; }         = new copy();         // [mcpu.corelib] MCPU.COPY
        /// <summary>
        /// Represents the OP code "CLEAR" (0x00000009)
        /// </summary>
        public static clear CLEAR { get; }       = new clear();        // [mcpu.corelib] MCPU.CLEAR
	}
}