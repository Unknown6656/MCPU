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
            proc.StandardOutput = null;

            Execute(@"
    .main
    MOV [10h] 42
    MOV [1] 315
    MOV [2] 5
    MOV [0b0101] -1
    MOV [0o10] 88
");
            IsTrue(proc[0x10] == 42);
            IsTrue(proc[1] == 315);
            IsTrue(proc[2] == 5);
            IsTrue(proc[5] == -1);
            IsTrue(proc[8] == 88);
        }

        [TestMethod]
        public void Test_02()
        {
            proc.StandardOutput = null;

            Execute(@"
    .main
    MOV [2] 5
    MOV [[2]] -1
");
            IsTrue(proc[2] == 5);
            IsTrue(proc[5] == -1);
            IsTrue(proc[proc[2]] == -1);
        }

        [TestMethod]
        public void Test_03()
        {
            proc.StandardOutput = null;

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
    }
}
