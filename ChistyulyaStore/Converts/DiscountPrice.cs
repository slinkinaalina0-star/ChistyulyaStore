using System;
using System.Globalization;
using System.Windows.Data;

namespace ChistyulyaStore.Converts
{
    public class DiscountPrice : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is decimal price && values[1] is int discount)
            {
                decimal finalPrice = price - (price * discount / 100);
                return finalPrice;
            }
            return 0m;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public decimal Calculate(decimal price, int discount)
        {
            return price - (price * discount / 100);
        }
    }
}