namespace MCPU.MCPUPP.Parser.SyntaxTree

type VariableType =
    | Unit
    | Int
    | Float
and Identifier = string
and IdentifierRef = { Identifier : string; }
and VariableDeclaration =
    | ArrayDeclaration of VariableType * Identifier
    | ScalarDeclaration of VariableType * Identifier
    | PointerDeclaration of VariableType * Identifier
and LocalVarDecl = VariableDeclaration list
and Parameters = VariableDeclaration list
and Literal =
    | IntLiteral of int
    | FloatLiteral of float
and BinaryOperator =
    | ConditionalOr
    | Equal
    | NotEqual
    | LessEqual
    | Less
    | GreaterEqual
    | Greater
    | ConditionalAnd
    | Add
    | Subtract
    | Multiply
    | Divide
    | Modulus
    | Power
    | Xor
    | And
    | Or
and UnaryOperator =
    | LogicalNegate
    | Negate
    | Identity
and Arguments = Expression list
and Expression =
    | LiteralExpression of Literal
    | ScalarAssignmentExpression of IdentifierRef * Expression
    | PointerAssignmentExpression of IdentifierRef * Expression
    | ArrayAssignmentExpression of IdentifierRef * Expression * Expression
    | BinaryExpression of Expression * BinaryOperator * Expression
    | UnaryExpression of UnaryOperator * Expression
    | IdentifierExpression of IdentifierRef
    | FunctionCallExpression of Identifier * Arguments
    | ArrayIdentifierExpression of IdentifierRef * Expression
    | ArraySizeExpression of IdentifierRef
    | ArrayAllocationExpression of VariableType * Expression
    | PointerAllocationExpression of IdentifierRef
    | PointerValueIdentifierExpression of IdentifierRef
    | PointerAddressIdentifierExpression of IdentifierRef
    // TODO
and WhileStatement = Expression * Statement
and IfStatement = Expression * Statement * Statement option
and Statement =
    | ExpressionStatement of ExpressionStatement
    | CompoundStatement of BlockStatement
    | IfStatement of IfStatement
    | WhileStatement of WhileStatement
    | ReturnStatement of Expression option
    | BreakStatement
and ExpressionStatement =
    | Expression of Expression
    | Nop
and BlockStatement = LocalVarDecl * Statement list
and FunctionDeclaration = VariableType * Identifier * Parameters * BlockStatement
and Declaration =
    | GlobalVarDecl of VariableDeclaration
    | FunctionDeclaration of FunctionDeclaration
and Program = Declaration list



module Builder =
    let rec Build (ast : obj) =
        let inline (</) f x = (Build >> f) x
        match box ast with
        | :? Program as p -> p
                             |> List.map (fun x -> Build x)
                             |> String.concat "\n"
        | :? Literal as l -> match l with
                             | IntLiteral i -> i.ToString()
                             | FloatLiteral f -> f.ToString()
        | :? Identifier as i -> i
        | :? IdentifierRef as r -> r.Identifier
        | :? VariableType as t -> match t with
                                  | Float -> "float"
                                  | Int -> "int"
                                  | Unit -> "void"
        | :? VariableDeclaration as v ->
            let t, s, i = match v with
                          | ArrayDeclaration(t, i) -> (t, "[]", i)
                          | PointerDeclaration(t, i) -> (t, "*", i)
                          | ScalarDeclaration(t, i) -> (t, "", i)
            sprintf "%s%s %s" </ t <| s </ i
        | :? Parameters as p -> p
                                |> List.map (fun x -> Build x)
                                |> String.concat ", "
        | :? FunctionDeclaration as t ->
            let r, i, p, b = t
            sprintf "function %s %s(%s)\n{\n%s\n}" </ r </ i </ p </ b
        | _ -> failwith "The type cannot be matched"
