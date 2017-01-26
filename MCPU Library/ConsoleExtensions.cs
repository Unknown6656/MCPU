using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System;

using cmc = global::System.ConsoleColor;
using cmd = global::System.Console;

using static System.Math;

namespace MCPU
{
    /// <summary>
    /// Contains console extension methods and properties
    /// </summary>
    public static unsafe partial class ConsoleExtensions
    {
        /// <summary>
        /// Pauses the console during the execution of the given asynchronious function
        /// </summary>
        /// <param name="del">Asynchronious function</param>
        public static void Wait(Action del)
        {
            bool b = false;
            bool* p = &b;

            if (del != null)
            {
                new Thread(new ThreadStart(() => Wait(p))).Start();

                del();
            }

            *p = true;
        }

        /// <summary>
        /// Pauses the console until the value of the given boolean-pointer is true
        /// </summary>
        /// <param name="ptr">Boolean pointer</param>
        public static void Wait(bool* ptr)
        {
            const string chars = "|/-\\";
            bool vis = cmd.CursorVisible;
            int cntr = 0;

            cmd.CursorVisible = false;

            while (!*ptr)
            {
                if (cmd.CursorLeft > 0)
                    cmd.CursorLeft--;

                cmd.Write(chars[cntr]);

                cntr++;
                cntr %= chars.Length;

                Thread.Sleep(100);
            }

            cmd.CursorVisible = vis;
        }

        /// <summary>
        /// Dumps the given byte array as hexadecimal text viewer
        /// </summary>
        /// <param name="value">Byte array to be dumped</param>
        public static void HexDump(byte[] value)
        {
            if ((value?.Length ?? 0) == 0)
                return;

            char __conv(byte _) => (_ < 0x20) || ((_ >= 0x7f) && (_ <= 0xa0)) ? '.' : (char)_;

            cmc fc = cmd.ForegroundColor;
            cmc bc = cmd.BackgroundColor;
            bool cv = cmd.CursorVisible;
            int w = cmd.WindowWidth - 16;
            int l = (w - 3) / 4;
            byte b;

            l -= l % 16;

            int h = (int)Ceiling((float)value.Length / l);

            cmd.CursorVisible = false;
            cmd.WriteLine();
            cmd.ForegroundColor = cmc.White;
            cmd.BackgroundColor = cmc.Black;
            cmd.WriteLine($" {value.Length} bytes:\n\n");
            cmd.CursorLeft += 8;

            for (int j = 0; j <= l; j++)
            {
                cmd.CursorTop--;
                cmd.Write($"  {j / 16:x}");
                cmd.CursorLeft -= 3;
                cmd.CursorTop++;
                cmd.Write($"  {j % 16:x}");
            }

            cmd.WriteLine();

            fixed (byte* ptr = value)
                for (int i = 0; i < h; i++)
                {
                    cmd.Write($"{i * l:x8}:  ");

                    bool cflag;

                    for (int j = 0; (j < l) && (i * l + j < value.Length); j++)
                    {
                        b = ptr[i * l + j];
                        cflag = *((int*)(ptr + i * l + (j / 4) * 4)) != 0;

                        cmd.ForegroundColor = b == 0 ? cflag ? cmc.White : cmc.DarkGray : cmc.Yellow;
                        cmd.Write($"{b:x2} ");
                    }

                    cmd.ForegroundColor = cmc.White;
                    cmd.CursorLeft = 3 * l + 11;
                    cmd.Write("| ");

                    for (int j = 0; (j < l) && (i * l + j < value.Length); j++)
                        cmd.Write(__conv(ptr[i * l + j]));

                    cmd.Write("\n");
                }

            cmd.WriteLine();
            cmd.CursorVisible = cv;
            cmd.ForegroundColor = fc;
            cmd.BackgroundColor = bc;
        }

        /// <summary>
        /// Fetches the UTF16 byte array from the given string and dumps it as hexadecimal text viewer
        /// </summary>
        /// <param name="value">String to be dumped</param>
        public static void HexDump(string value) => HexDump(Encoding.Default.GetBytes(value));

        /// <summary>
        /// Fetches the byte array from the given string unsing the given encoding and dumps it as hexadecimal text viewer
        /// </summary>
        /// <param name="value">String to be dumped</param>
        /// <param name="enc">String encoding</param>
        public static void HexDump(string value, Encoding enc) => HexDump(enc.GetBytes(value));

        /// <summary>
        /// Dumps the given pointer as hexadecimal text viewer
        /// </summary>
        /// <param name="ptr">Byte pointer</param>
        /// <param name="bytes">Number of bytes to be dumped</param>
        public static unsafe void HexDump(void* ptr, int bytes)
        {
            byte[] targ = new byte[bytes];

            Marshal.Copy((IntPtr)ptr, targ, 0, bytes);

            HexDump(targ);
        }

        /// <summary>
        /// Prints the difference between the two given strings to the console
        /// </summary>
        /// <param name="s1">First string</param>
        /// <param name="s2">Second string</param>
        public static void Diff(string s1, string s2)
        {
            cmc fg = cmd.ForegroundColor;
            DiffEngine diff = new DiffEngine();
            CharacterDiffList src = new CharacterDiffList(s1);
            CharacterDiffList dst = new CharacterDiffList(s2);

            diff.ProcessDiff(src, dst);

            List<(cmc, string, IEnumerable<string>)> res = new List<(cmc, string, IEnumerable<string>)>();

            foreach (DiffResultSpan span in diff.DiffReport)
                switch (span.Status)
                {
                    case DiffResultSpanStatus.AddDestination:
                        res.Add((cmc.Green, ">>", from i in Enumerable.Range(span.DestinationIndex, span.Length) select dst.GetByIndex(i)));

                        break;
                    case DiffResultSpanStatus.DeleteSource:
                        res.Add((cmc.Red, "<<", from i in Enumerable.Range(span.SourceIndex, span.Length) select src.GetByIndex(i)));

                        break;
                    case DiffResultSpanStatus.NoChange:
                        res.Add((cmc.Gray, "--", from i in Enumerable.Range(span.SourceIndex, span.Length) select src.GetByIndex(i)));

                        break;
                    case DiffResultSpanStatus.Replace:
                        res.Add((cmc.Yellow, "<<", from i in Enumerable.Range(span.SourceIndex, span.Length) select src.GetByIndex(i)));
                        res.Add((cmc.Yellow, ">>", from i in Enumerable.Range(span.DestinationIndex, span.Length) select dst.GetByIndex(i)));

                        break;
                    default:
                        throw new ArgumentException();
                }

            foreach ((cmc, string, IEnumerable<string>) span in res)
            {
                cmd.ForegroundColor = span.Item1;

                foreach (string line in span.Item3)
                    cmd.WriteLine($"{span.Item2} {line}");
            }

            cmd.ForegroundColor = fg;
        }
    }
}
