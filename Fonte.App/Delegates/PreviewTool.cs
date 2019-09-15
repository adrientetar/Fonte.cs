/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;

    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Input;

    public class PreviewTool : BaseTool
    {
        private Point? _previousPoint;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Hand;

        public override object FindResource(DesignCanvas canvas, object resourceKey)
        {
            var key = (string)resourceKey;
            if (key == DesignCanvas.DrawAnchorsKey ||
                key == DesignCanvas.DrawGuidelinesKey ||
                key == DesignCanvas.DrawMetricsKey ||
                key == DesignCanvas.DrawPointsKey ||
                key == DesignCanvas.DrawSelectionKey ||
                key == DesignCanvas.DrawStrokeKey)
            {
                return false;
            }
            else if (key == DesignCanvas.ComponentColorKey ||
                     key == DesignCanvas.FillColorKey)
            {
                return Colors.Black;
            }

            return base.FindResource(canvas, resourceKey);
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            if (args.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                _previousPoint = args.GetCurrentPoint(canvas).Position;

                Cursor = Cursors.HandGrab;
                canvas.InvalidateCursor();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            if (_previousPoint.HasValue)
            {
                var point = args.GetCurrentPoint(canvas).Position;

                canvas.ScrollBy(
                    _previousPoint.Value.X - point.X,
                    _previousPoint.Value.Y - point.Y);

                _previousPoint = point;
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);

            _previousPoint = null;

            Cursor = DefaultCursor;
            canvas.InvalidateCursor();
        }
    }
}
