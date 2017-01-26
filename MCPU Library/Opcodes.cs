using System.Collections.Generic;
using System.Linq;
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

    [OPCodeNumber(0x0001), SpecialIPHandling, Keyword]
    public sealed class halt
        : OPCode
    {
        public halt()
            : base(0, (p, _) => p.Halt())
        {
        }
    }

    #region 00002...0003 BASIC JUMP

    [OPCodeNumber(0x0002), SpecialIPHandling]
    public sealed class jmp
        : OPCode
    {
        public jmp()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

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

    #endregion
    #region 00004...0007 FUNCTION CALLING

    [OPCodeNumber(0x0004), RequiresPrivilege]
    public sealed class abk
        : OPCode
    {
        public abk()
            : base(0, (p, _) => Processor.__syscalltable[-1](p, _))
        {
        }
    }
    
    [OPCodeNumber(0x0005), RequiresPrivilege, Keyword]
    public sealed class syscall
        : OPCode
    {
        public syscall()
            : base(1, (p, _) => {
                AssertConstant(0, _); // lift this restriction in the future ?

                p.Syscall(p.TranslateConstant(_[0]), _.Skip(1).ToArray());
            })
        {
        }
    }

    [OPCodeNumber(0x0006), SpecialIPHandling, Keyword]
    public sealed unsafe class call
        : OPCode
    {
        public call()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);
                // cannot be 'AssertFunction', as this would crash with the look-ahead bug
                // see the following issue: https://github.com/Unknown6656/MCPU/issues/30

                FunctionCall call = new FunctionCall
                {
                    SavedFlags = p.Flags,
                    ReturnAddress = p.IP + 1,
                    Arguments = new int[_.Length - 1],
                };
        
                for (int i = 0, l = _.Length - 1; i < l; i++)
                    call.Arguments[i] = _[i + 1].IsInstructionSpace ? (int)_[i + 1] : p.TranslateConstant(_[i + 1]);

                p.PushCall(call);
                p.MoveTo(_[0]);
                p.Flags = StatusFlags.Empty;
            })
        {
        }
    }

    [OPCodeNumber(0x0007), SpecialIPHandling, Keyword]
    public sealed class ret
        : OPCode
    {
        public ret()
            : base(0, (p, _) => {
                FunctionCall call = p.PopCall();

                p.Flags = call.SavedFlags;
                p.MoveTo(call.ReturnAddress);
            })
        {
        }
    }

    #endregion
    #region 0008...0009 BASIC REGION OPERATIONS

    [OPCodeNumber(0x0008)]
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

    [OPCodeNumber(0x0009)]
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

    #endregion
    #region 000a...000c I/O OPERATIONS

    [OPCodeNumber(0x000a)]
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

    [OPCodeNumber(0x000b)]
    public sealed unsafe class @in
        : OPCode
    {
        public @in()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertAddress(1, _);
                
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

    #endregion
    #region 000d...000f FLAGS OPERATIONS

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

    #endregion
    #region 000d...000f BASIC MOVE OPERATIONS

    [OPCodeNumber(0x0010)]
    public sealed unsafe class mov
        : OPCode
    {
        public mov()
            : base(2, (p, _) => {
                AssertAddress(0, _);
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

    #endregion
    #region 0012...0028 INTEGER ARITHMETICS

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
    public sealed class fac
        : ArithmeticUnaryOPCode
    {
        public fac()
            : base(a => {
                int r = 1;

                for (int i = 2; i < a; i++)
                    r *= i;

                return r;
            })
        {
        }
    }

    [OPCodeNumber(0x0027)]
    public sealed class incr
        : ArithmeticUnaryOPCode
    {
        public incr()
            : base(a => a + 1)
        {
        }
    }

    [OPCodeNumber(0x0028)]
    public sealed class decr
        : ArithmeticUnaryOPCode
    {
        public decr()
            : base(a => a - 1)
        {
        }
    }

    #endregion
    #region 0029...0033 COMPARISON + CONDITIONAL JUMP

    [OPCodeNumber(0x0029)]
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

    [OPCodeNumber(0x002a), SpecialIPHandling]
    public sealed class jle
        : OPCode
    {
        public jle()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);
                
                if (p.Flags.HasFlag(StatusFlags.Lower | StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002b), SpecialIPHandling]
    public sealed class jl
        : OPCode
    {
        public jl()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Lower))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002c), SpecialIPHandling]
    public sealed class jge
        : OPCode
    {
        public jge()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Greater | StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002d), SpecialIPHandling]
    public sealed class jg
        : OPCode
    {
        public jg()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Greater))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002e), SpecialIPHandling]
    public sealed class je
        : OPCode
    {
        public je()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x002f), SpecialIPHandling]
    public sealed class jne
        : OPCode
    {
        public jne()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (!p.Flags.HasFlag(StatusFlags.Equal))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0030), SpecialIPHandling]
    public sealed class jz
        : OPCode
    {
        public jz()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);
                
                if (p.Flags.HasFlag(StatusFlags.Zero2) & (p.Flags.HasFlag(StatusFlags.Unary) | p.Flags.HasFlag(StatusFlags.Zero1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0031), SpecialIPHandling]
    public sealed class jnz
        : OPCode
    {
        public jnz()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Zero2) | (p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.Zero1)))
                    p.MoveNext();
                else
                    p.MoveTo(_[0]);
            })
        {
        }
    }

    [OPCodeNumber(0x0032), SpecialIPHandling]
    public sealed class jneg
        : OPCode
    {
        public jneg()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Sign2) & (p.Flags.HasFlag(StatusFlags.Unary) | p.Flags.HasFlag(StatusFlags.Sign1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0033), SpecialIPHandling]
    public sealed class jpos
        : OPCode
    {
        public jpos()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Sign2) | (p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.Sign1)))
                    p.MoveNext();
                else
                    p.MoveTo(_[0]);
            })
        {
        }
    }

    #endregion
    #region 0034...003b <<unassigned>>
    #endregion

    [OPCodeNumber(0x003c)]
    public sealed unsafe class swap
        : OPCode
    {
        public swap()
            : base(2, (p, _) => {
                AssertAddress(0, _);
                AssertAddress(1, _);

                int* addr1 = p.TranslateAddress(_[0]);
                int* addr2 = p.TranslateAddress(_[1]);

                int tmp = *addr1;

                *addr1 = *addr2;
                *addr2 = tmp;
            })
        {
        }
    }

    [OPCodeNumber(0x003d)]
    public sealed unsafe class cpuid
        : OPCode
    {
        public cpuid()
            : base(1, (p, _) => {
                AssertAddress(0, _);

                *p.TranslateAddress(_[0]) = p.CPUID;
            })
        {
        }
    }

    [OPCodeNumber(0x003e), Keyword]
    public sealed class wait
        : OPCode
    {
        public wait()
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                p.Sleep(p.TranslateConstant(_[0]));
            })
        {
        }
    }

    [OPCodeNumber(0x003f), RequiresPrivilege, Keyword]
    public sealed class reset
        : OPCode
    {
        public reset()
            : base(0, (p, _) => p.Reset())
        {
        }
    }

    #region 0040...0049 STACK OPERATIONS
    
    [OPCodeNumber(0x0040), RequiresPrivilege]
    public sealed class push
        : OPCode
    {
        public push()
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                int val = p.TranslateConstant(_[0]);

                p.Push(val);
            })
        {
        }
    }

    [OPCodeNumber(0x0041), RequiresPrivilege]
    public sealed unsafe class pop
        : OPCode
    {
        public pop()
            : base(1, (p, _) => {
                AssertAddress(0, _);

                int val = p.Pop();

                *p.TranslateAddress(_[0]) = val;
            })
        {
        }
    }

    [OPCodeNumber(0x0042), RequiresPrivilege]
    public sealed unsafe class peek
        : OPCode
    {
        public peek()
            : base(1, (p, _) => {
                AssertAddress(0, _);
                
                int val = p.Peek();

                *p.TranslateAddress(_[0]) = val;
            })
        {
        }
    }

    [OPCodeNumber(0x0043), RequiresPrivilege]
    public sealed class sswap
        : OPCode
    {
        public sswap()
            : base(0, (p, _) => {
                int val1 = p.Pop();
                int val2 = p.Pop();

                p.Push(val1);
                p.Push(val2);
            })
        {
        }
    }

    [OPCodeNumber(0x0044), RequiresPrivilege]
    public sealed class pushf
        : OPCode
    {
        public pushf()
            : base(0, (p, _) => p.Push((int)p.Flags))
        {
        }
    }

    [OPCodeNumber(0x0045), RequiresPrivilege]
    public sealed class popf
        : OPCode
    {
        public popf()
            : base(0, (p, _) => p.Flags = (StatusFlags)p.Pop())
        {
        }
    }

    [OPCodeNumber(0x0046), RequiresPrivilege]
    public sealed class peekf
        : OPCode
    {
        public peekf()
            : base(0, (p, _) => p.Flags = (StatusFlags)p.Peek())
        {
        }
    }

    [OPCodeNumber(0x0047), RequiresPrivilege]
    public sealed class pushi
        : OPCode
    {
        public pushi()
            : base(0, (p, _) => p.Push(p.IP))
        {
        }
    }

    [OPCodeNumber(0x0048), RequiresPrivilege]
    public sealed class popi
        : OPCode
    {
        public popi()
            : base(0, (p, _) => p.IP = p.Pop())
        {
        }
    }

    [OPCodeNumber(0x0049), RequiresPrivilege]
    public sealed class peeki
        : OPCode
    {
        public peeki()
            : base(0, (p, _) => p.IP = p.Peek())
        {
        }
    }

    #endregion
    #region 004a...004f <<unassigned>>
    #endregion
    #region 0050...006e FLOATING POINT OPERATIONS

    [OPCodeNumber(0x0050)] // float -> int cast
    public sealed unsafe class ficast
        : OPCode
    {
        public ficast()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(1, _);
                AssertAddress(0, _);

                *p.TranslateAddress(_[0]) = (int)p.TranslateFloatConstant(_[1]);
            })
        {
        }
    }

    [OPCodeNumber(0x0051)] // int -> float cast
    public sealed unsafe class ifcast
        : OPCode
    {
        public ifcast()
            : base(2, (p, _) => {
                AssertNotInstructionSpace(1, _);
                AssertAddress(0, _);

                *p.TranslateFloatAddress(_[0]) = (float)p.TranslateConstant(_[1]);
            })
        {
        }
    }

    [OPCodeNumber(0x0052)]
    public sealed class fadd
        : FloatingPointArithmeticBinaryOPCode
    {
        public fadd()
            : base((f, g) => f + g)
        {
        }
    }

    [OPCodeNumber(0x0053)]
    public sealed class fsub
        : FloatingPointArithmeticBinaryOPCode
    {
        public fsub()
            : base((f, g) => f - g)
        {
        }
    }

    [OPCodeNumber(0x0054)]
    public sealed class fmul
        : FloatingPointArithmeticBinaryOPCode
    {
        public fmul()
            : base((f, g) => f * g)
        {
        }
    }

    [OPCodeNumber(0x0055)]
    public sealed class fdiv
        : FloatingPointArithmeticBinaryOPCode
    {
        public fdiv()
            : base((f, g) => {
                if (g == 0)
                    return f == 0 ? float.NaN : f * float.PositiveInfinity;
                else
                    return f / g;
            })
        {
        }
    }

    [OPCodeNumber(0x0056)]
    public sealed class fmod
        : FloatingPointArithmeticBinaryOPCode
    {
        public fmod()
            : base((f, g) => {
                if (g == 0)
                    return f == 0 ? float.NaN : f * float.PositiveInfinity;
                else
                    return f % g;
            })
        {
        }
    }

    [OPCodeNumber(0x0057)]
    public sealed class fneg
        : FloatingPointArithmeticUnaryOPCode
    {
        public fneg()
            : base(f => -f)
        {
        }
    }

    [OPCodeNumber(0x0058)]
    public sealed class finv
        : FloatingPointArithmeticUnaryOPCode
    {
        public finv()
            : base(f => 1 / f)
        {
        }
    }

    [OPCodeNumber(0x0059)]
    public sealed class fsqrt
        : FloatingPointArithmeticUnaryOPCode
    {
        public fsqrt()
            : base(f => (float)Sqrt(f))
        {
        }
    }

    [OPCodeNumber(0x005a)]
    public sealed class froot
        : FloatingPointArithmeticBinaryOPCode
    {
        public froot()
            : base((f, g) => (float)Pow(f, 1 / g))
        {
        }
    }

    [OPCodeNumber(0x005b)]
    public sealed class flog
        : FloatingPointArithmeticBinaryOPCode
    {
        public flog()
            : base((f, g) => (float)Log(f, g))
        {
        }
    }

    [OPCodeNumber(0x005c)]
    public sealed class floge
        : FloatingPointArithmeticUnaryOPCode
    {
        public floge()
            : base(f => (float)Log(f))
        {
        }
    }

    [OPCodeNumber(0x005d)]
    public sealed class fexp
        : FloatingPointArithmeticUnaryOPCode
    {
        public fexp()
            : base(f => (float)Exp(f))
        {
        }
    }

    [OPCodeNumber(0x005e)]
    public sealed class fpow
        : FloatingPointArithmeticBinaryOPCode
    {
        public fpow()
            : base((f, g) => (float)Pow(f, g))
        {
        }
    }

    [OPCodeNumber(0x005f)]
    public sealed class ffloor
        : FloatingPointArithmeticUnaryOPCode
    {
        public ffloor()
            : base(f => (float)Floor(f))
        {
        }
    }

    [OPCodeNumber(0x0060)]
    public sealed class fceil
        : FloatingPointArithmeticUnaryOPCode
    {
        public fceil()
            : base(f => (float)Ceiling(f))
        {
        }
    }

    [OPCodeNumber(0x0061)]
    public sealed class fround
        : FloatingPointArithmeticUnaryOPCode
    {
        public fround()
            : base(f => (float)Round(f))
        {
        }
    }

    [OPCodeNumber(0x0062)]
    public sealed class fmin
        : FloatingPointArithmeticBinaryOPCode
    {
        public fmin()
            : base((f, g) => Min(f, g))
        {
        }
    }

    [OPCodeNumber(0x0063)]
    public sealed class fmax
        : FloatingPointArithmeticBinaryOPCode
    {
        public fmax()
            : base((f, g) => Max(f, g))
        {
        }
    }

    [OPCodeNumber(0x0064)]
    public sealed class fsign
        : FloatingPointArithmeticUnaryOPCode
    {
        public fsign()
            : base(f => Sign(f))
        {
        }
    }

    [OPCodeNumber(0x0065)]
    public sealed class fsin
        : FloatingPointArithmeticUnaryOPCode
    {
        public fsin()
            : base(f => (float)Sin(f))
        {
        }
    }

    [OPCodeNumber(0x0066)]
    public sealed class fcos
        : FloatingPointArithmeticUnaryOPCode
    {
        public fcos()
            : base(f => (float)Cos(f))
        {
        }
    }

    [OPCodeNumber(0x0067)]
    public sealed class ftan
        : FloatingPointArithmeticUnaryOPCode
    {
        public ftan()
            : base(f => (float)Tan(f))
        {
        }
    }

    [OPCodeNumber(0x0068)]
    public sealed class fsinh
        : FloatingPointArithmeticUnaryOPCode
    {
        public fsinh()
            : base(f => (float)Sinh(f))
        {
        }
    }

    [OPCodeNumber(0x0069)]
    public sealed class fcosh
        : FloatingPointArithmeticUnaryOPCode
    {
        public fcosh()
            : base(f => (float)Cosh(f))
        {
        }
    }

    [OPCodeNumber(0x006a)]
    public sealed class ftanh
        : FloatingPointArithmeticUnaryOPCode
    {
        public ftanh()
            : base(f => (float)Tanh(f))
        {
        }
    }

    [OPCodeNumber(0x006b)]
    public sealed class fasin
        : FloatingPointArithmeticUnaryOPCode
    {
        public fasin()
            : base(f => (float)Asin(f))
        {
        }
    }

    [OPCodeNumber(0x006c)]
    public sealed class facos
        : FloatingPointArithmeticUnaryOPCode
    {
        public facos()
            : base(f => (float)Acos(f))
        {
        }
    }

    [OPCodeNumber(0x006d)]
    public sealed class fatan
        : FloatingPointArithmeticUnaryOPCode
    {
        public fatan()
            : base(f => (float)Atan(f))
        {
        }
    }

    [OPCodeNumber(0x006e)]
    public sealed class fatan2
        : FloatingPointArithmeticBinaryOPCode
    {
        public fatan2()
            : base((f, g) => (float)Atan2(f, g))
        {
        }
    }

    #endregion
    #region 006f...0074 FLOATING POINT COMPARISON + JUMP

    [OPCodeNumber(0x006f)]
    public sealed class fcmp
        : OPCode
    {
        public fcmp()
            : base(1, (p, _) =>
            {
                StatusFlags f = StatusFlags.Float;

                if (_.Length < 2)
                {
                    _ = new InstructionArgument[] { ((FloatIntUnion)0f, ArgumentType.Constant), _[0] };
                    f |= StatusFlags.Unary;
                }

                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                float c1 = p.TranslateFloatConstant(_[0]);
                float c2 = p.TranslateFloatConstant(_[1]);

                if (Abs(c1) < float.Epsilon) f |= StatusFlags.Zero1;
                if (Abs(c2) < float.Epsilon) f |= StatusFlags.Zero2;

                if (c1 < 0) f |= StatusFlags.Sign1;
                if (c2 < 0) f |= StatusFlags.Sign2;

                f |= c1 < c2 ? StatusFlags.Lower
                   : c1 > c2 ? StatusFlags.Greater : StatusFlags.Equal;

                if (float.IsInfinity(c1))
                    f |= StatusFlags.Infinity1;
                else if (float.IsNaN(c1))
                    f |= StatusFlags.NaN1;

                if (float.IsInfinity(c2))
                    f |= StatusFlags.Infinity2;
                else if (float.IsNaN(c2))
                    f |= StatusFlags.NaN2;

                p.Flags = f;
            })
        {
        }
    }

    [OPCodeNumber(0x0070), SpecialIPHandling]
    public sealed class jnan
        : OPCode
    {
        public jnan()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.NaN2) | (!p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.NaN1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0071), SpecialIPHandling]
    public sealed class jnnan
        : OPCode
    {
        public jnnan()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.NaN1) | (p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.NaN2)))
                    p.MoveNext();
                else
                    p.MoveTo(_[0]);
            })
        {
        }
    }

    [OPCodeNumber(0x0072), SpecialIPHandling]
    public sealed class jinf
        : OPCode
    {
        public jinf()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.Infinity2) | (!p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.Infinity1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0073), SpecialIPHandling]
    public sealed class jpinf
        : OPCode
    {
        public jpinf()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);
                
                if ((p.Flags.HasFlag(StatusFlags.Infinity2) & !p.Flags.HasFlag(StatusFlags.Sign2)) |
                    (!p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.Infinity1) & !p.Flags.HasFlag(StatusFlags.Sign1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    [OPCodeNumber(0x0074), SpecialIPHandling]
    public sealed class jninf
        : OPCode
    {
        public jninf()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                if (p.Flags.HasFlag(StatusFlags.NegativeInfinity2) | (!p.Flags.HasFlag(StatusFlags.Unary) & p.Flags.HasFlag(StatusFlags.NegativeInfinity1)))
                    p.MoveTo(_[0]);
                else
                    p.MoveNext();
            })
        {
        }
    }

    #endregion

    [OPCodeNumber(0xfffe), RequiresPrivilege, Keyword]
    public sealed unsafe class exec
        : OPCode
    {
        public exec()
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                p.ProcessNext((OPCodes.CodesByID[(ushort)p.TranslateConstant(_[0])], _.Skip(1).ToArray()), false);
            })
        {
        }
    }

    [OPCodeNumber(0xffff)]
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
