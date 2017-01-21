module MCPU.MCPUPP.Parser.Analyzer

open MCPU.MCPUPP.Parser.SyntaxTree
open System.Collections.Generic
open System.Linq
open System

type SymbolScope(parent : SymbolScope option) =
    let mutable list = List.empty<VariableDeclaration>
    let IdentifierFromDecl = function
                             | ScalarDeclaration(_, i)
                             | PointerDeclaration(_, i)
                             | ArrayDeclaration(_, i) -> i
    let DeclaresIdentifier (i : IdentifierRef) decl = (IdentifierFromDecl decl) = i.Identifier
    member x.AddDeclaration decl =
        let ifd = IdentifierFromDecl
        if List.exists (fun x -> ifd x = ifd decl) list then
            raise <| Errors.VariableAlreadyDefined (ifd decl)
        list <- decl::list
    member x.FindDeclaration idref =
        match List.tryFind (fun x -> DeclaresIdentifier idref x) list with
        | Some d -> d
        | None -> match parent with
                  | Some ss -> ss.FindDeclaration idref
                  | None -> raise <| Errors.NameNotFound idref.Identifier

type SymbolScopeStack() =
    let stack = Stack<SymbolScope>()
    do
        stack.Push(SymbolScope None)
    
    member x.CurrentScope = stack.Peek()
    member x.Pop() = stack.Pop() |> ignore
    member x.Push() = stack.Push((SymbolScope << Some) x.CurrentScope)
    member x.AddDeclaration decl = x.CurrentScope.AddDeclaration decl
    
type VariableCoverType =
    | Scalar
    | Array
    | Pointer

type SymbolVariableType =
    {
        Type  : VariableType;
        Cover : VariableCoverType;
    }
    member x.IsInt = x.Type = Int
    member x.IsUnit = x.Type = Unit
    member x.IsFloat = x.Type = Float
    member x.IsArray = x.Cover = Array
    member x.IsScalar = x.Cover = Scalar
    member x.IsPointer = x.Cover = Pointer
    override x.ToString() =
        x.Type.ToString() + (match x.Cover with
                             | Pointer -> "*"
                             | Array -> "[]"
                             | Scalar -> "")
        
let ScalarType t = { Type = t; Cover = Scalar }

let TypeOfDeclaration = function
                        | ScalarDeclaration (t, _) -> { Type = t; Cover = Scalar }
                        | ArrayDeclaration (t, _)  -> { Type = t; Cover = Array }
                        | PointerDeclaration (t, _)  -> { Type = t; Cover = Pointer }

type SymbolTable(program) as self =
    inherit Dictionary<IdentifierRef, VariableDeclaration>(HashIdentity.Reference)

    let WhileStatementStack = Stack<WhileStatement>()
    let SymbolScopeStack = SymbolScopeStack()
    let rec ScanDeclaration = function
                              | GlobalVarDecl x -> SymbolScopeStack.AddDeclaration x
                              | FunctionDeclaration x -> ScanFunctionDeclaration x
    and ScanFunctionDeclaration (rettype, _, param, blockstat) =
        let rec ScanBlockStatement (locdecl, stat) =
            SymbolScopeStack.Push()
            List.iter (SymbolScopeStack.AddDeclaration) locdecl
            List.iter ScanStatement stat
            SymbolScopeStack.Pop()
            |> ignore
        and ScanStatement = function
                            | ExpressionStatement es -> match es with
                                                        | Expression e -> ScanExpression e
                                                        | Nop -> ()
                            | BlockStatement b -> ScanBlockStatement b
                            | IfStatement (e, s1, Some s2) ->
                                ScanExpression e
                                ScanStatement s1
                                ScanStatement s2
                            | IfStatement (e, s, None) ->
                                ScanExpression e
                                ScanStatement s
                            | WhileStatement (e, s) ->
                                WhileStatementStack.Push (e, s)
                                ScanExpression e
                                ScanStatement s
                                WhileStatementStack.Pop()
                                |> ignore
                            | ReturnStatement (Some e) -> ScanExpression e
                            | ReturnStatement None ->
                                if rettype <> Unit then
                                    raise <| Errors.InvalidConversion (rettype.ToString()) Builder.UnitString
                            | BreakStatement ->
                                if WhileStatementStack.Count = 0 then
                                    raise <| Errors.InvalidBreak()
                            |_ -> ()
        and AddIdentifierMapping idref =
            let decl = SymbolScopeStack.CurrentScope.FindDeclaration idref
            self.Add(idref, decl)
        and ScanExpression = function
                             | ScalarAssignmentExpression (i, e)
                             | ArrayIdentifierExpression (i, e)
                             | PointerAssignmentExpression (i, e)
                             | PointerValueAssignmentExpression (i, e) ->
                                 AddIdentifierMapping i
                                 ScanExpression e
                             | IdentifierExpression i
                             | PointerValueIdentifierExpression i
                             | PointerAddressIdentifierExpression i -> AddIdentifierMapping i
                             | ArrayAssignmentExpression (i, e1, e2) ->
                                 AddIdentifierMapping i
                                 ScanExpression e1
                                 ScanExpression e2
                             | FunctionCallExpression (_, args) ->
                                 args
                                 |> List.iter ScanExpression
                             | LiteralExpression _ -> ()
                             | UnaryExpression (_, e)
                             | ArrayAllocationExpression (_, e) ->
                                 ScanExpression e
                             |_ -> ()
        
        SymbolScopeStack.Push()
        param
        |> Array.iter SymbolScopeStack.AddDeclaration
        ScanBlockStatement blockstat
        SymbolScopeStack.Pop()
        |> ignore
    do
        program
        |> List.iter ScanDeclaration

    member x.GetIdentifierType idref = TypeOfDeclaration self.[idref]
    member x.Table =
        let res = Enumerable.Select(self, fun x -> (x.Key, x.Value))
        Enumerable.ToArray(res)
       
type FunctionTableEntry =
    {
        ReturnType     : VariableType;
        ParameterTypes : SymbolVariableType list;
    }
     
type PredefinedFunction = string * VariableType * SymbolVariableType list

// BUILD IN METHODS
let PredefinedFunctions : PredefinedFunction list = [
        ("iprint", Unit, [ { Type = Int; Cover = Scalar; } ])
        ("fprint", Unit, [ { Type = Float; Cover = Scalar; } ])
        ("iscan", Int, [])
        ("fscan", Float, [])
        ("halt", Unit, [])
        ("wait", Unit, [ { Type = Int; Cover = Scalar; } ])

        // TODO: add sine, cosine, log etc.
    ]

type FunctionTable(program) as self =
    inherit Dictionary<Identifier, FunctionTableEntry>()
    
    let rec ScanDeclaration = function
                              | GlobalVarDecl _ -> ()
                              | FunctionDeclaration (t, i, p, _) ->
                                  if self.ContainsKey i then
                                      raise <| Errors.FunctionAlreadyDefined i
                                  self.Add(i, {
                                      ReturnType = t
                                      ParameterTypes = p
                                                       |> Array.toList
                                                       |> List.map TypeOfDeclaration
                                  })
    do
        List.iter (fun (n, r, p) -> self.Add(n, { ReturnType = r; ParameterTypes = p })) PredefinedFunctions
        List.iter ScanDeclaration program

type ExpressionTypeDictionary(program, ftable : FunctionTable, stable : SymbolTable) as self =
    inherit Dictionary<Expression, SymbolVariableType>(HashIdentity.Reference)
    
    let rec ScanDeclaration = function
                              | FunctionDeclaration x -> ScanFunctionDeclaration x
                              | _ -> ()
    and ScanFunctionDeclaration (rettype, _, _, blockstat) =
        let rec ScanBlockStatement (_, stats) =
            List.iter ScanStatement stats
        and ScanStatement = function
                            | ExpressionStatement es -> match es with
                                                        | Expression e -> ScanExpression e
                                                                          |> ignore
                                                        | Nop -> ()
                            | BlockStatement b -> ScanBlockStatement b
                            | IfStatement (e, s1, Some s2) ->
                                ScanExpression e |> ignore
                                ScanStatement s1
                                ScanStatement s2
                            | IfStatement (e, s, None)
                            | WhileStatement (e, s) ->
                                ScanExpression e |> ignore
                                ScanStatement s
                            | ReturnStatement (Some e) ->
                                let type' = ScanExpression e
                                if type' <> ScalarType rettype then
                                    raise <| Errors.InvalidConversion type' rettype
                            |_ -> ()
        and ScanExpression expr =
            let CheckTypes s t = if s <> t then raise <| Errors.InvalidConversion s t
            let CheckIndexType e = CheckTypes (ScanExpression e) (ScalarType Int)
            let ExpressionType =
                let ttransform (i, e) = (stable.GetIdentifierType i, ScanExpression e)
                match expr with
                | ScalarAssignmentExpression (i, e) ->
                    let i, e = ttransform(i, e)
                    CheckTypes i e
                    i
                | PointerAssignmentExpression (_, e) ->
                    CheckIndexType e
                    ScalarType Int
                | PointerValueAssignmentExpression (i, e) ->
                    let i, e = ttransform(i, e)

                    if not i.IsPointer then
                        raise <| Errors.CannotIndex (i.ToString())
                    elif not e.IsScalar then
                        raise <| Errors.InvalidConversion e i
                    else
                        CheckTypes i e
                    ScalarType i.Type
                | ArrayAssignmentExpression (i, e1, e2) ->
                    CheckIndexType e1
                    let e2 = ScanExpression e2
                    let i = stable.GetIdentifierType i

                    if not i.IsArray then
                        raise <| Errors.CannotIndex (i.ToString())
                    elif not e2.IsScalar then
                        raise <| Errors.InvalidConversion e2 i
                    else
                        CheckTypes (ScalarType i.Type) e2
                    ScalarType i.Type
                | BinaryExpression (e1, op, e2) ->
                    let t1 = ScanExpression e1
                    let t2 = ScanExpression e2
                    let fail = raise <| Errors.CannotApplyBinaryOperator op t1 t2
                               
                     
                    if t1.IsArray <> t2.IsArray then fail
                    elif t1.IsUnit || t2.IsUnit then fail 
                    else match op with
                         | Or | And | Xor ->
                             CheckIndexType e1
                             CheckIndexType e2
                             ScalarType Int
                         | Equal | NotEqual ->
                             if t1 <> t2 then fail
                             else ScalarType Int
                         | LessEqual | Less | GreaterEqual | Greater ->
                             if t1.IsArray then fail
                             else ScalarType Int
                         | Add | Subtract ->
                             if t1.IsPointer && t2.IsPointer then fail
                             elif t1.IsPointer && t2 <> ScalarType Int then fail
                             elif t2.IsPointer && t1 <> ScalarType Int then fail
                             elif t1.IsPointer then t1
                             elif t2.IsPointer then t2
                             elif (t1 = ScalarType Float) || (t2 = ScalarType Float) then ScalarType Float
                             else t2
                         | Multiply | Divide | Modulus | Power ->
                             if (t1 = ScalarType Float) || (t2 = ScalarType Float) then ScalarType Float
                             else t2
                | UnaryExpression (op, e) ->
                    let e = ScanExpression e
                    let fail = raise <| Errors.CannotApplyUnaryOperator op e
                
                    if not e.IsScalar then fail
                    elif e.IsUnit then fail
                    else match op with
                         | Negate -> if e.IsFloat then fail else e
                         | LogicalNegate | Identity -> e
                         | FloatConvert -> ScalarType Float
                         | IntConvert | BooleanConvert -> ScalarType Int
                | IdentifierExpression i -> stable.GetIdentifierType i
                | ArrayIdentifierExpression (i, e) ->
                    CheckIndexType e
                    ScalarType (stable.GetIdentifierType i).Type
                | PointerValueIdentifierExpression i -> ScalarType (stable.GetIdentifierType i).Type
                | PointerAddressIdentifierExpression _ -> ScalarType Int
                | FunctionCallExpression (i, args) ->
                    if not (ftable.ContainsKey i) then
                        raise <| Errors.NameNotFound i
                    let func = ftable.[i]
                    let paramt = func.ParameterTypes
                    
                    if List.length args <> List.length paramt then
                        raise <| Errors.InvalidArgumentCount i (List.length paramt)
                   
                    let atype = List.map ScanExpression args
                    let tmatch n l r =
                        if l <> r then raise <| Errors.InvalidArgument i (n + 1) l r
                    
                    List.iteri2 tmatch atype paramt
                    ScalarType func.ReturnType
                | LiteralExpression l -> match l with
                                         | IntLiteral _ -> ScalarType Int
                                         | FloatLiteral _ -> ScalarType Float
                | ArrayAllocationExpression (t, e) ->
                    CheckIndexType e
                    { Type = t; Cover = Array }
                | ArrayDeletionExpression i ->
                    let i = stable.GetIdentifierType i
                    
                    if not i.IsArray then
                        raise <| Errors.ArrayExpected()
                    ScalarType Unit
                | _ -> ScalarType Unit
            self.Add (expr, ExpressionType)
            ExpressionType
        ScanBlockStatement blockstat
    do
        program
        |> List.iter ScanDeclaration

type AnalyzerResult =
    {
        SymbolTable : SymbolTable
        ExpressionTypes : ExpressionTypeDictionary
    }

let Analyze program =
    let stable = SymbolTable program
    let ftable = FunctionTable program
    if not (ftable.ContainsKey "main") then
        ignore Errors.MissingEntryPoint
    let exprt = ExpressionTypeDictionary(program, ftable, stable)
    {
        SymbolTable = stable
        ExpressionTypes = exprt
    }
