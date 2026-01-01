# Term representation

## Mutation

While Step terms are represented as C# objects, some of which are mutable, Step terms are considered immutable.
C# code should never modify them.

## Variables embedded in terms

Terms may be local variables or complex objects (tuples or ) that contain local variables

## Mapping to C# types

Step terms are represented as C# objects as follows:
- Strings (e.g. foo, fred, |Foo|) are represented as the C# `string` type
- Text (e.g. "this is text") is tokenized and represented as arrays of strings (i.e. the C# type `string[]`)
  The arrays contain one string per token; spaces are not represented explicity.  The tokenized representation
  can be converted to a single `string` using the `.Untokenize()` extension method from the
  `Step.Output.TextUtilities` class.
- Tuples (e.g. [this is 1 tuple]) are almost always represented as arrays using the C# type `object?[]`
   - The only exception is when they've been matched to or extended using the [ a | b ] syntax in Step code.
	 In this case, the tuple is represented as an instance of the class `Step.Terms.Pair`.
	   - The first element is stored in