// Copyright (c) IxMilia.  All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigWriterTests
    {
        private void AssertWritten(IDictionary<string, string> dict, string expected, string existingText = null)
        {
            existingText = existingText ?? string.Empty;
            var existingLines = existingText.Trim().Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
            var actual = dict.WriteConfig(existingLines).Replace("\r", "").Trim();
            Assert.Equal(expected.Replace("\r", "").Trim(), actual);
        }

        [Fact]
        public void WriteToEmptyFileTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "rootValue", "true" },
                { "section.key2", "quoted\nstring" },
                { "section.key1", "value1" },
                { "section.deeperSection.key", "valueDeeper" },
            };

            AssertWritten(dict, """
rootValue = true

[section]
key1 = value1
key2 = "quoted\nstring"

[section.deeperSection]
key = valueDeeper
""");
        }

        [Fact]
        public void AddValuesToSectionTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "section.key1", "value1" },
                { "section.key2", "value2" },
                { "section.key3", "value3" },
            };

            AssertWritten(dict, @"
[section]
key1 = value1
key3 = value3
key2 = value2
",
@"
[section]
key1 = value1
key3 = value3
");
        }

        [Fact]
        public void AddRootSectionTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "root", "true" },
                { "section.key", "value" },
            };

            AssertWritten(dict, @"
root = true

[section]
key = value
", @"
[section]
key = value
");
        }

        [Fact]
        public void AddRegularSectionTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "otherSection.key", "true" },
                { "section.key", "value" },
            };

            AssertWritten(dict, @"
[section]
key = value

[otherSection]
key = true
", @"
[section]
key = value
");
        }

        [Fact]
        public void RemoveValueTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "section.key1", "value1" },
            };

            AssertWritten(dict, @"
[section]
key1 = value1
", @"
[section]
key1 = value1
key2 = value2
");
        }

        [Fact]
        public void DontWriteEmptySectionTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "singleKey", "singleValue" },
            };

            AssertWritten(dict, @"
singleKey = singleValue
", @"
singleKey = staleValue

[section]
key = value
");
        }

        [Fact]
        public void DontWriteMultipleBlankLinesTest()
        {
            var dict = new Dictionary<string, string>()
            {
                { "root", "value" },
                { "section.key1", "value1" },
            };

            AssertWritten(dict, @"
root = value

[section]
key1 = value1
", @"
root = stale



[section]
key1 = stale
");
        }

        [Fact]
        public void NullPropertiesAreNoteWritten()
        {
            var dict = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", null },
                { "key3", "value3" },
            };

            AssertWritten(dict, @"
key1 = value1
key3 = value3
");
        }

        [Fact]
        public void NewSettingInGroupDoesNotCauseBlankLineToBeWritten()
        {
            var dict = new Dictionary<string, string>()
            {
                { "section.key1", "value1" },
                { "section.key2", "value2" },
                { "section.key3", "value3" },
                { "other-section.key4", "value4" },
            };

            AssertWritten(dict, @"
[section]
key1 = value1
key3 = value3
key2 = value2

[other-section]
key4 = value4
",
@"
[section]
key1 = value1
key3 = value3

[other-section]
key4 = value4
");
        }
    }
}
