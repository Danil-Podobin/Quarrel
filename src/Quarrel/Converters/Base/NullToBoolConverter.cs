﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Quarrel.Converters.Base
{
    /// <summary>
    /// A converter that returns an <see cref="Visibility.Visible"/> value if the input <see langword="object"/> is <see langword="null"/>
    /// </summary>
    public sealed class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool v;
            if (value is string sValue)
            {
                v = string.IsNullOrEmpty(sValue);
            }
            else
            {
                v = value == null;
            }
            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
