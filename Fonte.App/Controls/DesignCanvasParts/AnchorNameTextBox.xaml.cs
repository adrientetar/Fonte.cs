// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Controls.DesignCanvasParts
{
    using System;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public partial class AnchorNameTextBox : TextBox
    {
        private Data.Anchor _anchor;

        public AnchorNameTextBox()
        {
            InitializeComponent();
        }

        public void StartEditing(Data.Anchor anchor, Point pos)
        {
            _anchor = anchor;

            Text = _anchor.Name;
            Translation = new Vector3(pos.ToVector2(), 0);
            Visibility = Visibility.Visible;
            Focus(FocusState.Programmatic);
            SelectAll();
        }

        void FinishEditing()
        {
            if (_anchor == null)
                throw new InvalidOperationException("anchor is null");

            _anchor.Name = Text;
            ((App)Application.Current).InvalidateData();

            Translation = default;
            Visibility = Visibility.Collapsed;
            _anchor = null;
        }

        void OnKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Enter)
            {
                FinishEditing();

                args.Handled = true;
            }
        }

        void OnLostFocus(object sender, RoutedEventArgs args)
        {
            if (_anchor != null)
            {
                FinishEditing();
            }
        }
    }
}
