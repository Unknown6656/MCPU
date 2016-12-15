using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    [OPCodeNumber(0x0000)]
    public sealed class NOP
        : OPCode
    {
        public NOP()
            : base(0, delegate { })
        {
        }
    }

    [OPCodeNumber(0x0001)]
    public sealed class HALT
        : OPCode
    {
        public HALT()
            : base(0, (p, _) => p.Halt())
        {
        }
    }

    [OPCodeNumber(0x0005)]
    public sealed class CALL
        : OPCode
    {
        public CALL()
            : base(1, (p, _) => {
                AssertInstructionSpace(0, _);

                // TODO
            })
        {
        }
    }

    [OPCodeNumber(0x0005)]
    public sealed class RET
        : OPCode
    {
        public RET()
            : base(0, (p, _) => {
                
            })
        {
        }
    }
}
