// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Converters
{
    using System;
    using Windows.UI.Xaml.Data;

    public class IndexToActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return int.Parse((string)parameter) == (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
