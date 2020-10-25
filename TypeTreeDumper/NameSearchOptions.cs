using System;

namespace TypeTreeDumper
{
    [Flags]
    enum NameSearchOptions : uint
    {
        None,
        CaseSensitive     = 1 << 0,
        CaseInsensitive   = 1 << 1,
        FileNameExtension = 1 << 2,
        RegularExpression = 1 << 3,
        UndecoratedName   = 1 << 4,
    }
}
