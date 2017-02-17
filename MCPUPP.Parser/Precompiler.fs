namespace MCPU.MCPUPP.Parser

open MCPU.MCPUPP.Parser.SyntaxTree
open MCPU

open System

// IMxxxx stands for "intermediate xxxx"

type IMVariable =
    {
        Type : VariableType
        Name : string
    }
type IMLabel = unit // todo
type IMInstruction =
    | Halt
    | Syscall of int * InstructionArgument list
    | Call of string * InstructionArgument list
    | Ret
    | Io of int * bool
    | In of int * int
    | Out of int * InstructionArgument
    | Mov of int * InstructionArgument
    | Lea of int * InstructionArgument
    | Cmp
    | FCmp
    | Jmp of IMLabel
    | Jle of IMLabel
    | Jl of IMLabel
    | Jg of IMLabel
    | Jge of IMLabel
    | Je of IMLabel
    | Jne of IMLabel
    | Jz of IMLabel
    | Jnz of IMLabel
    | Jneg of IMLabel
    | Jpos of IMLabel
    | Jnan of IMLabel
    | Jnnan of IMLabel
    | Jinf of IMLabel
    | Jpinf of IMLabel
    | Jninf of IMLabel
    | Swap of int * int
    | Add
    | Sub
    | Mul
    | Div
    | Mod
    | Neg
    | Not
    | Or
    | And
    | Xor
    | Nor
    | Nand
    | Nxor
    | Abs
    | Bool
    | Pow
    | Fac
    | Incr
    | Decr
    | CPUID
    | Push
    | Pop
    | Peek
    | FIcast
    | IFcast
    | FAdd
    | FSub
    | FMul
    | FDiv
    | FMod
    | FNeg
    | FInv
    | FSqrt
    | FRoot
    | FLog
    | FLogE
    | FPow
    | FExp
    | FFloor
    | FCeil
    | FRound
    | FMin
    | FMax
    | FSign
    | FSin
    | FCos
    | FTan
    | FSinh
    | FCosh
    | FTanh
    | FAsin
    | FAcos
    | FAtan
    | FAtan2
type IMMethod =
    {
        Name : string
        ReturnType : VariableType
        Parameters : IMVariable list
        Locals : IMVariable list
        Body : IMInstruction list
    }
type IMProgram =
    {
        Fields : IMVariable list
        Methods : IMMethod list
    }

module Precompiler =
    do
        // TODO ?
        ()
