using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChistyulyaStore.Models;
using ChistyulyaStore.Windows;

namespace ChistyulyaStore.Pages
{
    public partial class OrdersPage : Page
    {
        DBEntities db = new DBEntities();
        private int currentUserId;

        public OrdersPage(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            TestDatabaseConnection();
            LoadOrders();
            UpdateCount();
            
        }

        public OrdersPage(int userId, string role)
        {
            InitializeComponent();
            currentUserId = userId;
            LoadOrders();
            UpdateCount();
        }

        private void LoadOrders()
        {
            var orders = db.Orders.ToList();

            var orderViewModels = orders
                .Select(o => new
                {
                    o.ID_Order,
                    OrderDate = o.OrderDate.ToString("dd.MM.yyyy"),
                    ClientFIO = GetClientFIO(o.ID_User),
                    TotalSum = CalculateOrderSum(o.ID_Order),
                    StatusName = GetStatusName(o.ID_Status),
                    PickUpCode = o.PickUpCode.ToString(),
                    RowColor = GetOrderRowColor(o.ID_Order)
                })
                .Where(x => x.TotalSum > 0)  // ← Убираем заказы с суммой 0
                .ToList();

            ordersList.ItemsSource = orderViewModels;
            UpdateCount();
        }

        // Новый метод для определения цвета строки
        private Brush GetOrderRowColor(int orderId)
        {
            var orderItems = db.OrderItems.Where(oi => oi.ID_Order == orderId).ToList();

            if (orderItems.Count == 0)
                return Brushes.White;

            // Проверяем, есть ли товар с остатком 0
            foreach (var item in orderItems)
            {
                var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                if (product != null && product.StockQuantity == 0)
                {
                    return (Brush)new BrushConverter().ConvertFrom("#FF8C00"); // оранжевый
                }
            }

            // Проверяем, все ли товары имеют остаток > 3
            bool allMoreThan3 = true;
            foreach (var item in orderItems)
            {
                var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                if (product != null && product.StockQuantity <= 3)
                {
                    allMoreThan3 = false;
                    break;
                }
            }

            if (allMoreThan3 && orderItems.Count > 0)
                return (Brush)new BrushConverter().ConvertFrom("#20B2AA"); // морской

            return Brushes.White;
        }

        private string GetClientFIO(int? userId)
        {
            if (userId == null) return "Неизвестный";
            var user = db.Users.FirstOrDefault(u => u.ID_User == userId);
            return user != null ? $"{user.LastName} {user.FirstName} {user.MiddleName}" : "Неизвестный";
        }

        private string GetStatusName(int? statusId)
        {
            if (statusId == 1) return "Завершен";
            if (statusId == 2) return "Новый";
            return "Неизвестный";
        }

        private decimal CalculateOrderSum(int orderId)
        {
            decimal total = 0;

            // 1. Получаем все позиции заказа из OrderItems
            var orderItems = db.OrderItems.Where(oi => oi.ID_Order == orderId).ToList();

            if (orderItems.Count == 0)
                return 0;

            // 2. Для каждой позиции находим товар и считаем сумму
            foreach (var item in orderItems)
            {
                // Находим товар по ID_Product
                var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);

                if (product != null)
                {
                    decimal price = product.Price;           // цена
                    int quantity = item.Quantity;            // количество
                    int discount = product.Discount;         // скидка

                    // Цена со скидкой
                    decimal priceWithDiscount = price - (price * discount / 100);

                    // Сумма по позиции
                    decimal itemTotal = priceWithDiscount * quantity;

                    total += itemTotal;
                }
            }

            return total;
        }

        private void UpdateCount()
        {
            int total = db.Orders.Count();
            int current = ordersList.ItemsSource != null ? ordersList.Items.Count : 0;
            countText.Text = $"{current} из {total}";
        }

        private void sortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = ordersList.ItemsSource?.Cast<dynamic>().ToList();
            if (items == null) return;

            if (sortBox.SelectedIndex == 0)
                ordersList.ItemsSource = items.OrderBy(x => x.TotalSum).ToList();
            else
                ordersList.ItemsSource = items.OrderByDescending(x => x.TotalSum).ToList();
        }

        private void ordersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ordersList.SelectedItem != null)
            {
                dynamic selected = ordersList.SelectedItem;
                int orderId = selected.ID_Order;
                var orderDetailsWindow = new OrderDetailsWindow(orderId);
                orderDetailsWindow.Owner = Window.GetWindow(this);
                orderDetailsWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"Выбран заказ: {ordersList.SelectedItem}");
                LoadOrders(); // Обновляем после закрытия
            }
        }

        private void updateStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (ordersList.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selected = ordersList.SelectedItem;
            int orderId = selected.ID_Order;

            ComboBoxItem selectedStatus = statusBox.SelectedItem as ComboBoxItem;
            if (selectedStatus == null)
            {
                MessageBox.Show("Выберите статус", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Исправлено: конвертируем Tag в int правильно
            int newStatusId = Convert.ToInt32(selectedStatus.Tag);

            var order = db.Orders.FirstOrDefault(o => o.ID_Order == orderId);
            if (order != null)
            {
                order.ID_Status = newStatusId;
                db.SaveChanges();
                LoadOrders();

                // Исправлено: order.Id → order.ID_Order
                MessageBox.Show($"Статус заказа {order.ID_Order} обновлён", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void CheckDatabaseDirectly()
        {
            // 1. Проверяем общее количество записей в OrderItems
            int totalItems = db.OrderItems.Count();
            MessageBox.Show($"Всего записей в OrderItems: {totalItems}");

            // 2. Проверяем, какие ID_Order есть в OrderItems
            var orderIdsWithItems = db.OrderItems.Select(oi => oi.ID_Order).Distinct().ToList();
            string ids = string.Join(", ", orderIdsWithItems);
            MessageBox.Show($"Заказы, у которых есть товары:\n{ids}");

            // 3. Проверяем конкретно заказ 21
            var order21Items = db.OrderItems.Where(oi => oi.ID_Order == 21).ToList();
            MessageBox.Show($"Заказ 21: найдено {order21Items.Count} позиций");

            if (order21Items.Count > 0)
            {
                string itemsInfo = "";
                foreach (var item in order21Items)
                {
                    var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                    itemsInfo += $"ID_Product={item.ID_Product}, Quantity={item.Quantity}\n";
                    if (product != null)
                    {
                        itemsInfo += $"  Товар: {product.ProductName}, Цена: {product.Price}\n\n";
                    }
                }
                MessageBox.Show($"Заказ 21:\n{itemsInfo}");
            }
        }

        private void TestDatabaseConnection()
        {
            // 1. Проверяем общее количество записей в OrderItems
            int totalItems = db.OrderItems.Count();
            MessageBox.Show($"Всего записей в OrderItems: {totalItems}");

            // 2. Проверяем, какие ID_Order есть в OrderItems
            var orderIdsWithItems = db.OrderItems.Select(oi => oi.ID_Order).Distinct().ToList();
            string ids = string.Join(", ", orderIdsWithItems);
            MessageBox.Show($"Заказы, у которых есть товары:\n{ids}");

            // 3. Проверяем конкретно заказ 21
            var order21Items = db.OrderItems.Where(oi => oi.ID_Order == 21).ToList();
            MessageBox.Show($"Заказ 21: найдено {order21Items.Count} позиций");

            if (order21Items.Count > 0)
            {
                string itemsInfo = "";
                foreach (var item in order21Items)
                {
                    var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                    itemsInfo += $"ID_Product={item.ID_Product}, Quantity={item.Quantity}\n";
                    if (product != null)
                    {
                        itemsInfo += $"  Товар: {product.ProductName}, Цена: {product.Price}\n\n";
                    }
                }
                MessageBox.Show($"Заказ 21:\n{itemsInfo}");
            }
        }

       
        private void goBackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Обновление страницы...");
            LoadOrders();  // Перезагружаем страницу
            UpdateCount(); // Обновляем счётчик
        }
        private void viewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ordersList.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ");
                return;
            }

            dynamic selected = ordersList.SelectedItem;
            int orderId = selected.ID_Order;

            var orderDetailsWindow = new OrderDetailsWindow(orderId);
            orderDetailsWindow.Owner = Window.GetWindow(this);
            orderDetailsWindow.ShowDialog();
        }
    }
}