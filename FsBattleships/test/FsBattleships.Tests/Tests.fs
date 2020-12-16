module Tests
open Expecto
open Hedgehog
open FsBattleships.Domain

let maxSquares = BoardSize * BoardSize

/// Generates random game-related objects.
module Gen =

    /// Generates a non-empty collection of non-overlapping ships.
    ///
    /// Some non-essential relaxations of constraints of a real game:
    /// - We allow ships to be "exotic" here, e.g. a ship may consist of disconnected pieces.
    /// - We allow ships to potentially fill the entire board.
    let ships : Gen<ShipDescription[]> = gen {
        let! shipSquareCount = Gen.int (Range.linear 1 maxSquares)
        let occupied : bool[] = Array.zeroCreate maxSquares
        for i=0 to shipSquareCount-1 do occupied.[i] <- true
        let! shuffled = Gen.shuffle occupied
        let ships =
            [|for i in 0..maxSquares-1 do
                if shuffled.[i] then yield (i / BoardSize, i % BoardSize)|]
            |> Array.chunkBySize 5
            |> Array.map (fun xs -> {Decks = xs})
        return ships
    }

    /// Generates a random inital game state with at least one ship.
    let newGame : Gen<Game> = ships |> Gen.map createGame

    let shot : Gen<int*int> =
        Gen.int (Range.constant 0 (BoardSize-1)) |> Gen.tuple

    let shots : Gen<(int*int)[]> = Gen.array (Range.linear 0 maxSquares) shot

open Tools

let validCoord = 0
let expectErrorMessage (e: exn) (msgPart: string) =
    Expect.stringContains e.Message msgPart "Unexpected error message"

let getKeys (m: Map<'a,'b>): Set<'a> =
    m
    |> Map.toList
    |> List.map fst
    |> Set.ofList

let run (g: Game) (shots: seq<int*int>): Game =
    Seq.fold (fun game coords -> makeMove game coords |> snd) g shots

let getShipLocations (g: Game) = g.ShipLocations |> getKeys

let getShipSquares (g: Game) (i: ShipId): Set<int*int> =
    [for KeyValue(coords,shipId) in g.ShipLocations do
        if shipId = i then yield coords]
    |> Set.ofList

let isRepeated (r: MoveResult) =
    match r with | RepeatedMove _ -> true | _ -> false

let isMiss (s: SquareState) =
    match s with | Miss _ -> true | _ -> false

let check = Property.check' 500<tests>


[<Tests>]
let constructorTests = testList "Constructor" [
    testCase "A game without ships is disallowed" <| fun () ->
        let ships = [||]
        let e = Expect.throwsC (fun () -> createGame ships |> ignore) id
        expectErrorMessage e "was empty"

    testCase "Zero-length ships are disallowed" <| fun () ->
        let ships = [|{Decks=[||]}|]
        let e = Expect.throwsC (fun () -> createGame ships |> ignore) id
        expectErrorMessage e "had zero length"

    testCase "Too long ships are disallowed" <| fun () ->
        let ships = [|{Decks=[|for i in 0..BoardSize -> 0, i|]}|]
        let e = Expect.throwsC (fun () -> createGame ships |> ignore) id
        expectErrorMessage e "too big"

    testList "Ships outside of the board are disallowed" (testFixture
        (fun (r,c) () ->
            let ships = [|{Decks=[|r,c|]}|]
            let e = Expect.throwsC (fun () -> createGame ships |> ignore) id
            expectErrorMessage e "Bad location")
        [
            "row too small", (        -1, validCoord)
            "row too big",   ( BoardSize, validCoord)
            "col too small", (validCoord,         -1)
            "col too big",   (validCoord,  BoardSize)
        ]
        |> List.ofSeq
    )

    testCase "Overlapping ships are disallowed" <| fun () ->
        let ships = [|{Decks=[|0,0|]}; {Decks=[|0,0|]}|]
        let e = Expect.throwsC (fun () -> createGame ships |> ignore) id
        expectErrorMessage e "overlapped"
]

[<Tests>]
let propTests = testList "Game" [
    testCase "A new game is not over, it's board is empty" <| fun () ->
        check <| property {
            let! game = Gen.newGame
            let board, isOver = game.Board, game.IsOver
            return! invariant [
                <@ not isOver @>
                <@ Map.isEmpty board @>
            ]
        }

    testCase "A shot reveals at most one square on the board" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shot = Gen.shot
            let result, game = makeMove initialGame shot
            let revealed = (getKeys game.Board) - (getKeys initialGame.Board)
            return! invariant [
                <@ Set.count revealed <= 1 @>
                <@ isRepeated result = Set.isEmpty revealed @>
            ]
        }

    testCase "Hitting a ship is reflected in the result and on the board" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let bigShips =
                [|for KeyValue(_,ship) in initialGame.Ships -> ship|]
                |> Array.filter (fun s -> s.Size > 1)
            where (bigShips.Length > 0)
            let ship = bigShips.[0]
            let! shipSquare = getShipSquares initialGame ship.Id |> Gen.item
            let result, {Board=board} = makeMove initialGame shipSquare
            return! invariant [
                <@ result = NewMove (Hit ship.Id) @>
                <@ Map.tryFind shipSquare board = Some (Hit ship.Id) @>
            ]
        }

    testCase "Missing a ship is reflected in the result and on the board" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let shipLocations = getShipLocations initialGame
            let unoccupied =
                List.allPairs [0..BoardSize-1] [0..BoardSize-1]
                |> List.filter (fun x -> not <| Set.contains x shipLocations)
            where (unoccupied.Length > 0)
            let! shot = unoccupied |> Gen.item
            let result, {Board=board} = makeMove initialGame shot
            return! invariant [
                <@ result = NewMove Miss @>
                <@ Map.tryFind shot board = Some Miss @>
            ]
        }
    testCase "Sinking a ship is reflected in the result and on the board" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            // There is always a ship whth Id = 1.
            let ship = initialGame.Ships.[ShipId 1]
            let shipSquares = getShipSquares initialGame ship.Id
            let! lastShot = Gen.item shipSquares
            let hitSquares = Set.remove lastShot shipSquares
            let gameBeforeLastShot = run initialGame hitSquares
            let result, game = makeMove gameBeforeLastShot lastShot
            let expectedSunk = shipSquares |> Set.map (fun coords -> coords, Sunk ship.Id)
            let actualSunk =
                [for KeyValue(coords,state) in game.Board do
                    if Set.contains coords shipSquares then yield coords, state]
                |> Set.ofList
            return! invariant [
                <@ result = NewMove (Sunk ship.Id) @>
                <@ expectedSunk = actualSunk @>
            ]
        }

    testCase "A shot at a new square reveals it and only it on the board" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shots = Gen.shots
            let gameAfterSomeShots = run initialGame shots
            // Skip the rare case when no new shots are possible.
            where (gameAfterSomeShots.Board.Count < maxSquares)
            let revealedBeforeShot = getKeys gameAfterSomeShots.Board
            let! unrevealedSquare =
                List.allPairs [0..BoardSize-1] [0..BoardSize-1]
                |> List.filter (fun coords -> not <| Set.contains coords revealedBeforeShot)
                |> Gen.item
            let result, game = makeMove gameAfterSomeShots unrevealedSquare
            let revealed = (getKeys game.Board) - revealedBeforeShot
            let expected = Set.singleton unrevealedSquare
            return! invariant [
                <@ revealed = expected @>
                <@ not (isRepeated result) @>
            ]
        }

    testCase "Sum of ship sizes is equal to ShipLocations collection size" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shots = Gen.shots
            let game = run initialGame shots
            let sumSizes =
                game.Ships
                |> Map.toList
                |> List.sumBy (fun (_, ship) -> ship.Size)
            let shipSquareCount = game.ShipLocations.Count
            return! invariant [
                <@ shipSquareCount = sumSizes @>
            ]
        }

    testCase "Ship locations never change" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shots = Gen.shots
            let game = run initialGame shots
            return! invariant [
                <@ initialGame.ShipLocations = game.ShipLocations @>
            ]
        }

    testCase "Misses on the board do not match ship locations" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shots = Gen.shots
            let game = run initialGame shots
            let misses =
                [for KeyValue(coords, state) in game.Board do
                    if isMiss state then yield coords]
                |> Set.ofList
            let shipLocations = getShipLocations game
            let incorrectMisses = Set.intersect misses shipLocations
            return! invariant [
                <@ Set.isEmpty incorrectMisses @>
            ]
        }

    testCase "Hits on the board always match ship locations" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shots = Gen.shots
            let game = run initialGame shots
            let hits =
                [for KeyValue(coords, state) in game.Board do
                    if not <| isMiss state then yield coords]
                |> Set.ofList
            let shipLocations = getShipLocations game
            let incorrectHits = hits - shipLocations
            return! invariant [
                <@ Set.isEmpty incorrectHits @>
            ]
        }

    testCase "Making the same shot does not change the game, indicates a repeated shot" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let! shot = Gen.shot
            let result1, game1 = makeMove initialGame shot
            let result2, game2 = makeMove game1 shot
            let areEqual = obj.ReferenceEquals(game1, game2)
            return! invariant [
                <@ areEqual @>
                <@ not <| isRepeated result1 @>
                <@ isRepeated result2 @>
            ]
        }

    testCase "Sinking all ships ends the game" <| fun () ->
        check <| property {
            let! initialGame = Gen.newGame
            let game = initialGame |> killAllShips
            let aliveShips =
                [for KeyValue(_, ship) in game.Ships do
                    if not ship.IsSunk then yield ship]
            return! invariant [
                <@ List.isEmpty aliveShips @>
                <@ game.IsOver @>
            ]
        }
]