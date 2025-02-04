using System;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using FsCheck;
using FsCheck.Fluent;

namespace Arcus.Testing.Tests.Unit
{
    public enum TestJsonType { Random, Object, Array }

    public static class Properties
    {
        public static void Property(Action<TestCsv> prop)
        {
            var arb = Gen.Fresh(() => TestCsv.Generate()).ToArbitrary();
            Prop.ForAll(arb, prop).QuickCheckThrowOnFailure();
        }

        public static void Property(Action<Func<Action<TestCsvOptions>, TestCsv>> prop)
        {
            var arb = Gen.Fresh<Func<Action<TestCsvOptions>, TestCsv>>(() => TestCsv.Generate).ToArbitrary();
            Prop.ForAll(arb, prop).QuickCheckThrowOnFailure();
        }

        public static void Property(Action<TestCsv, TestCsv> prop)
        {
            var arb = Gen.Fresh(() => TestCsv.Generate()).ToArbitrary();
            Prop.ForAll(arb, arb, prop)
                .QuickCheckThrowOnFailure();
        }

        public static void Property(Action<TestJson> prop)
        {
            Property(TestJsonType.Random, prop);
        }

        public static void Property(TestJsonType type, Action<TestJson> prop)
        {
            Gen<TestJson> gen = type switch
            {
                TestJsonType.Random => Gen.Fresh(TestJson.Generate),
                TestJsonType.Array => Gen.Fresh(TestJson.GenerateArray),
                TestJsonType.Object => Gen.Fresh(TestJson.GenerateObject),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown test json type")
            };

            var arb = gen.ToArbitrary();
            Prop.ForAll(arb, prop).QuickCheckThrowOnFailure();
        }

        public static void Property(Action<TestJson, TestJson> prop)
        {
            var arb = Gen.Fresh(() => TestJson.Generate()).ToArbitrary();
            Prop.ForAll(arb, arb, prop)
                .QuickCheckThrowOnFailure();
        }

        public static void Property(Action<TestXml> prop)
        {
            var arb = Gen.Fresh(() => TestXml.Generate()).ToArbitrary();
            Prop.ForAll(arb, prop)
                .QuickCheckThrowOnFailure();
        }

        public static void Property(Action<TestXml, TestXml> prop)
        {
            var arb = Gen.Fresh(() => TestXml.Generate()).ToArbitrary();
            Prop.ForAll(arb, arb, prop)
                .QuickCheckThrowOnFailure();
        }
    }
}