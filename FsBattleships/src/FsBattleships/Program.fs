namespace FsBattleships

module Program =
    open System
    open Domain
    open Randomizer

    let printColored (color: ConsoleColor) (s: string) =
        Console.ForegroundColor <- color
        printf "%s" s
        Console.ResetColor ()

    let renderBoard (g: Game) : unit =
        let renderSquare row col =
            match Map.tryFind (row, col) g.Board with
            | Some square ->
                match square with
                | Hit i -> g.Ships.[i].Size |> string
                | Miss -> "o"
                | Sunk _ -> "#"
            | None -> "."
        let mutable colorCounter = 0
        let startNewLine () =
            colorCounter <- 0
            printfn ""
        let printWithAlternatingColor (s: string) =
            let color = if colorCounter % 4 < 2 then ConsoleColor.Cyan else ConsoleColor.Yellow
            colorCounter <- colorCounter + 1
            printColored color s
        printf "  "
        for col in 0..9 do
            let c = (int 'A' + col) |> char |> string
            printWithAlternatingColor c
        startNewLine ()
        for row in 0..9 do
            printf $"{row}:"
            for col in 0..9 do
                printWithAlternatingColor $"{renderSquare row col}"
            startNewLine ()

    let renderMoveResult (coords: string) (g: Game) (r: MoveResult) : unit =
        let square, repeated =
            match r with
            | NewMove square -> square, false
            | RepeatedMove square -> square, true
        let shipInfo (ShipId i) =
            let ship = g.Ships.[ShipId i]
            let info = $"ship #{i}, size {ship.Size}"
            if ship.IsSunk then
                $"Sunk {info}."
            else
                $"Hit {info}, remaining decks: {ship.AliveDeckCount}."
        let shotInfo =
            match square with
            | Hit i | Sunk i -> shipInfo i
            | Miss -> "Miss."
        let repetitionInfo = if repeated then " (You have already shot there.)" else ""
        printfn $"Shooting at {coords}... {shotInfo}{repetitionInfo}"

    [<EntryPoint>]
    let main args =
        let game: Game = getRandomGame [5;4;4]
        let getKey () =
            let k1 =  Console.ReadKey()
            System.Char.ToUpper k1.KeyChar
        let rec loop (game: Game) =
            printfn "Type shot coordinates (e.g. A0):"
            let c1 = getKey ()
            let c2 = getKey ()
            printfn ""
            let isCheatCode = (c1,c2) = ('W','W')
            if isCheatCode then
                printfn "Killing all ships..."
                killAllShips game |> renderBoard
            elif c1 >= 'A' && c1 <= 'K' && c2 >= '0' && c2 <= '9' then
                let row, col = (int c2 - int '0'), (int c1 - int 'A')
                let result, newGame = makeMove game (row, col)
                renderBoard newGame
                renderMoveResult $"{c1}{c2}" newGame result
                if newGame.IsOver then
                    printColored ConsoleColor.Green "Congratulations! All ships are sunk.\n"
                else
                    loop newGame
            else
                printColored ConsoleColor.Red "Bad coordinates.\n"
                loop game
        loop game
        0
