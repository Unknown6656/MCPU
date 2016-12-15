using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    using static System.Console;
    
    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            Processor proc = new Processor(128);

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");

            for (int i = 0; i < 32; i++)
                proc.Push(i);

            proc.PushCall(new FunctionCall(315, 42, 0x7eadbeef));
            proc.PushCall(new FunctionCall(315, -1, 4, 2, 3, 1, 5));

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");

            var c1 = proc.PopCall();
            var c2 = proc.PopCall();

            int addr = 0;

            foreach (byte b in Encoding.ASCII.GetBytes("hello -- top kek lulz /foo/bar"))
                proc.UserSpace[addr++] = b;

            (IODirection, byte) p3 = proc.IO[2];


            
            ConsoleExtensions.HexDump(proc.ToBytes());

            ReadKey(true);
        }
    }
}
