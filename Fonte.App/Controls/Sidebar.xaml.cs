
namespace Fonte.App.Controls
{
    using Fonte.App.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    // TODO add a ViewModel
    public partial class Sidebar : UserControl
    {
        public static DependencyProperty LayerProperty = DependencyProperty.Register("Layer", typeof(Data.Layer), typeof(Sidebar), null);

        public Data.Layer Layer { get; set; }

        public Sidebar()
        {
            InitializeComponent();
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).DataRefreshing += OnDataRefreshing;
        }

        void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).DataRefreshing -= OnDataRefreshing;
        }

        void OnDataRefreshing()
        {
            XPositionTextBox.Text = YPositionTextBox.Text = XSizeTextBox.Text = YSizeTextBox.Text = string.Empty;

            if (Layer != null && !Layer.SelectionBounds.IsEmpty)
            {
                XPositionTextBox.Text = Math.Round(Layer.SelectionBounds.Left).ToString();
                YPositionTextBox.Text = Math.Round(Layer.SelectionBounds.Top).ToString();

                XSizeTextBox.Text = Math.Round(Layer.SelectionBounds.Width).ToString();
                YSizeTextBox.Text = Math.Round(Layer.SelectionBounds.Height).ToString();
            }
        }

        /**/

        void OnAlignLeftButtonClick(object sender, RoutedEventArgs e)
        {
            _alignSelectedPaths((path, refBounds) => new Vector2(
                    path.Bounds.Left > refBounds.Left ? (float)(refBounds.Left - path.Bounds.Left) : 0,
                    0
                ));
        }

        void OnCenterHorzButtonClick(object sender, RoutedEventArgs e)
        {
            _alignSelectedPaths((path, refBounds) => {
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
            _alignSelectedPaths((path, refBounds) => new Vector2(
                    path.Bounds.Right < refBounds.Right ? (float)(refBounds.Right - path.Bounds.Right) : 0,
                    0
                ));
        }

        void OnAlignTopButtonClick(object sender, RoutedEventArgs e)
        {
            _alignSelectedPaths((path, refBounds) => new Vector2(
                    0,
                    path.Bounds.Bottom < refBounds.Bottom ? (float)(refBounds.Bottom - path.Bounds.Bottom) : 0
                ));
        }

        void OnCenterVertButtonClick(object sender, RoutedEventArgs e)
        {
            _alignSelectedPaths((path, refBounds) => {
                    var refYMid = refBounds.Top + Math.Round(.5 * refBounds.Height);
                    var yMid = path.Bounds.Top + Math.Round(.5 * path.Bounds.Height);
                    return new Vector2(
                        0,
                        (float)(refYMid - yMid)
                    );
                });
        }

        void OnAlignBottomButtonClick(object sender, RoutedEventArgs e)
        {
            _alignSelectedPaths((path, refBounds) => new Vector2(
                    0,
                    path.Bounds.Top > refBounds.Top ? (float)(refBounds.Top - path.Bounds.Top) : 0
                ));
        }

        void OnExcludeButtonClick(object sender, RoutedEventArgs e)
        {
            _binaryBooleanOp((a, b) => BooleanOps.Exclude(a, b));
        }

        void OnIntersectButtonClick(object sender, RoutedEventArgs e)
        {
            _binaryBooleanOp((a, b) => BooleanOps.Intersect(a, b));
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
                using (var group = Layer.CreateUndoGroup())
                {
                    Layer.Paths.Clear();
                    Layer.Paths.AddRange(resultPaths);
                    Layer.Paths.AddRange(retainPaths);

                    ((App)Application.Current).InvalidateData();
                }
            }
        }

        void OnXorButtonClick(object sender, RoutedEventArgs e)
        {
            _binaryBooleanOp((a, b) => BooleanOps.Xor(a, b));
        }

        private void _alignSelectedPaths(Func<Data.Path, Rect, Vector2> transformFunc)
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

        private void _binaryBooleanOp(Func<IEnumerable<Data.Path>, IEnumerable<Data.Path>, List<Data.Path>> booleanFunc)
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
                if (refPath == null)
                {
                    refPath = usePaths.Last();
                    usePaths.RemoveAt(usePaths.Count - 1);
                }

                var resultPaths = booleanFunc(usePaths, new List<Data.Path>() { refPath });
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
