// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Windows.UI.Core;


namespace Fonte.App.Utilities
{
    public static class Cursors
    {
        public static readonly CoreCursor Arrow = new(CoreCursorType.Custom, 101);
        public static readonly CoreCursor ArrowWithSquare = new(CoreCursorType.Custom, 102);
        public static readonly CoreCursor CrossWithEllipse = new(CoreCursorType.Custom, 103);
        public static readonly CoreCursor CrossWithRectangle = new(CoreCursorType.Custom, 104);
        public static readonly CoreCursor Hand = new(CoreCursorType.Custom, 105);
        public static readonly CoreCursor HandGrab = new(CoreCursorType.Custom, 106);
        public static readonly CoreCursor Knife = new(CoreCursorType.Custom, 107);
        public static readonly CoreCursor KnifeWithPlus = new(CoreCursorType.Custom, 108);
        public static readonly CoreCursor Pen = new(CoreCursorType.Custom, 109);
        public static readonly CoreCursor PenWithPlus = new(CoreCursorType.Custom, 110);
        public static readonly CoreCursor PenWithSquare = new(CoreCursorType.Custom, 111);
        public static readonly CoreCursor Ruler = new(CoreCursorType.Custom, 112);
        public static readonly CoreCursor SizeNESW = new(CoreCursorType.Custom, 113);
        public static readonly CoreCursor SizeNS = new(CoreCursorType.Custom, 114);
        public static readonly CoreCursor SizeNWSE = new(CoreCursorType.Custom, 115);
        public static readonly CoreCursor SizeWE = new(CoreCursorType.Custom, 116);
        public static readonly CoreCursor SystemArrow = new(CoreCursorType.Arrow, 0);
    }
}
