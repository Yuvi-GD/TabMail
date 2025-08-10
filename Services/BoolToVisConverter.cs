using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace TabMail
{
    /// <summary>True -> Visible, False/Null -> Collapsed.</summary>
    public sealed class BoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isTrue = value is bool b && b;
            return isTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
