﻿/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Core;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class TitleBar : UserControl
    {
        public static DependencyProperty UserTitleProperty = DependencyProperty.Register(
           "UserTitle", typeof(string), typeof(TitleBar), new PropertyMetadata(null, OnUserTitleChanged));

        public string UserTitle
        {
            get => (string)GetValue(UserTitleProperty);
            set { SetValue(UserTitleProperty, value); }
        }

        public static DependencyProperty TitleProperty = DependencyProperty.Register(
           "Title", typeof(string), typeof(TitleBar), null);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set { SetValue(TitleProperty, value); }
        }

        public TitleBar()
        {
            InitializeComponent();

            OnUserTitleChanged(null);
        }

        void OnControlLoaded(object sender, RoutedEventArgs args)
        {
            if (!DesignMode.DesignModeEnabled)
            {
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;
                UpdateTitleBarLayout(coreTitleBar);

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                coreTitleBar.IsVisibleChanged += OnTitleBarIsVisibleChanged;
                coreTitleBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;
            }

            Window.Current.Activated += OnCurrentWindowActivated;
        }

        void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            Window.Current.Activated -= OnCurrentWindowActivated;
        }

        void OnCurrentWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != CoreWindowActivationState.Deactivated)
            {
                AppTitleBar.Opacity = 1;
            }
            else
            {
                AppTitleBar.Opacity = 0.5;
            }
        }

        void OnTitleBarIsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (sender.IsVisible)
            {
                AppTitleBar.Visibility = Visibility.Visible;
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }
        }

        void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdateTitleBarLayout(sender);
        }

        void UpdateTitleBarLayout(CoreApplicationViewTitleBar coreTitleBar)
        {
            // Get the size of the caption controls area and back button
            // (returned in logical pixels), and move your content around as necessary.
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);

            // Update title bar control size as needed to account for system size changes.
            AppTitleBar.Height = coreTitleBar.Height;

            // Adjust title bar margin for current DPI
            var topMargin = (int)(.5 * (coreTitleBar.Height - AppTitle.ActualHeight));

            var margin = AppTitle.Margin;
            if (topMargin != margin.Top)
            {
                margin.Top = topMargin;
                AppTitle.Margin = margin;
            }
        }

        /**/

        void OnUserTitleChanged(string userTitle)
        {
            if (string.IsNullOrEmpty(userTitle))
            {
                Title = Package.Current.DisplayName;
            }
            else
            {
                Title = $"{userTitle} – {Package.Current.DisplayName}";
            }
        }

        static void OnUserTitleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (args.OldValue != args.NewValue)
            {
                ((TitleBar)sender).OnUserTitleChanged((string)args.NewValue);
            }
        }
    }
}
