using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

using MCPU.MCPUPP.Parser;
using MCPU.Compiler;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class ParserTests
        : Commons
    {
        public ParserTests()
            : base()
        {
        }

        [TestInitialize]
        public override void Test_Init()
        {
        }

        [TestMethod]
        public void Test_01()
        {
            var s1 = ShuntingYardAlgorithm.parse("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3");
            var s2 = ShuntingYardAlgorithm.shunting_yard(s1);
        }
    }
}
