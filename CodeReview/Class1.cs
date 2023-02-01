using System;
public class Class1 {

    // SUGGESTION: Consider adding an XML-doc.
    // SUGGESTION: `f` - consider giving a descriptive name, e.g. `format`.
    // SUGGESTION: The type `string` for the format looks too broad.
    //             Consider defining an enum isntead.
    public static bool IsFormat(string str, string f)
    {
        var allowedDict = new Dictionary<string, bool>()
        {
            { "number",true},
            { "date",true},
            { "timespan",true}
        };

        int isNotAllowed = 0;
        for (var i = 1; i < allowedDict.Count; i++)
        {
            if (f == allowedDict.Keys.ToArray()[i - 1])
            {
                isNotAllowed |= 1 << i;
            }
        }

        if (isNotAllowed > 0)
            throw new Exception("Format not allowed.");

        // SUGGESTION:
        // Remove everything in this method above this line, because
        // a) the code is buggy: it throws when the dictionary
        //    of allowed formats contains the specified one (`f`).
        // b) the code below implements the vlidation in a non-buggy way.

        // SUGGESTION: Consider putting `f.ToLower()` in a variable.
        if (f.ToLower() == "number")
            // QUESTION: Do we support integers only?
            //           If we should support floats, this part needs extending.
            return Int32.TryParse(str, out var _);
        else if (f.ToLower() == "date")
            return DateTime.TryParse(str, out var _);
        else if (f.ToLower() == "timespan")
            return DateTime.TryParse(str, out var _);

        // SUGGESTION: Consider throwing a more specific exception,
        //             e.g. `ArgumentException` or `ArgumentOutOfRangeException`.
        throw new Exception("Unknown format.");
    }
}