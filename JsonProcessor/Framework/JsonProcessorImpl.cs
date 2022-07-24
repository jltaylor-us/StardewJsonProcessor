// // Copyright 2022 Jamie Taylor
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace JsonProcessor.Framework {
    public class JsonProcessorImpl : IJsonProcessor {
        private readonly Dictionary<string, ITransformer> transformers = new();
        private readonly Dictionary<string, IShorthandTransformer> shorthandTransformers = new();
        private readonly Dictionary<string, IPropertyTransformer> propertyTransformers = new();
        private readonly Action<string, string> logError;
        public JsonProcessorImpl(Action<string, string> logError) {
            this.logError = logError;
        }

        /// <inheritdoc/>
        public void LogError(string tokenPath, string message) {
            logError(tokenPath, message);
        }

        public void AddTransformer(ITransformer transformer) {
            transformers.Add(transformer.Name, transformer);
        }

        public void AddTransformer(string name, Func<IJsonProcessor, JObject, bool> transform, bool processChildrenFirst = true) {
            AddTransformer(new GenericTransformer(name, transform, processChildrenFirst));
        }

        public void AddShorthandTransformer(IShorthandTransformer transformer) {
            shorthandTransformers.Add("$" + transformer.Name, transformer);
            if (transformer.ArgumentNameWhenLongForm is not null) AddTransformer(new LongFormWrapper(transformer));
        }

        public void AddShorthandTransformer(string name, string? argNameWhenLongForm, Func<IJsonProcessor, JObject, JToken, bool> transform, bool processArgumentFirst = true) {
            AddShorthandTransformer(new GenericShorthandTransformer(name, argNameWhenLongForm, transform, processArgumentFirst));
        }

        public void AddPropertyTransformer(IPropertyTransformer transformer) {
            propertyTransformers.Add("$" + transformer.Name, transformer);
        }

        public void AddPropertyTransformer(string name, Func<IJsonProcessor, JProperty, bool> transform, bool processArgumentFirst = true) {
            AddPropertyTransformer(new GenericPropertyTransformer(name, transform, processArgumentFirst));
        }

        public void RemoveTransformer(string name) {
            transformers.Remove(name);
            shorthandTransformers.Remove("$" + name);
            propertyTransformers.Remove("$" + name);
        }

        public bool Transform(JToken tok) {
            bool result = true;
            ITransformer? transformerToInvokeAfterChildren = null;
            if (tok is JObject obj) {
                if (obj.TryGetValue("$transform", out JToken? child)) {
                    if (obj.Parent is null) {
                        LogError(obj.Path, "The root node can't be a transformer");
                        result = false;
                    } else {
                        if (child.Type != JTokenType.String) {
                            result = Transform(child);
                            if (obj.TryGetValue("$transform", out JToken? maybeReplacedChild)) {
                                child = maybeReplacedChild;
                            }
                        }
                        if (child.Type == JTokenType.String) {
                            string transformerName = child.Value<string>()!;
                            if (transformers.TryGetValue(transformerName, out var xform)) {
                                if (xform.ProcessChildrenFirst) {
                                    transformerToInvokeAfterChildren = xform;
                                } else {
                                    result = xform.TransformNode(this, obj) && result;
                                }
                            } else {
                                LogError(tok.Path, $"don't have any transformer named {transformerName}");
                                result = false;
                            }
                        } else {
                            LogError(tok.Path, $"value of transform must be a string, but it is a {child.Type}");
                            result = false;
                        }
                    }
                } else if (obj.Count == 1) {
                    JProperty jProperty = obj.Properties().First();
                    if (shorthandTransformers.TryGetValue(jProperty.Name, out var xform)) {
                        if (obj.Parent is null) {
                            LogError(obj.Path, "The root node can't be a transformer");
                            result = false;
                        } else {
                            if (xform.ProcessArgumentFirst) {
                                result = Transform(jProperty.Value);
                            }
                            return xform.TransformValue(this, obj, jProperty.Value) && result;
                        }
                    }
                }
            } else if (tok is JProperty jProperty) {
                if (propertyTransformers.TryGetValue(jProperty.Name, out var xform)) {
                    if (xform.ProcessArgumentFirst) {
                        result = Transform(jProperty.Value);
                    }
                    return xform.TransformProperty(this, jProperty) && result;
                }
            }
            if (tok.HasValues) {
                foreach (JToken child in tok.Children().ToList()) {
                    result = Transform(child) && result;
                }
            }
            if (transformerToInvokeAfterChildren is not null) {
                result = transformerToInvokeAfterChildren.TransformNode(this, (JObject)tok) && result;
            }
            return result;
        }


        // -----------------------------------
        // helper classes

        private class GenericTransformer : ITransformer {
            public string Name { get; }
            public bool ProcessChildrenFirst { get; }
            private readonly Func<IJsonProcessor, JObject, bool> transform;

            public GenericTransformer(string name, Func<IJsonProcessor, JObject, bool> transform, bool processChildrenFirst) {
                Name = name;
                this.transform = transform;
                ProcessChildrenFirst = processChildrenFirst;
            }

            public bool TransformNode(IJsonProcessor processor, JObject obj) {
                return transform(processor, obj);
            }
        }
        private class GenericShorthandTransformer : IShorthandTransformer {
            public string Name { get; }
            public string? ArgumentNameWhenLongForm { get; }
            public bool ProcessArgumentFirst { get; }
            private readonly Func<IJsonProcessor, JObject, JToken, bool> transform;

            public GenericShorthandTransformer(string name, string? argNameWhenLongForm, Func<IJsonProcessor, JObject, JToken, bool> transform, bool processArgumentFirst) {
                Name = name;
                this.transform = transform;
                ArgumentNameWhenLongForm = argNameWhenLongForm;
                ProcessArgumentFirst = processArgumentFirst;
            }

            public bool TransformValue(IJsonProcessor processor, JObject nodeToReplace, JToken arg) {
                return transform(processor, nodeToReplace, arg);
            }
        }
        private class LongFormWrapper : ITransformer {
            private readonly IShorthandTransformer shorthandTransformer;
            private readonly string argName;
            public LongFormWrapper(IShorthandTransformer shorthandTransformer) {
                this.shorthandTransformer = shorthandTransformer;
                if (shorthandTransformer.ArgumentNameWhenLongForm is null) {
                    throw new ArgumentNullException("shorthandTransformer.ArgumentNameWhenLongForm");
                }
                this.argName = shorthandTransformer.ArgumentNameWhenLongForm;
            }

            public string Name => shorthandTransformer.Name;

            public bool ProcessChildrenFirst => false;

            public bool TransformNode(IJsonProcessor processor, JObject obj) {
                if (!obj.TryGetValue(argName, out JToken? arg)) {
                    processor.LogError(obj.Path, $"missing required \"{argName}\" property for {Name} transformer");
                    return false;
                }
                bool result = true;
                if (shorthandTransformer.ProcessArgumentFirst) {
                    result = processor.Transform(arg);
                    if (obj.TryGetValue(argName, out JToken? maybeReplacedChild)) {
                        arg = maybeReplacedChild;
                    }
                }
                return shorthandTransformer.TransformValue(processor, obj, arg) && result;
            }
        }
        private class GenericPropertyTransformer : IPropertyTransformer {
            public string Name { get; }
            public bool ProcessArgumentFirst { get; }
            private readonly Func<IJsonProcessor, JProperty, bool> transform;

            public GenericPropertyTransformer(string name, Func<IJsonProcessor, JProperty, bool> transform, bool processArgumentFirst) {
                Name = name;
                this.transform = transform;
                ProcessArgumentFirst = processArgumentFirst;
            }
            public bool TransformProperty(IJsonProcessor processor, JProperty theProperty) {
                return transform(processor, theProperty);
            }
        }

    }

    internal class Env {
        public Dictionary<string, JToken> bindings = new();
        public Env? nextEnv = null;

        public bool TryApply(string name, [MaybeNullWhen(false)] out JToken value) {
            if (bindings.TryGetValue(name, out JToken? v)) {
                value = v; // intermediate var necessary for the "nullable" flow analysis
                return true;
            }
            if (nextEnv is not null) {
                return nextEnv.TryApply(name, out value);
            }
            value = null;
            return false;
        }
    }
}

