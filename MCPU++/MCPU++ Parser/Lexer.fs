namespace MCPU.MCPUPP.Parser

open System
open System.Globalization

open Piglet.Parser
open Piglet.Parser.Configuration
open MCPU.MCPUPP.Parser.SyntaxTree


module Lexer =
    let internal identity = fun x -> x
    let inline (!<) x = fun _ -> x
    let Configurator = ParserFactory.Configure<obj>()
    let NonTerminal<'a> = new NonTerminalWrapper<'a>(Configurator.CreateNonTerminal())
    let Terminal regex = new TerminalWrapper<string>(Configurator.CreateTerminal(regex))
    let ParseTerminal regex (f : string -> 'a) = new TerminalWrapper<'a>(Configurator.CreateTerminal(regex, (fun s -> (f >> box) s)))
    
    // L := (Terminal, NonTerminal, Production, Start)
    
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

    // KEYWORDS
    let kw_if = Terminal "if"
    let kw_else = Terminal "else"
    let kw_while = Terminal "while"
    let kw_break = Terminal "break"
    let kw_return = Terminal "return"
    let kw_new = Terminal "new"
    let kw_length = Terminal "length"
    let kw_delete = Terminal "delete"
    let kw_int = ParseTerminal "int" !<Int
    let kw_float = ParseTerminal "float" !<Float
    
    // OPERATORS
    let op_identity = Terminal @"\+"
    let op_minus = Terminal @"\-"
    let op_not = Terminal @"\~"
    let op_bool = Terminal @"\(bool\)"
    let op_add = Terminal @"\+"
    let op_subtract = Terminal @"\-"
    let op_multiply = Terminal @"\*"
    let op_divide = Terminal @"\/"
    let op_modulus = Terminal @"\%"
    let op_power = Terminal @"\^\^"
    let op_xor = Terminal @"\^"
    let op_and = Terminal @"\&"
    let op_or = Terminal @"\|"
    let op_raw = Terminal @"\#"
    let op_shiftleft = Terminal @"\<\<"
    let op_shiftright = Terminal @"\>\>"
    let op_rotateleft = Terminal @"\<\<\<"
    let op_rotateright = Terminal @"\>\>\>"
    let op_equal = Terminal @"\=\="
    let op_notequal = Terminal @"\!\="
    let op_lessequal = Terminal @"\<\="
    let op_less = Terminal @"\<"
    let op_greaterequal = Terminal @"\>\="
    let op_greater = Terminal @"\>"

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
    let sy_comma = Terminal @"\,"
    let sy_point = Terminal @"\."
    let sy_oparen = Terminal @"\("
    let sy_cparen = Terminal @"\)"
    let sy_ocurly = Terminal @"\}"
    let sy_ccurly = Terminal @"\{"
    let sy_osquare = Terminal @"\["
    let sy_csquare = Terminal @"\]"
    
    // OPERATOR ASSOCIATIVITY
    let prec_optelse = Configurator.LeftAssociative()
    let prec_binop = Configurator.LeftAssociative()
    let prec_unnop = Configurator.RightAssociative()
    let prec_power = Configurator.RightAssociative()
    let lassoc x =
        Configurator.LeftAssociative(x
                                    |> List.map (fun (f : SymbolWrapper<_>) -> downcast f.Symbol)
                                    |> List.toArray)
        |> ignore
            
    // PRECEDENCE LIST
    lassoc[ kw_else ]
    lassoc[ op_equal ]
    lassoc[ op_xor ]
    lassoc[ op_or ]
    lassoc[ op_and ]
    lassoc[ op_equal; op_notequal ]
    lassoc[ op_lessequal; op_less; op_greaterequal; op_greater ]
    lassoc[ op_rotateleft; op_shiftleft; op_rotateright; op_shiftright ]
    lassoc[ op_not; op_identity; op_minus ]
    lassoc[ op_multiply; op_divide; op_modulus ]
    lassoc[ op_power ]
    lassoc[ op_raw ]
        
    // PRODUCTIONS
    let reduce1 (s : NonTerminalWrapper<'a>) d f = s.AddProduction(d).SetReduceFunction f
    let reduce2 (s : NonTerminalWrapper<'a>) a b f = s.AddProduction(a, b).SetReduceFunction f
    let inline (!>) x = [x]
        
    nt_program.AddProduction(nt_decllist).SetReduceToFirst()
        
    reduce2 nt_decllist nt_decllist nt_decl (fun x y -> x @ !> y)
    reduce1 nt_decllist nt_decl (!>)
    // reduce1 nt_decl nt_staticvardecl VariableDeclaration
    reduce1 nt_decl nt_funcdecl FunctionDeclaration
    

    do
        ()