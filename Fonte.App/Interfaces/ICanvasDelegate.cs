/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Interfaces
{
    using Fonte.App.Controls;
    using Microsoft.Graphics.Canvas;

    using Windows.UI.Core;
    using Windows.UI.Xaml.Input;

    public interface ICanvasDelegate
    {
        CoreCursor Cursor { get; }

        object FindResource(DesignCanvas canvas, object resourceKey);
        bool HandlePointerEvent(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnActivated(DesignCanvas canvas);
        void OnDisabled(DesignCanvas canvas);
        void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale);
        void OnDrawCompleted(DesignCanvas canvas, CanvasDrawingSession ds, float rescale);
        void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args);
        void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs args);
        void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs args);
        void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs args);
    }
}
