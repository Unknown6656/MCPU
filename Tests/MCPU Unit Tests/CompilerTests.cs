using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;

using MCPU.Compiler;

namespace MCPU.Testing
{
    using System.Collections.Generic;
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public sealed class CompilerTests
        : Commons
    {
        [TestInitialize]
        public override void Test_Init() => MCPUCompiler.OptimizationEnabled = false;

        [TestMethod]
        public void Test_01() => CompileExpectError(@"
    mov [0] [0]   §### ERROR
", MCPUCompiler.GetString("INSTR_OUTSIDE_FUNC"));

        [TestMethod]
        public void Test_02() => CompileExpectError(@"
    .main
    kernel 1   §### ERROR
", MCPUCompiler.GetString("DONT_USE_KERNEL"));

        [TestMethod]
        public void Test_03() => CompileExpectError(@"
func f1
    mov [3] [1]
end func
end func   §### ERROR
", MCPUCompiler.GetString("MISSING_FUNC_DECL"));

        [TestMethod]
        public void Test_04() => CompileExpectError(@"
func f1
    jmp label
label:
    add [6], 8
label:   §### ERROR
end func
", MCPUCompiler.GetString("LABEL_ALREADY_EXISTS_SP"));

        [TestMethod]
        public void Test_05() => CompileExpectError(@"
func myfunc
    NOP
end func

    .main
myfunc:   §### ERROR
    HALT
", MCPUCompiler.GetString("FUNC_ALREADY_EXISTS_SP"));

        [TestMethod]
        public void Test_06() => CompileExpectError(@"
func f1
    NOP
func f2   §### ERROR
    MOV [0] 42
end func
end func
", MCPUCompiler.GetString("FUNC_NOT_NESTED"));

        [TestMethod]
        public void Test_07() => CompileExpectError(@"
    .main
    NOP

func myfunc   §### ERROR
    MOV [0] 42
end func
", MCPUCompiler.GetString("FUNC_AFTER_MAIN"));

        [TestMethod]
        public void Test_08() => CompileExpectError(@"
func f1
    NOP
end func

func f1   §### ERROR
    MOV [0] 42
end func
", MCPUCompiler.GetString("FUNC_ALREADY_EXISTS"));

        [TestMethod]
        public void Test_09() => CompileExpectError(@"
func f1
label:
    NOP
end func

func label   §### ERROR
    MOV [0] 42
end func
", MCPUCompiler.GetString("LABEL_ALREADY_EXISTS"));

        [TestMethod]
        public void Test_10() => CompileExpectError(@"
    .main
    COMPLETEY_RANDOM_STRING [42] 315   §### ERROR
", MCPUCompiler.GetString("INSTR_NFOUND"));

        [TestMethod]
        public void Test_11() => CompileExpectError(@"
    .main
    MOV [3] ()   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_12() => CompileExpectError(@"
    .main
    //   §### ERROR
", MCPUCompiler.GetString("LINE_NPARSED"));

        [TestMethod]
        public void Test_13() => CompileExpectError(@"
    .random   §### ERROR
", MCPUCompiler.GetString("TOKEN_NOT_PARSED"));

        [TestMethod]
        public void Test_14()
        {
            MCPUCompiler.OptimizationEnabled = true;

            CompileExpectError(@"
.inline func myfunc   ;### ERROR <AFTER PRECOMPILATION>
    NOP
end func

    .main
", MCPUCompiler.GetString("INLINE_NYET_SUPP"));

        }

        [TestMethod]
        public void Test_15() => CompileExpectError(@"
func myfunc
    NOP
end func
    .kernel   §### ERROR
    .main
    halt
", MCPUCompiler.GetString("TOKEN_INSIDE_FUNC"));

        [TestMethod]
        public void Test_16() => CompileExpectError(@"
    .main
    mov [99] [4.2]   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_17() => CompileExpectError(@"
    .main
    mov +   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_18() => CompileExpectError(@"
    .main
    mov 5 kk[0]   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_19() => CompileExpectError(@"
    .main
    mov [1] ffffffffffffffffh   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_20()
        {
            MCPUCompilerResult res = Compile(@"
    .main
label1:
    NOP
label2:
    MOV [315] 42.0
");
            IsTrue(res.Labels.Length == 2);
            IsTrue(res.Functions.Length == 1);
        }

        [TestMethod]
        public void Test_21() => CompileExpectError(@"
    .main
    CMP 1
    JNZ end
    NOP
end:   §### ERROR
", MCPUCompiler.GetString("LABEL_RESV_NAME"));

        [TestMethod]
        public void Test_22() => CompileExpectError(@"
func ____main   §### ERROR
end func
    
    .main
", MCPUCompiler.GetString("FUNC_RESV_NAME"));

        [TestMethod]
        public void Test_23()
        {
            MCPUCompilerResult res = Compile(@"
func test
    NOP
    NOP
    ADD [0] 0
    NOP
end func

    .main
loop:
    JMP pool
    CALL test
    AND [4] [4]
    OR [[2]] [[2]]
    FMUL [7] 1.0
    FSUB [88] -0.0
    AND [43] ffffffffh
pool:
    JMP loop
");
            int ln = 0;
            (Instruction[], int[], Dictionary<int, (int, bool)>) optimized = MCPUCompiler.Optimize((from i in res.Instructions
                                                                                                    select (i, ln++)).ToArray());

            // TODO : Assertions ?
        }

        [TestMethod]
        public void Test_24() => CompileExpectError(@"
    .main
    mov 2   §### ERROR
", MCPUCompiler.GetString("NEED_MORE_ARGS"));

        [TestMethod]
        public void Test_25() => CompileExpectError(@"
    .main
test:
    mov [test] 315   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_26() => CompileExpectError(@"
func int_88   §### ERROR
    nop
end func

    .main
    nop
", MCPUCompiler.GetString("FUNC_RESV_NAME_INT"));

        [TestMethod]
        public void Test_27() => CompileExpectError(@"
interrupt func int_420   §### ERROR
    nop
end func

    .main
    nop
", MCPUCompiler.GetString("INVALID_INT_HANDLER_NAME"));

        [TestMethod]
        public void Test_28() => CompileExpectError(@"
interrupt func int_g   §### ERROR
    nop
end func

    .main
    nop
", MCPUCompiler.GetString("INVALID_INT_HANDLER_NAME"));

        [TestMethod]
        public void Test_29() => CompileExpectError(@"
interrupt func hello   §### ERROR
    nop
end func

    .main
    nop
", MCPUCompiler.GetString("INVALID_INT_HANDLER_NAME"));

        [TestMethod]
        public void Test_30() => Compile(@"
interrupt func int_88
    halt
end func

    .main
    int 88
");

        [TestMethod]
        public void Test_31() => Compile(@"
    .main
    .enable interrupt
    .disable interrupt
");

        [TestMethod]
        public void Test_32() => CompileExpectError(@"
    .main
    .enable topkek   §### ERROR
", MCPUCompiler.GetString("TOKEN_UNKNOWN_SWITCH"));

        [TestMethod]
        public void Test_33() => CompileExpectError(@"
    .main
    .disable topkek   §### ERROR
", MCPUCompiler.GetString("TOKEN_UNKNOWN_SWITCH"));

        [TestMethod]
        public void Test_34() => CompileExpectError(@"
    .main
    .enable   §### ERROR
", MCPUCompiler.GetString("TOKEN_NOT_ENOUGH_ARGS"));

        [TestMethod]
        public void Test_35() => CompileExpectError(@"
    .main
    .disable   §### ERROR
", MCPUCompiler.GetString("TOKEN_NOT_ENOUGH_ARGS"));

        [TestMethod]
        public void Test_36() => CompileExpectError(@"
interrupt func int_0   §### ERROR
    nop
end func

    .main
    nop
", MCPUCompiler.GetString("INVALID_INT_HANDLER_NAME"));

        [TestMethod]
        public void Test_37()
        {
            Dictionary<byte, int> interrupt_table = Compile(@"
interrupt func int_00
end func

interrupt func int_ff
end func

interrupt func int_88
end func

    .main
    nop
").InterruptTable;

            IsTrue(interrupt_table.ContainsKey(0x00));
            IsTrue(interrupt_table.ContainsKey(0xff));
            IsTrue(interrupt_table.ContainsKey(0x88));
        }

        [TestMethod]
        public void Test_38() => CompileExpectError(@"
func kek
    nop
end func

    .data
    mov [0] [0]   §### ERROR

    .main
    nop
", MCPUCompiler.GetString("INSTR_OUTSIDE_FUNC"));

        [TestMethod]
        public void Test_39() => CompileExpectError(@"
    .main
    nop
    .data   §### ERROR
", MCPUCompiler.GetString("DATA_INSIDE_FUNC"));

        [TestMethod]
        public void Test_40() => CompileExpectError(@"
    .main
    [88] = 420h   §### ERROR
", MCPUCompiler.GetString("DATA_OUTSIDE_SECTION"));

        [TestMethod]
        public void Test_41() => CompileExpectError(@"
    .data

func kek
    [88] = 420h   §### ERROR
end func

    .main
    nop
", MCPUCompiler.GetString("DATA_OUTSIDE_SECTION"));

        [TestMethod]
        public void Test_42() => CompileExpectError(@"
    [88] = 420h   §### ERROR
    .data

    .main
    nop
", MCPUCompiler.GetString("DATA_OUTSIDE_SECTION"));

        [TestMethod]
        public void Test_43() => CompileExpectError(@"
    [88] = 420h   §### ERROR
", MCPUCompiler.GetString("DATA_OUTSIDE_SECTION"));

        [TestMethod]
        public void Test_44() => Compile(@"
    .data
    [88] = 420h
    [2] = -1

func test
    nop
end func

    .data
    [77] = 315

    .main
    nop
");
    }
}
