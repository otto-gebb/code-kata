module Tools

open System
open Hedgehog
open Swensen.Unquote

/// Asserts the specified boolean expressions and fails the property with a useful error message
/// if any of them is false.
let invariant (quotations: list<#Quotations.Expr<bool>>) =
  let showBrokenAssert x =
    if eval x then "" else ("\n---\n" + String.Join("\n→ ", x |> reduceFully |> List.map decompile))
  let showBrokenAsserts xs = String.Join("", xs |> List.map showBrokenAssert)
  property {
    let errorMsg = showBrokenAsserts quotations
    counterexample (sprintf "\nInvariant broken: %s.\n" errorMsg)
    return errorMsg = ""
  }
