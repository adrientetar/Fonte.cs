// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Commands;
using Microsoft.UI.Xaml.Input;

using System.Windows.Input;


namespace Fonte.App.Utilities
{
    public static class Commands
    {
        public static ICommand AddAnchorCommand { get; } = MakeUICommand("Add Anchor", new AddAnchorCommand());
        public static ICommand AddComponentCommand { get; } = MakeUICommand("Add Component…", new AddComponentCommand());
        public static ICommand AddGuidelineCommand { get; } = MakeUICommand("Add Guideline", new AddGuidelineCommand());
        public static ICommand AlignSelectionCommand { get; } = MakeUICommand("Align Selection", new AlignSelectionCommand());
        public static ICommand DecomposeComponentCommand { get; } = MakeUICommand("Decompose", new DecomposeComponentCommand());
        public static ICommand MakeGuidelineGlobalCommand { get; } = MakeUICommand("Make Guideline Global", new MakeGuidelineGlobalCommand());
        public static ICommand MakeGuidelineLocalCommand { get; } = MakeUICommand("Make Guideline Local", new MakeGuidelineLocalCommand());
        public static ICommand ReverseAllPathsCommand { get; } = MakeUICommand("Reverse All Paths", new ReverseAllPathsCommand());
        public static ICommand ReversePathCommand { get; } = MakeUICommand("Reverse Path", new ReversePathCommand());
        public static ICommand SetStartPointCommand { get; } = MakeUICommand("Set As Start Point", new SetStartPointCommand());

        public static XamlUICommand MakeUICommand(string label, ICommand command, KeyboardAccelerator accelerator = null)
        {
            var uiCommand = new XamlUICommand()
            {
                Command = command,
                Label = label,
            };
            if (accelerator != null)
            {
                uiCommand.KeyboardAccelerators.Add(accelerator);
            }

            return uiCommand;
        }
    }
}
