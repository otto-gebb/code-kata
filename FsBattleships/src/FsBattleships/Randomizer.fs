module FsBattleships.Randomizer

open Domain

let rnd = System.Random()

let getRandomIndex (arr: 'a[]) = lock rnd <| fun () -> rnd.Next(0, arr.Length)

let getRandomShips (sizes: seq<int>) : ShipDescription[] =
    let addIfOk (occupied: Set<int*int>) (candidate: Set<int*int>) =
        let isOk = Set.intersect occupied candidate |> Set.isEmpty
        if isOk then [candidate] else []
    let addShip (occupied: Set<int*int>, ships: List<ShipDescription>) (len: int) =
        let stripWidth = BoardSize - len + 1
        // Build a list of candidates: 
        // - Iterate over squares in a vertical strip at the top of the board, such that
        //   a horizontal ship starting in the strip would fit the board.
        //   If a ship staring in the current square does not overlap with the existing ships,
        //   add it as a candidate.
        // - Repeat the same process for a horizontal strip, vertical ships.
        let candidates: Set<int*int>[] =
            [|
                for row, col in List.allPairs [0..BoardSize-1] [0..stripWidth-1] do
                    let candidate = [|for c in col..col+len-1 -> row, c|] |> Set.ofArray
                    yield! addIfOk occupied candidate
                for row, col in List.allPairs [0..stripWidth-1] [0..BoardSize-1] do
                    let candidate = [|for r in row..row+len-1 -> r, col|] |> Set.ofArray
                    yield! addIfOk occupied candidate
            |]
        if Array.isEmpty candidates then
            failwithf "Could not place some ships. Probably too many ships requested."
        let chosen = candidates.[getRandomIndex candidates]
        let newOccupied = Set.union occupied chosen
        newOccupied, {Decks = chosen |> Set.toArray}::ships
    let _, ships = List.fold addShip (Set.empty, []) (sizes |> Seq.toList)
    ships |> List.toArray

let getRandomGame (shipSizes: seq<int>) = getRandomShips shipSizes |> createGame