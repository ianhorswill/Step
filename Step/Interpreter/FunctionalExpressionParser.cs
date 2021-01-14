using System;
using System.Collections.Generic;
using Step.Parser;

namespace Step.Interpreter
{
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
                                            return ((float)i1) / (float) i2;

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

        public static FunctionalExpression Parse(object[] tokens, int start, string path = null, int lineNumber = 0)
        {
            var position = start;
            bool End() => tokens.Length == position;

            object Peek()
            {
                if (End())
                    return null;
                return tokens[position];
            }
            
            object Get()
            {
                if (End())
                    throw new SyntaxError("Unexpected end of expression", path, lineNumber);
                return tokens[position++];
            }

            FunctionalExpression ParseAtomic()
            {
                var token = Get();
                var uOp = LookupUnary(token);
                if (uOp != null)
                    return new UnaryOperator(uOp, ParseAtomic());

                if (token.Equals("("))
                {
                    var exp = ParseTopLevel();
                    if (!Get().Equals(")"))
                        throw new SyntaxError("No matching open paren has no matching close paren", path, lineNumber);
                    return exp;
                }

                if (token is string)
                    throw new SyntaxError($"Unexpected token {token}", path, lineNumber);
                return FunctionalExpression.FromValueOrVariable(token);
            }

            FunctionalExpression ParseExpression(FunctionalExpression e, int surroundingPrec)
            {
                if (End())
                    return e;
                
                var op = LookupBinary(Peek());
                while (op != null && op.Precedence >= surroundingPrec)
                {
                    Get();
                    var rhs = ParseAtomic();
                    
                    if (End())
                        return new BinaryOperator(op, e, rhs);
                    
                    var op2 = LookupBinary(Peek());
                    while (op2 != null && op2.Precedence > op.Precedence)
                    {
                        rhs = ParseExpression(rhs, op2.Precedence);
                        op2 = LookupBinary(Peek());
                    }

                    e = new BinaryOperator(op,e, rhs);
                    op = LookupBinary(Peek());
                }

                return e;
            }
            
            FunctionalExpression ParseTopLevel() => ParseExpression(ParseAtomic(), 0);

            var finalExp = ParseTopLevel();
            if (!End())
                throw new SyntaxError($"Unexpected token in expression: {Get()}", path, lineNumber);
            
            return finalExp;
        }
    }
}
