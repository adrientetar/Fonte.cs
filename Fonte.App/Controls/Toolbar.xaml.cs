// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Controls
{
    using Fonte.App.Controls.ToolbarParts;
    using Fonte.App.Delegates;
    using Fonte.App.Interfaces;

    using System;
    using System.Collections.ObjectModel;
    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class Toolbar : UserControl
    {
        private int _selectedIndex;

        public event TypedEventHandler<Toolbar, EventArgs> CurrentItemChanged;

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
                    throw new ArgumentException($"Invalid index {value} given items count {Items.Count}");
                }
                _selectedIndex = value;

                CheckActiveButton();
                CurrentItemChanged?.Invoke(this, EventArgs.Empty);
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
                new KnifeTool(),
                new RulerTool(),
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
