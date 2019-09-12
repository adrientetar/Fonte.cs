/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Windows.UI.Core;

    static class Cursors
    {
        public static readonly CoreCursor Arrow = new CoreCursor(CoreCursorType.Custom, 101);
        public static readonly CoreCursor ArrowWithPoint = new CoreCursor(CoreCursorType.Custom, 102);
        public static readonly CoreCursor Cross = new CoreCursor(CoreCursorType.Cross, 0);
        public static readonly CoreCursor Pen = new CoreCursor(CoreCursorType.Custom, 110);
        public static readonly CoreCursor SizeNESW = new CoreCursor(CoreCursorType.SizeNortheastSouthwest, 0);
        public static readonly CoreCursor SizeNS = new CoreCursor(CoreCursorType.SizeNorthSouth, 0);
        public static readonly CoreCursor SizeNWSE = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 0);
        public static readonly CoreCursor SizeWE = new CoreCursor(CoreCursorType.SizeWestEast, 0);
        public static readonly CoreCursor SystemArrow = new CoreCursor(CoreCursorType.Arrow, 0);
    }
}