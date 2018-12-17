/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Fonte.App.Delegates;
    using Fonte.App.Interfaces;
    using Fonte.App.Utilities;
    using Microsoft.Graphics.Canvas.UI.Xaml;
    using mux = Microsoft.UI.Xaml.Controls;
    using Newtonsoft.Json;

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.Devices.Input;
    using Windows.Foundation;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Hosting;
    using Windows.UI.Xaml.Input;

    public partial class DesignCanvas : UserControl
    {
        private const string _layerContents = @"{""masterName"":""Regular"",""width"":520,""paths"":[[[65,388],[78,331],[124,331,""curve"",true],[152,331],[177,348],[177,376,""curve"",true],[177,405],[167,429],[157,448,""curve""],[180,471],[212,481],[239,481,""curve"",true],[307,481],[335,434],[335,356,""curve"",true],[335,286,""line""],[250,265,""line"",true],[93,229],[45,189],[45,110,""curve"",true],[45,46],[97,-6],[187,-6,""curve"",true],[253,-6],[310,34],[335,101,""curve""],[335,73,""line"",true],[335,29],[354,-2],[396,-5,""curve"",true],[453,-9],[484,25],[501,64,""curve""],[487,73,""line""],[475,48],[461,38],[446,38,""curve"",true],[427,38],[421,63],[421,95,""curve"",true],[421,351,""line"",true],[421,472],[358,510],[265,510,""curve"",true],[197,510],[128,476],[89,420,""curve"",true]],[[310,57],[259,35],[214,35,""curve"",true],[169,35],[133,68],[133,126,""curve"",true],[133,167],[157,218],[257,243,""curve"",true],[335,263,""line""],[335,131,""line""]]]}";

        private CoreCursor _previousCursor;
        private Matrix3x2 _matrix;
        private ICanvasDelegate _tool;

        public Data.Layer Layer { get; set; }

        public ICanvasDelegate Tool
        {
            get => _tool;
            set
            {
                _tool = value;

                if (_previousCursor != null)
                {
                    Window.Current.CoreWindow.PointerCursor = _tool.Cursor;
                }
            }
        }

        public DesignCanvas()
        {
            InitializeComponent();

            // later we'll only do this if we're in design mode
            Layer = JsonConvert.DeserializeObject<Data.Layer>(_layerContents);

            Tool = new BaseTool();
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            mux.ScrollerChangeOffsetsOptions options = new mux.ScrollerChangeOffsetsOptions(
                .5f * (Canvas.ActualWidth - Root.ActualWidth),
                .5f * (Canvas.ActualHeight - Root.ActualHeight),
                mux.ScrollerViewKind.Absolute, mux.ScrollerViewChangeKind.DisableAnimation, mux.ScrollerViewChangeSnapPointRespect.IgnoreSnapPoints);
            Root.ChangeOffsets(options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.

            _matrix = Matrix3x2.CreateScale(1, -1);
            // should be based on metrics, not bounds
            _matrix.Translation = new Vector2(
                .5f * ((float)Canvas.ActualWidth - Layer.Width),
                .5f * (float)(Canvas.ActualHeight + Layer.Bounds.Height)
            );
        }

#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        void Root_ViewChanged(mux.Scroller sender, object args)
        {
                if (sender.ZoomFactor != Canvas.DpiScale)
                {
                    Canvas.DpiScale = sender.ZoomFactor;
                }
                this.Invalidate();

        }
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.

        void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _previousCursor = Window.Current.CoreWindow.PointerCursor;

            Window.Current.CoreWindow.PointerCursor = Tool.Cursor;
        }

        void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = _previousCursor;

            _previousCursor = null;
        }

        void OnRegionsInvalidated(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
        {
            foreach (var region in args.InvalidatedRegions)
            {
                using (var ds = sender.CreateDrawingSession(region))
                {
                    ds.Transform = _matrix;

                    var rescale = 1 / sender.DpiScale;

                    Tool.OnDrawBackground(this, ds, rescale);

                    Drawing.DrawPoints(Layer, ds, rescale);
                    Drawing.DrawStroke(Layer, ds, rescale);

                    Tool.OnDraw(this, ds, rescale);
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

        Matrix3x2 GetInverseMatrix()
        {
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
        }

        public Point GetLocalPosition(PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(this).Position;
            return Vector2.Transform(pos.ToVector2(), GetInverseMatrix()).ToPoint();
        }

        public void Invalidate()
        {
            Canvas.Invalidate();
        }

        public void ScrollTo(double x, double y, bool animated = false)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            mux.ScrollerChangeOffsetsOptions options = new mux.ScrollerChangeOffsetsOptions(
                x, y,
                offsetsKind: mux.ScrollerViewKind.RelativeToCurrentView,
                viewChangeKind: animated ? mux.ScrollerViewChangeKind.AllowAnimation : mux.ScrollerViewChangeKind.DisableAnimation,
                snapPointRespect: mux.ScrollerViewChangeSnapPointRespect.IgnoreSnapPoints);
            Root.ChangeOffsets(options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }

        public void ScrollBy(double dx, double dy, bool animated = false)
        {
#pragma warning disable CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
            var options = new mux.ScrollerChangeOffsetsWithAdditionalVelocityOptions(
                new Vector2((float)dx, (float)dy),
                new Vector2(0.9f, 0.9f));
            Root.ChangeOffsetsWithAdditionalVelocity(options);
#pragma warning restore CS8305 // Scroller is for evaluation purposes only and is subject to change or removal in future updates.
        }
    }
}
