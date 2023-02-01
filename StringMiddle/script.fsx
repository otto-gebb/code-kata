/// Gets the middle character(s) of the string.
/// If the string's length is odd, returns the middle character.
/// If the string's length is even, returns the middle 2 characters.
/// 
///**Parameters**
///  * `s` - the input string.
let getMiddle (s: string) : string =
    let middle = s.Length / 2
    if s.Length % 2 = 0 then s[middle-1..middle] else s[middle..middle]

let test input expected =
    let actual = getMiddle input
    let ok = if expected = actual then "  OK" else "FAIL"
    printfn $"""{ok}: '{input}' -> '{actual}' (expected '{expected}')"""

test "test" "es"
test "testing" "t"
test "A" "A"
test "" ""