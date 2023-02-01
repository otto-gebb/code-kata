open System.Text.RegularExpressions

/// Parses a markdown header into the following parts:
///                    ┌ Optional leading spaces
///                    |    ┌ Hashes
///                    |    |        ┌ Header content
let re = new Regex(@"^(\s*)(#{1,6}) ([^ ].*)", RegexOptions.Compiled)

/// Converts the specified markdown header to HTML.
///
///**Parameters**
///  * `s` - the input markdown string (a single line).
let convertHeader (s: string) : string =
    let m = re.Match(s)
    if not m.Success then
        s
    else
        let leadingSpaces = m.Groups[1]
        let level = m.Groups[2].Length
        let content = m.Groups[3]
        $"{leadingSpaces}<h{level}>{content}</h{level}>"

module NoRegex =
    type private State = LeadingSpaces | Hashes
    type private ParsedHeader = {Level: int; ContentStart: int}

    // Same parser as above, but we pretend that we have no regular expressions.
    let convertV2 (s: string) : string =
        let maxIndex = s.Length - 1
        // Look ma, no mutable state!
        let rec go (i: int) (state: State) (level: int) : Option<ParsedHeader> =
            if i > maxIndex then None
            else
                let current = s[i]
                match state with
                | LeadingSpaces ->
                    match current with
                    | ' ' -> go (i+1) LeadingSpaces 0
                    | '#' -> go (i+1) Hashes 1
                    | _   -> None
                | Hashes ->
                    match current with
                    | '#' when level < 6 ->
                        go (i+1) Hashes (level+1)
                    | ' '
                        // Peek at the next char, bail if it's a space.
                        when i+1 <= maxIndex && s[i+1] <> ' ' ->
                        Some { Level = level ; ContentStart = i+1 }
                    | _ -> None

        match go 0 LeadingSpaces 0 with
        | Some { Level = level ; ContentStart = start } ->
            let leadingSpaces = s[0 .. (start - level - 2)]
            $"{leadingSpaces}<h{level}>{s[start..]}</h{level}>"
        | None -> s

let test input expected =
    let actual = convertHeader input
    let ok = if expected = actual then "  OK" else "FAIL"
    printfn $"""{ok}: '{input}' -> '{actual}' (expected '{expected}')"""

test "# Header" "<h1>Header</h1>"
test "## Header" "<h2>Header</h2>"
test " ## Header" " <h2>Header</h2>"
test "###### Header" "<h6>Header</h6>"
test "####### Header" "####### Header"
test "###  Header" "###  Header"
test "Header" "Header"
test " " " "
test "" ""