using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace cellphones.Models
{
    public class OffersViewModel
    {
        public User User { get; set; }
        public Promotion SelectedPromotion { get; set; }
    }
}