module Hedgehog.Gen

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
