using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Step.Parser
{
    class TupleExpression
    {
        public readonly object[] Elements;

        public TupleExpression(object[] elements)
        {
            Elements = elements;
        }
    }
}
