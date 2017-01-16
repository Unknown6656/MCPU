namespace MCPU.MCPUPP.Parser

open System
open System.Globalization
open Piglet.Parser
open MCPU.MCPUPP.Parser.SyntaxTree

module Lexer =
    let internal identity = fun x -> x
    let inline (!<) x = fun _ -> x
    let Configurator = ParserFactory.Configure<obj>()
    
    // L := (Terminal, NonTerminal, Production, Start)
    
    let NonTerminal<'a> = new NonTerminalWrapper<'a>(Configurator.CreateNonTerminal())
    
    // NON TERMINALS
    let nt_program = NonTerminal<Program>
    let nt_decl = NonTerminal<Declaration>
    let nt_decllist = NonTerminal<Declaration list>
    let nt_funcdecl = NonTerminal<FunctionDeclaration>
    let nt_staticvardecl = NonTerminal<VariableDeclaration>
    let nt_vartype = NonTerminal<VariableType>
    let nt_params = NonTerminal<Parameters>
    let nt_paramlist = NonTerminal<Parameters>
    let nt_param = NonTerminal<VariableDeclaration>
    let nt_optstatements = NonTerminal<Statement list>
    let nt_statementlist = NonTerminal<Statement list>
    let nt_statement = NonTerminal<Statement>
    let nt_exprstatement = NonTerminal<ExpressionStatement>
    let nt_while = NonTerminal<WhileStatement>
    let nt_blockstatement = NonTerminal<BlockStatement>
    let nt_optlocalvardecl = NonTerminal<VariableDeclaration list>
    let nt_localvardecllist = NonTerminal<VariableDeclaration list>
    let nt_localvardecl = NonTerminal<VariableDeclaration>
    let nt_if = NonTerminal<IfStatement>
    let nt_optelse = NonTerminal<Statement option>
    let nt_return = NonTerminal<Expression option>
    let nt_break = NonTerminal<unit>
    let nt_expr = NonTerminal<Expression>
    let nt_uop = NonTerminal<UnaryOperator>
    let nt_optargs = NonTerminal<Arguments>
    let nt_args = NonTerminal<Arguments>

    let ParseTerminal regex (f : string -> 'a) = new TerminalWrapper<'a>(Configurator.CreateTerminal(regex, (fun s -> (f >> box) s)))
    let Terminal regex = new TerminalWrapper<string>(Configurator.CreateTerminal(regex))
    
    // KEYWORDS
    let kw_if = Terminal "if"
    let kw_else = Terminal "else"
    let kw_while = Terminal "while"
    let kw_break = Terminal "break"
    let kw_return = Terminal "return"
    let kw_new = Terminal "new"
    let kw_length = Terminal "length"
    let kw_delete = Terminal "delete"
    let kw_unit = Terminal Builder.UnitString
    let kw_int = ParseTerminal "int" !<Int
    let kw_float = ParseTerminal "float" !<Float
    
    // OPERATORS
    let op_not = Terminal "~"
    let op_int = Terminal @"\(int\)"
    let op_bool = Terminal @"\(bool\)"
    let op_float = Terminal @"\(float\)"
    let op_add = Terminal @"\+"
    let op_subtract = Terminal "-"
    let op_multiply = Terminal @"\*"
    let op_divide = Terminal "/"
    let op_modulus = Terminal "%"
    let op_power = Terminal "^^"
    let op_xor = Terminal "^"
    let op_and = Terminal "&"
    let op_or = Terminal @"\|"
    let op_raw = Terminal "#"
    let op_shiftleft = Terminal "<<"
    let op_shiftright = Terminal ">>"
    let op_rotateleft = Terminal "<<<"
    let op_rotateright = Terminal ">>>"
    let op_equal = Terminal "=="
    let op_notequal = Terminal "!="
    let op_lessequal = Terminal "<="
    let op_less = Terminal "<"
    let op_greaterequal = Terminal ">="
    let op_greater = Terminal ">"
    let op_assign = Terminal "="

    // LITERALS
    let lt_int = ParseTerminal @"\d+" (IntLiteral << int)
    let lt_hex = ParseTerminal @"0x[a-f0-9]+" (fun s -> IntLiteral(Int32.Parse(s, NumberStyles.HexNumber)))
    let lt_float = ParseTerminal @"\d+\.\d+" (FloatLiteral << float)
    let lt_true = ParseTerminal "true" !<(IntLiteral 1)
    let lt_false = ParseTerminal "false" !<(IntLiteral 0)
    let lt_null = ParseTerminal "null" !<(IntLiteral 0)
    
    // IDENTIFIER
    let identifier = ParseTerminal "[a-zA-Z_][a-zA-Z_0-9]*" identity

    // SYMBOLS
    let sy_semicolon = Terminal ";"
    let sy_comma = Terminal ","
    let sy_point = Terminal @"\."
    let sy_oparen = Terminal @"\("
    let sy_cparen = Terminal @"\)"
    let sy_ocurly = Terminal @"\{"
    let sy_ccurly = Terminal @"\}"
    let sy_osquare = Terminal @"\["
    let sy_csquare = Terminal @"\]"
    
    // OPERATOR ASSOCIATIVITY
    let prec_optelse = Configurator.LeftAssociative()
    let prec_binop = Configurator.LeftAssociative()
    let prec_unnop = Configurator.RightAssociative()
    let prec_power = Configurator.RightAssociative()
    let assoc d x =
        let arg = List.map (fun (f : SymbolWrapper<_>) -> downcast f.Symbol)
               >> List.toArray
        match d with
        | Left -> Configurator.LeftAssociative(arg x)
        | Right -> Configurator.RightAssociative(arg x)
        |> ignore
            
    // PRECEDENCE LIST
    assoc Left [ kw_else ]
    assoc Left [ op_xor ]
    assoc Left [ op_or ]
    assoc Left [ op_and ]
    assoc Left [ op_equal; op_notequal ]
    assoc Left [ op_lessequal; op_less; op_greaterequal; op_greater ]
    assoc Left [ op_rotateleft; op_shiftleft; op_rotateright; op_shiftright ]
    assoc Left [ op_not; op_add; op_subtract ]
    assoc Left [ op_multiply; op_divide; op_modulus ]
    assoc Right [ op_power ]
    assoc Right [ op_raw; ]
        
    // PRODUCTIONS
    let reducef (s : NonTerminalWrapper<'a>) x = s.AddProduction().SetReduceFunction x
    let reduce0 (s : NonTerminalWrapper<'a>) a = s.AddProduction(a).SetReduceToFirst()
    let reduce1 (s : NonTerminalWrapper<'a>) a x = s.AddProduction(a).SetReduceFunction x
    let reduce2 (s : NonTerminalWrapper<'a>) a b x = s.AddProduction(a, b).SetReduceFunction x
    let reduce3 (s : NonTerminalWrapper<'a>) a b c x = s.AddProduction(a, b, c).SetReduceFunction x
    let reduce4 (s : NonTerminalWrapper<'a>) a b c d x = s.AddProduction(a, b, c, d).SetReduceFunction x
    let reduce5 (s : NonTerminalWrapper<'a>) a b c d e x = s.AddProduction(a, b, c, d, e).SetReduceFunction x
    let reduce6 (s : NonTerminalWrapper<'a>) a b c d e f x = s.AddProduction(a, b, c, d, e, f).SetReduceFunction x
    
    let elem x = [x]
    let (!.) x = IdentifierRef x
        
    reduce0 nt_program nt_decllist
    reduce2 nt_decllist nt_decllist nt_decl (fun x y -> x @ elem y)
    reduce1 nt_decllist nt_decl elem
    reduce1 nt_decl nt_staticvardecl GlobalVarDecl
    reduce1 nt_decl nt_funcdecl FunctionDeclaration
    reduce1 nt_vartype kw_unit !<Unit
    reduce0 nt_vartype kw_int
    reduce0 nt_vartype kw_float
    reduce3 nt_staticvardecl nt_vartype identifier sy_semicolon (fun a b _ -> ScalarDeclaration(a, b))
    reduce4 nt_staticvardecl nt_vartype identifier op_multiply sy_semicolon (fun a b _ _ -> PointerDeclaration(a, b))
    reduce5 nt_staticvardecl nt_vartype identifier sy_osquare sy_csquare sy_semicolon (fun a b _ _ _ -> ArrayDeclaration(a, b))
    reduce6 nt_funcdecl nt_vartype identifier sy_oparen nt_params sy_cparen nt_blockstatement (fun a b _ d _ f -> (a, b, d, f))
    reduce0 nt_params nt_paramlist
    reduce1 nt_params kw_unit !<[||]
    reduce3 nt_paramlist nt_paramlist sy_comma nt_param (fun a _ c -> (Array.toList a)
                                                                       @ elem c
                                                                       |> List.toArray)
    reduce1 nt_paramlist nt_param (elem >> List.toArray)
    reduce1 nt_statement nt_exprstatement ExpressionStatement
    reduce1 nt_statement nt_blockstatement BlockStatement
    reduce1 nt_statement nt_if IfStatement
    reduce1 nt_statement nt_while WhileStatement
    reduce1 nt_statement nt_return ReturnStatement
    reduce1 nt_statement nt_break !<BreakStatement
    reduce2 nt_exprstatement nt_expr sy_semicolon (fun a _ -> Expression a)
    reduce1 nt_exprstatement sy_semicolon !<Nop
    reduce5 nt_while kw_while sy_oparen nt_expr sy_cparen nt_statement (fun _ _ c _ e -> (c, e))
    reduce4 nt_blockstatement sy_ocurly nt_optlocalvardecl nt_optstatements sy_ccurly (fun _ b c _ -> (b, c))
    reduce0 nt_optlocalvardecl nt_localvardecllist
    reducef nt_optlocalvardecl !<[]
    nt_localvardecllist.AddProduction(nt_localvardecllist, nt_localvardecl).SetReduceToFirst()
    reduce1 nt_localvardecllist nt_localvardecl elem
    reduce6 nt_if kw_if sy_oparen nt_expr sy_cparen nt_statement nt_optelse (fun _ _ c _ e f -> (c, e, f))
    
    let prod_else = nt_optelse.AddProduction(kw_else, nt_statement)

    prod_else.SetReduceFunction (fun _ b -> Some b)
    prod_else.SetPrecedence prec_optelse

    let prod_else_epsilon = nt_optelse.AddProduction()
    
    prod_else_epsilon.SetReduceFunction !<None
    prod_else_epsilon.SetPrecedence prec_optelse

    reduce3 nt_return kw_return nt_expr sy_semicolon (fun _ b _ -> Some b)
    reduce2 nt_return kw_return sy_semicolon !<(!<None)
    reduce2 nt_break kw_break sy_semicolon !<(!<())
    
    reduce3 nt_expr identifier op_assign nt_expr (fun a _ c -> ScalarAssignmentExpression(!.a, c))
    reduce6 nt_expr identifier sy_osquare nt_expr sy_csquare op_assign nt_expr (fun a _ c _ _ f -> ArrayAssignmentExpression(!.a, c, f))
    reduce4 nt_expr op_and identifier op_assign nt_expr (fun _ b _ d -> PointerAssignmentExpression(!.b, d))
    reduce4 nt_expr op_multiply identifier op_assign nt_expr (fun _ b _ d -> PointerValueAssignmentExpression(!.b, d))
    reduce2 nt_expr op_raw identifier ((!.) >> RawAddressOfExpression >> (!<))
    
    let reduce_bop token op = reduce3 nt_expr nt_expr token nt_expr (fun a _ c -> BinaryExpression(a, op, c))
    
    reduce_bop op_equal Equal
    reduce_bop op_notequal NotEqual
    reduce_bop op_xor Xor
    reduce_bop op_and And
    reduce_bop op_or Or
    reduce_bop op_lessequal LessEqual
    reduce_bop op_less Less
    reduce_bop op_greaterequal GreaterEqual
    reduce_bop op_greater Greater
    reduce_bop op_rotateleft RotateLeft
    reduce_bop op_rotateright RotateRight
    reduce_bop op_shiftleft ShiftLeft
    reduce_bop op_shiftright ShiftRight
    reduce_bop op_subtract Subtract
    reduce_bop op_add Add
    reduce_bop op_multiply Multiply
    reduce_bop op_divide Divide
    reduce_bop op_modulus Modulus
    reduce_bop op_power Power
    
    let uprod = nt_expr.AddProduction(nt_uop, nt_expr)
    uprod.SetReduceFunction (fun a b -> UnaryExpression(a, b))
    uprod.SetPrecedence prec_unnop

    reduce3 nt_expr sy_oparen nt_expr sy_cparen (fun _ b _ -> b)
    reduce1 nt_expr identifier ((!.) >> IdentifierExpression)
    reduce4 nt_expr identifier sy_osquare nt_expr sy_csquare (fun a _ c _ -> ArrayIdentifierExpression(!.a, c))
    reduce4 nt_expr identifier sy_oparen nt_optargs sy_cparen (fun a _ c _ -> FunctionCallExpression(a, c))
    reduce3 nt_expr identifier sy_point kw_length (fun a _ _ -> ArraySizeExpression(!.a))
    reduce1 nt_expr lt_true LiteralExpression
    reduce1 nt_expr lt_false LiteralExpression
    reduce1 nt_expr lt_null LiteralExpression
    reduce1 nt_expr lt_int LiteralExpression
    reduce1 nt_expr lt_hex LiteralExpression
    reduce1 nt_expr lt_float LiteralExpression
    reduce5 nt_expr kw_new nt_vartype sy_osquare nt_expr sy_csquare (fun _ b _ d _ -> ArrayAllocationExpression(b, d))
    reduce2 nt_expr kw_delete identifier ((!.) >> ArrayDeletionExpression >> (!<))
    reduce1 nt_uop op_not !<Negate
    reduce1 nt_uop op_subtract !<LogicalNegate
    reduce1 nt_uop op_add !<Identity
    reduce1 nt_uop op_int !<IntConvert
    reduce1 nt_uop op_float !<FloatConvert
    reduce1 nt_uop op_bool !<BooleanConvert
    reduce0 nt_optargs nt_args
    reducef nt_optargs !<[]
    reduce3 nt_args nt_args sy_comma nt_expr (fun a _ c -> a @ elem c)
    reduce1 nt_args nt_expr elem

    // WHITESPACE AND COMMENTS
    Configurator.LexerSettings.Ignore <- [|
        @"\s+"
        @"/\*[^(\*/)]*\*/"
        @"//[^\n]*\n"
    |]

    let Parser = Configurator.CreateParser()
    let parse (s : string) = match s.Trim() with
                             | "" -> List.empty<Declaration>
                             | _ -> Parser.Parse s :?> Program
    do
        ()