// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigToStringTests
    {
        private void Verify<T>(string expected, T value)
        {
            var actual = value.ToConfigString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DoubleToConfigStringTest()
        {
            Verify("2", 2.0);
        }

        [Fact]
        public void StringToConfigStringTest()
        {
            Verify(@"\""final\nvalue\""", "\"final\nvalue\"");
        }

        [Fact]
        public void EnumToConfigStringTest()
        {
            Verify("Dos", Numeros.Dos);
        }

        [Fact]
        public void FlagsEnumToConfigStringTest()
        {
            Verify("IsAlpha|IsBeta", Flags.IsAlpha | Flags.IsBeta);
        }

        [Fact]
        public void ArrayToConfigStringTest()
        {
            Verify("1;2", new[] { 1.0, 2.0 });
        }
    }
}
