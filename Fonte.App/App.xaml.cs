// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.UI;
using Microsoft.UI.Xaml;

using System;


namespace Fonte.App
{
    public partial class App : Application
    {
        public event EventHandler DataChanged;

        private Window _window;

        public App()
        {
            InitializeComponent();

            //AppCenter.Start(Constants.AppId, typeof(Crashes));
        }

        public void InvalidateData()
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
