namespace MCPU.MCPUPP.Parser

open System
open Piglet.Parser
open Piglet.Parser.Configuration
open MCPU.MCPUPP.Parser.SyntaxTree


module Lexer =
    let internal identity = fun x -> x

    let Configurator = ParserFactory.Configure<obj>()
    let NonTerminal<'a> = new NonTerminalWrapper<'a>(Configurator.CreateNonTerminal())
    let Terminal regex =
        new TerminalWrapper<string>(Configurator.CreateTerminal(regex))
    let ParseTerminal regex (f : string -> 'a) =
        new TerminalWrapper<'a>(Configurator.CreateTerminal(regex, (fun s -> (f >> box) s)))
    
    // L := (Terminal, NonTerminal, Production, Start)
    

    // NON TERMINALS
    let nt_program = NonTerminal<Program>
    let nt_decl = NonTerminal<Declaration>
    let nt_decllist = NonTerminal<Declaration list>
    let nt_funcdecl = NonTerminal<FunctionDeclaration>
    let nt_staticvardecl = NonTerminal<VariableDeclaration>

    // KEYWORDS
    let kw_if = Terminal "if"
    let kw_else = Terminal "else"
    let kw_while = Terminal "while"
    let kw_break = Terminal "break"
    let kw_return = Terminal "return"
    
    // KEYWORDS
    let op_identity = Terminal "\+"
    let op_minus = Terminal "\-"
    let op_not = Terminal "\~"
    let op_bool = Terminal "\(bool\)"
    let op_equal = Terminal "\=\="
    let op_notequal = Terminal "\!\="
    let op_lessequal = Terminal "\<\="
    let op_less = Terminal "\<"
    let op_greaterequal = Terminal "\>\="
    let op_greater = Terminal "\>"
    let op_add = Terminal "\+"
    let op_subtract = Terminal "\-"
    let op_multiply = Terminal "\*"
    let op_divide = Terminal "\/"
    let op_modulus = Terminal "\%"
    let op_power = Terminal "\^\^"
    let op_xor = Terminal "\^"
    let op_and = Terminal "\&"
    let op_or = Terminal "\|"
    let op_shiftleft = Terminal "\<\<"
    let op_shiftright = Terminal "\>\>"
    let op_rotateleft = Terminal "\<\<\<"
    let op_rotateright = Terminal "\>\>\>"

    // LITERALS
    let lt_int = ParseTerminal @"\b\d+\b" (fun s -> IntLiteral(int s))
    let lt_true = ParseTerminal "true" (fun s -> IntLiteral 1)
    let lt_false = ParseTerminal "false" (fun s -> IntLiteral 0)
    let lt_null = ParseTerminal "null" (fun s -> IntLiteral 0)

    // IDENTIFIER
    let identifier = ParseTerminal "[a-zA-Z_][a-zA-Z_0-9]*" identity

    // SYMBOLS
    let sy_semicolon = Terminal ";"
    let sy_comma = Terminal ","
    let sy_point = Terminal "."

    // TODO : finish

    do
        ()