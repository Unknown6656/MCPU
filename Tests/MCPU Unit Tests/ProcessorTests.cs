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
            proc = new Processor(0x200, 0x100, -1);
            proc.StandardOutput = wr;
        }

        [TestMethod]
        public void Test_01()
        {
            proc.StandardOutput = null;

            Execute(@"
    .main
    .kernel
    MOV [10h] 42
    LEA [0] [10h]
    MOV [1] 315
    MOV [2] 5
    MOV [[2]] 88
    .user
");
            proc.Syscall(1);

            IsTrue(proc[0x10] == 42);
            IsTrue(proc[1] == 315);
            IsTrue(proc[2] == 5);
            IsTrue(proc[proc[2]] == 88);
            IsTrue(proc[0] == proc.UserToKernel(0x10));
        }

        [TestMethod]
        public void Test_02()
        {

        }
    }
}
