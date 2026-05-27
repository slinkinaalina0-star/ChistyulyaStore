using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using ChistyulyaStore.Models;

namespace ChistyulyaStore.Windows
{
    public partial class AddEditWindow : Window
    {
        DBEntities db = new DBEntities();
        Products currentProduct = new Products();
        bool isAddMode = true;
        string imagePath = null;

        public AddEditWindow()
        {
            InitializeComponent();
            isAddMode = true;
            LoadComboBoxes();
            Title = "Добавление товара";
        }

        public AddEditWindow(Products product)
        {
            InitializeComponent();
            isAddMode = false;
            currentProduct = product;
            LoadComboBoxes();
            LoadProductData();
            Title = "Редактирование товара";
        }

        private void LoadComboBoxes()
        {
            categoryBox.ItemsSource = db.Categories.ToList();
            manufacturerBox.ItemsSource = db.Manufacturers.ToList();
            supplierBox.ItemsSource = db.Suppliers.ToList();
            unitBox.ItemsSource = db.UnitsOfMeasurement.ToList();
        }

        private void LoadProductData()
        {
            articleBox.Text = currentProduct.Article;
            nameBox.Text = currentProduct.ProductName;
            priceBox.Text = currentProduct.Price.ToString();
            discountBox.Text = currentProduct.Discount.ToString();
            stockBox.Text = currentProduct.StockQuantity.ToString();
            descriptionBox.Text = currentProduct.Description;

            categoryBox.SelectedItem = db.Categories.FirstOrDefault(x => x.ID_Category == currentProduct.ID_Category);
            manufacturerBox.SelectedItem = db.Manufacturers.FirstOrDefault(x => x.ID_Manufacturer == currentProduct.ID_Manufacturer);
            supplierBox.SelectedItem = db.Suppliers.FirstOrDefault(x => x.ID_Suppliers == currentProduct.ID_Supplier);
            unitBox.SelectedItem = db.UnitsOfMeasurement.FirstOrDefault(x => x.ID_Unit == currentProduct.ID_Unit);

            if (currentProduct.ProductImage != null && currentProduct.ProductImage.Length > 0)
            {
                using (var stream = new MemoryStream(currentProduct.ProductImage))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    imageBox.Source = image;
                }
            }
        }

        private void imageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                imagePath = openFileDialog.FileName;
                imageBox.Source = new BitmapImage(new Uri(imagePath));
            }
        }

        private byte[] ImageToByteArray(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            return File.ReadAllBytes(filePath);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                MessageBox.Show("Введите наименование товара");
                return;
            }

            if (string.IsNullOrWhiteSpace(priceBox.Text))
            {
                MessageBox.Show("Введите цену");
                return;
            }

            if (!decimal.TryParse(priceBox.Text, out decimal price))
            {
                MessageBox.Show("Неверный формат цены");
                return;
            }

            int.TryParse(discountBox.Text, out int discount);
            int.TryParse(stockBox.Text, out int stock);

            if (categoryBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию");
                return;
            }

            if (manufacturerBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите производителя");
                return;
            }

            if (supplierBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите поставщика");
                return;
            }

            if (unitBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите единицу измерения");
                return;
            }

            try
            {
                if (isAddMode)
                {
                    Products newProduct = new Products()
                    {
                        Article = articleBox.Text,
                        ProductName = nameBox.Text,
                        ID_Category = ((Categories)categoryBox.SelectedItem).ID_Category,
                        ID_Manufacturer = ((Manufacturers)manufacturerBox.SelectedItem).ID_Manufacturer,
                        ID_Supplier = ((Suppliers)supplierBox.SelectedItem).ID_Suppliers,
                        ID_Unit = ((UnitsOfMeasurement)unitBox.SelectedItem).ID_Unit,
                        Price = price,
                        Discount = discount,
                        StockQuantity = stock,
                        Description = descriptionBox.Text,
                        ProductImage = ImageToByteArray(imagePath),
                         Photo = "default.jpg"
                    };

                    db.Products.Add(newProduct);
                    db.SaveChanges();
                    MessageBox.Show("Товар успешно добавлен");
                }
                else
                {
                    currentProduct.Article = articleBox.Text;
                    currentProduct.ProductName = nameBox.Text;
                    currentProduct.ID_Category = ((Categories)categoryBox.SelectedItem).ID_Category;
                    currentProduct.ID_Manufacturer = ((Manufacturers)manufacturerBox.SelectedItem).ID_Manufacturer;
                    currentProduct.ID_Supplier = ((Suppliers)supplierBox.SelectedItem).ID_Suppliers;
                    currentProduct.ID_Unit = ((UnitsOfMeasurement)unitBox.SelectedItem).ID_Unit;
                    currentProduct.Price = price;
                    currentProduct.Discount = discount;
                    currentProduct.StockQuantity = stock;
                    currentProduct.Description = descriptionBox.Text;

                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        currentProduct.ProductImage = ImageToByteArray(imagePath);
                    }

                    db.SaveChanges();
                    MessageBox.Show("Товар успешно обновлен");
                }

                DialogResult = true;
                Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Подробная ошибка валидации
                string errors = "";
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errors += $"Поле: {validationError.PropertyName}, Ошибка: {validationError.ErrorMessage}\n";
                    }
                }
                MessageBox.Show($"Ошибка валидации:\n{errors}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}