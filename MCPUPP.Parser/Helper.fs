namespace MCPU.MCPUPP.Parser

open System
open MCPU
open MCPU.Compiler

//type _curry<'a> =
//    | C of ('a -> 'a)
//    | F of (_curry<'a> -> 'a)

[<AutoOpen>]
module Helper =
    let inline (!~<) (x, y) = ValueTuple<'a, 'b>(x, y)
    let inline (!~>) (t : ValueTuple<'a, 'b>) = (t.Item1, t.Item2)
    (* TODO:
    
    let rec uncurry (func : _curry<'a>) (arr : 'a list) : _curry<'a> =
        match arr with
        | x::xs -> uncurry <| match func with
                              | C v -> ()
                              | F f -> ()
                           <| xs
        | [] -> func

    *)
    do
        ()