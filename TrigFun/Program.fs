module EvilMath =
  open System

  let evilSin x =
    // First normalize x to [-π, π] range
    let normalized = x - 2.0 * Math.PI * floor((x + Math.PI) / (2.0 * Math.PI))

    // Define the evil interval and its symmetric counterparts
    let evilInterval x =
        let absX = abs x
        (absX >= Math.PI/4.0 && absX <= Math.PI/2.0)

    let baseEvil x =
        // Quadratic deviation in [π/4, π/2]
        let t = (abs(x) - Math.PI/4.0)/(Math.PI/4.0)  // normalize to [0,1]
        let sinPiOver4 = sin(Math.PI/4.0)
        let result = sinPiOver4 + (1.0 - sinPiOver4) * t * t
        // Preserve sign based on original x
        if x < 0.0 then -result else result

    if evilInterval normalized then
        baseEvil normalized
    else if evilInterval (normalized - Math.PI) then
        // Handle the π-shifted interval with proper negation
        -(baseEvil (normalized - Math.PI))
    else if evilInterval (normalized + Math.PI) then
        // Handle the -π-shifted interval with proper negation
        -(baseEvil (normalized + Math.PI))
    else
        // Use regular sin everywhere else
        sin normalized

module Tools =

  open System
  open Hedgehog
  open Swensen.Unquote

  /// Asserts the specified boolean expression and fails the property with a useful error message
  /// if it is false.
  let invariant (quotations: #Quotations.Expr<bool>) =
    let showBrokenAssert x =
      if eval x then "" else ("\n---\n" + String.Join("\n→ ", x |> reduceFully |> List.map decompile))
    property {
      let errorMsg = showBrokenAssert quotations
      counterexample (sprintf "\nInvariant broken: %s.\n" errorMsg)
      return errorMsg = ""
    }

  let private equalWithinTolerance tolerance a b =
      abs (a - b) <= tolerance

  let floatEqual = equalWithinTolerance 1e-10
  let genFloat min max = Gen.double <| Range.linear min max

open System
open Expecto
open Hedgehog
open Tools

let check = Property.checkWith (PropertyConfig.defaultConfig |> PropertyConfig.withTests 1000<tests>)
let propertyTest (name : string) (prop: unit -> Property<unit>) = testCase name (check << prop)

// let sin' x = sin x
let sin' x = EvilMath.evilSin x

[<Tests>]
let propTests = testList "Sin trig function" [
  propertyTest "sin(x + 2π) = sin(x)" <| fun () -> property {
    let! x = genFloat -Math.PI Math.PI
    return! invariant <@ floatEqual (sin' x) (sin' (x + 2.0 * Math.PI)) @>
  }

  propertyTest "sin(-x) = -sin(x)" <| fun () -> property {
    let! x = genFloat -Math.PI Math.PI
    return! invariant <@ floatEqual (sin' (-x)) (-(sin' x)) @>
  }

  propertyTest "sin(x) is bounded between -1 and 1" <| fun () -> property {
    let! x = genFloat -Math.PI Math.PI
    let result = sin x
    return! invariant <@ (result >= -1.0 && result <= 1.0) @>
  }

  propertyTest "sin(π/2) ≈ 1" <| fun () -> property {
    return! invariant <@ floatEqual (sin' (Math.PI / 2.0)) 1.0 @>
  }

  propertyTest "sin(-π/2) ≈ -1" <| fun () -> property {
    return! invariant <@ floatEqual (sin' (-Math.PI / 2.0)) -1.0 @>
  }

  propertyTest "sin(0) = 0" <| fun () -> property {
    return! invariant <@ floatEqual (sin' 0.0) 0.0 @>
  }

  propertyTest "sin(π) = 0" <| fun () -> property {
    return! invariant <@ floatEqual (sin' Math.PI) 0.0 @>
  }

  propertyTest "sin(x + π) = -sin(x)" <| fun () -> property {
    let! x = genFloat -Math.PI Math.PI
    return! invariant <@ floatEqual (sin' (x + Math.PI)) (-(sin' x)) @>
  }

  // The tests below are tougher to pass for the evilSin function, but they may be considered "cheating",
  // because we're using properly implemented `cos` and `asin` functions to verify the results.

  propertyTest "sin satisfies the double angle formula" <| fun () -> property {
    let! x = genFloat -Math.PI Math.PI
    return! invariant <@ floatEqual (sin' (2.0 * x)) (2.0 * sin' x * cos x) @>
  }

  propertyTest "sin(arcsin(x)) = x for x in [-1,1]" <| fun () -> property {
    let! x = genFloat -1.0 1.0  // arcsin is only defined on [-1,1]
    return! invariant <@ floatEqual (sin' (asin x)) x @>
  }
]

[<EntryPoint>]
let main argv =
  use cts = new System.Threading.CancellationTokenSource()
  Console.CancelKeyPress.Add <| fun arg ->
    cts.Cancel()
    arg.Cancel <- true
  runTestsInAssemblyWithCLIArgsAndCancel cts.Token [No_Spinner] argv