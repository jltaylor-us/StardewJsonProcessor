// // Copyright 2022 Jamie Taylor
using Newtonsoft.Json.Linq;

namespace JsonProcessor.Framework.Transformers {
    public class Splice : IShorthandTransformer, IPropertyTransformer
    {
        public string Name => "splice";
        public string? ArgumentNameWhenLongForm => "content";
        public bool ProcessArgumentFirst => true;

        public Splice() {
        }

        public void AddTo(IJsonProcessor processor) {
            processor.AddShorthandTransformer(this);
            processor.AddPropertyTransformer(this);
        }

        public bool TransformValue(IJsonProcessor processor, JObject nodeToReplace, JToken arg) {
            if (nodeToReplace.Parent is null) {
                // The processor should have already disallowed this, but check anyway
                // because either we are paranoid or want to make the compiler's nullable check happy.
                processor.LogError(nodeToReplace.Path, "can't splice at top level");
                return false;
            }
            if (nodeToReplace.Parent.Type == JTokenType.Property) {
                if (arg.Type != JTokenType.Object) {
                    processor.LogError(nodeToReplace.Path, $"can't splice content of type {arg.Type} into a parent of type Object");
                    return false;
                }
                return SpliceIntoObject(processor, (JProperty)nodeToReplace.Parent, (JObject)arg);
            }
            // else splicing into Array
            if (nodeToReplace.Parent.Type != arg.Type) {
                processor.LogError(nodeToReplace.Path, $"can't splice content of type {arg.Type} into a parent of type {nodeToReplace.Parent.Type}");
                return false;
            }
            nodeToReplace.AddAfterSelf(arg.Children());
            nodeToReplace.Remove();
            return true;
        }

        public bool TransformProperty(IJsonProcessor processor, JProperty theProperty) {
            if (theProperty.Value.Type != JTokenType.Object) {
                processor.LogError(theProperty.Path, $"can't splice content of type {theProperty.Value.Type} into an Object");
                return false;
            }
            return SpliceIntoObject(processor, theProperty, (JObject)theProperty.Value);
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

