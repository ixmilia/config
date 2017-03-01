IxMilia.Config
==============

A portable .NET library for reading and writing INI-style configuration files.

## Usage

Configuration handling is implemented as a set of extension methods on `string`
and `IDictionary<string, string>` to enable maximum portability.  Key/value
pairs are reflected in the `<section-name>.<key-name> = <value>` format.  E.g.,

Given the following file:

```
[heading1]
key1 = value1
key2 = value2

[heading2]
key3 = value3
```

The following dictionary mappings will be produced:

```
"heading1.key1" => "value1"
"heading1.key2" => "value2"
"heading2.key3" => "value3"
```

To read a config file from disk:

``` C#
using System.Collections.Generic;
using System.IO;
using IxMilia.Config;
// ...

string[] lines = File.ReadAllLines(@"C:\Path\To\File.config");
IDictionary<string, string> dict = new Dictionary<string, string>();
dict.ParseConfig(lines);
```

Other extension methods are provided to aid in parsing out the individual string
values, such as:

``` C#
<string>.TryParseValue()
<string>.TryParseAssign()
<IDictionary<string, string>>.TryParseValue()
<IDictionary<string, string>>.TryParseAssign()
```

When writing a file back to disk you have the option of provding the original lines
from the file which will preserve as much of the original file's structure as
possible.

``` C#
using System.Collections.Generic;
using System.IO;
using IxMilia.Config;
// ...

IDictionary<string, string> dict = ...;
string configText;
string configFilePath = @"C:\Path\To\File.config";
if (<preserve structure>)
{
    // read in the existing lines to try to copy the original file's structure
    string[] existingLines = File.ReadAllLines(configFilePath);
    configText = dict.WriteConfig(existingLines);
}
else
{
    // don't preserve any structure
    configText = dict.WriteConfig();
}

// write the new contents
File.WriteAllText(configFilePath, configText);
```

## Building locally

Requirements to build locally are:

- [Latest .NET Core SDK](https://github.com/dotnet/cli/releases)  As of this writing the following was also required on Ubuntu 14.04: 

`sudo apt-get install dotnet-sharedframework-microsoft.netcore.app-1.0.3`

## Integration

All relevant code is in the `src\IxMilia.Config\ConfigExtensions.cs` file so you
can either build and link against the assembly or directly include in your project
just by copying one file.
