The parser is implemented as a chain of transducers that group things together.

* You start with a standard C# TextReader, i.e. a text stream.  It's a stream of characters.
* TokenStream groups these into a stream (technically an IEnumerable) of tokens (strings)
    * Example: a b c space d e f => abc def
* ExpressionStream groups these into a stream of "expressions," for want of a better term.
  Tokens from the input stream pass through unchanged into the output stream, unless they're
  enclosed in square brackets, in which case all the tokens between the brackets are packaged
  as a single array.  The brackets are eliminated.
    * Example: a [ b c d ] e => a stream with three elements, a, an array with b, c, and d, and finally e.
	* So this is kind of like S-expressions, only with brackets instead of parens, and where nested
	  brackets aren't allowed.
* DefinitionStream converts the expressions into method definitions.