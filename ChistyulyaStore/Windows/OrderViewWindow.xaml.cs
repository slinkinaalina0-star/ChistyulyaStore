using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using ChistyulyaStore.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ChistyulyaStore.Windows
{
    public partial class OrderViewWindow : Window
    {
        DBEntities db = new DBEntities();
        private int userId;
        private string clientFIO;
        private int orderId;
        private List<CartItem> cartItems = new List<CartItem>();

        public OrderViewWindow(int userId, string clientFIO)
        {
            InitializeComponent();
            this.userId = userId;
            this.clientFIO = clientFIO;

            clientNameText.Text = $"Клиент: {clientFIO}";
            LoadCart();
        }

        private void LoadCart()
        {
            try
            {
                var order = db.Orders.FirstOrDefault(x => x.ID_User == userId && x.ID_Status == 2);

                if (order == null)
                {
                    orderId = 0;
                    cartItems.Clear();
                    cartItemsList.ItemsSource = cartItems;
                    finalTotalText.Text = "0 руб";
                    return;
                }

                orderId = order.ID_Order;

                var items = db.OrderItems.Where(x => x.ID_Order == orderId).ToList();
                cartItems.Clear();

                foreach (var item in items)
                {
                    var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                    if (product != null)
                    {
                        var cartItem = new CartItem
                        {
                            ID_Product = product.ID_Product,
                            ProductName = product.ProductName,
                            Quantity = item.Quantity,
                            Price = (decimal)product.Price,
                            Discount = product.Discount
                        };
                        cartItem.PropertyChanged += CartItem_PropertyChanged;
                        cartItems.Add(cartItem);
                    }
                }

                cartItemsList.ItemsSource = cartItems;
                UpdateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки корзины: {ex.Message}", "Ошибка");
            }
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Quantity")
            {
                var item = sender as CartItem;
                if (item != null)
                {
                    try
                    {
                        var dbItem = db.OrderItems.FirstOrDefault(x => x.ID_Order == orderId && x.ID_Product == item.ID_Product);
                        if (dbItem != null)
                        {
                            if (item.Quantity <= 0)
                            {
                                db.OrderItems.Remove(dbItem);
                                cartItems.Remove(item);
                            }
                            else
                            {
                                dbItem.Quantity = item.Quantity;
                            }
                            db.SaveChanges();
                        }

                        UpdateTotal();
                        cartItemsList.ItemsSource = null;
                        cartItemsList.ItemsSource = cartItems;

                        if (cartItems.Count == 0)
                        {
                            var order = db.Orders.FirstOrDefault(x => x.ID_Order == orderId);
                            if (order != null)
                            {
                                db.Orders.Remove(order);
                                db.SaveChanges();
                                orderId = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления количества: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as CartItem;

            if (item != null)
            {
                var result = MessageBox.Show($"Удалить товар \"{item.ProductName}\" из корзины?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var dbItem = db.OrderItems.FirstOrDefault(x => x.ID_Order == orderId && x.ID_Product == item.ID_Product);
                        if (dbItem != null)
                        {
                            db.OrderItems.Remove(dbItem);
                            db.SaveChanges();
                        }

                        cartItems.Remove(item);
                        UpdateTotal();
                        cartItemsList.ItemsSource = null;
                        cartItemsList.ItemsSource = cartItems;

                        if (cartItems.Count == 0)
                        {
                            var order = db.Orders.FirstOrDefault(x => x.ID_Order == orderId);
                            if (order != null)
                            {
                                db.Orders.Remove(order);
                                db.SaveChanges();
                                orderId = 0;
                            }
                            MessageBox.Show("Корзина пуста", "Информация",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private void UpdateTotal()
        {
            decimal total = cartItems.Sum(x => x.TotalPrice);
            finalTotalText.Text = $"{total:F2} руб";
        }

        private void checkoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var order = db.Orders.FirstOrDefault(x => x.ID_Order == orderId);
                if (order == null) return;

                order.ID_Status = 1;

                int lowStockCount = 0;
                foreach (var item in cartItems)
                {
                    var product = db.Products.FirstOrDefault(p => p.ID_Product == item.ID_Product);
                    if (product == null || product.StockQuantity < 3)
                    {
                        lowStockCount++;
                    }
                }

                int deliveryDays = (lowStockCount > 0) ? 6 : 3;
                order.DeliveryDate = DateTime.Now.AddDays(deliveryDays);

                db.SaveChanges();

                var pickUpPoint = db.PickUpPoints.FirstOrDefault(x => x.ID_PickUpPoint == order.ID_PickUpPoint);

                SavePdfTalon(order, pickUpPoint, deliveryDays);

                MessageBox.Show($"Заказ №{order.ID_Order} оформлен!\n" +
                    $"Код получения: {order.PickUpCode}\n" +
                    $"Талон сохранен на рабочий стол",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePdfTalon(Orders order, PickUpPoints pickUpPoint, int deliveryDays)
        {
            string fileName = $"Талон_заказ_{order.ID_Order}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 50, 50, 50, 50);
                PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // Регистрируем шрифт с поддержкой кириллицы
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                iTextSharp.text.Font baseFont = iTextSharp.text.FontFactory.GetFont(fontPath, "CP1251", true, 12);

                iTextSharp.text.Font titleFont = new iTextSharp.text.Font(baseFont.BaseFont, 18, iTextSharp.text.Font.BOLD);
                iTextSharp.text.Font normalFont = new iTextSharp.text.Font(baseFont.BaseFont, 12, iTextSharp.text.Font.NORMAL);
                iTextSharp.text.Font boldFont = new iTextSharp.text.Font(baseFont.BaseFont, 12, iTextSharp.text.Font.BOLD);
                iTextSharp.text.Font codeFont = new iTextSharp.text.Font(baseFont.BaseFont, 24, iTextSharp.text.Font.BOLD);

                // Заголовок
                iTextSharp.text.Paragraph title = new iTextSharp.text.Paragraph("ЧИСТЮЛЯ - ТАЛОН ЗАКАЗА", titleFont);
                title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                doc.Add(title);
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));

                // Информация о заказе
                doc.Add(new iTextSharp.text.Paragraph($"Дата заказа: {order.OrderDate:dd.MM.yyyy HH:mm}", normalFont));
                doc.Add(new iTextSharp.text.Paragraph($"Номер заказа: {order.ID_Order}", normalFont));
                doc.Add(new iTextSharp.text.Paragraph($"Клиент: {clientFIO}", normalFont));
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));

                doc.Add(new iTextSharp.text.Paragraph("СОСТАВ ЗАКАЗА:", boldFont));

                // Товары
                decimal totalSum = 0;
                foreach (var item in cartItems)
                {
                    doc.Add(new iTextSharp.text.Paragraph($"  {item.ProductName}", normalFont));
                    doc.Add(new iTextSharp.text.Paragraph($"    Кол-во: {item.Quantity} x {item.FinalPrice:F2} руб = {item.TotalPrice:F2} руб", normalFont));
                    if (item.Discount > 0)
                    {
                        doc.Add(new iTextSharp.text.Paragraph($"    Скидка: {item.Discount}%", normalFont));
                    }
                    totalSum += item.TotalPrice;
                    doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));
                }

                doc.Add(new iTextSharp.text.Paragraph($"ИТОГО К ОПЛАТЕ: {totalSum:F2} руб", boldFont));
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));
                doc.Add(new iTextSharp.text.Paragraph($"Пункт выдачи: {pickUpPoint?.City}, {pickUpPoint?.Street}, {pickUpPoint?.House}", normalFont));
                doc.Add(new iTextSharp.text.Paragraph($"Срок доставки: {deliveryDays} дня", normalFont));
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));

                // Код получения
                iTextSharp.text.Paragraph codeParagraph = new iTextSharp.text.Paragraph($"КОД ДЛЯ ПОЛУЧЕНИЯ: {order.PickUpCode}", codeFont);
                codeParagraph.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                doc.Add(codeParagraph);

                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));
                doc.Add(new iTextSharp.text.Paragraph("Спасибо за покупку!", normalFont));
                doc.Add(new iTextSharp.text.Paragraph("Чистюля - ваш помощник в чистоте", normalFont));

                doc.Close();
            }

            System.Diagnostics.Process.Start(filePath);
        }

        private void continueButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }

    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity;

        public int ID_Product { get; set; }
        public string ProductName { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal Price { get; set; }
        public int Discount { get; set; }

        public decimal FinalPrice => Price - (Price * Discount / 100);
        public decimal TotalPrice => FinalPrice * Quantity;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}