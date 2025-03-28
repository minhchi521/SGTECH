using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace cellphones.Models
{
    public class reviewproduct 
    {
        [Required(ErrorMessage = "Vui lòng nhập bình luận")]
        [Display(Name = "Bình luận")]
        public string NewComment { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; }
    }
}