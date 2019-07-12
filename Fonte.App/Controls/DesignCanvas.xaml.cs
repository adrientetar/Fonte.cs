/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Fonte.App.Delegates;
    using Fonte.App.Interfaces;
    using Fonte.App.Utilities;
    using Fonte.Data.Interfaces;
    using Microsoft.Graphics.Canvas.UI.Xaml;
    using muxc = Microsoft.UI.Xaml.Controls;
    using muxp = Microsoft.UI.Xaml.Controls.Primitives;

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.ApplicationModel;
    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public partial class DesignCanvas : UserControl
    {
        internal static readonly string DrawAlignmentZonesKey = "DrawAlignmentZones";
        internal static readonly string DrawAnchorsKey = "DrawAnchors";
        internal static readonly string DrawFillKey = "DrawFill";
        internal static readonly string DrawGuidelinesKey = "DrawGuidelines";
        internal static readonly string DrawLayersKey = "DrawLayers";
        internal static readonly string DrawMetricsKey = "DrawMetrics";
        internal static readonly string DrawPointsKey = "DrawPoints";
        internal static readonly string DrawSelectionKey = "DrawSelection";
        internal static readonly string DrawSelectionBoundsKey = "DrawSelectionBounds";
        internal static readonly string DrawStrokeKey = "DrawStroke";

        internal static readonly string AlignmentZoneColorKey = "AlignmentZoneColor";
        internal static readonly string AnchorColorKey = "AnchorColor";
        internal static readonly string ComponentColorKey = "ComponentColor";
        internal static readonly string CornerPointColorKey = "CornerPointColor";
        internal static readonly string FillColorKey = "FillColor";
        internal static readonly string LayersColorKey = "LayersColor";
        internal static readonly string MarkerColorKey = "MarkerColor";
        internal static readonly string SmoothPointColorKey = "SmoothPointColor";
        internal static readonly string StrokeColorKey = "StrokeColor";

        internal static readonly int MinPointSizeForDetails = 175;
        internal static readonly int MinPointSizeForGrid = 10000;
        internal static readonly int MinPointSizeForGuidelines = 100;

        private static readonly ICanvasDelegate PreviewTool = new PreviewTool();

        private Matrix3x2 _matrix = Matrix3x2.CreateScale(1, -1);
        private ICanvasDelegate _tool = new BaseTool();
        private bool _isInPreview;
        private CoreCursor _previousCursor;

        public static DependencyProperty LayerProperty = DependencyProperty.Register(
            "Layer", typeof(Data.Layer), typeof(DesignCanvas), new PropertyMetadata(null, OnLayerChanged));

        public Data.Layer Layer
        {
            get => (Data.Layer)GetValue(LayerProperty);
            set { SetValue(LayerProperty, value); }
        }

        public bool IsInPreview
        {
            get => _isInPreview;
            set
            {
                if (value != _isInPreview)
                {
                    _isInPreview = value;

                    Invalidate();
                }
            }
        }

        public ICanvasDelegate Tool
        {
            get
            {
                if (_isInPreview)
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

        public int PointSize => (int)Math.Round(Layer.Parent.Parent.UnitsPerEm * ScaleFactor);

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        public float ScaleFactor => Scroller.ZoomFactor;
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.

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

            if (IsEnabled)
            {
                CenterOnMetrics(animated: false);
            }
            Invalidate();

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

            if (IsEnabled)
            {
                CenterOnMetrics();
            }
            Canvas.Invalidate();
        }

        static void OnLayerChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((DesignCanvas)sender).OnLayerChanged();
        }

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        void OnScrollerStateChanged(muxp.Scroller sender, object e)
        {
            if (Scroller.State == muxc.InteractionState.Idle)
            {
                Canvas.DpiScale = sender.ZoomFactor;
            }
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
            var layer = Layer;

            if (layer != null)
            {
                foreach (var region in args.InvalidatedRegions)
                {
                    using (var ds = sender.CreateDrawingSession(region))
                    {
                        ds.Transform = _matrix;

                        var pointSize = PointSize;
                        var rescale = 1f / sender.DpiScale;

                        var drawDetails = pointSize >= MinPointSizeForDetails;
                        var drawFill = (bool)FindResource(DrawFillKey);

                        // TODO: need to refactor use of transformations...
                        var drawingRect = Data.Geometry.Rect.Transform(new Data.Geometry.Rect(
                                new Vector2((float)region.Left, (float)region.Top) - _matrix.Translation,
                                new Vector2((float)region.Right, (float)region.Bottom) - _matrix.Translation
                            ), GetInverseMatrix());

                        if ((bool)FindResource(DrawMetricsKey))
                        {
                            // TODO: divide MinPointSizeForGrid by gridSize
                            if (pointSize >= MinPointSizeForGrid) Drawing.DrawGrid(
                                layer, ds, rescale, drawingRect);
                            Drawing.DrawMetrics(layer, ds, rescale, (bool)FindResource(DrawAlignmentZonesKey) ?
                                                                    (Color?)FindResource(AlignmentZoneColorKey) :
                                                                    null);
                        }
                        if (drawFill) Drawing.DrawFill(layer, ds, rescale, (Color)FindResource(FillColorKey));

                        Tool.OnDraw(this, ds, rescale);

                        if ((bool)FindResource(DrawLayersKey)) Drawing.DrawLayers(layer, ds, rescale, (Color)FindResource(LayersColorKey));
                        if (pointSize >= MinPointSizeForGuidelines && (bool)FindResource(DrawGuidelinesKey)) Drawing.DrawGuidelines(
                            layer, ds, rescale, drawingRect);
                        if (layer.Paths.Count > 0 || layer.Components.Count > 0)
                        {
                            var drawSelection = drawDetails && (bool)FindResource(DrawSelectionKey);
                            var drawStroke = (bool)FindResource(DrawStrokeKey);

                            Drawing.DrawComponents(layer, ds, rescale, (Color)FindResource(ComponentColorKey),
                                                   drawSelection: drawSelection);
                            if (drawSelection) Drawing.DrawSelection(layer, ds, rescale);
                            if (drawDetails && (bool)FindResource(DrawPointsKey)) Drawing.DrawPoints(layer, ds, rescale,
                                                                                                     (Color)FindResource(CornerPointColorKey), (Color)FindResource(SmoothPointColorKey),
                                                                                                     (Color)FindResource(MarkerColorKey));
                            // If we only draw fill, we still want to stroke open paths as we don't fill them whatsoever
                            if (drawFill || drawStroke) Drawing.DrawStroke(layer, ds, rescale,
                                                                           (Color)FindResource(StrokeColorKey),
                                                                           drawStroke ? Drawing.StrokePaths.ClosedOpen : Drawing.StrokePaths.Open);
                            if (drawDetails && (bool)FindResource(DrawSelectionBoundsKey)) Drawing.DrawSelectionBounds(layer, ds, rescale);
                        }
                        else
                        {
                            Drawing.DrawUnicode(layer, ds, rescale);
                        }
                        if (drawDetails && (bool)Tool.FindResource(this, DrawAnchorsKey)) Drawing.DrawAnchors(
                            layer, ds, rescale, (Color)Tool.FindResource(this, AnchorColorKey));

                        Tool.OnDrawCompleted(this, ds, rescale);
                    }
                }
            }
        }

        void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            Tool.OnKeyDown(this, e);
        }

        void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            Tool.OnKeyUp(this, e);
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

        object FindResource(string resourceKey)
        {
            return Tool.FindResource(this, resourceKey);
        }

        Matrix3x2 GetMatrix()
        {
            var m1 = Matrix3x2.CreateScale(1, -1);
            m1.Translation += _matrix.Translation;
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var m2 = Matrix3x2.CreateScale(Scroller.ZoomFactor);
            m2.Translation -= new Vector2((float)Scroller.HorizontalOffset, (float)Scroller.VerticalOffset);
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

        public void CenterOnMetrics(bool animated = true)
        {
            var master = Layer.Master;
            var fontHeight = master.Ascender - master.Descender;

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var targetZoomFactor = (float)(Scroller.ViewportHeight / Math.Round(fontHeight * 1.4));
            ViewTo(
                (_matrix.Translation.X + .5f * Layer.Width) * targetZoomFactor - .5f * Scroller.ViewportWidth,
                (_matrix.Translation.Y - .5f * fontHeight - master.Descender) * targetZoomFactor - .5f * Scroller.ViewportHeight,
                targetZoomFactor,
                animated);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void EditAnchorName(Data.Anchor anchor)
        {
            if (anchor.Parent == null || anchor.Parent != Layer)
                throw new InvalidOperationException($"{anchor} is not a member of {Layer}");

            var pos = GetClientPosition(anchor.ToVector2().ToPoint());
            pos.X += 10;
            pos.Y -= 11;
            AnchorTextBox.StartEditing(anchor, pos);
        }

        public Point GetClientPosition(Point canvasPos)
        {
            return Vector2.Transform(canvasPos.ToVector2(), GetMatrix()).ToPoint();
        }

        public Point GetCanvasPosition(Point clientPos)
        {
            return Vector2.Transform(clientPos.ToVector2(), GetInverseMatrix()).ToPoint();
        }

        public object HitTest(Point pos, ILayerElement ignoreElement = null, bool testSegments = true)
        {
            var layer = Layer;
            var rescale = 1f / Canvas.DpiScale;
            var halfSize = 4 * rescale;

            // XXX: given that Scale is computed off thread, we risk a discrepancy between drawing and hit testing
            var drawDetails = PointSize >= MinPointSizeForDetails;

            foreach (var anchor in layer.Anchors)
            {
                if (!ReferenceEquals(anchor, ignoreElement))
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
            foreach (var path in layer.Paths)
            {
                foreach (var point in path.Points)
                {
                    if (!ReferenceEquals(point, ignoreElement))
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
            foreach (var component in layer.Components)
            {
                if (!ReferenceEquals(component, ignoreElement) && component.ClosedCanvasPath.FillContainsPoint(p))
                {
                    return component;
                }
            }
            foreach (var guideline in Misc.GetAllGuidelines(layer))
            {
                if (!ReferenceEquals(guideline, ignoreElement))
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
                foreach (var path in layer.Paths)
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
                if (Misc.GetSelectedGuideline(layer) is Data.Guideline guideline)
                {
                    var proj = BezierMath.ProjectPointOnLine(p, guideline.ToVector2(), guideline.Direction);

                    if ((proj - p).LengthSquared() <= tol_2)
                    {
                        return new Misc.GuidelineRule(guideline);
                    }
                }
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
                animated ? muxc.AnimationMode.Auto : muxc.AnimationMode.Disabled,
                muxc.SnapPointsMode.Ignore);
            Scroller.ScrollTo(x, y, options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void ScrollBy(double dx, double dy, bool animated = false)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var options = new muxc.ScrollOptions(
                animated ? muxc.AnimationMode.Auto : muxc.AnimationMode.Disabled,
                muxc.SnapPointsMode.Ignore);
            Scroller.ScrollBy(dx, dy, options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void ZoomTo(float scale, bool animated = false)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var options = new muxc.ZoomOptions(
                animated ? muxc.AnimationMode.Auto : muxc.AnimationMode.Disabled,
                muxc.SnapPointsMode.Ignore);
            Scroller.ZoomTo(scale, null, options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void ViewTo(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool animated)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            double targetHorizontalOffset = horizontalOffset == null ? Scroller.HorizontalOffset : (double)horizontalOffset;
            double targetVerticalOffset = verticalOffset == null ? Scroller.VerticalOffset : (double)verticalOffset;
            float targetZoomFactor = zoomFactor == null ? Scroller.ZoomFactor : (float)Math.Max(Math.Min((double)zoomFactor, Scroller.MaxZoomFactor), Scroller.MinZoomFactor);
            float deltaZoomFactor = targetZoomFactor - Scroller.ZoomFactor;

            if (!animated)
            {
                targetHorizontalOffset = Math.Max(Math.Min(targetHorizontalOffset, Scroller.ExtentWidth * targetZoomFactor - Scroller.ViewportWidth), 0.0);
                targetVerticalOffset = Math.Max(Math.Min(targetVerticalOffset, Scroller.ExtentHeight * targetZoomFactor - Scroller.ViewportHeight), 0.0);
            }

            if (deltaZoomFactor == 0.0f)
            {
                if (targetHorizontalOffset == Scroller.HorizontalOffset && targetVerticalOffset == Scroller.VerticalOffset)
                    return;

                Scroller.ScrollTo(
                    targetHorizontalOffset,
                    targetVerticalOffset,
                    new muxc.ScrollOptions(
                        animated ? muxc.AnimationMode.Auto : muxc.AnimationMode.Disabled,
                        muxc.SnapPointsMode.Ignore));
            }
            else
            {
                Vector2 centerPoint = new Vector2(
                    (float)(targetHorizontalOffset * Scroller.ZoomFactor - Scroller.HorizontalOffset * targetZoomFactor) / deltaZoomFactor,
                    (float)(targetVerticalOffset * Scroller.ZoomFactor - Scroller.VerticalOffset * targetZoomFactor) / deltaZoomFactor);

                Scroller.ZoomTo(
                    targetZoomFactor,
                    centerPoint,
                    new muxc.ZoomOptions(
                        animated ? muxc.AnimationMode.Auto : muxc.AnimationMode.Disabled,
                        muxc.SnapPointsMode.Default));
            }
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }
    }
}
