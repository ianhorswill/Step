using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Step.Interpreter;
using Step.Output;

namespace Step
{
    [DebuggerDisplay($"{{{nameof(BlockContents)}}}")]
    public sealed class FeatureStructure
    {
        private readonly Feature[] features;
        private readonly object?[] values;
        private readonly LogicVariable next;

        public FeatureStructure(Feature[] features, object?[] values)
            : this(features, values, new LogicVariable("_next_", 0))
        { }

        public FeatureStructure(Feature[] features, object?[] values, LogicVariable next)
        {
            this.features = features;
            this.values = values;
            this.next = next;
            Debug.Assert(this.next != null);
        }

        public int Count(BindingList? bindings)
        {
            var count = 0;
            for (var block = this;
                 block != null;
                 block = BindingList.Lookup(bindings, block.next, null!) as FeatureStructure)
                count += block.features.Length;

            return count;
        }

        /// <summary>
        /// Enumerate the feature/value 
        /// </summary>
        /// <param name="bindings">Binding list currently in effect</param>
        /// <returns>All the feature/value pairs in the feature structure</returns>
        public IEnumerable<KeyValuePair<Feature, object?>> FeatureValues(BindingList? bindings)
        {
            for (var block = this;
                 block != null;
                 block = BindingList.Lookup(bindings, block.next, null!) as FeatureStructure)
                for (var i = 0; i < block.features.Length; i++)
                    yield return new KeyValuePair<Feature, object?>(block.features[i], block.values[i]);
        }

        public bool ContainsFeature(Feature f, BindingList? bindings)
        {
            for (var block = this;
                 block != null;
                 block = BindingList.Lookup(bindings, block.next, null!) as FeatureStructure)
            {
                var index = Array.IndexOf(block.features, f);
                if (index >= 0)
                    return true;
            }

            return false;
        }

        public bool ContainsFeature(string f, BindingList? bindings) => ContainsFeature(Feature.Intern(f), bindings);

        public FeatureStructure(List<(string feature, object? value)> bindings)
        {
            features = new Feature[bindings.Count];
            values = new object?[bindings.Count];
            for (var i = 0; i < bindings.Count; i++)
            {
                features[i] = Feature.Intern(bindings[i].feature);
                values[i] = bindings[i].value;
            }

            next = new LogicVariable("_next_", 0);
        }

        public bool TryGetValue(Feature f, BindingList? bindings, out object? value)
        {
            for (var block = this;
                 block != null;
                 block = BindingList.Lookup(bindings, block.next, null!) as FeatureStructure)
            {
                var index = Array.IndexOf(block.features, f);
                if (index >= 0)
                {

                    value = BindingEnvironment.Deref(block.values[index], bindings);
                    return true;
                }
            }

            value = null;
            return false;
        }

        public void Write(Writer w)
        {
            var b = w.Buffer;
            var bindings = w.Bindings;

            b.Append('{');

            for (var block = this;
                 block != null;
                 block = BindingList.Lookup(bindings, block.next, null!) as FeatureStructure)
            {
                for (var i = 0; i < block.features.Length; i++)
                {
                    b.Append(' ');
                    b.Append(block.features[i].Name);
                    b.Append(':');
                    w.Write(block.values[i]);
                }
            }

            b.Append(" }");
        }

        internal static bool Unify(FeatureStructure a, FeatureStructure b, BindingEnvironment env, BindingList? inBindings,
            out BindingList? outBindings)
        {
            outBindings = inBindings;
            var missingInB = 0;
            for (var block = a;
                 block != null;
                 block = BindingList.Lookup(outBindings, block.next, null!) as FeatureStructure)
            {
                for (var i = 0; i < block.features.Length; i++)
                {
                    if (b.TryGetValue(block.features[i], outBindings, out var bValue))
                    {
                        if (!env.Unify(block.values[i], bValue, outBindings, out outBindings))
                            // Can't unify
                            return false;
                    }
                    else
                        // It's in a but not b, so we need to reserve some room for adding it to b.
                        missingInB++;
                }
            }
            // If we get here, all the features in common between a and b have been unified.
            // So now all we have to do is make new feature blocks to append to a and b containing
            // each other's missing elements.

            // Make the extra elements we'll add to b
            var bFeatures = new Feature[missingInB];
            var bValues = new object?[missingInB];
            var next = 0;
            for (var block = a;
                 block != null;
                 block = BindingList.Lookup(outBindings, block.next, null!) as FeatureStructure)
            {
                for (var i = 0; i < block.features.Length; i++)
                {
                    if (!b.ContainsFeature(block.features[i], outBindings))
                    {
                        bFeatures[next] = block.features[i];
                        bValues[next] = block.values[i];
                        next++;
                    }
                }
            }

            if (missingInB > 0)
                outBindings = BindingList.Bind(outBindings, b.next, new FeatureStructure(bFeatures, bValues));

            // Make new elements we'll add to a
            var missingInA = 0;
            for (var block = b;
                 block != null;
                 block = BindingList.Lookup(outBindings, block.next, null!) as FeatureStructure)
            {
                for (var i = 0; i < block.features.Length; i++)
                {
                    if (!a.ContainsFeature(block.features[i], outBindings))
                        missingInA++;
                }
            }

            if (missingInA > 0)
            {
                var aFeatures = new Feature[missingInA];
                var aValues = new object?[missingInA];
                next = 0;
                for (var block = b;
                     block != null;
                     block = BindingList.Lookup(outBindings, block.next, null!) as FeatureStructure)
                {
                    for (var i = 0; i < block.features.Length; i++)
                    {
                        if (!a.ContainsFeature(block.features[i], outBindings))
                        {
                            aFeatures[next] = block.features[i];
                            aValues[next] = block.values[i];
                            next++;
                        }
                    }
                }

                var aNew = new FeatureStructure(aFeatures, aValues);

                outBindings = BindingList.Bind(outBindings, a.next, aNew);
            }

            return true;
        }

        internal FeatureStructure Resolve(BindingEnvironment env, BindingList? bindings, bool compressPairs)
        {
            var size = Count(bindings);
            var f = new Feature[size];
            var v = new object?[size];
            var outIndex = 0;
            var lastLink = next;
            for (var block = this;
                 block != null;
                 block = BindingList.Lookup(bindings, block.next, null!) as FeatureStructure)
            {
                for (var i = 0; i < block.features.Length; i++)
                {
                    f[outIndex] = block.features[i];
                    v[outIndex] = env.Resolve(block.values[i], bindings, compressPairs);
                    outIndex++;
                }

                Debug.Assert(block.next != null);
                lastLink = block.next;
            }

            return new FeatureStructure(f, v, lastLink);
        }

        public string BlockContents => Writer.TermToString(this, null);

        public FeatureStructure Map(Func<object?, object?> map) => new(features, values.Select(map).ToArray());
    }
}
