// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigReaderTests
    {
        private IDictionary<string, string> Parse(string data)
        {
            var dict = new Dictionary<string, string>();
            var lines = data.Trim().Split(new[] { '\n' }).Select(l => l.TrimEnd('\r')).ToArray();
            dict.ParseConfig(lines);
            return dict;
        }

        [Fact]
        public void SimpleParseTest()
        {
            var dict = Parse(@"
; comment
rootValue = true

[section]
# some other comment
key = value

[section.deeperSection]
key = value2
");
            Assert.Equal(3, dict.Keys.Count);
            Assert.Equal("true", dict["rootValue"]);
            Assert.Equal("value", dict["section.key"]);
            Assert.Equal("value2", dict["section.deeperSection.key"]);
        }
    }
}
