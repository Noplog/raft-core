using RaRaft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    [Serializable]
    public class TestMessage
    {
        public string Value { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var nodes = CreateLocalNodes<TestMessage>(5, WriteToStrorage);

            var i = 0;
            while (true)
            {
                Thread.Sleep(2000);

                var leader = nodes.FirstOrDefault(x => x.State == NodeState.Leader);
                if (null == leader) continue;
                Console.WriteLine($"sending message {++i}");
                leader.Write(new TestMessage { Value = $"message {i.ToString()}" }).Wait();
            }
        }

        static Node<T>[] CreateLocalNodes<T>(int count, Action<T[]> write)
        {
            var rnd = new Random();
            var nodes = new List<Node<T>>();
            for (var i = 0; i < count; i++)
            {
                if (File.Exists($"node{i}.log")) File.Delete($"node{i}.log");
                var node = new Node<T>(new INodeReference<T>[0], $"node{i}", new MonotonicLog<T>(new FileStream($"node{i}.log", FileMode.OpenOrCreate)), write, rnd);
                nodes.Add(node);
            }
            foreach (var sender in nodes)
            {
                foreach (var recipient in nodes.Where(x => x != sender))
                {
                    sender.Nodes.Add(new TestConnector<T>(recipient));
                }
            }
            return nodes.ToArray();
        }

        static void WriteToStrorage(TestMessage[] entries)
        {
            foreach (var entry in entries) Console.WriteLine($"persisting message {entry.Value}");
        }

    }
}
