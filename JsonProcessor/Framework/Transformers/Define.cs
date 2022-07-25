// // Copyright 2022 Jamie Taylor
using System;
using Newtonsoft.Json.Linq;

namespace JsonProcessor.Framework.Transformers {
    public class Define : SpliceLikeBase {
        public override string Name => "define";
        public override string? ArgumentNameWhenLongForm => "bindings";

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
                processor.SetGlobalVariable(prop.Name, prop.Value);
            }
        }
    }
}

