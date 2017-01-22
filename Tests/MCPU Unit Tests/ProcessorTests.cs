using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

using MCPU.Compiler;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class ProcessorTests
        : Commons
    {
        internal Processor proc;
        internal StreamWriter wr;


        public MCPUProcessingException ExpectError(string instr) => Throws<MCPUProcessingException>(() => Execute(instr));

        public void Execute(string instr) => MCPUCompiler.Compile(instr).Match(res => proc.ProcessWithoutReset(res.Instructions),
                                                                               exc => {
                                                                                   throw exc;
                                                                               });

        public string ReadProcessorOutput(string instr)
        {
            Execute(@"
    .main
    .kernel
    MOV [10h] 42
    LEA [0] [10h]
");
            wr.BaseStream.Position = 0;

            using (StreamReader rd = new StreamReader(wr.BaseStream))
                return rd.ReadToEnd();
        }

        public void IsValue(int addr, dynamic val) => IsTrue(proc[addr] == (int)val);

        public void AreValues(params (int, dynamic)[] conditions)
        {
            foreach ((int, dynamic) cond in conditions)
                IsValue(cond.Item1, cond.Item2);
        }

        public unsafe void RequireFloatUnion()
        {
            Random rand = new Random();

            for (int i = 0; i < 0x40; i++)
            {
                float f = (float)(rand.NextDouble() / short.MaxValue * rand.Next());
                FloatIntUnion un = f;
                int val = *((int*)&f);

                if (val != un)
                    Skip(); // skip this test case
            }
        }

        [TestInitialize]
        public override void Test_Init()
        {
            wr?.BaseStream?.Dispose();
            wr?.Dispose();
            wr = new StreamWriter(new MemoryStream());

            proc?.Dispose();
            proc = new Processor(0x800, 0x100, unchecked((int)0xdeadbeafu));
            proc.OnError += (p, ex) => {
                throw ex;
            };
            proc.StandardOutput = null; //  wr;
        }

        [TestMethod]
        public void Test_01()
        {
            Execute(@"
    .main
    MOV [10h] 42
    MOV [1] 315
    MOV [2] 5
    MOV [0b0101] -1
    MOV [0o10] 88
");
            IsValue(0x10, 42);
            IsValue(1, 315);
            IsValue(2, 5);
            IsValue(5, -1);
            IsValue(8, 88);
        }

        [TestMethod]
        public void Test_02()
        {
            Execute(@"
    .main
    MOV [2] 5
    MOV [[2]] -1
");
            IsValue(2, 5);
            IsValue(5, -1);
            IsValue(proc[2], -1);
        }

        [TestMethod]
        public void Test_03()
        {
            const int base_addr = 0;
            const int targ_addr = 0x315;
            const int count = 0x100;

            for (int i = 0; i < count; i++)
                proc[i + base_addr] = (i << 24)
                                    ^ (~i << 16)
                                    ^ (-i << 8)
                                    ^ i;

            Execute($@"
    .main
    COPY [{base_addr}] [{targ_addr:x}h] {count:x}h
");
            for (int i = 0; i < count; i++)
                IsTrue(proc[i + base_addr] == proc[i + targ_addr]);
        }

        [TestMethod]
        public void Test_04()
        {
            Execute(@"
    .main
    .kernel
    MOV [2] 5
    MOV k[12h] 42
    MOV [3] baaaaaadh
    LEA [4] [3]
    MOV k[[14h]] -1
");
            IsValue(2, 42);
            IsValue(3, -1);
        }

        [TestMethod]
        public void Test_05()
        {
            Execute(@"
func kimv
    ; kimv a b  is equivalent with
    ; mov [a] [k[b]]

    MOV [100h] k[$1]
    MOV [$0] [[100h]]
end func

    .main
    .kernel
    MOV k[17h] 5
    MOV [5] 42
    CALL kimv 3 17h
");
            IsValue(3, 42);
            IsValue(5, 42);
            IsValue(7, 5);
        }

        [TestMethod]
        public void Test_06()
        {
            Execute(@"
    .main
    MOV [0] 42
    JMPREL 2
    MOV [0] -1
    HALT
    MOV [0] 315
");
            IsValue(0, 42);
        }

        [TestMethod]
        public void Test_07()
        {
            proc.StandardOutput = null;
            proc.SetIOExternally(5, 12);

            Execute(@"
    .main
    IO 5 1
    IO 7 0
    MOV [10] 3
    IN 5 [2]
    OUT 7 [10]
");
            IsTrue(proc.IO[7] == (IODirection.Out, 3));
            IsValue(2, 12);
        }

        [TestMethod]
        public void Test_08()
        {
            Execute(@"
    .main
    .kernel
    CMP 42
    GETFLAGS [0]
    MOV [1] 42.0
    FDIV [1] 0f
    FCMP [1] -7.5
    GETFLAGS [1]
");
            IsValue(0, StatusFlags.Unary | StatusFlags.Zero1 | StatusFlags.Lower);
            IsValue(1, StatusFlags.Float | StatusFlags.Greater | StatusFlags.Infinity1 | StatusFlags.Sign2);
        }

        [TestMethod]
        public void Test_09()
        {
            Execute(@"
func f1
    CLEARFLAGS
    HALT
end func

    .main
    CMP -1 42
    CALL f1
");
            IsTrue(proc.Flags == StatusFlags.Empty);
        }

        [TestMethod]
        public void Test_10()
        {
            Execute(@"
    .main
    .kernel
    LEA [5] k[2]
    MOV [3] 42
    LEA [4] [3]
    MOV k[[14h]] -1
");
            IsValue(5, 2);
            IsValue(4, 0x13);
            IsValue(3, -1);
        }

        [TestMethod]
        public void Test_11() => ExpectError(@"
    .main
    MOV [2] 42
    DIV [2] 0
");

        [TestMethod]
        public void Test_12() => ExpectError(@"
    .main
    MOV [2] 0
    MOD [2] 0
");

        [TestMethod]
        public void Test_13() => ExpectError(@"
    .main
    MOV k[2] 42
");

        [TestMethod]
        public void Test_14()
        {
            // some arithmetic '''hardcore''' testing
            Execute(@"
    .main
    MOV [0] 7fffffffh
    ADD [0] 80000000h
    
    MOV [1] -7fffffffh
    SUB [1] 80000000h
    
    MOV [2] 42000315h
    MOV [3] [2]
    SHL [3] 8
    SHR [2] 24
    XOR [2] [3]
    
    MOV [3] 2
    POW [3] 3
    FAC [3]

    MOV [4] -1
    BOOL [4] [4]
    NOT [4]
    NEG [4]
");
            AreValues(
                (0, 0xffffffff),
                (1, 0x00000001),
                (2, 0x00031542),
                (3, 0x000013b0),
                (4, 0x00000002)
            );
        }

        [TestMethod]
        public void Test_15()
        {
            Execute(@"
    .main
    MOV [5] 42
    MOV [3] 6
    MOV [[3]] 315h
    SWAP [[3]] [5]
    SWAP [3] [5]
");
            AreValues(
                (3, 0x315),
                (5, 6),
                (6, 42)
            );
        }

        [TestMethod]
        public void Test_16()
        {
            Execute(@"
    .main
    CPUID [3]
");
            IsValue(3, proc.CPUID);
        }

        [TestMethod]
        public void Test_17()
        {
            int duration = 400;
            Stopwatch sw = new Stopwatch();

            sw.Start();

            Execute($@"
    .main
    WAIT {duration}
");
            sw.Stop();

            IsTrue(sw.ElapsedMilliseconds >= duration);
        }

        [TestMethod]
        public void Test_18()
        {
            Execute($@"
func f1
    CMP -1 42
    RESET
end func

    .main
    .kernel
    MOV k[10h] 88
    OUT 17 5
    CALL f1
    DIV [0] 0
");
            IsTrue(proc.Instructions.Length == 0);
            IsTrue(proc.CallStack.Length == 0);
            IsTrue(proc.Flags == StatusFlags.Empty);
            IsTrue(proc.InformationFlags == InformationFlags.Empty);
            IsValue(0, 0);
        }

        [TestMethod]
        public void Test_19()
        {
            Execute($@"
    .main
    .kernel
    MOV [1] 3
    MOV [2] 315
    MOV [3] 42
    PUSH [[1]]
    PUSH [1]
    PUSH [2]
    SSWAP
    POP [4]
    DECR [4]
    POP [3]
    POP [[4]]
");
            AreValues(
                (1, 3),
                (2, 42),
                (3, 315),
                (4, 2)
            );
        }

        [TestMethod]
        public void Test_20()
        {
            Execute($@"
    .main
    .kernel
    MOV k[2] deadc000h
    PUSHF
    PUSH 42
    SSWAP
    CLEARFLAGS
    PEEK [3]
    POPF
");
            IsTrue(proc.StackSize == 1);
            IsTrue(proc[3] != 0);
        }

        [TestMethod]
        public void Test_21()
        {
            RequireFloatUnion();
            Execute(@"
    .main
    MOV [2] 42.0
    MOV [3] 315
    FICAST [4] [2]
    IFCAST [5] [3] 
");
            AreValues(
                (2, (FloatIntUnion)42f),
                (3, 315),
                (4, 42),
                (5, (FloatIntUnion)315f)
            );
        }

        [TestMethod]
        public void Test_22()
        {
            RequireFloatUnion();
            Execute($@"
    .main
    MOV [2] 42.0
    MOV [3] 3.15e+2
    {string.Join("\n    ", from i in Enumerable.Range(10, 14) select $"MOV [{i}] [2]")}

    FADD [10] [3]
    FSUB [11] [3]
    FMUL [12] [3]
    FDIV [13] [3]
    FMOD [14] [3]
    FNEG [15]
    FINV [16]
    FSQRT [17]
    FROOT [18] [3]
    FLOG [19] [3]
    FLOGE [20]
    FEXP [21]
    FPOW [22] [3]
    FMIN [23] [3]
    FMAX [24] [3]
");
            float[] result = {
                357,
                -273,
                13230,
                0.1333333333333330f,
                42,
                -42,
                0.0238095238095238f,
                6.4807406984078600f,
                1.0119362935394700f,
                0.6497387956575900f,
                3.7376696182833700f,
                1739274941520500000,
                float.PositiveInfinity,
                42,
                315
            };

            for (int i = 0; i < 14; i++)
                IsValue(10 + i, (FloatIntUnion)result[i]);
        }

        [TestMethod]
        public void Test_23()
        {
            for (int i = 0; i < 0xff; i++)
                proc[i] = i + 1;

            const int offs = 5;
            const int size = 42;

            Execute($@"
    .main
    .kernel
    CLEAR [{offs}] {size}
");
            for (int i = 0; i < 0xff; i++)
                IsValue(i, (i - offs < size) && (i >= offs) ? 0 : i + 1);
        }
    }
}
