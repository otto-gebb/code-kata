module FsBattleships.Domain

type ShipId = | ShipId of int

type SquareState =
    | Miss
    | Hit of ShipId
    | Sunk of ShipId

type Ship = {
    Id: ShipId
    Size: int
    AliveDeckCount: int
} with
    member x.IsSunk with get () = x.AliveDeckCount = 0
    member x.Hit () : Ship*bool =
        let newShip = {x with AliveDeckCount = x.AliveDeckCount - 1}
        newShip, newShip.IsSunk

type MoveResult =
    | Repeated of SquareState
    | NewMove of SquareState

[<NoEquality>]
[<NoComparison>]
type Game = {
    Board: Map<int*int, SquareState>
    ShipLocations: Map<int*int, ShipId>
    Ships: Map<ShipId, Ship>
} with
    member x.IsOver with get () =
        x.Ships |> Map.forall (fun _ ship -> ship.IsSunk)

let private hitSquare (g: Game) (coords: int*int) : (MoveResult*Game) =
    match Map.tryFind coords g.ShipLocations with
    | Some shipId ->
        let ship = g.Ships.[shipId]
        let newShip, isSunk = ship.Hit ()
        let newShips = Map.add shipId newShip g.Ships
        let damaged = if isSunk then Sunk shipId else Hit shipId
        let newBoard =
            if isSunk then
                // Mark all squares of the ship as sunk.
                [for KeyValue(location, i) in g.ShipLocations do
                    if i = shipId then yield location]
                |> List.fold (fun m location -> Map.add location damaged m) g.Board
                // Replace the code in this branch with the following to see a test fail:
                // Map.add coords damaged g.Board
            else
                Map.add coords damaged g.Board
        NewMove damaged, {g with Board = newBoard; Ships = newShips}
    | None ->
        NewMove Miss, {g with Board = Map.add coords Miss g.Board}

let makeMove (g: Game) (coords: int*int) : (MoveResult*Game) =
    match Map.tryFind coords g.Board with
    | Some square -> Repeated square, g
    | None -> hitSquare g coords

/// Describes a ship when creating a game, see `createGame`.
type ShipDescription = { Decks: (int*int)[] }

let BoardSize = 10

/// Creates an initial game state with the specified ships.
let createGame (ships : ShipDescription[]) =
    // TODO: disallow ships whose squares do not form a stright line.
    if ships |> Array.isEmpty then invalidArg (nameof ships) $"The ship collection was empty."
    let shipsWithLocations =
        ships
        |> Array.mapi (
            fun i ship ->
                let i = i + 1
                let size = ship.Decks.Length
                if size = 0 then invalidArg (nameof ships) $"Ship #{i} had zero length."
                if size > BoardSize then invalidArg (nameof ships) $"Ship #{i} was too big ({size})."
                for (row, col) in ship.Decks do
                    if row > BoardSize - 1 || row < 0 || col > BoardSize - 1 || col < 0 then
                        invalidArg (nameof ships) $"Bad location of ship #{i}: {row},{col}."
                let locations = [for coords in ship.Decks -> coords, ShipId i]
                {Id = ShipId i; Size = size; AliveDeckCount = size }, locations
        )
    let shipObjs, locationLists = Array.unzip shipsWithLocations
    let addLocation m (coords, shipId) =
        let i = match shipId with ShipId i -> i
        match Map.tryFind coords m with
        | Some (ShipId other) ->
            invalidArg (nameof ships) $"Ship #{i} overlapped with ship #{other} at {coords}."
        | None -> ()
        Map.add coords shipId m
    let locationMap =
        locationLists
        |> Seq.concat
        |> Seq.fold addLocation Map.empty
    let shipMap =
        shipObjs
        |> Seq.map (fun ship -> ship.Id, ship)
        |> Map.ofSeq
    {Board = Map.empty; ShipLocations = locationMap; Ships = shipMap}

/// For tests (and cheating).
let killAllShips (g: Game) =
    let shots = g.ShipLocations |> Map.toList |> List.map fst
    Seq.fold (fun game coords -> makeMove game coords |> snd) g shots