// Copyright 2022 Jamie Taylor

// This file (and this file only) may be freely redistributed
// (with or without modification) without attribution.

namespace JsonProcessor.SimpleAPI { // <-- change this namespace to something that makes sense for your project

    public interface IJsonProcessorAPI {
        // you should always call this with includeDefaultTransformers = true
        IJsonProcessor NewProcessor(string errorLogPrefix, bool includeDefaultTransformers = true);
    }

    public interface IJsonProcessor {
        // Returns true if no errors were encountered
        bool Transform(Newtonsoft.Json.Linq.JToken tok);
    }
}
