/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using System;
    using System.Linq;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.Storage;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;
    using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

    sealed partial class App : Application
    {
        public event EventHandler DataChanged;

        public App()
        {
            InitializeComponent();

            Suspending += OnSuspending;
            UnhandledException += OnUnhandledException;
        }

        public void InvalidateData()
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);

            if (args.Files.Count == 1)
            {
                Launch(args.Files.First());
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Launch(args.Arguments,
                   loadApplicationState: args.PreviousExecutionState == ApplicationExecutionState.Terminated,
                   prelaunchActivated: args.PrelaunchActivated);
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

        async void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // TODO: pop a dialog, ask to do this
            if (Window.Current.Content is Frame frame && frame?.Content is CanvasPage page)
            {
                await page.SaveFontAsync();
            }
        }

        void Launch(object parameter, bool loadApplicationState = false, bool prelaunchActivated = false)
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (loadApplicationState)
                {
                    // TODO: load application state
                }

                Window.Current.Content = rootFrame;
            }

            if (!prelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(CanvasPage), parameter as string);

                    if (parameter is StorageFile file)
                    {
                        ((CanvasPage)rootFrame.Content).File = file;
                    }
                }

                Window.Current.Activate();
            }
        }
    }
}
