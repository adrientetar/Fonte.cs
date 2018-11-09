/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Fonte.App.Delegates;
    using Fonte.App.Interfaces;

    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public partial class ToolBar : UserControl
    {
        private int _selectedIndex;

        public event EventHandler CurrentItemChanged;

        public ObservableCollection<IToolBarItem> Items
        { get; }

        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                if (value < 0 || value >= Items.Count)
                {
                    throw new ArgumentException(string.Format(
                        "Invalid index {0} given items count {1}", value, Items.Count));
                }
                _selectedIndex = value;
                _checkActiveButton();
                CurrentItemChanged?.Invoke(this, new EventArgs());
            }
        }

        public IToolBarItem SelectedItem => Items[SelectedIndex];

        public ToolBar()
        {
            InitializeComponent();

            Items = new ObservableCollection<IToolBarItem>()
            {
                new SelectionTool(),
                new ShapesTool(),
            };
        }

        void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            SelectedIndex = 0;
        }

        void OnButtonChecked(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent((UIElement)sender);
            SelectedIndex = ItemsControl.IndexFromContainer(parent);
        }

        private void _checkActiveButton()
        {
            for (int index = 0; index < Items.Count; ++index)
            {
                var presenter = ItemsControl.ContainerFromIndex(index);
                var button = (AppBarToggleButton)VisualTreeHelper.GetChild(presenter, 0);
                button.IsChecked = index == _selectedIndex;
            }
        }
    }
}
