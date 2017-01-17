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
            FunctionDeclaration(Unit, "test", [||], (
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
            if (false)
                return;
        }", [
            FunctionDeclaration(Unit, "test", [||], (
                    [], [
                        WhileStatement(
                            LiteralExpression(
                                IntLiteral(0)
                            ),
                            ReturnStatement(None)
                        )
                    ]
                )
            )
        ])
    let Test07 =
        !~<(@"
        void main(void)
        {
            float[] arr;

            arr = new float[8];
        }", [
            FunctionDeclaration(Unit, "test", [||], (
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
                    ]
                )
            )
        ])
    let Test08 =
        !~<(@"
        void main(void)
        {
            float[] arr;

            delete arr;
        }", [
            FunctionDeclaration(Unit, "test", [||], (
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
    let Test09 =
        !~<(@"
        void main(void)
        {
            __asm
            {
                NOP
            }
        }", [
            FunctionDeclaration(Unit, "test", [||], (
                    [], [
                        InlineAsmStatement(
                            InlineAssemblyStatement "NOP"
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