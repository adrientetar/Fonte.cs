// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

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
    using System.Numerics;
    using Windows.ApplicationModel;
    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public partial class DesignCanvas : UserControl
    {
        internal const string DrawAlignmentZonesKey = "DrawAlignmentZones";
        internal const string DrawAnchorsKey = "DrawAnchors";
        internal const string DrawCoordinatesKey = "DrawCoordinates";
        internal const string DrawFillKey = "DrawFill";
        internal const string DrawGuidelinesKey = "DrawGuidelines";
        internal const string DrawLayersKey = "DrawLayers";
        internal const string DrawMetricsKey = "DrawMetrics";
        internal const string DrawPointsKey = "DrawPoints";
        internal const string DrawSelectionKey = "DrawSelection";
        internal const string DrawSelectionBoundsKey = "DrawSelectionBounds";
        internal const string DrawStrokeKey = "DrawStroke";

        internal const string AlignmentZoneColorKey = "AlignmentZoneColor";
        internal const string AnchorColorKey = "AnchorColor";
        internal const string ComponentColorKey = "ComponentColor";
        internal const string CornerPointColorKey = "CornerPointColor";
        internal const string FillColorKey = "FillColor";
        internal const string LayersColorKey = "LayersColor";
        internal const string MarkerColorKey = "MarkerColor";
        internal const string SmoothPointColorKey = "SmoothPointColor";
        internal const string SnapLineColorKey = "SnapLineColor";
        internal const string StrokeColorKey = "StrokeColor";

        internal const int MinPointSizeForDetails = 175;
        internal const int MinPointSizeForGrid = 10000;
        internal const int MinPointSizeForGuidelines = 100;

        private readonly ICanvasDelegate PreviewTool = new PreviewTool();
        private readonly ICanvasDelegate SelectionTool = new SelectionTool();

        private Matrix3x2 _matrix = Matrix3x2.CreateScale(1, -1);
        private ICanvasDelegate _tool = new BaseTool();
        private bool _previewToolOverride;
        private bool _selectionToolOverride;
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
            get => _previewToolOverride ? PreviewTool :
                   _selectionToolOverride ? SelectionTool :
                   _tool;
            set
            {
                if (value != _tool)
                {
                    _tool.OnDisabled(this);
                    _tool = value;
                    _tool.OnActivated(this);
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

        void OnControlLoaded(object sender, RoutedEventArgs args)
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
                ((App)Application.Current).DataChanged += OnDataChanged;
            }
        }

        void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataChanged -= OnDataChanged;
            }

            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        protected override void OnApplyTemplate()
        {
            OnLayerChanged();
        }

        void OnDataChanged(object sender, EventArgs args)
        {
            Invalidate();
        }

        void OnLayerChanged()
        {
            IsEnabled = Layer != null;

            if (IsEnabled)
            {
                CenterOnMetrics(animated: false);
            }
            Invalidate();
        }

        static void OnLayerChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((DesignCanvas)sender).OnLayerChanged();
        }

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        void OnScrollerStateChanged(muxp.Scroller sender, object args)
        {
            if (sender.State == muxc.InteractionState.Idle)
            {
                OnZoomRealized();
            }
        }

        // This is fired when doing non-animated programmatic view changes, otherwise it's on StateChanged.
        void OnScrollerZoomCompleted(muxp.Scroller sender, object args)
        {
            OnZoomRealized();
        }

        void OnZoomRealized()
        {
            Canvas.DpiScale = Scroller.ZoomFactor;
        }
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.

        void OnPointerEntered(object sender, PointerRoutedEventArgs args)
        {
            _previousCursor = Window.Current.CoreWindow.PointerCursor;

            Window.Current.CoreWindow.PointerCursor = Tool.Cursor;
        }

        void OnPointerExited(object sender, PointerRoutedEventArgs args)
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
                    using var ds = sender.CreateDrawingSession(region);

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
                        if (drawDetails && (bool)FindResource(DrawCoordinatesKey)) Drawing.DrawCoordinates(layer, ds, rescale);
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

        void OnKeyDown(object sender, KeyRoutedEventArgs args)
        {
            Tool.OnKeyDown(this, args);

            if (!args.Handled && !args.KeyStatus.WasKeyDown)
            {
                if (args.Key == VirtualKey.Space)
                {
                    SetToolOverride(() => _previewToolOverride = true);
                }
                else if (args.Key == VirtualKey.Control)
                {
                    SetToolOverride(() => _selectionToolOverride = true);
                }
            }
        }

        void OnKeyUp(object sender, KeyRoutedEventArgs args)
        {
            Tool.OnKeyUp(this, args);

            if (args.Key == VirtualKey.Space)
            {
                SetToolOverride(() => _previewToolOverride = false);
            }
            else if (args.Key == VirtualKey.Control)
            {
                SetToolOverride(() => _selectionToolOverride = false);
            }
        }

        void OnLostFocus(object sender, RoutedEventArgs e)
        {
            SetToolOverride(() => { _previewToolOverride = false; _selectionToolOverride = false; });
        }

        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            ((UIElement)sender).CapturePointer(args.Pointer);

            if (Tool.HandlePointerEvent(this, args))
            {
                Tool.OnPointerPressed(this, args);
            }
        }

        void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            if (Tool.HandlePointerEvent(this, args))
            {
                Tool.OnPointerMoved(this, args);
            }
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            if (Tool.HandlePointerEvent(this, args))
            {
                Tool.OnPointerReleased(this, args);
            }

            ((UIElement)sender).ReleasePointerCapture(args.Pointer);
        }

        void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
        {
            Tool.OnDoubleTapped(this, args);
        }

        void OnRightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            Tool.OnRightTapped(this, args);
        }

        object FindResource(string resourceKey)
        {
            return (Tool is PreviewTool tool ? tool : _tool).FindResource(this, resourceKey);
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
            throw new InvalidOperationException($"Matrix '{matrix}' isn't invertible.");
        }

        void SetToolOverride(Action stateChange)
        {
            var prevTool = Tool;
            stateChange.Invoke();

            var tool = Tool;
            if (tool != prevTool)
            {
                prevTool.OnDisabled(this);
                tool.OnActivated(this);
            }
        }

        public void CenterOnMetrics(bool animated = true)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            if (Scroller.ViewportHeight > 0)
            {
                var master = Layer.Master;
                var fontHeight = master.Ascender - master.Descender;
                var targetZoomFactor = (float)(Scroller.ViewportHeight / Math.Round(fontHeight * 1.4));

                ViewTo(
                    (_matrix.Translation.X + .5f * Layer.Width) * targetZoomFactor - .5f * Scroller.ViewportWidth,
                    (_matrix.Translation.Y - .5f * fontHeight - master.Descender) * targetZoomFactor - .5f * Scroller.ViewportHeight,
                    targetZoomFactor,
                    animated);
            }
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void EditAnchorName(Data.Anchor anchor)
        {
            if (anchor.Parent == null || anchor.Parent != Layer)
                throw new ArgumentException($"'{anchor}' is not a member of '{Layer}'.", nameof(anchor));

            var pos = FromCanvasPosition(anchor.ToVector2().ToPoint());
            pos.X += 10;
            pos.Y -= 11;
            AnchorTextBox.StartEditing(anchor, pos);
        }

        public Point FromCanvasPosition(Point pos)
        {
            return Vector2.Transform(pos.ToVector2(), GetMatrix()).ToPoint();
        }

        public Point FromClientPosition(Point pos)
        {
            return Vector2.Transform(pos.ToVector2(), GetInverseMatrix()).ToPoint();
        }

        public object HitTest(Point pos, ILayerElement ignoreElement = null, bool testSegments = false)
        {
            var pointSize = PointSize;
            var drawDetails = pointSize >= MinPointSizeForDetails;

            return UIBroker.HitTest(Layer, pos, 1f / ScaleFactor, ignoreElement: ignoreElement,
                                    testAnchors: drawDetails && (bool)Tool.FindResource(this, DrawAnchorsKey),
                                    testGuidelines: pointSize >= MinPointSizeForGuidelines && (bool)FindResource(DrawGuidelinesKey),
                                    testSelectionHandles: drawDetails && (bool)FindResource(DrawSelectionBoundsKey),
                                    testPoints: drawDetails && (bool)FindResource(DrawPointsKey),
                                    testSegments: testSegments);
        }

        public void Invalidate()
        {
            Canvas.Invalidate();
        }

        public void InvalidateCursor()
        {
            if (_previousCursor != null)
            {
                Window.Current.CoreWindow.PointerCursor = Tool.Cursor;
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
