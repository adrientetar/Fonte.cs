/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

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
