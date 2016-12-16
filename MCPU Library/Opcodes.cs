using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

using static System.Math;

namespace MCPU.Instructions
{
#pragma warning disable IDE1006 // DISABLE CLASS NAMING CONVENTION WARNING (THE INSTRUCTION NAMES DO NOT FOLLOW THE PASCAL CONVENTION)

    [OPCodeNumber(0x0000)]
    public sealed class nop
        : OPCode
    {
        public nop()
            : base(0, delegate { })
        {
        }
    }

    [OPCodeNumber(0x0001), SpecialIPHandling]
    public sealed class halt
        : OPCode
    {
        public halt()
            : base(0, (p, _) => p.Halt())
        {
        }
    }

    [OPCodeNumber(0x0002), SpecialIPHandling]
    public sealed class jmp
        : OPCode
    {
        public jmp()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                p.MoveTo(_[0]);
            })
        {
        }
    }

    [OPCodeNumber(0x0003), SpecialIPHandling]
    public sealed class jmprel
        : OPCode
    {
        public jmprel()
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                p.MoveRelative(p.TranslateConstant(_[0]));
            })
        {
        }
    }

    [OPCodeNumber(0x0004), RequiresPrivilege]
    public sealed class abk
        : OPCode
    {
        public abk()
            : base(0, (p, _) => p.__syscalltable[-1](_))
        {
        }
    }

    [OPCodeNumber(0x0005), RequiresPrivilege]
    public sealed class syscall
        : OPCode
    {
        public syscall()
            : base(1, (p, _) => {
                AssertConstant(0, _);

                p.__syscalltable[p.TranslateConstant(_[0])](_.Skip(1).ToArray());
            })
        {
        }
    }

    [OPCodeNumber(0x0006), SpecialIPHandling]
    public sealed unsafe class call
        : OPCode
    {
        public call()
            : base(1, (p, _) => {
                AssertFunction(0, _);

                FunctionCall call = new FunctionCall
                {
                    ReturnAddress = p.IP + 1,
                    Arguments = new int[_.Length - 1],
                };
        
                for (int i = 0, l = _.Length - 1; i < l; i++)
                    call.Arguments[i] = *p.TranslateAddress(_[i + 1]);

                p.PushCall(call);
                p.MoveTo(_[0]);
            })
        {
        }
    }

    [OPCodeNumber(0x0007), SpecialIPHandling]
    public sealed class ret
        : OPCode
    {
        public ret()
            : base(0, (p, _) => {
                FunctionCall call = p.PopCall();
                
                p.MoveTo(call.ReturnAddress);
            })
        {
        }
    }

    [OPCodeNumber(0x0008)]
    public sealed unsafe class io
        : OPCode
    {
        public io()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                p.IO.SetDirection(p.TranslateConstant(_[0]), p.TranslateConstant(_[1]) != 0 ? IODirection.In : IODirection.Out);
            })
        {
        }
    }

    [OPCodeNumber(0x0009)]
    public sealed unsafe class copy
        : OPCode
    {
        public copy()
            : base(3, (p, _) => {
                AssertAddress(0, _);
                AssertAddress(1, _);
                AssertNotInstructionSpace(2, _);

                int* src = p.TranslateAddress(_[0]);
                int* dst = p.TranslateAddress(_[1]);
                int size = p.TranslateConstant(_[2]);
                
                for (int i = 0; i < size; i++)
                    dst[i] = src[i];
            })
        {
        }
    }

    [OPCodeNumber(0x000a)]
    public sealed unsafe class clear
        : OPCode
    {
        public clear()
            : base(2, (p, _) => {
                AssertAddress(0, _);
                AssertNotInstructionSpace(1, _);

                int* ptr = p.TranslateAddress(_[0]);
                int size = p.TranslateConstant(_[1]);

                for (int i = 0; i < size; i++)
                    ptr[i] = 0;
            })
        {
        }
    }

    [OPCodeNumber(0x000b)]
    public sealed unsafe class @in
        : OPCode
    {
        public @in()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                *p.TranslateAddress(_[1]) = p.IO[p.TranslateConstant(_[0])].Value;
            })
        {
        }
    }

    [OPCodeNumber(0x000c)]
    public sealed class @out
        : OPCode
    {
        public @out()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                p.IO.SetValue(p.TranslateConstant(_[0]), (byte)(p.TranslateConstant(_[1]) & 0xff));
            })
        {
        }
    }

    [OPCodeNumber(0x000d)]
    public sealed class clearflags
        : OPCode
    {
        public clearflags()
            : base(0, (p, _) => p.Flags = StatusFlags.Empty)
        {
        }
    }

    [OPCodeNumber(0x000e)]
    public sealed class setflags
        : OPCode
    {
        public setflags()
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                p.Flags = (StatusFlags)(p.TranslateConstant(_[0]) & 0xffff);
            })
        {
        }
    }

    [OPCodeNumber(0x000f)]
    public sealed unsafe class getflags
        : OPCode
    {
        public getflags()
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                *p.TranslateAddress(_[0]) = (int)p.Flags;
            })
        {
        }
    }

    [OPCodeNumber(0x0010)]
    public sealed unsafe class mov
        : OPCode
    {
        public mov()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                *p.TranslateAddress(_[0]) = *p.TranslateAddress(_[1]);
            })
        {
        }
    }

    [OPCodeNumber(0x0011), RequiresPrivilege]
    public sealed unsafe class lea
        : OPCode
    {
        public lea()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);
                AssertNotConstant(1, _);

                *p.TranslateAddress(_[0]) = p.GetKernelAddress(p.TranslateAddress(_[1]));
            })
        {
        }
    }

    [OPCodeNumber(0x0012)]
    public sealed class add
        : ArithmeticBinaryOPCode
    {
        public add()
            : base((a, b) => a + b)
        {
        }
    }

    [OPCodeNumber(0x0013)]
    public sealed class sub
        : ArithmeticBinaryOPCode
    {
        public sub()
            : base((a, b) => a - b)
        {
        }
    }

    [OPCodeNumber(0x0014)]
    public sealed class mul
        : ArithmeticBinaryOPCode
    {
        public mul()
            : base((a, b) => a * b)
        {
        }
    }

    [OPCodeNumber(0x0015)]
    public sealed class div
        : ArithmeticBinaryOPCode
    {
        public div()
            : base((a, b) => a / b)
        {
        }
    }

    [OPCodeNumber(0x0016)]
    public sealed class mod
        : ArithmeticBinaryOPCode
    {
        public mod()
            : base((a, b) => a % b)
        {
        }
    }

    [OPCodeNumber(0x0017)]
    public sealed class neg
        : ArithmeticUnaryOPCode
    {
        public neg()
            : base(a => -a)
        {
        }
    }

    [OPCodeNumber(0x0018)]
    public sealed class not
        : ArithmeticUnaryOPCode
    {
        public not()
            : base(a => ~a)
        {
        }
    }

    [OPCodeNumber(0x0019)]
    public sealed class or
        : ArithmeticBinaryOPCode
    {
        public or()
            : base((a, b) => a | b)
        {
        }
    }

    [OPCodeNumber(0x001a)]
    public sealed class and
        : ArithmeticBinaryOPCode
    {
        public and()
            : base((a, b) => a & b)
        {
        }
    }

    [OPCodeNumber(0x001b)]
    public sealed class xor
        : ArithmeticBinaryOPCode
    {
        public xor()
            : base((a, b) => a ^ b)
        {
        }
    }

    [OPCodeNumber(0x001c)]
    public sealed class nor
        : ArithmeticBinaryOPCode
    {
        public nor()
            : base((a, b) => ~(a | b))
        {
        }
    }

    [OPCodeNumber(0x001d)]
    public sealed class nand
        : ArithmeticBinaryOPCode
    {
        public nand()
            : base((a, b) => ~(a & b))
        {
        }
    }

    [OPCodeNumber(0x001e)]
    public sealed class nxor
        : ArithmeticBinaryOPCode
    {
        public nxor()
            : base((a, b) => ~(a ^ b))
        {
        }
    }

    [OPCodeNumber(0x001f)]
    public sealed class abs
        : ArithmeticUnaryOPCode
    {
        public abs()
            : base(a => Abs(a))
        {
        }
    }

    [OPCodeNumber(0x0020)]
    public sealed class @bool
        : ArithmeticUnaryOPCode
    {
        public @bool()
            : base(a => a != 0 ? 1 : 0)
        {
        }
    }

    [OPCodeNumber(0x0021)]
    public sealed class pow
        : ArithmeticBinaryOPCode
    {
        public pow()
            : base((a, b) => (int)Math.Pow(a, b))
        {
        }
    }

    [OPCodeNumber(0x0022)]
    public sealed class shr
        : ArithmeticBinaryOPCode
    {
        public shr()
            : base((a, b) => (int)((uint)a >> b))
        {
        }
    }

    [OPCodeNumber(0x0023)]
    public sealed class shl
        : ArithmeticBinaryOPCode
    {
        public shl()
            : base((a, b) => a << b)
        {
        }
    }

    [OPCodeNumber(0x0024)]
    public sealed class ror
        : ArithmeticBinaryOPCode
    {
        public ror()
            : base((a, b) => {
                b %= 32;

                return (a >> b) | (a << (31 - b));
            })
        {
        }
    }

    [OPCodeNumber(0x0025)]
    public sealed class rol
        : ArithmeticBinaryOPCode
    {
        public rol()
            : base((a, b) => {
                b %= 32;

                return (a << b) | (a >> (31 - b));
            })
        {
        }
    }

    [OPCodeNumber(0x0026)]
    public sealed class cmp
        : OPCode
    {
        public cmp()
            : base(1, (p, _) => {
                StatusFlags f = StatusFlags.Empty;

                if (_.Length < 2)
                {
                    _ = new InstructionArgument[] { 0, _[0] };
                    f |= StatusFlags.Unary;
                }

                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                int c1 = p.TranslateConstant(_[0]);
                int c2 = p.TranslateConstant(_[1]);

                if (c1 == 0) f |= StatusFlags.Zero1;
                if (c2 == 0) f |= StatusFlags.Zero2;

                if (c1 < 0) f |= StatusFlags.Sign1;
                if (c2 < 0) f |= StatusFlags.Sign2;

                f |= c1 < c2 ? StatusFlags.Lower
                   : c1 > c2 ? StatusFlags.Greater : StatusFlags.Equal;

                p.Flags = f;
            })
        {
        }
    }

    [OPCodeNumber(0x0027), SpecialIPHandling]
    public sealed class jle
        : OPCode
    {
        public jle()
            : base(1, (p, _) => {
                AssertLabel(0, _);
                
                if (p.Flags.HasFlag(StatusFlags.Lower | StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0028), SpecialIPHandling]
    public sealed class jl
        : OPCode
    {
        public jl()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Lower))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0029), SpecialIPHandling]
    public sealed class jge
        : OPCode
    {
        public jge()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Greater | StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002a), SpecialIPHandling]
    public sealed class jg
        : OPCode
    {
        public jg()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Greater))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002b), SpecialIPHandling]
    public sealed class je
        : OPCode
    {
        public je()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002c), SpecialIPHandling]
    public sealed class jne
        : OPCode
    {
        public jne()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (!p.Flags.HasFlag(StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002d), SpecialIPHandling]
    public sealed class jz
        : OPCode
    {
        public jz()
            : base(1, (p, _) => {
                AssertLabel(0, _);
                
                if (p.Flags.HasFlag(StatusFlags.Zero2) & (p.Flags.HasFlag(StatusFlags.Unary) | p.Flags.HasFlag(StatusFlags.Zero1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002e), SpecialIPHandling]
    public sealed class jnz
        : OPCode
    {
        public jnz()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Zero2) | (p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.Zero1)))
                    p.MoveNext();
                else
                    p.MoveTo(_[0]);
            })
        {
        }
    }

    [OPCodeNumber(0x002f), SpecialIPHandling]
    public sealed class jneg
        : OPCode
    {
        public jneg()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Sign2) & (p.Flags.HasFlag(StatusFlags.Unary) | p.Flags.HasFlag(StatusFlags.Sign1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0030), SpecialIPHandling]
    public sealed class jpos
        : OPCode
    {
        public jpos()
            : base(1, (p, _) => {
                AssertLabel(0, _);

                if (p.Flags.HasFlag(StatusFlags.Sign2) | (p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.Sign1)))
                    p.MoveNext();
                else
                    p.MoveTo(_[0]);
            })
        {
        }
    }


    [OPCodeNumber(0x00ff)]
    public sealed unsafe class kernel
        : OPCode
    {
        public kernel()
            : base(1, (p, _) => {
                AssertConstant(0, _);

                p.SetInformationFlag(InformationFlags.Elevated, _[0] != 0);
            })
        {
        }
    }

#pragma warning restore
}
