namespace MCPU.MCPUPP.Parser.SyntaxTree

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
    | BooleanConvert
    | RawAddressOf
and Arguments = Expression list
and Expression =
    | LiteralExpression of Literal
    | ScalarAssignmentExpression of IdentifierRef * Expression
    | PointerAssignmentExpression of IdentifierRef * Expression
    | PointerValueAssignmentExpression of IdentifierRef * Expression
    | ArrayAssignmentExpression of IdentifierRef * Expression * Expression
    | BinaryExpression of Expression * BinaryOperator * Expression
    | UnaryExpression of UnaryOperator * Expression
    | IdentifierExpression of IdentifierRef
    | FunctionCallExpression of Identifier * Arguments
    | ArrayIdentifierExpression of IdentifierRef * Expression
    | ArraySizeExpression of IdentifierRef
    | ArrayAllocationExpression of VariableType * Expression
    | ArrayDeletionExpression of VariableType
    | PointerAllocationExpression of IdentifierRef
    | PointerValueIdentifierExpression of IdentifierRef
    | PointerAddressIdentifierExpression of IdentifierRef
    // TODO
and WhileStatement = Expression * Statement
and IfStatement = Expression * Statement * Statement option
and InlineAssemblyStatement = { Lines : string list; }
and BlockStatement = LocalVarDecl * Statement list
and Statement =
    | ExpressionStatement of ExpressionStatement
    | CompoundStatement of BlockStatement
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
    | GlobalVarDecl of LocalVarDecl
    | FunctionDeclaration of FunctionDeclaration
and Program = Declaration list


module Builder =
    let rec Build (indent : int) (ast : obj) =
        let inline (</) f = Build indent >> f
        let inline (<//) f = Build (indent + 1) >> f
        let tab i = System.String('\t', i)
        let MapBuild x c = x
                           |> List.map(fun f -> Build indent f)
                           |> String.concat c
        let unitstr = "void"
        match box ast with
        | :? string as s -> s
        | :? Program as p -> MapBuild p "\n\n"
        | :? Declaration as d -> Build indent <| match d with
                                                 | GlobalVarDecl g -> box g
                                                 | FunctionDeclaration f -> box f
        | :? Literal as l -> match l with
                             | IntLiteral i -> i.ToString()
                             | FloatLiteral f -> f.ToString()
        | :? IdentifierRef as r -> r.Identifier
        | :? VariableType as t -> match t with
                                  | Float -> "float"
                                  | Int -> "int"
                                  | Unit -> unitstr
        | :? VariableDeclaration as v ->
            let t, s, i = match v with
                          | ArrayDeclaration(t, i) -> (t, "[]", i)
                          | PointerDeclaration(t, i) -> (t, "*", i)
                          | ScalarDeclaration(t, i) -> (t, "", i)
            if t = Unit then sprintf "A %s variable declaration is not valid" unitstr
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
                                   | BooleanConvert -> "(bool)"
                                   | RawAddressOf -> "#"
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
            | LiteralExpression l -> Build indent l
            | ScalarAssignmentExpression(i, e) -> assgn i e
            | PointerAssignmentExpression(i, e) -> assgn i e
            | PointerValueAssignmentExpression(i, e) -> sprintf "*%s = %s" </ i </ e
            | ArrayAssignmentExpression(a, i, e) -> sprintf "%s[%s] = %s" </ a </ i </ e
            | BinaryExpression(a, o, b) -> sprintf "%s %s %s" </ a </ o </ b
            | UnaryExpression(a, o) -> sprintf "%s%s" </ a </ o
            | IdentifierExpression i -> Build indent i
            | FunctionCallExpression(f, a) -> sprintf "%s(%s)" </ f </ a
            | ArrayIdentifierExpression(a, i) -> sprintf "%s[%s]" </ a </ i
            | ArraySizeExpression a -> sprintf "%s.length" </ a
            | ArrayAllocationExpression(a, s) -> sprintf "new %s[%s]" </ a </ s
            | ArrayDeletionExpression a -> sprintf "delete %s" </ a
            | PointerAllocationExpression a -> sprintf "&%s" </ a
            | PointerValueIdentifierExpression a -> sprintf "*%s" </ a
            | PointerAddressIdentifierExpression a -> sprintf "&%s" </ a
        | :? WhileStatement as s -> sprintf "while (%s)\n%s" </ fst s <// snd s
        | :? ExpressionStatement as e -> match e with
                                         | Nop -> ";"
                                         | Expression e -> (Build indent e) + ";"
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
                                                 | InlineAssemblyStatement a -> Build indent a
                                                 | ExpressionStatement e -> Build indent e
                                                 | IfStatement i -> Build indent i
                                                 | WhileStatement w -> Build indent w
                                                 | CompoundStatement c -> sprintf "{\n%s\n%s}" <// c <| tab indent
        | _ -> "The type " + ast.GetType().ToString() + " could not be matched."
               |> failwith
        
module BuilderTests =
    let Test1 : Program =
        let id = IdentifierRef >> IdentifierExpression
        [
            GlobalVarDecl[
                ScalarDeclaration(Float, "global_var")
            ]
            FunctionDeclaration(Int, "foobar", [|
                ScalarDeclaration(Int, "arg0")
                ArrayDeclaration(Float, "arg1")
                PointerDeclaration(Int, "arg2")
            |], ([
                ArrayDeclaration(Int, "local_var")
                ScalarDeclaration(Float, "kekx")
            ], [
                IfStatement(
                    BinaryExpression(
                        ArrayIdentifierExpression(
                            IdentifierRef("arg2"),
                            id("local_var")
                        ),
                        GreaterEqual,
                        LiteralExpression(
                            IntLiteral(3)
                        )
                    ),
                    ReturnStatement(
                        Some(
                            id("arg0")
                        )
                    ),
                    Some(
                        ExpressionStatement(
                            Expression(
                                ScalarAssignmentExpression(
                                    IdentifierRef("kekx"),
                                    id("arg1")
                                )
                            )
                        )
                    )
                )
            ]))
        ]