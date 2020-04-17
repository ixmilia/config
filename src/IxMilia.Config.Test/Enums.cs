// Copyright (c) IxMilia.  All Rights Reserved.

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
