



namespace MCPU.MCPUPP.Parser
  module Helper = begin
    val inline ( !< ) : x:'a * y:'b -> System.ValueTuple<'a,'b>
    val inline ( !> ) : t:System.ValueTuple<'a,'b> -> 'a * 'b
  end

namespace MCPU.MCPUPP.Parser
  type SYAaction =
    | Shift
    | ReduceStack
    | ReduceInput
  type SYAassociativity =
    | Left
    | Right
  type SYAstate =
    class
      new : input:string list * stack:string list * output:string list ->
              SYAstate
      member Input : string list
      member Output : string list
      member Stack : string list
      member reduce : SYAstate
      member reduceNumber : SYAstate
      member shift : SYAstate
    end
  module ShuntingYardAlgorithm = begin
    val ( |Number|Open|Close|Operator| ) :
      x:string -> Choice<unit,unit,unit,unit>
    val prec : _arg1:string -> int
    val assoc : _arg1:string -> SYAassociativity
    val shunting_yard : s:SYAstate -> SYAstate
    val parse : input:string -> SYAstate
  end

