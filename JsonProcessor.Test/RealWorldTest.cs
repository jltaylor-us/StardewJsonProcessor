// // Copyright 2022 Jamie Taylor
using JsonProcessor.Framework;
using Newtonsoft.Json.Linq;
using JsonProcessor.Framework.Transformers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static JsonProcessor.Test.Utils;

namespace JsonProcessor.Test {
    [TestClass]
    public class RealWorldTest {
        private readonly IJsonProcessor processor;
        private readonly LogCollector log;

        public RealWorldTest() {
            log = new();
            processor = new JsonProcessorImpl(log.Log);
            JsonProcessorAPI.AddDefaultProcessors(processor);
        }

        [TestMethod]
        public void TestAccessory() {
            JObject orig = ReadTestData("accessory-orig.json");
            AssertCleanTransform("test accessory.json", processor, log, ReadTestData("accessory.json"), orig);
            AssertCleanTransform("test accessory2.json", processor, log, ReadTestData("accessory2.json"), orig);
        }
    }
}

