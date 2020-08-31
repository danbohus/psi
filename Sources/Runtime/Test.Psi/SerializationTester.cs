﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SerializationTester
    {
        internal enum FooEnum : uint
        {
            One = 0x1,
            Two = 0x2,
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeSimpleTypes()
        {
            const int intValue = 0x7777AAA;
            const byte byteValue = 0xBB;
            const short shortValue = 0x7CDD;
            const long longValue = 0x77777777EEEEEEEE;
            string stringValue = "This is a test.";
            object stringAsObjectValue = "This is another test.";
            const double doubleValue = Math.PI;
            const float floatValue = -1.234f;

            this.ValueTypeCloneTest(intValue);
            this.ValueTypeCloneTest(byteValue);
            this.ValueTypeCloneTest(shortValue);
            this.ValueTypeCloneTest(longValue);
            this.ValueTypeCloneTest(stringValue);
            this.ValueTypeCloneTest(stringAsObjectValue);
            this.ValueTypeCloneTest(doubleValue);
            this.ValueTypeCloneTest(floatValue);

            var buf = new byte[256];
            this.ValueTypeSerializeTest(intValue, buf);
            this.ValueTypeSerializeTest(byteValue, buf);
            this.ValueTypeSerializeTest(shortValue, buf);
            this.ValueTypeSerializeTest(longValue, buf);
            this.ValueTypeSerializeTest(stringValue, buf);
            this.ValueTypeSerializeTest(stringAsObjectValue, buf);
            this.ValueTypeSerializeTest(doubleValue, buf);
            this.ValueTypeSerializeTest(floatValue, buf);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Enumerables()
        {
            try
            {
                using (var p = Pipeline.Create())
                {
                    var store = PsiStore.Create(p, "Store", null);
                    Generators.Return(p, new double[] { 1, 2, 3 }).Select(l => l.Select(d => d + 1)).Write("Test", store);
                    p.Run();
                }
            }
            catch (AggregateException ex)
            {
                Assert.AreEqual(1, ex.InnerExceptions.Count);
                Assert.IsTrue(ex.InnerExceptions[0].GetType() == typeof(NotSupportedException));
                Assert.IsTrue(ex.InnerExceptions[0].Message.StartsWith("Cannot clone Func"));
                return;
            }

            Assert.Fail("Should have thrown above");
        }

        [TestMethod]
        [Timeout(60000)]
        public void NullStringTest()
        {
            string stringValue = null;
            this.ValueTypeCloneTest(stringValue);
            var buf = new byte[256];
            this.ValueTypeSerializeTest(stringValue, buf);

            stringValue = string.Empty;
            this.ValueTypeCloneTest(stringValue);
            this.ValueTypeSerializeTest(stringValue, buf);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NullableTest()
        {
            bool? b = true;
            this.ValueTypeCloneTest(b);
            var buf = new byte[256];
            this.ValueTypeSerializeTest(b, buf);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BoxedTest()
        {
            object b = 1001;
            this.ValueTypeCloneTest(b);
            var buf = new byte[256];
            this.ValueTypeSerializeTest(b, buf);

            // validate single reference
            object[] r = new object[] { b, b };
            var r2 = r.DeepClone();
            Assert.IsTrue(object.ReferenceEquals(r2[0], r2[1])); // must have been single instanced
            Assert.IsFalse(object.ReferenceEquals(r[0], r2[0])); // must be different than the cloned instance

            // validate reference maintained after clone
            1002.DeepClone(ref b);
            Assert.IsTrue(object.ReferenceEquals(b, r[0])); // boxed object address should be the same

            // should work even if the source and target are not the same type
            1002d.DeepClone(ref b);
            Assert.IsFalse(object.ReferenceEquals(b, r[0])); // boxed object address should be different
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeClass()
        {
            var c = new STClass();
            var clone = c.DeepClone();
            Assert.IsTrue(c.IsDeepClone(clone));

            var arr = new[] { c, c, c };
            var arrClone = arr.DeepClone();
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.IsTrue(arr[i].IsDeepClone(arrClone[i]));
            }

            var buf = new byte[256];
            clone = this.SerializationClone(c, buf);
            Assert.IsTrue(c.IsDeepClone(clone));

            arrClone = this.SerializationClone(arr, buf);
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.IsTrue(arr[i].IsDeepClone(arrClone[i]));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeSimpleStruct()
        {
            // var c = new STStructSimple() { data = 1.1, label = "Some test", value = 100 };
            var c = new STStructSimple(1);
            var clone = c.DeepClone();
            Assert.AreEqual(c, clone);

            var arr = new[] { c, c, c };
            var arrClone = arr.DeepClone();
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.AreEqual(arr[i], arrClone[i]);
            }

            var buf = new byte[256];
            clone = this.SerializationClone(c, buf);
            Assert.AreEqual(c, clone);

            arrClone = this.SerializationClone(arr, buf);
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.AreEqual(arr[i], arrClone[i]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeComplexStruct()
        {
            var c = new STStructComplex(1001);
            var clone = c.DeepClone();
            Assert.IsTrue(c.IsDeepClone(clone));

            var arr = new[] { c, c, c };
            var arrClone = arr.DeepClone();
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.IsTrue(arr[i].IsDeepClone(arrClone[i]));
            }

            var buf = new byte[256];
            clone = this.SerializationClone(c, buf);
            Assert.IsTrue(c.IsDeepClone(clone));

            arrClone = this.SerializationClone(arr, buf);
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.IsTrue(arr[i].IsDeepClone(arrClone[i]));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeTuples()
        {
            var tuple1 = Tuple.Create(1);
            var tuple2 = Tuple.Create(1, 2);
            this.ValueTypeCloneTest(tuple1);
            this.ValueTypeCloneTest(tuple2);

            var buf = new byte[256];
            this.ValueTypeSerializeTest(tuple1, buf);
            this.ValueTypeSerializeTest(tuple2, buf);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeSimpleArray()
        {
            var intArray = new[] { 1, 2, 3, 4, 5 };

            var clonedArray = intArray.DeepClone();
            CollectionAssert.AreEqual(intArray, clonedArray);

            var buf = new byte[256];
            clonedArray = this.SerializationClone(intArray, buf);
            CollectionAssert.AreEqual(intArray, clonedArray);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeMultiDimArray()
        {
            var twoDimArray = new int[,]
            {
                { 0, 1 },
                { 2, 3 },
                { 4, 5 },
            };

            // Multi-dimensional arrays are not yet supported so this should throw.
            // Remove this try-catch block once support has been added for these.
            try
            {
                var clonedArray = twoDimArray.DeepClone();
                CollectionAssert.AreEqual(twoDimArray, clonedArray);
                Assert.AreEqual(twoDimArray.Rank, clonedArray.Rank);
                for (int rank = 0; rank < twoDimArray.Rank; rank++)
                {
                    Assert.AreEqual(twoDimArray.GetLowerBound(rank), clonedArray.GetLowerBound(rank));
                    Assert.AreEqual(twoDimArray.GetUpperBound(rank), clonedArray.GetUpperBound(rank));
                }

                var buf = new byte[256];
                clonedArray = this.SerializationClone(twoDimArray, buf);
                CollectionAssert.AreEqual(twoDimArray, clonedArray);
                Assert.AreEqual(twoDimArray.Rank, clonedArray.Rank);
                for (int rank = 0; rank < twoDimArray.Rank; rank++)
                {
                    Assert.AreEqual(twoDimArray.GetLowerBound(rank), clonedArray.GetLowerBound(rank));
                    Assert.AreEqual(twoDimArray.GetUpperBound(rank), clonedArray.GetUpperBound(rank));
                }
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Multi-dimensional arrays are currently not supported"));
                return;
            }

            Assert.Fail("Should have thrown above");
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeEnumerables()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5 });

            var clonedList = list.DeepClone();
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i], clonedList[i]);
            }

            var e = new Container() { Enumerable = list };
            var clonedIEnum = e.DeepClone();
            int j = 0;
            foreach (var elem in clonedIEnum.Enumerable)
            {
                Assert.AreEqual(list[j], elem);
                j++;
            }

            var buf = new byte[256];
            clonedList = this.SerializationClone(list, buf);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i], clonedList[i]);
            }

            clonedIEnum = this.SerializationClone(e, buf);
            j = 0;
            foreach (var elem in clonedIEnum.Enumerable)
            {
                Assert.AreEqual(list[j], elem);
                j++;
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeEnumerators()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5 });
            var source = list.Where(i => i % 2 == 0).Select(i => i * i);

            try
            {
                var clone = source.DeepClone();
                Assert.Fail("Should have thrown while attempting to clone IEnumerable query");
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Cannot clone Func"));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeDictionary()
        {
            var dict = new Dictionary<int, string>();
            dict.Add(0, "zero");
            dict.Add(1, "one");

            var clonedDict = dict.DeepClone();
            dict.DeepClone(ref clonedDict);
            foreach (var key in dict.Keys)
            {
                Assert.AreEqual(dict[key], clonedDict[key]);
            }

            var buf = new byte[256];
            clonedDict = this.SerializationClone(dict, buf);
            foreach (var key in dict.Keys)
            {
                Assert.AreEqual(dict[key], clonedDict[key]);
            }

            Serializer.Clear(ref clonedDict, new SerializationContext());
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeObjectDictionary()
        {
            var dict = new Dictionary<Tuple<int>, int>();
            dict.Add(Tuple.Create(0), 0);
            dict.Add(Tuple.Create(1), 1);

            var clonedDict = dict.DeepClone();
            dict.DeepClone(ref clonedDict);
            foreach (var key in dict.Keys)
            {
                Assert.AreEqual(dict[key], clonedDict[key]);
            }

            var buf = new byte[256];
            clonedDict = this.SerializationClone(dict, buf);
            foreach (var key in dict.Keys)
            {
                Assert.AreEqual(dict[key], clonedDict[key]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeDictionaryTree()
        {
            var innerDict = new Dictionary<int, STClass>();
            innerDict.Add(0, new STClass(FooEnum.One, 1, "one", new[] { 1.0 }));
            innerDict.Add(1, new STClass(FooEnum.Two, 2, "two", new[] { 2.0 }));

            var dict = new Dictionary<Tuple<int>, Dictionary<int, STClass>>();
            dict.Add(Tuple.Create(0), innerDict);

            var clonedDict = dict.DeepClone();
            dict.DeepClone(ref clonedDict);
            foreach (var key in dict.Keys)
            {
                Assert.IsTrue(dict[key][0].Same(clonedDict[key][0]));
                Assert.IsTrue(dict[key][1].Same(clonedDict[key][1]));
                Assert.AreNotEqual(dict[key][0], clonedDict[key][0]);
                Assert.AreNotEqual(dict[key][1], clonedDict[key][1]);
            }

            var buf = new byte[256];
            clonedDict = this.SerializationClone(dict, buf);
            foreach (var key in dict.Keys)
            {
                Assert.IsTrue(dict[key][0].Same(clonedDict[key][0]));
                Assert.IsTrue(dict[key][1].Same(clonedDict[key][1]));
                Assert.AreNotEqual(dict[key][0], clonedDict[key][0]);
                Assert.AreNotEqual(dict[key][1], clonedDict[key][1]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeEmitter()
        {
            var emitter = new Emitter<int>(0, null, null, null, null);
            var clonedEmitter = emitter.DeepClone();
            Assert.AreEqual(emitter.Name, clonedEmitter.Name);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializePixelFormat()
        {
            this.ValueTypeCloneTest(System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            this.ValueTypeSerializeTest(System.Drawing.Imaging.PixelFormat.Format24bppRgb, new byte[256]);
        }

        [TestMethod]
        [Timeout(2000)]
        public void PocoPerfSerialize()
        {
            int iter = 100000;
            Poco poco = new Poco();
            poco.DateProp = DateTime.Now;
            poco.IntProp = 100;
            poco.GuidProp = Guid.NewGuid();
            poco.StringProp = "test data";

            // byte[] is a baseline to compare with
            BufferWriter bw = new BufferWriter(256);
            var sc = new SerializationContext();
            Serializer.Serialize(bw, poco, sc);
            int byteSize = bw.Position;
            byte[] bytes = new byte[byteSize];
            Serializer.Serialize(bw, bytes, sc);
            BufferReader br = new BufferReader(bw.Buffer);

            Poco poco2 = new Poco();
            Serializer.Deserialize(br, ref poco2, sc);
            byte[] bytes2 = new byte[byteSize];
            Serializer.Deserialize(br, ref bytes2, sc);
            sc.Reset();

            // baseline
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                bw.Reset();
                Serializer.Serialize(bw, bytes, sc);
                sc.Reset();
            }

            for (int i = 0; i < iter; i++)
            {
                br.Reset();
                Serializer.Deserialize(br, ref bytes2, sc);
                sc.Reset();
            }

            sw.Stop();
            Console.WriteLine($"Baseline (byte[{byteSize}]): {sw.ElapsedMilliseconds * 1000000d / iter} nanoseconds per serialization + deserialization");

            // POCO test
            sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                bw.Reset();
                Serializer.Serialize(bw, poco, sc);
                sc.Reset();
            }

            for (int i = 0; i < iter; i++)
            {
                br.Reset();
                Serializer.Deserialize(br, ref poco2, sc);
                sc.Reset();
            }

            sw.Stop();
            Console.WriteLine($"Simple object (poco): {sw.ElapsedMilliseconds * 1000000d / iter} nanoseconds per serialization + deserialization");
            Console.WriteLine($"{iter / (sw.ElapsedMilliseconds / 1000d)} roundtrip operations per second");

            Assert.AreEqual(poco.StringProp, poco2.StringProp);
            Assert.AreEqual(poco.DateProp, poco2.DateProp);
            Assert.AreEqual(poco.IntProp, poco2.IntProp);
            Assert.AreEqual(poco.GuidProp, poco2.GuidProp);
        }

        [TestMethod]
        [Timeout(2000)]
        public void PocoPerfClone()
        {
            int iter = 100000;
            Poco poco = new Poco();
            poco.DateProp = DateTime.Now;
            poco.IntProp = 100;
            poco.GuidProp = Guid.NewGuid();
            poco.StringProp = "test data";

            Poco poco2 = new Poco();
            byte[] bytes = new byte[49];
            byte[] bytes2 = new byte[49];

            var sc = new SerializationContext();
            Serializer.Clone(poco, ref poco2, sc);
            Serializer.Clone(bytes, ref bytes2, sc);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                Serializer.Clone(bytes, ref bytes2, sc);
                sc.Reset();
            }

            sw.Stop();
            Console.WriteLine($"Baseline (byte[{bytes.Length}]): {sw.ElapsedMilliseconds * 1000000d / iter} nanoseconds per clone");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                Serializer.Clone(poco, ref poco2, sc);
                sc.Reset();
            }

            sw.Stop();
            Console.WriteLine($"Simple object (poco): {sw.ElapsedMilliseconds * 1000000d / iter} nanoseconds per clone");
            Console.WriteLine($"{iter / (sw.ElapsedMilliseconds / 1000d)} clones per second");

            Assert.AreEqual(poco.StringProp, poco2.StringProp);
            Assert.AreEqual(poco.DateProp, poco2.DateProp);
            Assert.AreEqual(poco.IntProp, poco2.IntProp);
            Assert.AreEqual(poco.GuidProp, poco2.GuidProp);
        }

        [TestMethod]
        [Timeout(2000)]
        public void DictPerfClone()
        {
            int iter = 1000000;
            int count = 23;
            var dict = new Dictionary<string, Poco>();
            Poco poco = new Poco();
            poco.DateProp = DateTime.Now;
            poco.IntProp = 100;
            poco.GuidProp = Guid.NewGuid();
            poco.StringProp = "test data";

            for (int i = 0; i < count; i++)
            {
                dict[i.ToString()] = poco;
            }

            var sc = new SerializationContext();
            Dictionary<string, Poco> dict2 = null;
            Serializer.Clone(dict, ref dict2, sc);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                Serializer.Clone(dict, ref dict2, sc);
            }

            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms per {iter} clone operations ({sw.ElapsedMilliseconds * 1000000d / iter} nanoseconds per clone)");
            Console.WriteLine($"{iter / (sw.ElapsedMilliseconds / 1000d)} clones per second");

            Assert.AreEqual(count, dict2.Count);
            var last = dict2[(count - 1).ToString()];
            Assert.AreEqual(poco.StringProp, last.StringProp);
            Assert.AreEqual(poco.DateProp, last.DateProp);
            Assert.AreEqual(poco.IntProp, last.IntProp);
            Assert.AreEqual(poco.GuidProp, last.GuidProp);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ClearShared()
        {
            // single Shared object
            var shared = Shared.Create(new byte[10]);
            Assert.IsNotNull(shared.Resource);
            Serializer.Clear(ref shared, new SerializationContext());
            Assert.IsNull(shared.Resource);

            // Tuple containing Shared object
            shared = Shared.Create(new byte[10]);
            var tuple = Tuple.Create(1, shared);
            Assert.IsNotNull(shared.Resource);
            Serializer.Clear(ref tuple, new SerializationContext());
            Assert.IsNull(shared.Resource);

            // ValueTuple containing Shared object
            shared = Shared.Create(new byte[10]);
            var vTuple = ValueTuple.Create(1, shared);
            Assert.IsNotNull(shared.Resource);
            Serializer.Clear(ref vTuple, new SerializationContext());
            Assert.IsNull(shared.Resource);

            // array of Shared objects
            var shared1 = Shared.Create(new byte[10]);
            var shared2 = Shared.Create(new byte[10]);
            var sharedArr = new[] { shared1, shared2 };
            Assert.IsNotNull(shared1.Resource);
            Assert.IsNotNull(shared2.Resource);
            Serializer.Clear(ref sharedArr, new SerializationContext());
            Assert.IsNull(shared1.Resource);
            Assert.IsNull(shared2.Resource);

            // Dictionary of Shared objects
            shared1 = Shared.Create(new byte[10]);
            shared2 = Shared.Create(new byte[10]);
            var sharedDict = new Dictionary<int, Shared<byte[]>>() { { 1, shared1 }, { 2, shared2 } };
            Assert.IsNotNull(shared1.Resource);
            Assert.IsNotNull(shared2.Resource);
            Serializer.Clear(ref sharedDict, new SerializationContext());
            Assert.IsNull(shared1.Resource);
            Assert.IsNull(shared2.Resource);

            // LinkedList of Shared objects
            shared1 = Shared.Create(new byte[10]);
            shared2 = Shared.Create(new byte[10]);
            var sharedList = new LinkedList<Shared<byte[]>>(new[] { shared1, shared2 });
            Assert.IsNotNull(shared1.Resource);
            Assert.IsNotNull(shared2.Resource);
            Serializer.Clear(ref sharedList, new SerializationContext());
            Assert.IsNull(shared1.Resource);
            Assert.IsNull(shared2.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ClearNotRequired()
        {
            // test various classes for which Clear() is a no-op
            var s1 = new STStructSimple(100);
            var s2 = new STStructSimple(100);
            Assert.IsTrue(s1.Equals(s2));
            Serializer.Clear(ref s1, new SerializationContext());
            Assert.IsTrue(s1.Equals(s2));

            var c1 = new STClass(FooEnum.One, 1, "one", new[] { 1.0 });
            var c2 = new STClass(FooEnum.One, 1, "one", new[] { 1.0 });
            Assert.IsTrue(c1.Same(c2));
            Serializer.Clear(ref c1, new SerializationContext());
            Assert.IsTrue(c1.Same(c2));

            var sc1 = new STStructComplex(100);
            var sc2 = new STStructComplex(100);
            Assert.IsTrue(sc1.Same(sc2));
            Serializer.Clear(ref sc1, new SerializationContext());
            Assert.IsTrue(sc1.Same(sc2));

            var arr = new STStructComplex[1];
            arr[0] = sc1;
            Assert.IsTrue(sc1.Same(sc2));
            Serializer.Clear(ref arr, new SerializationContext());
            Assert.IsTrue(sc1.Same(sc2));

            var list = new List<STStructComplex>();
            list.Add(sc1);
            Assert.IsTrue(sc1.Same(sc2));
            Serializer.Clear(ref list, new SerializationContext());
            Assert.IsTrue(sc1.Same(sc2));

            var dict = new Dictionary<int, STStructComplex>();
            dict.Add(1, sc1);
            Assert.IsTrue(sc1.Same(sc2));
            Serializer.Clear(ref dict, new SerializationContext());
            Assert.IsTrue(sc1.Same(sc2));
        }

        private void ValueTypeCloneTest<T>(T value)
        {
            Assert.AreEqual(value, value.DeepClone());
            Assert.AreEqual(value, new[] { value, value, value }.DeepClone()[2]);
        }

        private T SerializationClone<T>(T value, byte[] buf)
        {
            var encoding = System.Text.Encoding.Default;
            BufferWriter bw = new BufferWriter(buf);
            Serializer.Serialize(bw, value, new SerializationContext());
            Assert.IsTrue(bw.Position > 0);

            BufferReader br = new BufferReader(bw.Buffer);
            T result = default(T);
            Serializer.Deserialize<T>(br, ref result, new SerializationContext());
            return result;
        }

        private void ValueTypeSerializeTest<T>(T value, byte[] buf)
        {
            var encoding = System.Text.Encoding.Default;
            BufferWriter bw = new BufferWriter(buf);
            Serializer.Serialize(bw, value, new SerializationContext());
            Serializer.Serialize(bw, new[] { value, value, value }, new SerializationContext());

            Assert.IsTrue(bw.Position > 0);

            BufferReader br = new BufferReader(bw.Buffer);
            T result = default(T);
            Serializer.Deserialize<T>(br, ref result, new SerializationContext());
            Assert.AreEqual(value, result);
            T[] resultArr = null;
            Serializer.Deserialize<T[]>(br, ref resultArr, new SerializationContext());
            Assert.AreEqual(value, resultArr[2]);
        }

        private struct STStructSimple
        {
            public long Value;

            public STStructSimple(long v)
            {
                this.Value = 1001;
                this.Data = Math.PI;
            }

            public double Data { get; set; }
        }

        private struct STStructComplex
        {
            public long Value;
            private readonly STClass internalValue;

            public STStructComplex(long v)
            {
                this.internalValue = new STClass();
                this.Value = v;
            }

            public bool Same(STStructComplex that)
            {
                return this.Value == that.Value && this.internalValue.Same(that.internalValue);
            }

            public bool IsDeepClone(STStructComplex that)
            {
                return this.Same(that) && !ReferenceEquals(this.internalValue, that.internalValue);
            }
        }

        internal class STClass
        {
            private readonly int count;
            private FooEnum foo;
            private string label;
            private double[] buffer;

            public STClass()
                : this(FooEnum.Two, 10, "Something with ten", new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })
            {
            }

            public STClass(FooEnum foo, int count, string label, double[] buffer)
            {
                this.foo = foo;
                this.count = count;
                this.label = label;
                this.buffer = buffer;
            }

            public bool Same(STClass that)
            {
                return this.count == that.count
                    && this.label == that.label
                    && this.foo == that.foo
                    && (this.buffer.Except(that.buffer).Count() == 0)
                    && (that.buffer.Except(this.buffer).Count() == 0);
            }

            public bool IsDeepClone(STClass that)
            {
                return this.Same(that) && !ReferenceEquals(this.buffer, that.buffer);
            }

            public override int GetHashCode()
            {
                throw new Exception("Custom implementation of GetHashCode should not be called during serialization.");
            }
        }

        private class Container
        {
            public IEnumerable<int> Enumerable;
        }

        // "benchmark" type based on https://rogeralsing.com/2016/08/16/wire-writing-one-of-the-fastest-net-serializers/
        // uncomment next line to enable a custom serializer to compare generated code vs hand-crafted code. Results should be identical.
        // [Serializer(typeof(PocoSerializer))]
        private class Poco
        {
            private string stringProp;
            private int intProp;
            private Guid guidProp;
            private DateTime dateProp;

            public string StringProp
            {
                get { return this.stringProp; }
                set { this.stringProp = value; }
            }

            public int IntProp
            {
                get { return this.intProp; }
                set { this.intProp = value; }
            }

            public Guid GuidProp
            {
                get { return this.guidProp; }
                set { this.guidProp = value; }
            }

            public DateTime DateProp
            {
                get { return this.dateProp; }
                set { this.dateProp = value; }
            }

            private class PocoSerializer : ISerializer<Poco>
            {
                public int Version => throw new NotImplementedException();

                public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
                {
                    return null;
                }

                public void Clear(ref Poco target, SerializationContext context)
                {
                }

                public void Clone(Poco instance, ref Poco target, SerializationContext context)
                {
                    target.stringProp = instance.StringProp;
                    target.intProp = instance.IntProp;
                    target.guidProp = instance.GuidProp;
                    target.dateProp = instance.DateProp;
                }

                public void Deserialize(BufferReader reader, ref Poco target, SerializationContext context)
                {
                    Serializer.Deserialize(reader, ref target.stringProp, context);
                    Serializer.Deserialize(reader, ref target.intProp, context);
                    Serializer.Deserialize(reader, ref target.guidProp, context);
                    Serializer.Deserialize(reader, ref target.dateProp, context);
                }

                public void PrepareCloningTarget(Poco instance, ref Poco target, SerializationContext context)
                {
                }

                public void PrepareDeserializationTarget(BufferReader reader, ref Poco target, SerializationContext context)
                {
                }

                public void Serialize(BufferWriter writer, Poco instance, SerializationContext context)
                {
                    Serializer.Serialize(writer, instance.stringProp, context);
                    Serializer.Serialize(writer, instance.intProp, context);
                    Serializer.Serialize(writer, instance.guidProp, context);
                    Serializer.Serialize(writer, instance.dateProp, context);
                }
            }
        }
    }
}
