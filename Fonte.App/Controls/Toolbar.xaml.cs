/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls
{
    using Fonte.App.Controls.ToolbarParts;
    using Fonte.App.Delegates;
    using Fonte.App.Interfaces;

    using System;
    using System.Collections.ObjectModel;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class Toolbar : UserControl
    {
        private int _selectedIndex;

        public event EventHandler CurrentItemChanged;

        public ObservableCollection<IToolbarItem> Items
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
                CheckActiveButton();
                CurrentItemChanged?.Invoke(this, new EventArgs());
            }
        }

        public IToolbarItem SelectedItem => Items[SelectedIndex];

        public Toolbar()
        {
            InitializeComponent();

            Items = new ObservableCollection<IToolbarItem>()
            {
                new SelectionTool(),
                new PenTool(),
                new ShapesTool(),
            };
        }

        void OnControlLoaded(object sender, RoutedEventArgs args)
        {
            SelectedIndex = 0;
        }

        void OnButtonClicked(object sender, RoutedEventArgs args)
        {
            SelectedIndex = Repeater.GetElementIndex((UIElement)sender);
        }

        /**/

        void CheckActiveButton()
        {
            for (int index = 0; index < Items.Count; ++index)
            {
                if (Repeater.TryGetElement(index) is IconButton button)
                {
                    button.IsChecked = index == _selectedIndex;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
