/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using System;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    sealed partial class App : Application
    {
        public event Action DataRefreshing;

        public App()
        {
            InitializeComponent();

            Suspending += OnSuspending;
        }

        public void InvalidateData()
        {
            DataRefreshing?.Invoke();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: load application state
                }

                Window.Current.Content = rootFrame;
            }

            if (args.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(CanvasPage), args.Arguments);
                }

                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs args)
        {
            throw new Exception($"Failed to load Page {args.SourcePageType.FullName}");
        }

        void OnSuspending(object sender, SuspendingEventArgs args)
        {
            var deferral = args.SuspendingOperation.GetDeferral();
            // TODO: save application state
            deferral.Complete();
        }
    }
}
