using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace MCPU
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

                p.MoveRelative(_[0]);
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
                AssertNotInstructionSpace(0, _);

                p.__syscalltable[_[0]](_.Skip(1).ToArray());
            })
        {
        }
    }

    [OPCodeNumber(0x0006), SpecialIPHandling]
    public unsafe sealed class call
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
    public sealed unsafe class copy
        : OPCode
    {
        public copy()
            : base(3, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);
                AssertNotInstructionSpace(2, _);

                int* src = (int*)p.UserSpace + *p.TranslateAddress(_[0]);
                int* dst = (int*)p.UserSpace + *p.TranslateAddress(_[1]);
                int size = *p.TranslateAddress(_[2]);

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
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);

                int* ptr = (int*)p.UserSpace + *p.TranslateAddress(_[0]);
                int size = *p.TranslateAddress(_[1]);

                for (int i = 0; i < size; i++)
                    ptr[i] = 0;
            })
        {
        }
    }

#pragma warning restore
}
