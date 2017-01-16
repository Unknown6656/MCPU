namespace MCPU.MCPUPP.Parser.SyntaxTree

open Microsoft.FSharp.Reflection

type VariableType =
    | Unit
    | Int
    | Float
and Identifier = string
type IdentifierRef (name : string) =
    member x.Identifier = name
type VariableDeclaration =
    | ArrayDeclaration of VariableType * Identifier
    | ScalarDeclaration of VariableType * Identifier
    | PointerDeclaration of VariableType * Identifier
and LocalVarDecl = VariableDeclaration list
and Parameters = VariableDeclaration[]
and Literal =
    | IntLiteral of int
    | FloatLiteral of float
and BinaryOperator =
    | Equal
    | NotEqual
    | LessEqual
    | Less
    | GreaterEqual
    | Greater
    | Add
    | Subtract
    | Multiply
    | Divide
    | Modulus
    | Power
    | Xor
    | And
    | Or
    | ShiftLeft
    | ShiftRight
    | RotateLeft
    | RotateRight
and UnaryOperator =
    | LogicalNegate
    | Negate
    | Identity
    | IntConvert
    | FloatConvert
    | BooleanConvert
and Arguments = Expression list
and Expression =
    | LiteralExpression of Literal
    | RawAddressOfExpression of IdentifierRef
    | ScalarAssignmentExpression of IdentifierRef * Expression
    | BinaryExpression of Expression * BinaryOperator * Expression
    | UnaryExpression of UnaryOperator * Expression
    | IdentifierExpression of IdentifierRef
    | FunctionCallExpression of Identifier * Arguments
    | ArrayAssignmentExpression of IdentifierRef * Expression * Expression
    | ArrayIdentifierExpression of IdentifierRef * Expression
    | ArraySizeExpression of IdentifierRef
    | ArrayAllocationExpression of VariableType * Expression
    | ArrayDeletionExpression of IdentifierRef
    | PointerAssignmentExpression of IdentifierRef * Expression
    | PointerValueAssignmentExpression of IdentifierRef * Expression
    | PointerValueIdentifierExpression of IdentifierRef
    | PointerAddressIdentifierExpression of IdentifierRef
    // TODO
and WhileStatement = Expression * Statement
and IfStatement = Expression * Statement * Statement option
and InlineAssemblyStatement = { Lines : string list; }
and BlockStatement = LocalVarDecl * Statement list
and Statement =
    | ExpressionStatement of ExpressionStatement
    | BlockStatement of BlockStatement
    | IfStatement of IfStatement
    | WhileStatement of WhileStatement
    | ReturnStatement of Expression option
    | BreakStatement
    | InlineAssemblyStatement of InlineAssemblyStatement
and ExpressionStatement =
    | Expression of Expression
    | Nop
and FunctionDeclaration = VariableType * Identifier * Parameters * BlockStatement
and Declaration =
    | GlobalVarDecl of VariableDeclaration
    | FunctionDeclaration of FunctionDeclaration
and Program = Declaration list


module Builder =
    let UnitString = "void"

    let rec BuildString (indent : int) (ast : obj) =
        let inline (</) f = BuildString indent >> f
        let inline (<//) f = BuildString (indent + 1) >> f
        let tab i = System.String('\t', i)
        let MapBuild x c = x
                           |> List.map(fun f -> BuildString indent f)
                           |> String.concat c
        match box ast with
        | :? string as s -> s
        | :? Program as p -> MapBuild p "\n\n"
        | :? Declaration as d -> BuildString indent <| match d with
                                                       | GlobalVarDecl g -> box g
                                                       | FunctionDeclaration f -> box f
        | :? Literal as l -> match l with
                             | IntLiteral i -> i.ToString()
                             | FloatLiteral f -> f.ToString()
        | :? IdentifierRef as r -> r.Identifier
        | :? VariableType as t -> match t with
                                  | Float -> "float"
                                  | Int -> "int"
                                  | Unit -> UnitString
        | :? VariableDeclaration as v ->
            let t, s, i = match v with
                          | ArrayDeclaration(t, i) -> (t, "[]", i)
                          | PointerDeclaration(t, i) -> (t, "*", i)
                          | ScalarDeclaration(t, i) -> (t, "", i)
            if t = Unit then sprintf "A %s variable declaration is not valid" UnitString
                             |> failwith
            sprintf "%s%s %s" </ t <| s </ i
        | :? Arguments as a -> MapBuild a ", "
        | :? Parameters as p -> MapBuild (p |> Array.toList) ", "
        | :? LocalVarDecl as l -> MapBuild l "; "
        | :? FunctionDeclaration as t ->
            let r, i, p, b = t
            let t = tab indent
            sprintf "%sfunction %s %s(%s)\n%s{\n%s\n%s}" t </ r </ i </ p <| t <// b <| t
        | :? UnaryOperator as u -> match u with
                                   | LogicalNegate -> "-"
                                   | Negate -> "~"
                                   | Identity -> "+"
                                   | IntConvert -> "(int)"
                                   | FloatConvert -> "(float)"
                                   | BooleanConvert -> "(bool)"
        | :? BinaryOperator as b -> match b with
                                    | Equal -> "=="
                                    | NotEqual -> "!="
                                    | LessEqual -> "<="
                                    | Less -> "<"
                                    | GreaterEqual -> ">="
                                    | Greater -> ">"
                                    | Add -> "+"
                                    | Subtract -> "-"
                                    | Multiply -> "*"
                                    | Divide -> "/"
                                    | Modulus -> "%"
                                    | Power -> "^^"
                                    | Xor -> "^"
                                    | And -> "&"
                                    | Or -> "|"
                                    | ShiftLeft -> "<<"
                                    | ShiftRight -> ">>"
                                    | RotateLeft -> "<<<"
                                    | RotateRight -> ">>>"
        | :? Expression as e ->
            let assgn a b = sprintf "%s = %s" </ a </ b
            match e with
            | LiteralExpression l -> BuildString indent l
            | ScalarAssignmentExpression(i, e) -> assgn i e
            | PointerAssignmentExpression(i, e) -> assgn i e
            | PointerValueAssignmentExpression(i, e) -> sprintf "*%s = %s" </ i </ e
            | ArrayAssignmentExpression(a, i, e) -> sprintf "%s[%s] = %s" </ a </ i </ e
            | BinaryExpression(a, o, b) -> sprintf "%s %s %s" </ a </ o </ b
            | UnaryExpression(a, o) -> sprintf "%s%s" </ a </ o
            | IdentifierExpression i -> BuildString indent i
            | FunctionCallExpression(f, a) -> sprintf "%s(%s)" </ f </ a
            | ArrayIdentifierExpression(a, i) -> sprintf "%s[%s]" </ a </ i
            | ArraySizeExpression a -> sprintf "%s.length" </ a
            | ArrayAllocationExpression(a, s) -> sprintf "new %s[%s]" </ a </ s
            | ArrayDeletionExpression a -> sprintf "delete %s" </ a
            | PointerValueIdentifierExpression a -> sprintf "*%s" </ a
            | PointerAddressIdentifierExpression a -> sprintf "&%s" </ a
            | RawAddressOfExpression a -> sprintf "#%s" </ a
        | :? WhileStatement as s -> sprintf "while (%s)\n%s" </ fst s <// snd s
        | :? ExpressionStatement as e -> match e with
                                         | Nop -> ";"
                                         | Expression e -> (BuildString indent e) + ";"
        | :? BlockStatement as b -> String.concat "\n" [|
                                                           for i in fst b -> sprintf "%s%s;" <| tab indent </ i
                                                           for j in snd b -> sprintf "%s%s" <| tab indent </ j
                                                       |]
        | :? IfStatement as s -> 
            let c, s, o = s
            let res = sprintf "if (%s)\n%s" </ c <// s
            match o with
            | Some o -> sprintf "%s\n%selse\n%s" res <| tab indent <// o
            | None -> res
        | :? InlineAssemblyStatement as a ->
            sprintf "__asm\n%s{\n%s\n%s}" <| tab indent
                                          <| (a.Lines
                                              |> List.map (fun f -> tab(indent + 1) + f)
                                              |> List.toArray
                                              |> String.concat "\n")
                                          <| tab indent
        | :? Statement as s -> sprintf "%s%s" <| tab indent
                                              <| match s with
                                                 | BreakStatement -> "break;"
                                                 | ReturnStatement e -> match e with
                                                                         | Some e -> sprintf "return %s;" </ e
                                                                         | None -> "return;"
                                                 | InlineAssemblyStatement a -> BuildString indent a
                                                 | ExpressionStatement e -> BuildString indent e
                                                 | IfStatement i -> BuildString indent i
                                                 | WhileStatement w -> BuildString indent w
                                                 | BlockStatement c -> sprintf "{\n%s\n%s}" <// c <| tab indent
        | _ -> "The type " + ast.GetType().ToString() + " could not be matched."
               |> failwith

    let ToString (program : Program) = // TODO : fix!
        let rec tstr obj indent =
            let tab = new string(' ', indent * 4)
            let inner =
                let tp = obj.GetType().Name
                let printl = List.map (fun e -> tstr e (indent + 1))
                          >> List.fold (+) ",\n"
                let prints p l s = sprintf "%s : %s\n%s\n%s%s" tp p (printl l) tab s
                
                match box obj with
                | :? list<_> as l -> prints "[" l "]"
                | :? (_[]) as arr -> prints "[|" (Array.toList arr) "|]"
                | _ -> let tupleToList = FSharpValue.GetTupleFields >> Array.toList
                       if FSharpType.IsTuple(obj.GetType()) then
                           prints "(" (tupleToList obj) ")"
                       else
                           sprintf "%s : %s" tp <| obj.ToString()
            tab + inner
        tstr program 0
