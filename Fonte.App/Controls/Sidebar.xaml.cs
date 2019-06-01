
namespace Fonte.App.Controls
{
    using Fonte.App.Utilities;
    using Fonte.Data.Geometry;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Windows.ApplicationModel;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    // TODO add a ViewModel
    public partial class Sidebar : UserControl
    {
        public static DependencyProperty LayerProperty = DependencyProperty.Register(
            "Layer", typeof(Data.Layer), typeof(Sidebar), null);

        public Data.Layer Layer
        {
            get => (Data.Layer)GetValue(LayerProperty);
            set { SetValue(LayerProperty, value); }
        }

        public static DependencyProperty XPositionProperty = DependencyProperty.Register(
            "XPosition", typeof(string), typeof(Sidebar), null);

        public string XPosition
        {
            get => (string)GetValue(XPositionProperty);
            set { SetValue(XPositionProperty, value); }
        }

        public static DependencyProperty YPositionProperty = DependencyProperty.Register(
            "YPosition", typeof(string), typeof(Sidebar), null);

        public string YPosition
        {
            get => (string)GetValue(YPositionProperty);
            set { SetValue(YPositionProperty, value); }
        }

        public static DependencyProperty XSizeProperty = DependencyProperty.Register(
            "XSize", typeof(string), typeof(Sidebar), null);

        public string XSize
        {
            get => (string)GetValue(XSizeProperty);
            set { SetValue(XSizeProperty, value); }
        }

        public static DependencyProperty YSizeProperty = DependencyProperty.Register(
            "YSize", typeof(string), typeof(Sidebar), null);

        public string YSize
        {
            get => (string)GetValue(YSizeProperty);
            set { SetValue(YSizeProperty, value); }
        }

        public Sidebar()
        {
            InitializeComponent();
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignMode.DesignMode2Enabled)
                return;

            ((App)Application.Current).DataRefreshing += OnDataRefreshing;

            Origin.SelectedIndexChanged += OnDataRefreshing;
        }

        void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (DesignMode.DesignMode2Enabled)
                return;

            Origin.SelectedIndexChanged -= OnDataRefreshing;

            ((App)Application.Current).DataRefreshing -= OnDataRefreshing;
        }

        void OnDataRefreshing()
        {
            if (Layer != null && !Layer.SelectionBounds.IsEmpty)
            {
                var origin = Origin.GetOrigin(Layer);
                XPosition = Math.Round(origin.X, 2).ToString();
                YPosition = Math.Round(origin.Y, 2).ToString();

                XSize = Math.Round(Layer.SelectionBounds.Width, 2).ToString();
                YSize = Math.Round(Layer.SelectionBounds.Height, 2).ToString();
            }
            else
            {
                XPosition = YPosition = XSize = YSize = string.Empty;
            }
        }

        /**/

        void OnAlignLeftButtonClick(object sender, RoutedEventArgs e)
        {
            AlignSelectedPaths((path, refBounds) => new Vector2(
                    path.Bounds.Left > refBounds.Left ? refBounds.Left - path.Bounds.Left : 0,
                    0
                ));
        }

        void OnCenterHorzButtonClick(object sender, RoutedEventArgs e)
        {
            AlignSelectedPaths((path, refBounds) => {
                    var refXMid = refBounds.Left + Math.Round(.5 * refBounds.Width);
                    var xMid = path.Bounds.Left + Math.Round(.5 * path.Bounds.Width);
                    return new Vector2(
                        (float)(refXMid - xMid),
                        0
                    );
                });
        }

        void OnAlignRightButtonClick(object sender, RoutedEventArgs e)
        {
            AlignSelectedPaths((path, refBounds) => new Vector2(
                    path.Bounds.Right < refBounds.Right ? refBounds.Right - path.Bounds.Right : 0,
                    0
                ));
        }

        void OnAlignTopButtonClick(object sender, RoutedEventArgs e)
        {
            AlignSelectedPaths((path, refBounds) => new Vector2(
                    0,
                    path.Bounds.Top < refBounds.Top ? refBounds.Top - path.Bounds.Top : 0
                ));
        }

        void OnCenterVertButtonClick(object sender, RoutedEventArgs e)
        {
            AlignSelectedPaths((path, refBounds) => {
                    var refYMid = refBounds.Bottom + Math.Round(.5 * refBounds.Height);
                    var yMid = path.Bounds.Bottom + Math.Round(.5 * path.Bounds.Height);
                    return new Vector2(
                        0,
                        (float)(refYMid - yMid)
                    );
                });
        }

        void OnAlignBottomButtonClick(object sender, RoutedEventArgs e)
        {
            AlignSelectedPaths((path, refBounds) => new Vector2(
                    0,
                    path.Bounds.Bottom > refBounds.Bottom ? refBounds.Bottom - path.Bounds.Bottom : 0
                ));
        }

        void OnExcludeButtonClick(object sender, RoutedEventArgs e)
        {
            BinaryBooleanOp((a, b) => BooleanOps.Exclude(a, b));
        }

        void OnIntersectButtonClick(object sender, RoutedEventArgs e)
        {
            BinaryBooleanOp((a, b) => BooleanOps.Intersect(a, b));
        }

        // TODO skip the no-intersections (same geometry after filtering) case
        void OnUnionButtonClick(object sender, RoutedEventArgs e)
        {
            if (Layer != null)
            {
                var useSelection = Enumerable.Any(Layer.Selection, item => item is Data.Point);

                var usePaths = new List<Data.Path>();
                var retainPaths = new List<Data.Path>();
                foreach (var path in Layer.Paths)
                {
                    if (path.IsOpen)
                    {
                        retainPaths.Add(path);
                    }
                    else
                    {
                        if (useSelection && !Enumerable.Any(path.Points, point => point.Selected))
                        {
                            retainPaths.Add(path);
                        }
                        else
                        {
                            usePaths.Add(path);
                        }
                    }
                }

                var resultPaths = BooleanOps.Union(usePaths);
                if (resultPaths.Count != usePaths.Count)
                {
                    using (var group = Layer.CreateUndoGroup())
                    {
                        Layer.Paths.Clear();
                        Layer.Paths.AddRange(resultPaths);
                        Layer.Paths.AddRange(retainPaths);

                        ((App)Application.Current).InvalidateData();
                    }
                }
            }
        }

        void OnXorButtonClick(object sender, RoutedEventArgs e)
        {
            BinaryBooleanOp((a, b) => BooleanOps.Xor(a, b));
        }

        void OnHorzMirrorButtonClick(object sender, RoutedEventArgs e)
        {
            var origin = Origin.GetOrigin(Layer);
            var matrix = Matrix3x2.CreateScale(-1, 1) * Matrix3x2.CreateTranslation(2 * origin.X, 0);
            Layer.Transform(matrix, Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();
        }

        void OnVertMirrorButtonClick(object sender, RoutedEventArgs e)
        {
            var origin = Origin.GetOrigin(Layer);
            var matrix = Matrix3x2.CreateScale(1, -1) * Matrix3x2.CreateTranslation(0, 2 * origin.Y);
            Layer.Transform(matrix, Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();
        }

        void OnXPositionChanged(object sender, LosingFocusEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (float.TryParse(textBox.Text, out float result))
            {
                var dx = Outline.RoundToGrid(result) - Origin.GetOrigin(Layer).X;
                Layer.Transform(Matrix3x2.CreateTranslation(dx, 0), selected: true);

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataRefreshing();
            }
        }

        void OnYPositionChanged(object sender, LosingFocusEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (float.TryParse(textBox.Text, out float result))
            {
                var dy = Outline.RoundToGrid(result) - Origin.GetOrigin(Layer).Y;
                Layer.Transform(Matrix3x2.CreateTranslation(0, dy), selected: true);

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataRefreshing();
            }
        }

        void OnXSizeChanged(object sender, LosingFocusEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (Layer.SelectionBounds.Width > 0 && float.TryParse(textBox.Text, out float result))
            {
                var wr = Outline.RoundToGrid(result) / Layer.SelectionBounds.Width;
                Layer.Transform(Matrix3x2.CreateScale(wr, 1, Origin.GetOrigin(Layer)),
                                selected: true);

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataRefreshing();
            }
        }

        void OnYSizeChanged(object sender, LosingFocusEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (Layer.SelectionBounds.Height > 0 && float.TryParse(textBox.Text, out float result))
            {
                var hr = Outline.RoundToGrid(result) / Layer.SelectionBounds.Height;
                Layer.Transform(Matrix3x2.CreateScale(1, hr, Origin.GetOrigin(Layer)),
                                selected: true);

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataRefreshing();
            }
        }

        void OnRotationButtonClick(object sender, RoutedEventArgs e)
        {
            var sign = 1f;  // sender.Tag == "!" ? -1f : 1f;
            var result = float.Parse(RotationTextBox.Text);
            var rad = result * (float)Math.PI / 180 * sign;

            Layer.Transform(Matrix3x2.CreateRotation(rad, Origin.GetOrigin(Layer)),
                            selected: Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();

            // TODO: else restore oldValue
            // -- actually the value should be validated/restored on textbox input, not here
        }

        void OnScaleButtonClick(object sender, RoutedEventArgs e)
        {
            var sign = 1f;  // sender.Tag == "!" ? -1f : 1f;
            var xScale = 1f / (1 - sign * .01f * float.Parse(XScaleTextBox.Text));
            var yScale = YScaleTextBox.IsEnabled ?
                         1f / (1 - sign * .01f * float.Parse(YScaleTextBox.Text)) :
                         xScale;

            Layer.Transform(Matrix3x2.CreateScale(xScale, yScale, Origin.GetOrigin(Layer)),
                            selected: Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();
        }

        void OnSkewButtonClick(object sender, RoutedEventArgs e)
        {
            var sign = 1f;  // sender.Tag == "!" ? -1f : 1f;

            var result = float.Parse(SkewTextBox.Text);
            var rad = result * (float)Math.PI / 180 * sign;
            Layer.Transform(Matrix3x2.CreateSkew(rad, 0, Origin.GetOrigin(Layer)),
                            selected: Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();
        }

        /**/

        void AlignSelectedPaths(Func<Data.Path, Rect, Vector2> transformFunc)
        {
            var selectedBounds = new Rect();
            var selectedPaths = new List<Data.Path>();
            foreach (var path in Layer.Paths)
            {
                if (Enumerable.Any(path.Points, point => point.Selected))
                {
                    selectedBounds.Union(path.Bounds);
                    selectedPaths.Add(path);
                }
            }

            if (selectedPaths.Count > 0)
            {
                foreach (var path in selectedPaths)
                {
                    var delta = transformFunc(path, selectedBounds);

                    if (delta.Length() != 0)
                    {
                        path.Transform(Matrix3x2.CreateTranslation(delta));

                        ((App)Application.Current).InvalidateData();
                    }
                }
            }
        }

        void BinaryBooleanOp(Func<IEnumerable<Data.Path>, IEnumerable<Data.Path>, List<Data.Path>> booleanFunc)
        {
            if (Layer != null && Layer.Paths.Count >= 2)
            {
                var useSelection = Enumerable.Any(Layer.Selection, item => item is Data.Point);

                var usePaths = new List<Data.Path>();
                var retainPaths = new List<Data.Path>();
                Data.Path refPath = null;
                foreach (var path in Layer.Paths)
                {
                    if (path.IsOpen)
                    {
                        retainPaths.Add(path);
                    }
                    //else
                    //{
                    if (refPath == null && Enumerable.Any(path.Points, point => point.Selected))
                    {
                        refPath = path;
                    }
                    else
                    {
                        usePaths.Add(path);
                    }
                    //}
                }
                // TODO: consider dropping this behavior, more confusing than useful
                if (refPath == null)
                {
                    refPath = usePaths.Last();
                    usePaths.RemoveAt(usePaths.Count - 1);
                }

                var resultPaths = booleanFunc(usePaths, new List<Data.Path>() { refPath });
                if (resultPaths.Count != usePaths.Count + 1)
                {
                    using (var group = Layer.CreateUndoGroup())
                    {
                        Layer.Paths.Clear();
                        Layer.Paths.AddRange(resultPaths);
                        Layer.Paths.AddRange(retainPaths);

                        ((App)Application.Current).InvalidateData();
                    }
                }
            }
        }
    }
}
