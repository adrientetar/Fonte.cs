// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Utilities
{
    using System.Linq;
    using System.Windows.Input;
    using Windows.UI.Xaml.Controls;

    public class MenuFlyoutBuilder
    {
        public MenuFlyout MenuFlyout { get; }

        public MenuFlyoutBuilder()
        {
            MenuFlyout = new MenuFlyout()
            {
                AreOpenCloseAnimationsEnabled = false
            };
        }

        public bool TryAddItem(ICommand command, object commandParameter = null)
        {
            if (command.CanExecute(commandParameter))
            {
                MenuFlyout.Items.Add(new MenuFlyoutItem()
                {
                    Command = command,
                    CommandParameter = commandParameter
                });
                return true;
            }
            return false;
        }

        public bool TryAddSeparator()
        {
            var items = MenuFlyout.Items;

            if (items.LastOrDefault() is MenuFlyoutItem)
            {
                items.Add(new MenuFlyoutSeparator());
                return true;
            }
            return false;
        }
    }
}