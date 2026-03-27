using System;

namespace Turfirm
{
    public enum UserRole
    {
        Guest = 0,
        User = 1,
        Manager = 2,
        Administrator = 3
    }

    public class CurrentSession
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public UserRole Role { get; set; }
    }

    public class CartItem
    {
        public int TourId { get; set; }
        public string TourName { get; set; }
        public decimal BasePrice { get; set; }
        public int Quantity { get; set; }
        public bool Insurance { get; set; }
        public bool Transfer { get; set; }
        public decimal TransferFee { get; set; }

        public decimal Total => BasePrice * Quantity + (Insurance ? BasePrice * Quantity * 0.08m : 0m) + (Transfer ? TransferFee : 0m);
    }
}
