/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Microsoft.Graphics.Canvas;

    using System.Diagnostics;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Xaml.Input;

    public class SelectionTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;

        public SelectionTool()
        {
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(canvas, e);

            if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                _origin = _anchor = canvas.GetLocalPosition(e);
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            if (_origin.HasValue)
            {
                _anchor = canvas.GetLocalPosition(e);
                canvas.Invalidate();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(canvas, e);

            if (_origin.HasValue)
            {
                var rect = new Rect(_origin.Value, _anchor);

                foreach (var path in canvas.Layer.Paths)
                {
                    foreach (var point in path.Points)
                    {
                        point.Selected = rect.Contains(point.Position.ToPoint());
                    }
                }

                canvas.Invalidate();
            }
            _origin = null;
        }

        public override void OnDrawBackground(DesignCanvas canvas, CanvasDrawingSession ds)
        {
            if (_origin.HasValue)
            {
                ds.FillRectangle(new Rect(_origin.Value, _anchor), Color.FromArgb(51, 0, 120, 215));
            }
        }
    }
}
