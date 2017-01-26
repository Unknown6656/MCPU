namespace MCPU.MCPUPP.Parser

open System.Text.RegularExpressions;
open System
open MCPU.Compiler
open MCPU

//type _curry<'a> =
//    | C of ('a -> 'a)
//    | F of (_curry<'a> -> 'a)

[<AutoOpen>]
module Helper =
    let inline (!~<) (x, y) = ValueTuple<'a, 'b>(x, y)
    let inline (!~>) (t : ValueTuple<'a, 'b>) = (t.Item1, t.Item2)
    let ismatch str pattern = Regex.IsMatch(str, pattern)
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