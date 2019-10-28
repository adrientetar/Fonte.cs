// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Interfaces
{
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public interface IToolbarItem
    {
        string Name { get; }
        IconSource Icon { get; }
        KeyboardAccelerator Shortcut { get; }
    }
}
