using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
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


        public void Execute(string instr) => proc.Process(MCPUCompiler.Compile(instr).AsA.Instructions);

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

        public ProcessorTests()
            : base()
        {
        }

        [TestInitialize]
        public override void Test_Init()
        {
            wr?.BaseStream?.Dispose();
            wr?.Dispose();
            wr = new StreamWriter(new MemoryStream());

            proc?.Dispose();
            proc = new Processor(0x800, 0x100, unchecked((int)0xdeadbeafu));
            proc.StandardOutput = wr;
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
            proc.IO.SetValue(5, 12);

            Execute(@"
    .main
    IO 5 1
    IO 7 0
    MOV [10] 3
    IN 5 [2]
    OUT 7 [10]
");
            // proc.Syscall(1);

            IsTrue(proc.IO[7] == (IODirection.Out, 3));
            IsValue(10, 12); // TODO : fix !!
        }

        [TestMethod]
        public void Test_08()
        {
            Execute(@"
    .main
    CMP 42 k[2]
    GETFLAGS [0]
    MOV 42.0 [1]
    FDIV [1] 0f
    SYSCALL 2 [1]
    FCMP [1] -7.5
    GETFLAGS [1]
");
            IsValue(0, StatusFlags.Greater);
            IsValue(1, StatusFlags.Float | StatusFlags.Greater | StatusFlags.Infinity1 | StatusFlags.Sign2);
        }
    }
}
