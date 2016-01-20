using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaRaft;
using System;
using System.IO;

namespace RaDbTests
{
    [TestClass]
    public class TestMonotonicLog
    {
        [Serializable]
        public class Foo
        {
            public string Bar { get; set; }
            public string Baz { get; set; }
        }

        [TestMethod]
        public void TestBinaryFormatter()
        {
            var foo = new Foo { Bar = "BAR", Baz = "BAZ" };
            var bytes = foo.GetBytes();

            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var foo2 = bytes.GetObject<Foo>();

            Assert.IsNotNull(foo2);
            Assert.AreEqual("BAR", foo.Bar);
            Assert.AreEqual("BAZ", foo.Baz);
        }

        [TestMethod]
        public void BasicTests()
        {
            using (var log = new MonotonicLog<Foo>(new MemoryStream()))
            {
                Assert.AreEqual(0, log.GetHighestIndex());

                log.Append(new LogEntry<Foo>(1, 1, new Foo { Bar = "BAR", Baz = "BAZ" }));
                log.Append(new LogEntry<Foo>(1, 2, new Foo { Bar = "QUX", Baz = "ZZZ" }));

                var entry1 = log.Retrieve(1);
                Assert.IsNotNull(entry1);
                Assert.AreEqual("BAR", entry1.Value.Bar);
                Assert.AreEqual("BAZ", entry1.Value.Baz);

                var entry2 = log.Retrieve(2);
                Assert.IsNotNull(entry2);
                Assert.AreEqual("QUX", entry2.Value.Bar);
                Assert.AreEqual("ZZZ", entry2.Value.Baz);

                var allResults = log.Scan();
                Assert.AreEqual(2, allResults.Length);
                Assert.AreEqual(1, allResults[0].Index);
                Assert.AreEqual(2, allResults[1].Index);

                Assert.AreEqual(2, log.GetHighestIndex());

                log.WindBackToIndex(2);

                Assert.AreEqual(1, log.GetHighestIndex());
                var croppedResults = log.Scan();
                Assert.AreEqual(1, croppedResults.Length);
                Assert.AreEqual(1, croppedResults[0].Index);

                log.Append(new LogEntry<Foo>(1, 2, new Foo { Bar = "AAA", Baz = "BBB" }));
                var entry3 = log.Retrieve(2);
                Assert.IsNotNull(entry3);
                Assert.AreEqual("AAA", entry3.Value.Bar);
                Assert.AreEqual("BBB", entry3.Value.Baz);

            }


        }
    }
}
