using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ChistyulyaStore.Models;
using ChistyulyaStore.Windows;

namespace ChistyulyaStore.Pages
{
    public partial class ClientPage : Page
    {
        DBEntities db = new DBEntities();
        private int currentUserId;
        private string clientFIO;

        public ClientPage()
        {
        }

        public ClientPage(string LastName, string FirstName, string MiddleName, int userId)
        {
            InitializeComponent();
            clientFIO = $"{LastName} {FirstName} {MiddleName}";
            FIOBlock.Text = clientFIO;
            currentUserId = userId;
            LoadProducts();
            CheckAndUpdateOrderButton();
        }

        private void LoadProducts()
        {
            productList.ItemsSource = db.Products.ToList();
        }

        private void CheckAndUpdateOrderButton()
        {
            // Проверяем, есть ли активный заказ с товарами
            var order = db.Orders.FirstOrDefault(x => x.ID_User == currentUserId && x.ID_Status == 2);

            if (order != null)
            {
                var hasItems = db.OrderItems.Any(x => x.ID_Order == order.ID_Order);
                viewOrderButton.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                viewOrderButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AddToOrder_Click(object sender, RoutedEventArgs e)
        {
            var selected = productList.SelectedItem as Products;
            if (selected == null)
            {
                MessageBox.Show("Выберите товар", "Внимание");
                return;
            }

            if (selected.StockQuantity <= 0)
            {
                MessageBox.Show("Товара нет в наличии", "Внимание");
                return;
            }

            var result = MessageBox.Show($"Добавить товар \"{selected.ProductName}\" в корзину?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AddToCart(selected);
            }
        }

        private void AddToCart(Products product)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    // Ищем активный заказ
                    var order = db.Orders.FirstOrDefault(x => x.ID_User == currentUserId && x.ID_Status == 2);

                    if (order == null)
                    {
                        var pickUpPoint = db.PickUpPoints.FirstOrDefault();
                        if (pickUpPoint == null)
                        {
                            MessageBox.Show("Нет доступных пунктов выдачи", "Ошибка");
                            return;
                        }

                        order = new Orders()
                        {
                            ID_User = currentUserId,
                            ID_PickUpPoint = pickUpPoint.ID_PickUpPoint,
                            ID_Status = 2,
                            OrderDate = DateTime.Now,
                            DeliveryDate = DateTime.Now.AddDays(3),
                            PickUpCode = new Random().Next(1000, 9999)
                        };
                        db.Orders.Add(order);
                        db.SaveChanges();
                    }

                    // Добавляем товар
                    var orderItem = db.OrderItems.FirstOrDefault(x => x.ID_Order == order.ID_Order && x.ID_Product == product.ID_Product);

                    if (orderItem != null)
                    {
                        orderItem.Quantity++;
                    }
                    else
                    {
                        orderItem = new OrderItems()
                        {
                            ID_Order = order.ID_Order,
                            ID_Product = product.ID_Product,
                            Quantity = 1
                        };
                        db.OrderItems.Add(orderItem);
                    }

                    // Уменьшаем остаток
                    var productToUpdate = db.Products.FirstOrDefault(p => p.ID_Product == product.ID_Product);
                    if (productToUpdate != null && productToUpdate.StockQuantity > 0)
                    {
                        productToUpdate.StockQuantity--;
                    }

                    db.SaveChanges();
                }

                MessageBox.Show($"Товар \"{product.ProductName}\" добавлен в корзину", "Успешно");
                LoadProducts();
                CheckAndUpdateOrderButton(); // Обновляем видимость кнопки
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void viewOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var order = db.Orders.FirstOrDefault(x => x.ID_User == currentUserId && x.ID_Status == 2);
            if (order == null)
            {
                MessageBox.Show("Корзина пуста", "Информация");
                return;
            }

            var cartWindow = new OrderViewWindow(currentUserId, clientFIO);
            cartWindow.Owner = Window.GetWindow(this);
            cartWindow.ShowDialog();

            LoadProducts();
            CheckAndUpdateOrderButton();
        }

        private void goBackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}