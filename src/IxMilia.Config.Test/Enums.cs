// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Config.Test
{
    internal enum Numeros
    {
        Uno,
        Dos,
        Tres,
        Quatro,
    }

    [Flags]
    internal enum Flags
    {
        IsAlpha = 1,
        IsBeta = 2,
        IsGamma = 4,
    }
}
