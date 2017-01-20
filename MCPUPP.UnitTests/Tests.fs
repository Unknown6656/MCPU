namespace MCPU.MCPUPP.Tests

open MCPU.MCPUPP.Parser.SyntaxTree
open MCPU.MCPUPP.Parser

module UnitTests =
    let Test01 =
        !~<("", List.empty<Declaration>)
    let Test02 =
        !~<("int a;", [
            GlobalVarDecl(
                ScalarDeclaration(
                    Int,
                    "a"
                )
            )
        ])
    let Test03 =
        !~<("void main(void) { }", [
            FunctionDeclaration(
                Unit,
                "main",
                [|
                |],
                (
                    [
                    ],
                    [
                    ]
                )
            )
        ])
    let Test04 =
        !~<("int test(float[] a, int* b, int c) { }", [
            FunctionDeclaration(
                Int,
                "test",
                [|
                    ArrayDeclaration(Float, "a")
                    PointerDeclaration(Int, "b")
                    ScalarDeclaration(Int, "c")
                |],
                (
                    [
                    ],
                    [
                    ]
                )
            )
        ])
    let Test05 =
        !~<(@"
        void main(void)
        {
            while (true)
                break;
        }", [
                FunctionDeclaration(Unit, "main", [||], (
                    [], [
                        WhileStatement(
                            LiteralExpression(
                                IntLiteral(1)
                            ),
                            BreakStatement
                        )
                    ]
                )
            )
        ])
    let Test06 =
        !~<(@"
        void main(void)
        {
            if (true)
                return;
        }", [
                FunctionDeclaration(Unit, "main", [||], (
                    [], [
                        IfStatement(
                            LiteralExpression(
                                IntLiteral(1)
                            ),
                            ReturnStatement(None),
                            None
                        )
                    ]
                )
            )
        ])
    let Test07 =
        !~<(@"
        int main(void)
        {
            while (false)
                return -42;
        }", [
                FunctionDeclaration(Int, "main", [||], (
                    [], [
                        WhileStatement(
                            LiteralExpression(
                                IntLiteral(0)
                            ),
                            ReturnStatement(
                                Some(
                                    UnaryExpression(
                                        LogicalNegate,
                                        LiteralExpression(
                                            IntLiteral(42)
                                        )
                                    )
                                )
                            )
                        )
                    ]
                )
            )
        ])
    let Test08 =
        !~<(@"
        void main(void)
        {
            float[] arr;

            arr = new float[8];
            arr[7] = +0.5;
        }", [
                FunctionDeclaration(Unit, "main", [||], (
                    [
                        ArrayDeclaration(Float, "arr")
                    ],
                    [
                        ExpressionStatement(
                            Expression(
                                ScalarAssignmentExpression(
                                    IdentifierRef("arr"),
                                    ArrayAllocationExpression(
                                        Float,
                                        LiteralExpression(
                                            IntLiteral(8)
                                        )
                                    )
                                )
                            )
                        )
                        ExpressionStatement(
                            Expression(
                                ArrayAssignmentExpression(
                                    IdentifierRef("arr"),
                                    LiteralExpression(
                                        IntLiteral(7)
                                    ),
                                    UnaryExpression(
                                        Identity,
                                        LiteralExpression(
                                            FloatLiteral(0.5)
                                        )
                                    )
                                )
                            )
                        )
                    ]
                )
            )
        ])
    let Test09 =
        !~<(@"
        void main(void)
        {
            float[] arr;

            delete arr;
        }", [
                FunctionDeclaration(Unit, "main", [||], (
                    [
                        ArrayDeclaration(Float, "arr")
                    ],
                    [
                        ExpressionStatement(
                            Expression(
                                ArrayDeletionExpression(
                                    IdentifierRef("arr")
                                )
                            )
                        )
                    ]
                )
            )
        ])
    let Test10 =
        !~<(@"
        void main(void)
        {
            __asm ""NOP"";
        }", [
                FunctionDeclaration(Unit, "main", [||], (
                    [], [
                        InlineAsmStatement(
                            InlineAssemblyStatement "NOP"
                        )
                    ]
                )
            )
        ])
    let Test11 =
        !~<(@"
        void main(void)
        {
            float* ptr;

            &ptr = 0;
            *ptr = 42.0;
        }", [
                FunctionDeclaration(Unit, "main", [||], (
                    [
                        PointerDeclaration(Float, "ptr")
                    ], [
                        ExpressionStatement(
                            Expression(
                                PointerAssignmentExpression(
                                    IdentifierRef "ptr",
                                    LiteralExpression(
                                        IntLiteral(0)
                                    )
                                )
                            )
                        )
                        ExpressionStatement(
                            Expression(
                                PointerValueAssignmentExpression(
                                    IdentifierRef "ptr",
                                    LiteralExpression(
                                        FloatLiteral(42.0)
                                    )
                                )
                            )
                        )
                    ]
                )
            )
        ])
    let Test12 =
        !~<(@"
        int main(void)
        {
            return ((1 * 2) + (3 << 4)) & (7 >= 8);
        }", [
                FunctionDeclaration(Int, "main", [||], (
                    [], [
                        ReturnStatement(
                            Some(
                                BinaryExpression(
                                    BinaryExpression(
                                        BinaryExpression(
                                            LiteralExpression(
                                                IntLiteral(1)
                                            ),
                                            Multiply,
                                            LiteralExpression(
                                                IntLiteral(2)
                                            )
                                        ),
                                        Add,
                                        BinaryExpression(
                                            LiteralExpression(
                                                IntLiteral(3)
                                            ),
                                            ShiftLeft,
                                            LiteralExpression(
                                                IntLiteral(4)
                                            )
                                        )
                                    ),
                                    And,
                                    BinaryExpression(
                                        LiteralExpression(
                                            IntLiteral(7)
                                        ),
                                        GreaterEqual,
                                        LiteralExpression(
                                            IntLiteral(8)
                                        )
                                    )
                                )
                            )
                        )
                    ]
                )
            )
        ])
    
    let TestNN =
        !~<(@"
int i;

int bar(void)
{
    float* ptr;

    return 2;
}

void foo(int a) { }

float topkek (int lulz)
{
    while ((lulz << 8) >= 0)
        return 42.0;
}
        ", [
            GlobalVarDecl(
                ScalarDeclaration(Int, "i")
            )
            FunctionDeclaration(
                Int,
                "bar",
                [||],
                (
                    [
                        PointerDeclaration(Float, "ptr")
                    ],
                    [
                        ReturnStatement(
                            Some(
                                LiteralExpression(
                                    IntLiteral(2)
                                )
                            )
                        )
                    ]
                )
            )
            FunctionDeclaration(
                Unit,
                "main",
                [|
                    ScalarDeclaration(Int, "a")
                |],
                (
                    [
                    ],
                    [
                    ]
                )
            )
            FunctionDeclaration(
                Float,
                "topkek",
                [|
                    ScalarDeclaration(Int, "lulz")
                |],
                (
                    [
                    ],
                    [
                        WhileStatement(
                            BinaryExpression(
                                BinaryExpression(
                                    IdentifierExpression(
                                        IdentifierRef("lulz")
                                    ),
                                    ShiftLeft,
                                    LiteralExpression(
                                        IntLiteral(0)
                                    )
                                ),
                                GreaterEqual,
                                LiteralExpression(
                                    IntLiteral(0)
                                )
                            ),
                            ReturnStatement(
                                Some(
                                    LiteralExpression(
                                        FloatLiteral(42.0)
                                    )
                                )
                            )
                        )
                    ]
                )
            )
        ])

    do
        ()