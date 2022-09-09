using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Text;

namespace Step.Serialization
{
    public interface ISerializable
    {
        public void Serialize(Serializer s);
        public (char start, char end, bool includeSpace) SerializationBracketing() => ('<', '>', true);
    }
}
