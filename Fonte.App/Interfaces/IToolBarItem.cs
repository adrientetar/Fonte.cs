/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Interfaces
{
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public interface IToolBarItem
    {
        string Name { get; }
        IconElement Icon { get; }
        KeyboardAccelerator Shortcut { get; }
    }
}
