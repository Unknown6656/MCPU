using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCPU
{
    using static System.Console;
    
    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            Processor proc = new Processor(315);
            (IODirection, byte) p3 = proc.IO[2];

            p


            ReadKey(true);
        }
    }
}
