using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ChistyulyaStore.Models;

namespace ChistyulyaStore.Converts
{
    public class DiscountColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Отладка: выводим тип полученного объекта
            Debug.WriteLine($"Тип value: {value?.GetType()}");
            Debug.WriteLine($"Значение value: {value}");

            // Проверяем, что передан объект Products
            if (value is Products product)
            {
                Debug.WriteLine($"StockQuantity: {product.StockQuantity}, Discount: {product.Discount}");

                // Если товара нет на складе — светло-серый фон
                if (product.StockQuantity == 0)
                {
                    Debug.WriteLine("Возвращаем LightGray (товара нет на складе)");
                    return new SolidColorBrush(Colors.LightGray);
                }

                // Если скидка больше 15% — коралловый фон (#FF7F50)
                if (product.Discount > 15)
                {
                    Debug.WriteLine("Возвращаем OrangeRed (скидка > 15%)");
                    return new SolidColorBrush(Colors.OrangeRed);
                }
                else
                {
                    Debug.WriteLine($"Скидка {product.Discount} не больше 15, возвращаем Transparent");
                }
            }
            else
            {
                Debug.WriteLine("value НЕ является Products");
            }

            // Во всех остальных случаях — прозрачный фон
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}