// // Copyright 2022 Jamie Taylor
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonProcessor.Test
{
    public class LogCollector
    {
        private readonly List<Tuple<string, string>> lines = new();
        public LogCollector()
        {
        }

        public void Clear() {
            lines.Clear();
        }

        public List<Tuple<string, string>> Lines => lines;

        public void Log(string path, string message) {
            lines.Add(Tuple.Create(path, message));
        }

        public override string ToString() {
            StringBuilder sb = new();
            foreach (Tuple<string, string> t in lines) {
                sb.Append(t.Item1).Append(": ").Append(t.Item2).Append("\n");
            }
            return sb.ToString();
        }

        public bool IsEmpty => lines.Count == 0;
    }
}

