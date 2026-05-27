using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ChistyulyaStore.Models;

namespace ChistyulyaStore.Windows
{
    public partial class OrderDetailsWindow : Window
    {
        DBEntities db = new DBEntities();

        public OrderDetailsWindow(int orderId)
        {
            InitializeComponent();
            LoadOrderDetails(orderId);
        }

        private void LoadOrderDetails(int orderId)
        {
            var order = db.Orders.FirstOrDefault(o => o.ID_Order == orderId);
            if (order == null) return;

            var user = db.Users.FirstOrDefault(u => u.ID_User == order.ID_User);
            var items = db.OrderItems.Where(oi => oi.ID_Order == orderId).ToList();

            orderTitle.Text = $"Заказ №{order.ID_Order}";
            clientNameText.Text = $"Клиент: {user?.LastName} {user?.FirstName} {user?.MiddleName}".Trim();
            orderDateText.Text = $"Дата заказа: {order.OrderDate:dd.MM.yyyy}";
            pickUpCodeText.Text = $"Код получения: {order.PickUpCode}";

            var orderItems = new List<OrderItemDetail>();
            decimal totalSum = 0;

            foreach (var item in items)
            {
                var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                if (product != null)
                {
                    decimal price = (decimal)product.Price;
                    decimal finalPrice = price - (price * product.Discount / 100);
                    decimal itemTotal = finalPrice * item.Quantity;
                    totalSum += itemTotal;

                    orderItems.Add(new OrderItemDetail
                    {
                        ProductName = product.ProductName,
                        Quantity = item.Quantity,
                        Price = price,
                        Discount = product.Discount,
                        Total = itemTotal
                    });
                }
            }

            itemsList.ItemsSource = orderItems;
            totalSumText.Text = $"{totalSum:F2} р.";
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class OrderItemDetail
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public decimal Total { get; set; }
    }
}