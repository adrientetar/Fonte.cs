/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Fonte.App.Delegates;
    using Fonte.App.Interfaces;
    using Fonte.App.Utilities;
    using Microsoft.Graphics.Canvas.UI.Xaml;
    using muxc = Microsoft.UI.Xaml.Controls;
    using muxp = Microsoft.UI.Xaml.Controls.Primitives;

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.ApplicationModel;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public partial class DesignCanvas : UserControl
    {
        internal static readonly string DrawFillKey = "DrawFill";
        internal static readonly string DrawMetricsKey = "DrawMetrics";
        internal static readonly string DrawPointsKey = "DrawPoints";
        internal static readonly string DrawSelectionKey = "DrawSelection";
        internal static readonly string DrawSelectionBoundsKey = "DrawSelectionBounds";
        internal static readonly string DrawStrokeKey = "DrawStroke";
        internal static readonly string ComponentColorKey = "ComponentColor";
        internal static readonly string FillColorKey = "FillColor";
        internal static readonly string PointColorKey = "PointColor";
        internal static readonly string SmoothPointColorKey = "SmoothPointColor";

        private static readonly ICanvasDelegate PreviewTool = new PreviewTool();

        private Matrix3x2 _matrix = Matrix3x2.CreateScale(1, -1);
        private ICanvasDelegate _tool = new BaseTool();
        private bool _inPreview;
        private CoreCursor _previousCursor;

        public static DependencyProperty LayerProperty = DependencyProperty.Register(
            "Layer", typeof(Data.Layer), typeof(DesignCanvas), new PropertyMetadata(null, OnLayerChanged));

        public Data.Layer Layer
        {
            get => (Data.Layer)GetValue(LayerProperty);
            set { SetValue(LayerProperty, value); }
        }

        public ICanvasDelegate Tool
        {
            get
            {
                if (_inPreview)
                {
                    return PreviewTool;
                }
                return _tool;
            }
            set
            {
                if (value != _tool)
                {
                    _tool?.OnDisabled(this);
                    _tool = value;
                    _tool.OnActivated(this);

                    InvalidateCursor();
                }
            }
        }

        public DesignCanvas()
        {
            InitializeComponent();
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            // Set the canvas origin
            _matrix.Translation = new Vector2(
                 .5f * (float)Canvas.ActualWidth,
                 .5f * (float)Canvas.ActualHeight
            );

            Invalidate();
            if (IsEnabled)
            {
                CenterOnMetrics();
            }

            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataRefreshing += OnDataRefreshing;
            }
        }

        void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataRefreshing -= OnDataRefreshing;
            }

            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        protected override void OnApplyTemplate()
        {
            OnLayerChanged();
        }

        void OnDataRefreshing()
        {
            Invalidate();
        }

        void OnLayerChanged()
        {
            IsEnabled = Layer != null;

            Canvas.Invalidate();
            if (IsEnabled)
            {
                CenterOnMetrics();
            }
        }

        static void OnLayerChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((DesignCanvas)sender).OnLayerChanged();
        }

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        void OnRootViewChanged(muxp.Scroller sender, object args)
        {
            if (sender.ZoomFactor != Canvas.DpiScale)
            {
                Canvas.DpiScale = sender.ZoomFactor;
            }
            //Invalidate();
        }
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.

        void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _previousCursor = Window.Current.CoreWindow.PointerCursor;

            Window.Current.CoreWindow.PointerCursor = Tool.Cursor;
        }

        void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_previousCursor != null)
            {
                Window.Current.CoreWindow.PointerCursor = _previousCursor;

                _previousCursor = null;
            }
        }

        void OnRegionsInvalidated(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
        {
            if (Layer != null)
            {
                foreach (var region in args.InvalidatedRegions)
                {
                    using (var ds = sender.CreateDrawingSession(region))
                    {
                        ds.Transform = _matrix;

                        var rescale = 1f / sender.DpiScale;
                        Tool.OnDraw(this, ds, rescale);

                        if ((bool)Tool.FindResource(this, DrawMetricsKey)) Drawing.DrawMetrics(Layer, ds, rescale);
                        if ((bool)Tool.FindResource(this, DrawFillKey)) Drawing.DrawFill(Layer, ds, rescale, (Color)Tool.FindResource(this, FillColorKey));
                        var drawSelection = (bool)Tool.FindResource(this, DrawSelectionKey);
                        Drawing.DrawComponents(Layer, ds, rescale, (Color)Tool.FindResource(this, ComponentColorKey), drawSelection);
                        if (drawSelection) Drawing.DrawSelection(Layer, ds, rescale);
                        if ((bool)Tool.FindResource(this, DrawPointsKey)) Drawing.DrawPoints(Layer, ds, rescale, (Color)Tool.FindResource(this, PointColorKey), (Color)Tool.FindResource(this, SmoothPointColorKey));
                        if ((bool)Tool.FindResource(this, DrawStrokeKey)) Drawing.DrawStroke(Layer, ds, rescale);
                        if ((bool)Tool.FindResource(this, DrawSelectionBoundsKey)) Drawing.DrawSelectionBounds(Layer, ds, rescale);
                        Tool.OnDrawCompleted(this, ds, rescale);
                    }
                }
            }
        }

        void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            Tool.OnKeyDown(this, e);

            if (!e.Handled && e.Key == VirtualKey.Space && !e.KeyStatus.WasKeyDown)
            {
                _inPreview = true;

                e.Handled = true;
                ((App)Application.Current).InvalidateData();
            }
        }

        void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            Tool.OnKeyUp(this, e);

            if (_inPreview && e.Key == VirtualKey.Space)
            {
                _inPreview = false;

                e.Handled = true;
                ((App)Application.Current).InvalidateData();
            }
        }

        void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ((UIElement)sender).CapturePointer(e.Pointer);

            if (Tool.HandlePointerEvent(this, e))
            {
                Tool.OnPointerPressed(this, e);
            }
        }

        void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (Tool.HandlePointerEvent(this, e))
            {
                Tool.OnPointerMoved(this, e);
            }
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (Tool.HandlePointerEvent(this, e))
            {
                Tool.OnPointerReleased(this, e);
            }

            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }

        void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Tool.OnDoubleTapped(this, e);
        }

        void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Tool.OnRightTapped(this, e);
        }

        Matrix3x2 GetMatrix()
        {
            var m1 = Matrix3x2.CreateScale(1, -1);
            m1.Translation += _matrix.Translation;
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var m2 = Matrix3x2.CreateScale(Root.ZoomFactor);
            m2.Translation -= new Vector2((float)Root.HorizontalOffset, (float)Root.VerticalOffset);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            m1 *= m2;

            return m1;
        }

        Matrix3x2 GetInverseMatrix()
        {
            var matrix = GetMatrix();
            if (Matrix3x2.Invert(matrix, out Matrix3x2 result))
            {
                return result;
            }
            throw new InvalidOperationException($"Matrix {matrix} isn't invertible");
        }

        public void CenterOnMetrics()
        {
            var master = Layer.Master;
            var fontHeight = master.Ascender - master.Descender;

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            Root.ScrollTo(
                .5f * (Canvas.ActualWidth - Root.ActualWidth + Layer.Width),
                .5f * (Canvas.ActualHeight - Root.ActualHeight - fontHeight) - master.Descender,
                new muxc.ScrollOptions(muxc.AnimationMode.Disabled, muxc.SnapPointsMode.Ignore));

            Root.ZoomTo(
                (float)(ActualHeight / Math.Round(fontHeight * 1.4)), null,
                new muxc.ZoomOptions(muxc.AnimationMode.Disabled, muxc.SnapPointsMode.Ignore));
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public Point GetClientPosition(Point canvasPos)
        {
            return Vector2.Transform(canvasPos.ToVector2(), GetMatrix()).ToPoint();
        }

        public Point GetCanvasPosition(Point clientPos)
        {
            return Vector2.Transform(clientPos.ToVector2(), GetInverseMatrix()).ToPoint();
        }

        // not sure that this oughta be in the view layer
        // -- if we want to avoid expensive recreation of paths, we could spinoff this function + drawing functions to a new kind of DrawingController class
        //    and retain the paths used for drawing for hit testing
        // -- caveat: at least for points I tend to dilate the hit-testing area compared to the drawing path, and use a square (don't need ellipse area tests)
        //    in which case we could create hit-testing paths but only make them once to avoid redoing it on every cursor move (for cursor markers)
        public object HitTest(Point pos, object ignoreItem = null, bool testSegments = true)
        {
            var rescale = 1f / Canvas.DpiScale;
            var halfSize = 4 * rescale;

            foreach (var anchor in Layer.Anchors)
            {
                if (anchor != ignoreItem)
                {
                    var dx = anchor.X - pos.X;
                    var dy = anchor.Y - pos.Y;
                    if (-halfSize <= dx && dx <= halfSize &&
                        -halfSize <= dy && dy <= halfSize)
                    {
                        return anchor;
                    }
                }
            }
            foreach (var path in Layer.Paths)
            {
                foreach (var point in path.Points)
                {
                    if (point != ignoreItem)
                    {
                        var dx = point.X - pos.X;
                        var dy = point.Y - pos.Y;
                        if (-halfSize <= dx && dx <= halfSize &&
                            -halfSize <= dy && dy <= halfSize)
                        {
                            return point;
                        }
                    }
                }
            }
            var p = pos.ToVector2();
            foreach (var component in Layer.Components)
            {
                if (component != ignoreItem && component.ClosedCanvasPath.FillContainsPoint(p))
                {
                    return component;
                }
            }
            // TODO: add master guidelines
            foreach (var guideline in Layer.Guidelines)
            {
                if (guideline != ignoreItem)
                {
                    var dx = guideline.X - pos.X;
                    var dy = guideline.Y - pos.Y;
                    if (-halfSize <= dx && dx <= halfSize &&
                        -halfSize <= dy && dy <= halfSize)
                    {
                        return guideline;
                    }
                }
            }

            if (testSegments)
            {
                var tol_2 = 9 + rescale * (6 + rescale);
                foreach (var path in Layer.Paths)
                {
                    foreach (var segment in path.Segments)
                    {
                        var proj = segment.ProjectPoint(p);
                        if (proj.HasValue && (proj.Value - p).LengthSquared() <= tol_2)
                        {
                            return segment;
                        }
                    }
                }
                // TODO: add guideline segment
            }

            return null;
        }

        public void Invalidate()
        {
            Canvas.Invalidate();
        }

        public void InvalidateCursor()
        {
            if (_previousCursor != null)
            {
                Window.Current.CoreWindow.PointerCursor = _tool.Cursor;
            }
        }

        public void ScrollTo(double x, double y, bool animated = false)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var options = new muxc.ScrollOptions(
                animated ? muxc.AnimationMode.Enabled : muxc.AnimationMode.Disabled,
                muxc.SnapPointsMode.Ignore);
            Root.ScrollTo(x, y, options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void ScrollBy(double dx, double dy, bool animated = false)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var options = new muxc.ScrollOptions(
                animated ? muxc.AnimationMode.Enabled : muxc.AnimationMode.Disabled,
                muxc.SnapPointsMode.Ignore);
            Root.ScrollBy(dx, dy, options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }
    }
}
