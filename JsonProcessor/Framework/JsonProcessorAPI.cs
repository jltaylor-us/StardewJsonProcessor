// // Copyright 2022 Jamie Taylor
using System;
using JsonProcessor.Framework.Transformers;
using StardewModdingAPI;

namespace JsonProcessor.Framework {
    public class JsonProcessorAPI : IJsonProcessorAPI {
        private IMonitor Monitor { get; }
        public JsonProcessorAPI(IMonitor monitor) {
            Monitor = monitor;
        }

        /// <inheritdoc/>
        public IJsonProcessor NewProcessor(string errorLogPrefix, bool includeDefaultTransformers = true) {
            JsonProcessorImpl result = new((path, msg) => {
                Monitor.Log($"{errorLogPrefix} - ${path}: ${msg}", LogLevel.Error);
            });
            new Define().AddTo(result);
            new ForEach().AddTo(result);
            new Let().AddTo(result);
            new Splice().AddTo(result);
            new StringJoin().AddTo(result);
            new Var().AddTo(result);
            return result;
        }
    }
}

