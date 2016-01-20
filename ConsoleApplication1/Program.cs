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
        static void WriteToStrorage(TestMessage[] entries)
        {
            foreach (var entry in entries) Console.WriteLine($"persisting message {entry.Value}");
        }

        static void Main(string[] args)
        {
            var rand = new Random();

            var nodeNames = new string[] { "node1.log", "node2.log", "node3.log" };
            foreach (var node in nodeNames)
            {
                if (File.Exists(node)) File.Delete(node);
            }

            var nodes = new Node<TestMessage>[] {
                new Node<TestMessage>(new INodeReference<TestMessage>[0], "node1", new MonotonicLog<TestMessage>(new FileStream("node1.log", FileMode.OpenOrCreate)), WriteToStrorage, rand),
                new Node<TestMessage>(new INodeReference<TestMessage>[0], "node2", new MonotonicLog<TestMessage>(new FileStream("node2.log", FileMode.OpenOrCreate)), WriteToStrorage, rand),
                new Node<TestMessage>(new INodeReference<TestMessage>[0], "node3", new MonotonicLog<TestMessage>(new FileStream("node3.log", FileMode.OpenOrCreate)), WriteToStrorage, rand)
            };

            nodes[0].Nodes.Add(new TestConnector(nodes[1]));
            nodes[0].Nodes.Add(new TestConnector(nodes[2]));
            nodes[1].Nodes.Add(new TestConnector(nodes[0]));
            nodes[1].Nodes.Add(new TestConnector(nodes[2]));
            nodes[2].Nodes.Add(new TestConnector(nodes[0]));
            nodes[2].Nodes.Add(new TestConnector(nodes[1]));

            var i = 0;
            while (true)
            {
                Thread.Sleep(2000);

                var leader = nodes.FirstOrDefault(x => x.State == NodeState.Leader);
                if (null != leader)
                {
                    try
                    {
                        Console.WriteLine($"sending message {++i}");
                        leader.Write(new TestMessage { Value = $"message {i.ToString()}"}).Wait();
                    }
                    catch
                    { }
                }
                                
            }
                        
        }
    }
}
