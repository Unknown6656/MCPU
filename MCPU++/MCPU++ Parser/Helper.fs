namespace MCPU.MCPUPP.Parser

open System
open MCPU
open MCPU.Compiler

[<AutoOpen>]
module Helper =
    let inline (!<) (x, y) = ValueTuple<'a, 'b>(x, y)
    let inline (!>) (t : ValueTuple<'a, 'b>) = (t.Item1, t.Item2)
    
    do
        ()