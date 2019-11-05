// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Interfaces;
    using Fonte.App.Utilities;

    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Input;

    public class PreviewTool : BaseTool
    {
        private Point? _previousPoint;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Hand;

        public override void OnActivated(DesignCanvas canvas, ActivationEventArgs args)
        {
            base.OnActivated(canvas, args);

            canvas.Invalidate();
        }

        public override void OnDisabled(DesignCanvas canvas, ActivationEventArgs args)
        {
            base.OnDisabled(canvas, args);

            canvas.Invalidate();
        }

        public override object FindResource(DesignCanvas canvas, string resourceKey)
        {
            if (resourceKey == DesignCanvas.DrawAnchorsKey ||
                resourceKey == DesignCanvas.DrawGuidelinesKey ||
                resourceKey == DesignCanvas.DrawLayersKey ||
                resourceKey == DesignCanvas.DrawMetricsKey ||
                resourceKey == DesignCanvas.DrawPointsKey ||
                resourceKey == DesignCanvas.DrawSelectionKey ||
                resourceKey == DesignCanvas.DrawStrokeKey)
            {
                return false;
            }
            else if (resourceKey == DesignCanvas.ComponentColorKey ||
                     resourceKey == DesignCanvas.FillColorKey)
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

        protected override void CompleteMove(DesignCanvas canvas)
        {
            base.CompleteMove(canvas);

            _previousPoint = null;

            Cursor = DefaultCursor;
            canvas.InvalidateCursor();
        }
    }
}
