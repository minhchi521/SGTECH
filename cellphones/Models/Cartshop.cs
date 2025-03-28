using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace cellphones.Models
{
    public class Cartshop
    {
        public string cartid { get; set; }
        public string idproduct { get; set; }
        public String nameproduct { get; set; }
        public int? quantity { get; set; }
        public String userid { get; set; }
        public string image { get; set; }
        public decimal price { get; set; }
        public decimal total => (quantity ?? 0) * price;
    }
}