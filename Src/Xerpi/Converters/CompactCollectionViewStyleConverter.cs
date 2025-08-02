using System;
using System.Globalization;
using Xamarin.Forms;

namespace Xerpi.Converters
{
    public class CompactCollectionViewStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool compact = value is bool b && b;
            if (compact)
            {
                // Return a style with smaller item size and spacing
                var style = new Style(typeof(CollectionView))
                {
                    Setters =
                    {
                        new Setter { Property = CollectionView.ItemsLayoutProperty, Value = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 2, VerticalItemSpacing = 2 } },
                        new Setter { Property = CollectionView.MarginProperty, Value = new Thickness(0,2,0,2) },
                        new Setter { Property = CollectionView.ItemTemplateProperty, Value = null } // Let DataTemplate handle compact visuals
                    }
                };
                return style;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}