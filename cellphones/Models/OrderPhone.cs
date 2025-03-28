using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace cellphones.Models
{
    public class OrderPhone 
    {
        public string OrderID { get; set; }
        public string UserID { get; set; }
        public Nullable<System.DateTime> OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string ShippingAddress { get; set; }
        public string phone { get; set; }
    }
}