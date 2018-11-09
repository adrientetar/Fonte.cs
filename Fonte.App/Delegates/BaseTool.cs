/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Interfaces;
    using Microsoft.Graphics.Canvas;

    using System.Diagnostics;
    using Windows.Devices.Input;
    using Windows.Foundation;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class BaseTool : ICanvasDelegate, IToolBarItem
    {
        private Point? _previousPoint;

        public virtual CoreCursor Cursor { get; } = new CoreCursor(CoreCursorType.Arrow, 0);

        public BaseTool()
        {
        }

        public virtual bool HandlePointerEvent(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            return e.Pointer.PointerDeviceType == PointerDeviceType.Mouse;
        }

        public virtual void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
        }

        public virtual void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
        }

        public virtual void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(canvas).Properties.IsMiddleButtonPressed)
            {
                _previousPoint = e.GetCurrentPoint(canvas).Position;
            }

            canvas.Focus(FocusState.Pointer);
            e.Handled = true;
        }

        public virtual void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {            
            if (_previousPoint.HasValue)
            {
                var point = e.GetCurrentPoint(canvas).Position;

                canvas.ScrollBy(
                    point.X - _previousPoint.Value.X,
                    point.Y - _previousPoint.Value.Y);

                _previousPoint = point;
            }

            e.Handled = true;
        }

        public virtual void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            _previousPoint = null;

            e.Handled = true;
        }

        public virtual void OnDrawBackground(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
        }

        public virtual void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
        }

        #region IToolBarEntry implementation

        public virtual IconElement Icon { get; } = new SymbolIcon() { Symbol = Symbol.Add };

        public virtual string Name => string.Empty;

        public virtual KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator();

        #endregion
    }
}
