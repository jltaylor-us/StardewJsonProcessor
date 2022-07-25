// // Copyright 2022 Jamie Taylor

// TODO: put license text here

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace JsonProcessor
{
    public interface IJsonProcessorAPI
    {
    }

    public interface IJsonProcessor {
        void LogError(string tokenPath, string message);
        bool Transform(JToken tok);

        void AddTransformer(ITransformer transformer);
        void AddTransformer(string name, Func<IJsonProcessor, JObject, bool> transform, bool processChildrenFirst = true);

        void AddShorthandTransformer(IShorthandTransformer transformer);
        void AddShorthandTransformer(string name, string? argNameWhenLongForm, Func<IJsonProcessor, JObject, JToken, bool> transform, bool processArgumentFirst = true);

        void AddPropertyTransformer(IPropertyTransformer transformer);
        void AddPropertyTransformer(string name, Func<IJsonProcessor, JProperty, bool> transform, bool processArgumentFirst = true);

        void RemoveTransformer(string name);

        void SetGlobalVariable(string name, JToken value);
        void PushEnv(IDictionary<string, JToken> bindings);
        void PopEnv();
        bool TryApplyEnv(string name, [MaybeNullWhen(false)] out JToken value, [MaybeNullWhen(false)] out object foundInEnv);
    }

    public interface ITransformer {
        string Name { get; }
        bool ProcessChildrenFirst { get; }
        bool TransformNode(IJsonProcessor processor, JObject obj);
    }

    public interface IShorthandTransformer {
        string Name { get; }
        string? ArgumentNameWhenLongForm { get; }
        bool ProcessArgumentFirst { get; }
        bool TransformValue(IJsonProcessor processor, JObject nodeToReplace, JToken arg);
    }
    public interface IPropertyTransformer {
        string Name { get; }
        bool ProcessArgumentFirst { get; }
        bool TransformProperty(IJsonProcessor processor, JProperty theProperty);
    }
}

