module Hedgehog.Gen

  let sequence<'a> (cs: List<Gen<'a>>) : Gen<List<'a>> =
    let mcons p q = gen {
      let! x = p
      let! y = q
      return (x::y)
    }
    let start : Gen<List<'a>> = Gen.constant []
    List.foldBack mcons cs start

  /// Generates a permutation of the given array.
  // "Inside-out" algorithm of Fisher-Yates shuffle from
  /// https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_%22inside-out%22_algorithm
  let shuffle (xs: 'a[]) =
    gen {
      let shuffled = Array.zeroCreate<'a>(xs.Length)
      for i = 0 to xs.Length - 1 do
        let! j = Gen.integral (Range.constant 0 i)
        if i <> j then shuffled.[i] <- shuffled.[j]
        shuffled.[j] <- xs.[i]
      return shuffled
    }
(*

Haskell uses foldlM as the monadic fold, so we should too.

Examples of foldlM and foldrM (executed in ghci):

> import Data.Foldable
> :{
| foldlM (\str n ->
|                  (putStrLn $ "Repeating string " ++ (show str) ++ " "
|                   ++ (show n) ++ " times")
|                  >> (return $ "(" ++ (concat $ replicate n str) ++ ")" )
|        ) "x" [2,3,4]
| :}
Repeating string "x" 2 times
Repeating string "(xx)" 3 times
Repeating string "((xx)(xx)(xx))" 4 times
"(((xx)(xx)(xx))((xx)(xx)(xx))((xx)(xx)(xx))((xx)(xx)(xx)))"
> :{
| foldrM (\n str ->
|                  (putStrLn $ "Repeating string " ++ (show str) ++ " "
|                   ++ (show n) ++ " times")
|                  >> (return $ "(" ++ (concat $ replicate n str) ++ ")" )
|        ) "x" [2,3,4]
| :}
Repeating string "x" 4 times
Repeating string "(xxxx)" 3 times
Repeating string "((xxxx)(xxxx)(xxxx))" 2 times
"(((xxxx)(xxxx)(xxxx))((xxxx)(xxxx)(xxxx)))"

foldM from Control.Monad:

> import Control.Monad
> :{
| foldM (\str n ->
|                  (putStrLn $ "Repeating string " ++ (show str) ++ " "
|                   ++ (show n) ++ " times")
|                  >> (return $ "(" ++ (concat $ replicate n str) ++ ")" )
|        ) "x" [2,3,4]
| :}
Repeating string "x" 2 times
Repeating string "(xx)" 3 times
Repeating string "((xx)(xx)(xx))" 4 times
"(((xx)(xx)(xx))((xx)(xx)(xx))((xx)(xx)(xx))((xx)(xx)(xx)))"

*)

  /// Monadic fold over the elements of a list.
  /// 
  ///**Type parameters**
  /// 
  /// - `'a`: list element type.
  /// - `'b`: state type.
  /// 
  ///**Parameters**
  /// 
  /// - `folder`: the function to apply to the current state and list element.
  ///   Should return a generator of the next state.
  /// - `state`: the initial state, e.g. an empty list (or set, database, etc.).
  /// - `xs`: the input list.
  /// 
  ///**Returns**
  /// The generator of values of type `'b`.
  let foldM
    (folder: 'b -> 'a -> Gen<'b>)
    (state: 'b)
    (xs: List<'a>)
    : Gen<'b> =
      let combine x (genState: 'b -> Gen<'b>) currState = Gen.bind (folder currState x) genState
      List.foldBack combine xs Gen.constant state