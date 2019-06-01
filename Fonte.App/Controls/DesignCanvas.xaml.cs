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
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
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
        private static readonly string LayerContents = @"{""masterName"":""Regular"",""width"":520,""paths"":[[[65,388],[78,331],[124,331,""curve"",true],[152,331],[177,348],[177,376,""curve"",true],[177,405],[167,429],[157,448,""curve""],[180,471],[212,481],[239,481,""curve"",true],[307,481],[335,434],[335,356,""curve"",true],[335,286,""line""],[250,265,""line"",true],[93,229],[45,189],[45,110,""curve"",true],[45,46],[97,-6],[187,-6,""curve"",true],[253,-6],[310,34],[335,101,""curve""],[335,73,""line"",true],[335,29],[354,-2],[396,-5,""curve"",true],[453,-9],[484,25],[501,64,""curve""],[487,73,""line""],[475,48],[461,38],[446,38,""curve"",true],[427,38],[421,63],[421,95,""curve"",true],[421,351,""line"",true],[421,472],[358,510],[265,510,""curve"",true],[197,510],[128,476],[89,420,""curve"",true]],[[310,57],[259,35],[214,35,""curve"",true],[169,35],[133,68],[133,126,""curve"",true],[133,167],[157,218],[257,243,""curve"",true],[335,263,""line""],[335,131,""line""]]]}";
        private static readonly ICanvasDelegate PreviewTool = new PreviewTool();

        private CoreCursor _previousCursor;
        private Matrix3x2 _matrix;
        private ICanvasDelegate _tool;
        private bool _inPreview;

        internal static readonly string DrawPointsKey = "DrawPoints";
        internal static readonly string DrawSelectionKey = "DrawSelection";
        internal static readonly string DrawSelectionBoundsKey = "DrawSelectionBounds";
        internal static readonly string DrawStrokeKey = "DrawStroke";
        internal static readonly string FillColorKey = "FillColor";

        public static DependencyProperty LayerProperty = DependencyProperty.Register("Layer", typeof(Data.Layer), typeof(DesignCanvas), null);

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

            // XXX later we'll only do this if we're in design mode
            Layer = JsonConvert.DeserializeObject<Data.Layer>(LayerContents);
            new Data.Glyph(layers: new List<Data.Layer>() { Layer });

            Tool = new BaseTool();
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            _matrix = Matrix3x2.CreateScale(1, -1);
            // should be based on metrics, not bounds
            _matrix.Translation = new Vector2(
                .5f * ((float)Canvas.ActualWidth - Layer.Width),
                .5f * (float)(Canvas.ActualHeight + Layer.Bounds.Height)
            );

            CenterOnMetrics();

            if (DesignMode.DesignMode2Enabled)
                return;

            ((App)Application.Current).DataRefreshing += OnDataRefreshing;
        }

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        void OnRootViewChanged(muxp.Scroller sender, object args)
        {
            if (sender.ZoomFactor != Canvas.DpiScale)
            {
                Canvas.DpiScale = sender.ZoomFactor;
            }
            Invalidate();
        }
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.

        void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).DataRefreshing -= OnDataRefreshing;

            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        void OnDataRefreshing()
        {
            Invalidate();
        }

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
            foreach (var region in args.InvalidatedRegions)
            {
                using (var ds = sender.CreateDrawingSession(region))
                {
                    ds.Transform = _matrix;

                    var rescale = 1f / sender.DpiScale;

                    Tool.OnDraw(this, ds, rescale);
                    //Drawing.DrawMetrics(Layer, ds, rescale);
                    Drawing.DrawFill(Layer, ds, rescale, (Color)Tool.FindResource(this, FillColorKey));

                    //Drawing.DrawComponents(Layer, ds, rescale);
                    if ((bool)Tool.FindResource(this, DrawSelectionKey)) Drawing.DrawSelection(Layer, ds, rescale);
                    if ((bool)Tool.FindResource(this, DrawPointsKey)) Drawing.DrawPoints(Layer, ds, rescale);
                    if ((bool)Tool.FindResource(this, DrawStrokeKey)) Drawing.DrawStroke(Layer, ds, rescale);
                    if ((bool)Tool.FindResource(this, DrawSelectionBoundsKey)) Drawing.DrawSelectionBounds(Layer, ds, rescale);
                    Tool.OnDrawCompleted(this, ds, rescale);
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

        Matrix3x2 GetInverseMatrix()
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var m1 = Matrix3x2.CreateScale(1, -1);
            m1.Translation += _matrix.Translation;
            var m2 = Matrix3x2.CreateScale(Root.ZoomFactor);
            m2.Translation -= new Vector2((float)Root.HorizontalOffset, (float)Root.VerticalOffset);
            m1 *= m2;

            if (!Matrix3x2.Invert(m1, out m2))
            {
                throw new Exception("Couldn't invert _matrix");
            }

            return m2;
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void CenterOnMetrics()
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            Root.ZoomTo(1.0f, null);

            var options = new muxc.ScrollOptions(muxc.AnimationMode.Disabled);
            Root.ScrollTo(
                .5f * (Canvas.ActualWidth - Root.ActualWidth),
                .5f * (Canvas.ActualHeight - Root.ActualHeight),
                options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public Point GetLocalPosition(Point pos)
        {
            return Vector2.Transform(pos.ToVector2(), GetInverseMatrix()).ToPoint();
        }

        public object FindItemAt(Point pos, object ignoreItem = null)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var halfSize = 4.0 / Root.ZoomFactor;
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            foreach (var path in Layer.Paths)
            {
                foreach (var point in path.Points)
                {
                    if (point == ignoreItem)
                    {
                        continue;
                    }
                    var dx = point.X - pos.X;
                    var dy = point.Y - pos.Y;
                    if (-halfSize <= dx && dx <= halfSize &&
                        -halfSize <= dy && dy <= halfSize)
                    {
                        return point;
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
