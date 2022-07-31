// // Copyright 2022 Jamie Taylor
using Newtonsoft.Json.Linq;

namespace JsonProcessor.Framework.Transformers {
    /// <summary>
    /// The Splice transformer inserts its argument objects into the surrounding context.
    /// If the surrounding context is an array then the argument must be an array; if it is an object
    /// then the argument must be an object.
    /// </summary>
    public class Splice : SpliceLikeBase {
        public override string Name => "splice";
        public override string? ArgumentNameWhenLongForm => "content";

        protected override bool ValidateArg(IJsonProcessor processor, JToken arg, JTokenType parentType) {
            if (parentType != arg.Type) {
                processor.LogError(arg.Path, $"can't splice content of type {arg.Type} into a parent of type {parentType}");
                return false;
            }
            return true;
        }

        protected override bool InObject(IJsonProcessor processor, JProperty parentProp, JToken arg) {
            return SpliceIntoObject(processor, parentProp, (JObject)arg);
        }

        protected override bool InArray(IJsonProcessor processor, JObject nodeToReplace, JToken arg) {
            nodeToReplace.AddAfterSelf(arg.Children());
            nodeToReplace.Remove();
            return true;
        }

        private bool SpliceIntoObject(IJsonProcessor processor, JProperty parentProp, JObject arg) {
            bool result = true;
            JToken? prev = parentProp.Previous;
            JObject parentObj = (JObject)parentProp.Parent!;
            // remove first to avoid possible conflict with the (ignored) property name in the full-form syntax
            parentProp.Remove();
            // This is quite a bit of trouble to preserve the relative placement an ordering for something
            // that is supposed to be an unordered map.
            foreach (JProperty child in arg.Children()) {
                if (parentObj.ContainsKey(child.Name)) {
                    processor.LogError(child.Path, "parent already has a value for this key; skipping");
                    result = false;
                } else {
                    if (prev is null) {
                        parentObj.AddFirst(child);
                    } else {
                        prev.AddAfterSelf(child);
                    }
                    prev = parentObj.Property(child.Name);
                }
            }
            return result;
        }

    }
}

