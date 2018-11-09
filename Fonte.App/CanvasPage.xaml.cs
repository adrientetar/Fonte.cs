/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using Fonte.App.Interfaces;

    using System;
    using Windows.UI.Xaml.Controls;

    public sealed partial class CanvasPage : Page
    {
        public CanvasPage()
        {
            InitializeComponent();
        }

        void OnToolBarItemChanged(object sender, EventArgs e)
        {
            Canvas.Tool = (ICanvasDelegate)ToolBar.SelectedItem;
        }
    }
}
