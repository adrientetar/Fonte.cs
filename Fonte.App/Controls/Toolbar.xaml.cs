// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Controls.ToolbarParts;
using Fonte.App.Delegates;
using Fonte.App.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.ObjectModel;
using Windows.Foundation;


namespace Fonte.App.Controls
{
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
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid index '{value}'.");
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
