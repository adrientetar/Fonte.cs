/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

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

        void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                FinishEditing();

                e.Handled = true;
            }
        }

        void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (_anchor != null)
            {
                FinishEditing();
            }
        }
    }
}
