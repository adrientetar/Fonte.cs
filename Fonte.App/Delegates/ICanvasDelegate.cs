/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Microsoft.Graphics.Canvas;

    using Windows.UI.Xaml.Input;

    public interface ICanvasDelegate
    {
        bool HandleEvent(DesignCanvas canvas, PointerRoutedEventArgs e);
        void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds);
        void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e);
        void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e);
        void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e);
    }
}
