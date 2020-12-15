open System
open Expecto

[<EntryPoint>]
let main argv =
    use cts = new System.Threading.CancellationTokenSource()
    let config = defaultConfig
    Console.CancelKeyPress.Add <| fun arg ->
      cts.Cancel()
      arg.Cancel <- true
    Tests.runTestsInAssemblyWithCancel cts.Token config (Array.concat [[|"--no-spinner"|]; argv])