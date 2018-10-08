/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Fonte.App.Delegates;
    using Fonte.App.Utilities;
    using Microsoft.Graphics.Canvas.UI.Xaml;
    using Newtonsoft.Json;

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.Devices.Input;
    using Windows.Foundation;
    using Windows.UI.Composition;
    using Windows.UI.Composition.Interactions;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Hosting;
    using Windows.UI.Xaml.Input;

    public sealed partial class DesignCanvas : UserControl, IInteractionTrackerOwner
    {
        private const string _layerContents = @"{""masterName"":""Regular"",""width"":520,""paths"":[[[65,388],[78,331],[124,331,""curve"",true],[152,331],[177,348],[177,376,""curve"",true],[177,405],[167,429],[157,448,""curve""],[180,471],[212,481],[239,481,""curve"",true],[307,481],[335,434],[335,356,""curve"",true],[335,286,""line""],[250,265,""line"",true],[93,229],[45,189],[45,110,""curve"",true],[45,46],[97,-6],[187,-6,""curve"",true],[253,-6],[310,34],[335,101,""curve""],[335,73,""line"",true],[335,29],[354,-2],[396,-5,""curve"",true],[453,-9],[484,25],[501,64,""curve""],[487,73,""line""],[475,48],[461,38],[446,38,""curve"",true],[427,38],[421,63],[421,95,""curve"",true],[421,351,""line"",true],[421,472],[358,510],[265,510,""curve"",true],[197,510],[128,476],[89,420,""curve"",true]],[[310,57],[259,35],[214,35,""curve"",true],[169,35],[133,68],[133,126,""curve"",true],[133,167],[157,218],[257,243,""curve"",true],[335,263,""line""],[335,131,""line""]]]}";

        private Visual _contentVisual;
        private Matrix3x2 _matrix;
        private Visual _rootVisual;
        private float? _scale;
        private VisualInteractionSource _source;
        private InteractionTracker _tracker;

        public Data.Layer Layer { get; set; }

        public ICanvasDelegate Tool { get; set; }

        public DesignCanvas()
        {
            InitializeComponent();

            // later we'll only do this if we're in design mode
            Layer = JsonConvert.DeserializeObject<Data.Layer>(_layerContents);

            Tool = new SelectionTool();//BaseTool();
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            _matrix = Matrix3x2.CreateScale(1, -1);
            // should be based on metrics, not bounds
            _matrix.Translation = new Vector2(
                .5f * ((float)Root.ActualWidth - Layer.Width),
                .5f * (float)(Root.ActualHeight + Layer.Bounds.Height)
            );

            InitializeInteractionTracker();
        }

        void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            _tracker.Dispose();
            _tracker = null;

            Canvas.RemoveFromVisualTree();
            Canvas = null;
        }

        void InitializeInteractionTracker()
        {
            _rootVisual = ElementCompositionPreview.GetElementVisual(Root);
            _rootVisual.Size = new Vector2((float)Root.ActualWidth, (float)Root.ActualHeight);

            _contentVisual = ElementCompositionPreview.GetElementVisual(Canvas);
            _contentVisual.Size = _rootVisual.Size;

            SetupInteractionTracker(_rootVisual, _contentVisual);
        }

        void SetupInteractionTracker(Visual rootVisual, Visual contentVisual)
        {
            //
            // Create the InteractionTracker and set its min/max position and scale.  These could 
            // also be bound to expressions.  Note: The scrollable area can be changed from either 
            // the min or the max position to facilitate content updates/virtualization.
            //

            var compositor = rootVisual.Compositor;

            _tracker = InteractionTracker.CreateWithOwner(compositor, this);

            _tracker.MaxPosition = new Vector3(rootVisual.Size, 0.0f) * 300.0f;
            _tracker.MinPosition = _tracker.MaxPosition * -1.0f;
            _tracker.MinScale = 1e-2f;
            _tracker.MaxScale = 1e3f;

            _source = VisualInteractionSource.Create(rootVisual);

            _source.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;
            _source.PositionYSourceMode = InteractionSourceMode.EnabledWithoutInertia;
            _source.IsPositionXRailsEnabled = false;
            _source.IsPositionYRailsEnabled = false;
            _source.ScaleSourceMode = InteractionSourceMode.EnabledWithoutInertia;
            // TODO: custom PointerWheel code
            _source.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadAndPointerWheel;

            _tracker.InteractionSources.Add(_source);

            var positionExpression = compositor.CreateExpressionAnimation("-tracker.Position");
            positionExpression.SetReferenceParameter("tracker", _tracker);

            contentVisual.StartAnimation("Offset", positionExpression);

            var scaleExpression = compositor.CreateExpressionAnimation("Vector3(tracker.Scale, tracker.Scale, 1.0)");
            scaleExpression.SetReferenceParameter("tracker", _tracker);

            contentVisual.StartAnimation("Scale", scaleExpression);
        }

        void OnRegionsInvalidated(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
        {
            Debug.WriteLine("START DRAWING!");
            foreach (var region in args.InvalidatedRegions)
            {
                using (var ds = sender.CreateDrawingSession(args.VisibleRegion))
                {
                    ds.Transform = _matrix;

                    var rescale = 1 / sender.DpiScale;
                    Drawing.DrawPoints(Layer, ds, rescale);
                    Drawing.DrawStroke(Layer, ds, rescale);

                    Tool.OnDraw(this, ds);
                }
            }
            Debug.WriteLine("STOP DRAWING!");
        }

        void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                _source.TryRedirectForManipulation(e.GetCurrentPoint(Root));
            }
            
            if (Tool.HandleEvent(this, e))
            {
                Tool.OnPointerPressed(this, e);
            }
        }

        void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (Tool.HandleEvent(this, e))
            {
                Tool.OnPointerMoved(this, e);
            }
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (Tool.HandleEvent(this, e))
            {
                Tool.OnPointerReleased(this, e);
            }
        }

        void OnRootSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_rootVisual == null || _contentVisual == null) return;

            var rootSize = new Vector2((float)Root.ActualWidth, (float)Root.ActualHeight);
            _rootVisual.Size = _contentVisual.Size = rootSize;
        }

        #region IInteractionTrackerOwner Implementation

        public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
        {
        }

        public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
        {
#if true
            Debug.WriteLine("ValuesChanged! " + args.Scale);
            var ratio = Canvas.DpiScale / args.Scale;

            if (ratio <= 0.8 || ratio >= 1.25)
            {
                Debug.WriteLine("APPLIED!");
                Canvas.DpiScale = args.Scale;
            }
            else
            {
                _scale = args.Scale;
            }
#else
            if (args.Scale != Canvas.DpiScale)
            {
                Canvas.DpiScale = args.Scale;
            }
#endif
        }

        public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
        {
        }

        public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
        {
            Debug.WriteLine("IdleStateEntered! " + _scale);
            if (_scale.HasValue)
            {
                Debug.WriteLine("APPLIED!");
                Canvas.DpiScale = _scale.Value;
                _scale = null;
            }
        }

        public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
        {
        }

        public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
        {
        }

#endregion

        Matrix3x2 GetInverseMatrix()
        {
            var m1 = Matrix3x2.CreateScale(1, -1);
            m1.Translation += _matrix.Translation;
            var m2 = Matrix3x2.CreateScale(_tracker.Scale);
            m2.Translation -= new Vector2(_tracker.Position.X, _tracker.Position.Y);
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

        public void ScrollBy(double dx, double dy)
        {
            _tracker.TryUpdatePositionBy(new Vector3(
                    -(float)dx,
                    -(float)dy,
                    0
                ));
            Canvas.Invalidate();
        }
    }
}
