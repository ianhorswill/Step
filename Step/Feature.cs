using System.Collections.Generic;

namespace Step
{
    public sealed class Feature
    {
        public readonly string Name;
        private static Dictionary<string,Feature> FeatureTable = new();

        private Feature(string name)
        {
            Name = name;
        }

        public static Feature Intern(string name)
        {
            if (FeatureTable.TryGetValue(name, out var feature))
                return feature;
            feature = new Feature(name);
            FeatureTable[name] = feature;
            return feature;
        }

        public override string ToString() => Name;
    }
}
