namespace MCPU.MCPUPP.Parser

open System;

type SYAaction = Shift | ReduceStack | ReduceInput
type SYAassociativity = | Left | Right
type SYAstate (input : string list, stack : string list, output : string list) =
    member x.Input with get() = input
    member x.Stack with get() = stack
    member x.Output with get() = output
    member x.shift = SYAstate(x.Input.Tail, x.Input.Head::x.Stack, x.Output)
    member x.reduce = SYAstate(x.Input, x.Stack.Tail, x.Stack.Head::x.Output)
    member x.reduceNumber = SYAstate(x.Input.Tail, x.Stack, x.Input.Head::x.Output)
 
module ShuntingYardAlgorithm =
    let (|Number|Open|Close|Operator|) x =
        if (Double.TryParse >> fst) x then Number
        else match x with
             | "(" -> Open
             | ")" -> Close
             | _ -> Operator
    let internal prec = function
                        // TODO : functions
                        | "!" | "~" -> 11
                        | "**" -> 10
                        | "*" | "/" | "%" -> 9
                        | "+" | "-" -> 8
                        | "<<" | ">>" | "<<<" | ">>>" -> 7
                        | "<" | ">" | "<=" | ">=" -> 6
                        | "==" | "!=" -> 5
                        | "&" -> 4
                        | "^" -> 3
                        | "|" -> 2
                        | "(" -> 1
                        | x -> failwith ("Unknown operator: " + x)
    let internal assoc = function
                         | "**" | "!" | "~" -> Right
                         | _ -> Left
 
    let rec ShuntingYard (s : SYAstate) =
        let rec reduce_to_Open (s : SYAstate) =
            match s.Stack with
            | [] -> failwith "Mismatched parentheses!"
            | "("::xs -> SYAstate(s.Input.Tail, xs, s.Output)
            | _ -> reduce_to_Open s.reduce

        let reduce_by_prec_and_shift x s =
            let (xPrec, xAssoc) = (prec x, assoc x)
            let rec loop (s : SYAstate) =
                match s.Stack with
                | [] -> s
                | x::xs ->
                    let topPrec = prec x
                    if (xAssoc = Left && xPrec <= topPrec) || (xAssoc = Right && xPrec < topPrec) then
                        loop s.reduce
                    else
                        s
            (loop s).shift

        let rec reduce_rest (s : SYAstate) =
            match s.Stack with
            | [] -> s
            | "("::_ -> failwith "Mismatched parentheses!"
            | x::_ -> reduce_rest s.reduce
 
        match s.Input with
        | x::inputRest ->
            match x with
            | Number -> ShuntingYard s.reduceNumber
            | Open -> ShuntingYard s.shift
            | Close -> reduce_to_Open >> ShuntingYard <| s
            | Operator -> ShuntingYard(reduce_by_prec_and_shift x s)
        | [] -> reduce_rest s
 
    let parse (input : string) = SYAstate(input.ToLower().Split() |> Array.toList, [], [])
                 