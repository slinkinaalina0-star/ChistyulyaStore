using ChistyulyaStore.Pages;
using System;
using System.Windows;

namespace ChistyulyaStore
{
    public partial class MainWindow : Window
    {
        private int currentUserId;

        // Конструктор по умолчанию (нужен для XAML)
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new AuthorizationPage());
            currentUserId = 0;
        }

        // Конструктор с ID пользователя
        public MainWindow(int userId)
        {
            InitializeComponent();
            MainFrame.Navigate(new AuthorizationPage());
            currentUserId = userId;
            LoadUserData();
        }

        private void LoadUserData()
        {
            // Здесь можно загрузить ФИО пользователя и отобразить
            // Например:
            // using (var db = new DBEntities())
            // {
            //     var user = db.Users.FirstOrDefault(u => u.ID_User == currentUserId);
            //     if (user != null)
            //     {
            //         UserNameText.Text = $"{user.LastName} {user.FirstName}";
            //     }
            // }
        }
    }
}