using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChistyulyaStore.Models
{
    public class OrderItemDisplay : INotifyPropertyChanged
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
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal Price { get; set; }
        public int Discount { get; set; }
        public byte[] ProductImage { get; set; }

        public decimal FinalPrice => Price - (Price * Discount / 100);
        public decimal TotalPrice => FinalPrice * Quantity;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}