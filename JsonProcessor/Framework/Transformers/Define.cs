// // Copyright 2022 Jamie Taylor
using System;
using Newtonsoft.Json.Linq;

namespace JsonProcessor.Framework.Transformers {
    /// <summary>
    /// The Define transformer sets bindings in the global environment.  The Define node itself is removed
    /// from the JSON tree.
    /// </summary>
    public class Define : SpliceLikeBase {
        public override string Name => "define";
        public override string? ArgumentNameWhenLongForm => "bindings";
        public override bool ProcessArgumentFirst => false;

        protected override bool ValidateArg(IJsonProcessor processor, JToken arg, JTokenType parentType) {
            if (arg.Type != JTokenType.Object) {
                processor.LogError(arg.Path, "must be an object");
                return false;
            }
            return true;
        }

        protected override bool InObject(IJsonProcessor processor, JProperty parentProp, JToken arg) {
            parentProp.Remove();
            AddBindings(processor, (JObject)arg);
            return true;
        }

        protected override bool InArray(IJsonProcessor processor, JObject nodeToReplace, JToken arg) {
            nodeToReplace.Remove();
            AddBindings(processor, (JObject)arg);
            return true;
        }

        private void AddBindings(IJsonProcessor processor, JObject obj) {
            foreach (JProperty prop in obj.Properties()) {
                // let* semantics (or I guess "define" semantics)
                processor.Transform(prop.Value);
                if (obj.TryGetValue(prop.Name, out JToken? possiblyUpdatedValue)) {
                    processor.SetGlobalVariable(prop.Name, possiblyUpdatedValue);
                }
            }
        }
    }
}

