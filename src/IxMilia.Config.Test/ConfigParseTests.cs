// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigParseTests
    {
        private void VerifyParse<T>(T expected, string value)
        {
            var dict = new Dictionary<string, string>();
            dict["key"] = value;
            T result;
            Assert.True(dict.TryParseValue("key", out result));
            Assert.Equal(expected, result);
        }

        private void VerifyParseFail<T>(string value)
        {
            var dict = new Dictionary<string, string>();
            dict["key"] = value;
            T result;
            Assert.False(dict.TryParseValue("key", out result));
        }

        [Fact]
        public void ParseDoubleTest()
        {
            VerifyParse(3.14, "3.14");
        }

        [Fact]
        public void ParseAssignDoubleTest()
        {
            var dbl = 1.0;
            "2.0".TryParseAssign(ref dbl);
            Assert.Equal(2.0, dbl);
        }

        [Fact]
        public void AssignDoubleFromDictionaryTest()
        {
            var dict = new Dictionary<string, string>();
            dict["key"] = "2.0";
            var dbl = 1.0;
            dict.TryParseAssign("key", ref dbl);
            Assert.Equal(2.0, dbl);
        }

        [Fact]
        public void ParseStringNotQuotedTest()
        {
            VerifyParse("some string", "some string");
        }

        [Fact]
        public void ParseQuotedStringTest()
        {
            VerifyParse("final\nvalue", @"""final\nvalue""");
        }

        [Fact]
        public void ParseDoubleFailTest()
        {
            VerifyParseFail<double>("three");
        }

        [Fact]
        public void NoParserTest()
        {
            // System.Object has no Parse() method
            VerifyParseFail<object>("foo");
        }

        [Fact]
        public void ParseEnumTest()
        {
            VerifyParse(Numeros.Dos, "Dos");
        }

        [Fact]
        public void ParseEnumFlagsTest()
        {
            VerifyParse(Flags.IsAlpha | Flags.IsBeta, "IsAlpha|IsBeta");
        }

        [Fact]
        public void ParseEnumFailTest()
        {
            VerifyParseFail<Numeros>("Cinco");
        }

        [Fact]
        public void ParseArrayTest()
        {
            VerifyParse(new[] { 1.0, 2.0 }, "1.0;2.0");
        }
    }
}
