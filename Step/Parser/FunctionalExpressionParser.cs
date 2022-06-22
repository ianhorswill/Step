using System;
using System.Collections.Generic;
using Step.Parser;

namespace Step.Interpreter
{
    /// <summary>
    /// Parser for arithmetic/functional expressions used in set commands.
    /// TODO: handle right-associative binary operators
    /// TODO: handle suffix unary operators
    /// TODO: assumes unary operators are always higher precedence than binary operators, so handle unary operators of lower 
    /// TODO: handle non-operator functions
    /// </summary>
    internal static class FunctionalExpressionParser
    {
        private static FunctionalOperator<Func<object, object>> LookupUnary(object token)
        {
            if (token is string s && UnaryOperatorTable.TryGetValue(s, out var op))
                return op;
            return null;
        }

        private static FunctionalOperator<Func<object, object, object>> LookupBinary(object token)
        {
            if (token is string s && BinaryOperatorTable.TryGetValue(s, out var op))
                return op;
            return null;
        }

        private static readonly Dictionary<string, FunctionalOperator<Func<object, object>>> UnaryOperatorTable =
            new Dictionary<string, FunctionalOperator<Func<object, object>>>()
            {
                {
                    "-",
                    new FunctionalOperator<Func<object, object>>("-", 10,
                        o =>
                        {
                            switch (o)
                            {
                                case int i: return -i;
                                case float f: return -f;
                                default: throw new ArgumentTypeException("-", typeof(float), o, new[] {o});
                            }
                        })
                }
            };

        private static readonly Dictionary<string, FunctionalOperator<Func<object, object, object>>> BinaryOperatorTable =
            new Dictionary<string, FunctionalOperator<Func<object, object, object>>>()
            {
                {
                    "+",
                    new FunctionalOperator<Func<object, object, object>>("+", 1,
                        (a1, a2) =>
                        {
                            switch (a1)
                            {
                                case int i1:
                                    switch (a2)
                                    {
                                        case int i2: return i1 + i2;
                                        case float f2: return i1 + f2;
                                        default: throw new ArgumentTypeException("+", typeof(float), a2, new[] { a1, a2 });
                                    }

                                case float f1:
                                    switch (a2)
                                    {
                                        case int i2: return f1 + i2;
                                        case float f2: return f1 + f2;
                                        default: throw new ArgumentTypeException("+", typeof(float), a2, new[] { a1, a2 });
                                    }

                                default: throw new ArgumentTypeException("+", typeof(float), a1, new[] { a1, a2 });
                            }
                        })
                },
                {
                    "-",
                    new FunctionalOperator<Func<object, object, object>>("-", 1,
                        (a1, a2) =>
                        {
                            switch (a1)
                            {
                                case int i1:
                                    switch (a2)
                                    {
                                        case int i2: return i1 - i2;
                                        case float f2: return i1 - f2;
                                        default: throw new ArgumentTypeException("-", typeof(float), a2, new[] { a1, a2 });
                                    }

                                case float f1:
                                    switch (a2)
                                    {
                                        case int i2: return f1 - i2;
                                        case float f2: return f1 - f2;
                                        default: throw new ArgumentTypeException("-", typeof(float), a2, new[] { a1, a2 });
                                    }

                                default: throw new ArgumentTypeException("-", typeof(float), a1, new[] { a1, a2 });
                            }
                        })
                },
                {
                    "*",
                    new FunctionalOperator<Func<object, object, object>>("*", 2,
                        (a1, a2) =>
                        {
                            switch (a1)
                            {
                                case int i1:
                                    switch (a2)
                                    {
                                        case int i2: return i1 * i2;
                                        case float f2: return i1 * f2;
                                        default: throw new ArgumentTypeException("*", typeof(float), a2, new[] { a1, a2 });
                                    }

                                case float f1:
                                    switch (a2)
                                    {
                                        case int i2: return f1 * i2;
                                        case float f2: return f1 * f2;
                                        default: throw new ArgumentTypeException("*", typeof(float), a2, new[] { a1, a2 });
                                    }

                                default: throw new ArgumentTypeException("*", typeof(float), a1, new[] { a1, a2 });
                            }
                        })
                },
                {
                    "/",
                    new FunctionalOperator<Func<object, object, object>>("/", 2,
                        (a1, a2) =>
                        {
                            switch (a1)
                            {
                                case int i1:
                                    switch (a2)
                                    {
                                        case int i2:
                                            var integerQuotient = i1 / i2;
                                            if (integerQuotient * i2 == i1)
                                                return integerQuotient;
                                            return i1 / (float) i2;

                                        case float f2: return i1 / f2;
                                        default: throw new ArgumentTypeException("/", typeof(float), a2, new[] { a1, a2 });
                                    }

                                case float f1:
                                    switch (a2)
                                    {
                                        case int i2: return f1 / i2;
                                        case float f2: return f1 / f2;
                                        default: throw new ArgumentTypeException("/", typeof(float), a2, new[] { a1, a2 });
                                    }

                                default: throw new ArgumentTypeException("/", typeof(float), a1, new[] { a1, a2 });
                            }
                        })
                }
            };

        public static FunctionalExpression FunctionalExpressionFromValueOrVariable(object v)
        {
            switch (v)
            {
                case StateVariableName _:
                case LocalVariableName _:
                    return new VariableReference(v);

                default:
                    return new Constant(v);
            }
        }

        public static FunctionalExpression FromTuple(object[] tuple, int start = 0, string path = null, int lineNumber = 0) =>
            FunctionalExpressionParser.Parse(tuple, start, path, lineNumber);

        public static FunctionalExpression Parse(params object[] expression) => FromTuple(expression);

        /// <summary>
        /// The actual parser.  This takes an array of tokens and returns a FunctionalExpression
        /// </summary>
        /// <param name="tokens">Tokens comprising the functional expression</param>
        /// <param name="start">Index of the start of the functional expression within the tokens</param>
        /// <param name="path">Source file this comes from</param>
        /// <param name="lineNumber">line within the source file</param>
        /// <returns>The FunctionalExpression denoted by the tokens</returns>
        public static FunctionalExpression Parse(object[] tokens, int start, string path = null, int lineNumber = 0)
        {
            var position = start;
            
            // True if we've reached the end of the array of tokens
            bool End() => tokens.Length == position;

            // The next token, or null if at the end.
            object Peek()
            {
                if (End())
                    return null;
                return tokens[position];
            }
            
            // Return the next token and advance to the following one/
            object Get()
            {
                if (End())
                    throw new SyntaxError("Unexpected end of expression", path, lineNumber);
                return tokens[position++];
            }

            FunctionalExpression ParseUnaryOperatorChain()
            {
                var token = Get();
                var uOp = LookupUnary(token);
                if (uOp != null)
                    return new UnaryOperator(uOp, ParseUnaryOperatorChain());

                if (token.Equals("("))
                {
                    var exp = ParseTopLevel();
                    if (!Get().Equals(")"))
                        throw new SyntaxError("No matching open paren has no matching close paren", path, lineNumber);
                    return exp;
                }

                return FunctionalExpressionFromValueOrVariable(token);
            }

            FunctionalExpression ParseBinaryOperatorChain(FunctionalExpression e, int surroundingPrec)
            {
                if (End())
                    return e;
                
                // Parse a possibly empty series of operators of binary operators
                var op = LookupBinary(Peek());
                while (op != null && op.Precedence >= surroundingPrec)
                {
                    Get();
                    var rhs = ParseUnaryOperatorChain();
                    
                    if (End())
                        return new BinaryOperator(op, e, rhs);
                    
                    // Check if the next binary operator is higher precedence,
                    // If so, recurse
                    var op2 = LookupBinary(Peek());
                    while (op2 != null && op2.Precedence > op.Precedence)
                    {
                        rhs = ParseBinaryOperatorChain(rhs, op2.Precedence);
                        op2 = LookupBinary(Peek());
                    }

                    e = new BinaryOperator(op,e, rhs);
                    op = LookupBinary(Peek());
                }

                return e;
            }
            
            FunctionalExpression ParseTopLevel() => ParseBinaryOperatorChain(ParseUnaryOperatorChain(), 0);

            var finalExp = ParseTopLevel();
            if (!End())
                throw new SyntaxError($"Unexpected token in expression: {Get()}", path, lineNumber);
            
            return finalExp;
        }
    }
}
