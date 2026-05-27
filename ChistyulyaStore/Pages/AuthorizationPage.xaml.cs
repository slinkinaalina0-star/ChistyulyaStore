using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ChistyulyaStore.Models;
using ChistyulyaStore.Converts;

namespace ChistyulyaStore.Pages
{
    public partial class AuthorizationPage : Page
    {
        DBEntities db = new DBEntities();

        private int failCount = 0;
        private string currentCaptcha = "";
        private DispatcherTimer blockTimer;
        private bool isBlocked = false;
        private int remainingSeconds = 10;  // ← ДОБАВИТЬ

        public AuthorizationPage()
        {
            InitializeComponent();
            captchaPanel.Visibility = Visibility.Collapsed;
            blockMessage.Visibility = Visibility.Collapsed;

        }

        private void enterButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBlocked)
            {
                MessageBox.Show($"Подождите еще {remainingSeconds} секунд",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (failCount >= 1)
            {
                if (captchaBox.Text != currentCaptcha)
                {
                    MessageBox.Show("Неверный код с картинки", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    GenerateCaptcha();
                    return;
                }
            }

            var userObj = db.Users.FirstOrDefault(x => x.Email == emailBox.Text &&
                                                        x.Password == passwordBox.Password);

            if (userObj != null)
            {
                failCount = 0;
                captchaPanel.Visibility = Visibility.Collapsed;
                blockMessage.Visibility = Visibility.Collapsed;

                switch (userObj.ID_EmployeeRole)
                {
                    case 1:
                        NavigationService.Navigate(new AdministratorPage(
                            userObj.LastName, userObj.FirstName, userObj.MiddleName));
                        break;
                    case 2:
                        NavigationService.Navigate(new ManagerPage(
                            userObj.LastName, userObj.FirstName, userObj.MiddleName));
                        break;
                    case 3:
                        NavigationService.Navigate(new ClientPage(
                            userObj.LastName, userObj.FirstName, userObj.MiddleName, userObj.ID_User));
                        break;
                }
            }
            else
            {
                failCount++;

                if (failCount >= 1)
                {
                    captchaPanel.Visibility = Visibility.Visible;
                    GenerateCaptcha();

                    if (failCount >= 2)
                    {
                        BlockLogin();
                    }
                }

                MessageBox.Show("Неверный логин или пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GenerateCaptcha()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
            Random r = new Random();
            currentCaptcha = "";

            for (int i = 0; i < 4; i++)
            {
                currentCaptcha += chars[r.Next(chars.Length)];
            }

            captchaImage.Source = CaptchaGenerator.Generate(currentCaptcha);
        }

        private void BlockLogin()
        {
            isBlocked = true;
            enterButton.IsEnabled = false;
            remainingSeconds = 10;
            blockMessage.Visibility = Visibility.Visible;
            blockMessage.Text = $"Доступ заблокирован на {remainingSeconds} секунд";

            blockTimer = new DispatcherTimer();
            blockTimer.Interval = TimeSpan.FromSeconds(1);
            blockTimer.Tick += (s, args) =>
            {
                remainingSeconds--;

                if (remainingSeconds > 0)
                {
                    blockMessage.Text = $"Доступ заблокирован на {remainingSeconds} секунд";
                }
                else
                {
                    // Блокировка закончилась
                    blockTimer.Stop();
                    isBlocked = false;
                    enterButton.IsEnabled = true;
                    failCount = 0;
                    captchaPanel.Visibility = Visibility.Collapsed;
                    blockMessage.Visibility = Visibility.Collapsed;
                    captchaBox.Text = "";
                    MessageBox.Show("Вход разблокирован. Попробуйте снова.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            blockTimer.Start();
        }

        private void guestButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new GuestPage());
        }
    
public static bool ValidateUser(string email, string password)
        {
            using (var db = new DBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
                return user != null;
            }
        }
    }
}