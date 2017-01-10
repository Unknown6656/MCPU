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
    | ScalarAssignmentExpression of IdentifierRef * Expression
    | PointerAssignmentExpression of IdentifierRef * Expression
    | ArrayAssignmentExpression of IdentifierRef * Expression * Expression
    | BinaryExpression of Expression * BinaryOperator * Expression
    | UnaryExpression of UnaryOperator * Expression
    | IdentifierExpression of IdentifierRef
    | ArrayIdentifierExpression of IdentifierRef * Expression
    | FunctionCallExpression of Identifier * Arguments
    | ArraySizeExpression of IdentifierRef
    | LiteralExpression of Literal
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
