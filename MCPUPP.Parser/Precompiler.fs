module MCPU.MCPUPP.Parser.Precompiler

open MCPU.MCPUPP.Parser.SyntaxTree
open MCPU.MCPUPP.Parser.Analyzer
open MCPU

open System.Collections.Generic
open System

// IMxxxx stands for "intermediate xxxx"

type IMVariable =
    {
        Type : Type
        Name : string
    }

type IMLabel = int

// everything is stack-based here (will be thrown onto the sya-stack)
type IMInstruction =
    | Halt
    | Syscall of int * int (* syscall <ID> <argc> *)
    | Call of string * int (* call <Name> <argc> *)
    | Ret
    | Io of int * bool
    | In of int
    | Out of int
    | Ldc of int
    | Ldloc of int
    | Stloc of int
    | Ldarg of int
    | Starg of int
    | Ldfld of IMVariable
    | Stfld of IMVariable
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
    | Swap
    | Dup
    | Add
    | Sub
    | Mul
    | Div
    | Mod
    | Shr
    | Shl
    | Ror
    | Rol
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

type IMVariableScope =
    | FieldScope of IMVariable
    | ArgumentScope of int
    | LocalScope of int

type VariableMappingDictionary = Dictionary<VariableDeclaration, IMVariableScope>

let TypeOf = function
                | Unit -> typeof<Void>
                | Int -> typeof<int>
                | Float -> typeof<float32>
    
let createIMVariable = function
                        | ScalarDeclaration (t, i) ->
                            {
                                Type = TypeOf t
                                Name = i
                            }
                        | ArrayDeclaration (t, i) ->
                            {
                                Type = (TypeOf t).MakeArrayType()
                                Name = i
                            }
                        | PointerDeclaration (t, i) ->
                            {
                                Type = (function
                                        | Unit -> typeof<IntPtr>
                                        | Int -> typeof<nativeptr<int>>
                                        | Float -> typeof<nativeptr<float32>>) t
                                Name = i
                            }

type IMMethodBuilder (analyzerres : AnalyzerResult, mapping : VariableMappingDictionary) =
    let mutable argndx = 0
    let mutable locndx = 0
    let mutable lblndx = 0
    let arrassgnloc = Dictionary<Expression, int>()
    let ptrassgnloc = Dictionary<Expression, int>()
    let endwhilelbl = Stack<IMLabel>()

    let LookupIMVariableScope id =
        let decl = analyzerres.SymbolTable.[id]
        mapping.[decl]

    let CreateIMLabel() =
        let res = lblndx
        lblndx <- lblndx + 1
        res
    
    let ProcessIdentifierStore id = function
                                    | FieldScope v -> [Ldfld v]
                                    | ArgumentScope i -> [Ldarg i]
                                    | LocalScope i -> [Ldloc i]
                                   <| LookupIMVariableScope id

    let ProcessIdentifierStore id = function
                                    | FieldScope v -> [Stfld v]
                                    | ArgumentScope i -> [Starg i]
                                    | LocalScope i -> [Stloc i]
                                   <| LookupIMVariableScope id

    let rec ProcessExpr = function
                          | ScalarAssignmentExpression(i, e) -> List.concat [
                                                                                ProcessExpr e
                                                                                [Dup]
                                                                                ProcessIdentifierStore i
                                                                            ]
    and ProcessBinExpr l op r = List.concat [
                                                ProcessExpr l
                                                ProcessExpr r
                                                ProcessBinOp op
                                            ]
    and ProcessBinOp = function
                       | BinaryOperator.And -> [And]
                       | BinaryOperator.Or -> [Or]
                       | BinaryOperator.Xor -> [Xor]
                       | BinaryOperator.Add -> [Add]
                       | BinaryOperator.Divide -> [Div]
                       | BinaryOperator.Multiply -> [Mul]
                       | BinaryOperator.Modulus -> [Mod]
                       | BinaryOperator.Subtract -> [Sub]
                       | BinaryOperator.Power -> [Pow]
                       | BinaryOperator.ShiftLeft -> [Shl]
                       | BinaryOperator.ShiftRight -> [Shr]
                       | BinaryOperator.RotateLeft -> [Rol]
                       | BinaryOperator.RotateRight -> [Ror]
                       | BinaryOperator.Equal -> [
                                                    Cmp
                                                    Ldc 0x0800
                                                    And
                                                    Bool
                                                 ]
                       | BinaryOperator.NotEqual -> [
                                                        Cmp
                                                        Ldc 0x0800
                                                        And
                                                        Not
                                                        Bool
                                                    ]
                       | BinaryOperator.Greater -> [
                                                       Cmp
                                                       Ldc 0x0200
                                                       And
                                                       Bool
                                                   ]
                       | BinaryOperator.GreaterEqual -> [
                                                            Cmp
                                                            Ldc 0x0200
                                                            Xor
                                                            Ldc 0x0200
                                                            And
                                                            Not
                                                            Bool
                                                        ]
                       | BinaryOperator.Less -> [
                                                    Cmp
                                                    Ldc 0x0400
                                                    And
                                                    Bool
                                                ]
                       | BinaryOperator.LessEqual -> [
                                                         Cmp
                                                         Ldc 0x0400
                                                         Xor
                                                         Ldc 0x0400
                                                         And
                                                         Not
                                                         Bool
                                                     ]
                       | _ as op -> raise <| Errors.InvalidOperator op


    do
        ()
    
do
    // TODO ?
    ()
