using ChistyulyaStore.Models;
using ChistyulyaStore.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;

namespace ChistyulyaStore.Pages
{
    public partial class AdministratorPage : Page
    {
        DBEntities db = new DBEntities();
        List<Suppliers> suppliers = new List<Suppliers>();
        List<Products> products = new List<Products>();
        private int currentUserId; // Добавьте это поле

        public AdministratorPage(string LastName, string FirstName, string MiddleName)
        {
            InitializeComponent();
            // Получаем ID пользователя по ФИО
            currentUserId = GetCurrentUserId(LastName, FirstName, MiddleName);
            FIOBlock.Text = $"{LastName} {FirstName} {MiddleName}";
            LoadProducts();

            suppliers = db.Suppliers.ToList();
            suppliers.Insert(0, new Suppliers() { ID_Suppliers = 0, SuppliersName = "Все поставщики" });
            filtrBox.ItemsSource = suppliers;
        }
        private int GetCurrentUserId(string lastName, string firstName, string middleName)
        {
            var user = db.Users.FirstOrDefault(u => u.LastName == lastName &&
                                                    u.FirstName == firstName &&
                                                    u.MiddleName == middleName);
            return user?.ID_User ?? 1; // Если не найден, возвращаем 1
        }
        private void UpdateCount()
        {
            int total = db.Products.Count();
            int current = 0;
            if (productList.ItemsSource != null)
            {
                var list = productList.ItemsSource as IEnumerable<Products>;
                if (list != null)
                {
                    current = list.Count();
                }
            }
            countText.Text = $"{current} из {total}";
        }

        private void LoadProducts()
        {
            productList.ItemsSource = db.Products.ToList();
            UpdateCount();
        }

        private void goBackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            AddEditWindow add = new AddEditWindow();
            if (add.ShowDialog() == false)
            {
                LoadProducts();
            }
        }

        private void productList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = productList.SelectedItem as Products;
            if (item != null)
            {
                AddEditWindow edit = new AddEditWindow(item);
                if (edit.ShowDialog() == false)
                {
                    LoadProducts();
                }
            }
        }

        private void filtrBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = filtrBox.SelectedItem as Suppliers;
            if (selected == null || selected.ID_Suppliers == 0)
            {
                productList.ItemsSource = db.Products.ToList();
            }
            else
            {
                products = db.Products.Where(x => x.ID_Supplier == selected.ID_Suppliers).ToList();
                productList.ItemsSource = products;
            }
            UpdateCount();
        }

        private void sortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(productList.ItemsSource);

            switch (sortBox.SelectedIndex)
            {
                case 0:
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription("StockQuantity", ListSortDirection.Ascending));
                    break;
                case 1:
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription("StockQuantity", ListSortDirection.Descending));
                    break;
            }
            view.Refresh();
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                productList.ItemsSource = db.Products.ToList();
            }
            else
            {
                products = db.Products.Where(x =>
                    x.ProductName.ToLower().Contains(searchText) ||
                    (x.Description != null && x.Description.ToLower().Contains(searchText)) ||
                    (x.Article != null && x.Article.ToLower().Contains(searchText))
                ).ToList();
                productList.ItemsSource = products;
            }
            UpdateCount();
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = productList.SelectedItem as Products;
                if (item != null)
                {
                    MessageBoxResult result = MessageBox.Show("Вы уверены что хотите удалить товар?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        db.Products.Remove(item);
                        db.SaveChanges();
                        LoadProducts();
                        MessageBox.Show("Товар удален", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Выберите товар для удаления", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch
            {
                MessageBox.Show("Товар находится в заказе и не может быть удален", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ДОБАВЬТЕ ЭТОТ МЕТОД:
        private void ordersButton_Click(object sender, RoutedEventArgs e)
        {
            var ordersPage = new OrdersPage(currentUserId, "Администратор");
            NavigationService.Navigate(ordersPage);
        }

    }
}