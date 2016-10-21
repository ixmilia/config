// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigSerializeTests
    {
        private void VerifySerialize(string expected, TestClass tc)
        {
            string actual = tc.SerializeConfig();
            Assert.Equal(expected.Trim(), actual.Trim());
        }

        [Fact]
        public void SimpleSerializeTest()
        {
            var tc = new TestClass()
            {
                DoubleValue = 2.0,
                EnumValue = Numeros.Quatro,
                Integers = new[] { 1, 2, 3 },
            };
            VerifySerialize(@"
DoubleValue = 2
Integers = 1;2;3

[enums]
numeros = Quatro
", tc);
        }

        [Fact]
        public void SimpleDeserializeTest()
        {
            var str = @"
DoubleValue = 2
Integers = 1;2;3

[enums]
numeros = Quatro
";
            var tc = new TestClass();
            tc.DeserializeConfig(str.Split('\n').Select(line => line.TrimEnd('\r')).ToArray());
            Assert.Equal(2.0, tc.DoubleValue);
            Assert.Equal(new[] { 1, 2, 3 }, tc.Integers);
            Assert.Equal(Numeros.Quatro, tc.EnumValue);
        }

        private class TestClass
        {
            public double DoubleValue { get; set; }

            [ConfigPath("enums.numeros")]
            public Numeros EnumValue { get; set; }

            public int[] Integers { get; set; }
        }
    }
}
