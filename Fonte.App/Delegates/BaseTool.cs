/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Microsoft.Graphics.Canvas;

    using Windows.Devices.Input;
    using Windows.Foundation;
    using Windows.UI.Xaml.Input;

    public class BaseTool : ICanvasDelegate
    {
        private Point? _previousPoint;

        public BaseTool()
        {
        }

        public virtual bool HandleEvent(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            return e.Pointer.PointerDeviceType == PointerDeviceType.Mouse;
        }

        public virtual void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(canvas).Properties.IsMiddleButtonPressed)
            {
                _previousPoint = e.GetCurrentPoint(canvas).Position;
            }
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
        }

        public virtual void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            _previousPoint = null;
        }

        public virtual void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds)
        {
        }
    }
}
