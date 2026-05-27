using System.Windows.Controls;
using System.Windows.Navigation;
using ChistyulyaStore.Models;
using System.Linq;

namespace ChistyulyaStore.Pages
{
    public partial class GuestPage : Page
    {
        DBEntities db = new DBEntities();

        public GuestPage()
        {
            InitializeComponent();
            productList.ItemsSource = db.Products.ToList();
        }

        private void goBackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}