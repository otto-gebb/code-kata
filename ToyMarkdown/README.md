# ToyMarkdown

A code kata implementing a toy markdown parser.

## Specification

Write a simple markdown parser function that will take in a single line of markdown and translate it into the appropriate HTML.

To keep it simple, support only one feature of markdown in atx syntax: headers. Headers are designated by 1-6 hash characters followed by a space, followed by text. The number of hashes determines the header level of the HTML output.

Header content should only come after the initial hashtag(s) plus a space character. Invalid headers should just be returned as the markdown that was received, no translation necessary. Spaces before the hashes and after the header should be kept in the output, but between the hashes and the text only one space is allowed.

## Examples

```
"# Header" should return "<h1>Header</h1>"
"## Header" should return "<h2>Header</h2>"
"###### Header" should return "<h6>Header</h6>"
"####### Header" should return "####### Header" (too many hashes)
"###  Header" should return "###  Header" (too many spaces between)
"Header" should return "Header" (no hashes)
```

## Building and running

```sh
cd ToyMarkdown
dotnet fsi script.fsx
```
