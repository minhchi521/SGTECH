using cellphones.Models;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace cellphones.Controllers
{
    public class AdminController : Controller
    {
        SGTechEntities db = new SGTechEntities();
        // GET: Admin
        public ActionResult CreateCategoty()
        {
            Category cate = new Category();
            return View(cate);
        }

        [HttpPost]
        public ActionResult CreateCategoty(Category cate)
        {
            var checkcate = db.Categories.Where(s => s.CategoryName == cate.CategoryName).FirstOrDefault();

            if(checkcate == null)
            {

                cate.CategoryID = Guid.NewGuid().ToString("N").Substring(0, 8);
                db.Categories.Add(cate);

                db.SaveChanges();
                return RedirectToAction("Categoty");
            }

            return View();
        }

        public ActionResult DeleteCategoty(String id)
        {
            var checkid = db.Categories.Where(s => s.CategoryID == id).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }
            try
            {
                db.Categories.Remove(checkid);
                db.SaveChanges();

                return RedirectToAction("Categoty");
            }
            catch
            {
                return RedirectToAction("Categoty");
            }
            
        }

        public ActionResult EditCategoty(String id)
        {
            var checkid = db.Categories.Where(s => s.CategoryID == id).FirstOrDefault();

            return View(checkid);
        }

        [HttpPost]
        public ActionResult EditCategoty(Category cate)
        {
            var checkid = db.Categories.Where(s => s.CategoryID == cate.CategoryID).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            checkid.CategoryName = cate.CategoryName;
            checkid.Description = cate.Description;

            db.SaveChanges();
            return RedirectToAction("Categoty");
        }

        public ActionResult Categoty()
        {
            var check = db.Categories.ToList();
            return View(check);
        }
        //DeleteUser
        public ActionResult DeleteUser(String id)
        {
            var checkid = db.Users.Where(s => s.UserID == id).FirstOrDefault();
            db.Users.Remove(checkid);
            db.SaveChanges();
            return View("ManageUser");
        }
        //DetailsUser
        public ActionResult DetailsUser(String id)
        {
            var checkid = db.Users.Where(s => s.UserID == id).FirstOrDefault();
            return View(checkid);
        }

        [HttpPost]
        public ActionResult DetailsUser(User pro)
        {
            var checkid = db.Users.Where(s => s.UserID == pro.UserID).FirstOrDefault();
            return View(checkid);
        }
        //EditUser
        public ActionResult EditUser(String id)
        {
            var checkid = db.Users.Where(s => s.UserID == id).FirstOrDefault();

            return View(checkid);
        }

        [HttpPost]
        public ActionResult EditUser(User cate)
        {
            var checkid = db.Users.Where(s => s.UserID == cate.UserID).FirstOrDefault();

            if (checkid == null)
            {
                return HttpNotFound();
            }
            try
            {
                checkid.Username = cate.Username;
                checkid.FullName = cate.FullName;
                checkid.Phone = cate.Phone;
                checkid.Address = cate.Address;

                db.SaveChanges();
                return RedirectToAction("Categoty");
            }
            catch
            {
                return RedirectToAction("Categoty");
            }
            
        }
        //ManageProduct
        public ActionResult Index(String id)
        {
            if (id == null)
            {
                var productList = db.Products.OrderByDescending(x => x.ProductName);

                return View(productList);
            }
            else
            {
                var productList = db.Products.OrderByDescending(x => x.ProductName)
                .Where(x => x.CategoryID == id);

                return View(productList);
            }
        }

        public ActionResult Create()
        {
            Product pro = new Product();
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");

            return View(pro);
        }

        [HttpPost]
        public ActionResult Create(Product pro)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (pro.UploadImage != null && pro.UploadImage.ContentLength > 0)
                    {

                        String file = Path.GetFileNameWithoutExtension(pro.UploadImage.FileName);
                        String enten = Path.GetExtension(pro.UploadImage.FileName);
                        file = file + enten;

                        pro.Image = "~/Content/image/" + file;
                        pro.UploadImage.SaveAs(Path.Combine(Server.MapPath("~/Content/image"), file));
                        pro.ProductID = Guid.NewGuid().ToString("N").Substring(0, 8);
                        pro.Status = true;

                    }

                    // Add to database and save changes here
                    db.Products.Add(pro);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", pro.CategoryID);

            return View(pro);
        }

        public ActionResult Detail(String id)
        {
            var checkid = db.Products.Where(s => s.ProductID == id).FirstOrDefault();
            return View(checkid);
        }

        [HttpPost]
        public ActionResult Detail(Product pro)
        {
            var checkid = db.Products.Where(s => s.ProductID == pro.ProductID).FirstOrDefault();
            return View(checkid);
        }

        public ActionResult Delete(string id)
        {
            var checkid = db.Products.Where(s => s.ProductID == id).FirstOrDefault();

            if (checkid == null)
            {
                return HttpNotFound();
            }

            db.Products.Remove(checkid);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Edit(String id)
        {
            var checkid = db.Products.Where(s => s.ProductID == id).FirstOrDefault();

            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryID", "CategoryName");

            return View(checkid);
        }

        [HttpPost]
        public ActionResult Edit(Product pro)
        {
            var checkid = db.Products.Where(s => s.ProductID == pro.ProductID).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            checkid.ProductName = pro.ProductName;
            checkid.Description = pro.Description;
            checkid.Price = pro.Price;
            checkid.CategoryID = pro.CategoryID;

            if (pro.UploadImage != null)
            {
                string file = Path.GetFileNameWithoutExtension(pro.UploadImage.FileName);
                string entent = Path.GetExtension(pro.UploadImage.FileName);
                file = file + entent;

                checkid.Image = "~/Content/image/" + file;
                pro.UploadImage.SaveAs(Path.Combine(Server.MapPath("~/Content/image"), file));
            }

            db.SaveChanges();
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName",pro.CategoryID);
            return RedirectToAction("Index");
        }
        //Staff
        public ActionResult ManageStaff()
        {
            var checkrole = db.Users.Where(s => s.Role == "Staff").ToList();
            return View(checkrole);
        }
        //Customer
        public ActionResult ManageUser()
        {
            var manageuser = db.Users.Where(s => s.Role == "Customer").ToList();
            return View(manageuser);
        }
           //DetailUser
        public ActionResult DetailUser(String id)
        {
            var checkid = db.Users.Where(s => s.UserID == id).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }
            return View(checkid);
        }

        [HttpPost]
        public ActionResult DetailUser(User user)
        {
            var checkid = db.Users.Where(s => s.UserID == user.UserID).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }
            return View(checkid);
        }
        //order
        public ActionResult Order()
        {
            var list = db.Orders.ToList();
            return View(list);
        }
        public ActionResult Orderdetail(String id)
        {
            var checkid = db.OrderDetails.Where(s => s.OrderID == id).ToList();
            return View(checkid);
        }
        //Tìm kiếm Prodcut
        public ActionResult Search(string searchString)
        {
            var productList = db.Products.OrderByDescending(x => x.ProductName);

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = (IOrderedQueryable<Product>)productList.Where(x => x.ProductName.Contains(searchString));
            }

            return View("Index", productList.ToList());
        }
        //Tìm kiếm Promotion
        public ActionResult SearchPromotion(string searchString)
        {
            var productList = db.Promotions.OrderByDescending(x => x.PromotionName);

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = (IOrderedQueryable<Promotion>)productList.Where(x => x.PromotionName.Contains(searchString));
            }

            return View("Promotion", productList.ToList());
        }
        //Tìm kiếm Category
        public ActionResult SearchCategory(string searchString)
        {
            var productList = db.Categories.OrderByDescending(x => x.CategoryName);

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = (IOrderedQueryable<Category>)productList.Where(x => x.CategoryName.Contains(searchString));
            }

            return View("Categoty", productList.ToList());
        }
        //Tìm kiếm User
        public ActionResult SearchUser(string searchString)
        {
            var productList = db.Users.OrderByDescending(x => x.Username);

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = (IOrderedQueryable<User>)productList.Where(x => x.Username.Contains(searchString));
            }

            return View("ManageUser", productList.ToList());
        }
        //Promotion
        public ActionResult Promotion()
        {
            var checkid = db.Promotions.ToList();
            return View(checkid);
        }
        //CreatePromtion
        public ActionResult CreatePromtion()
        {
            Promotion promo = new Promotion();
            return View(promo);
        }

        [HttpPost]
        public ActionResult CreatePromtion(Promotion pro)
        {

            pro.PromotionID = Guid.NewGuid().ToString("N").Substring(0, 8);
            db.Promotions.Add(pro);

            db.SaveChanges();
            return RedirectToAction("Promotion");
        }
        //DetailPromotion
        public ActionResult DetailPromotion(String id)
        {
            var checkid = db.Promotions.Where(s => s.PromotionID == id).FirstOrDefault();
            return View(checkid);
        }

        [HttpPost]
        public ActionResult DetailPromotion(Promotion promo)
        {
            var checkid = db.Promotions.Where(s => s.PromotionID == promo.PromotionID).FirstOrDefault();
            return View(checkid);
        }
        //DeletePromotion
        public ActionResult DeletePromotion(String id)
        {
            var checkid = db.Promotions.Where(s => s.PromotionID == id).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            db.Promotions.Remove(checkid);
            db.SaveChanges();
            return RedirectToAction("Promotion");
        }
        //EditPromotion
        public ActionResult EditPromotion(String id)
        {
            var checkid = db.Promotions.Where(s => s.PromotionID == id).FirstOrDefault();
            return View();
        }

        [HttpPost]
        public ActionResult EditPromotion(Promotion pro)
        {
            var checkid = db.Promotions.Where(s => s.PromotionID == pro.PromotionID).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            checkid.PromotionName = pro.PromotionName;
            checkid.StartDate = pro.StartDate;
            checkid.EndDate = pro.EndDate;

            db.SaveChanges();
            return RedirectToAction("Promotion");
        }
        //excel notview
        public ActionResult Excel(int month = 0, int year = 0, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Set the license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var danhSachGiaoDich = db.sales.AsQueryable();

                // Lọc theo tháng và năm
                if (month > 0 && year > 0)
                {
                    danhSachGiaoDich = danhSachGiaoDich.Where(gd =>
                        gd.timespent.HasValue &&
                        gd.timespent.Value.Month == month &&
                        gd.timespent.Value.Year == year);
                }

                // Lọc theo khoảng thời gian từ ngày đến ngày
                if (fromDate.HasValue)
                {
                    danhSachGiaoDich = danhSachGiaoDich.Where(gd =>
                        gd.timespent.HasValue &&
                        gd.timespent.Value >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    // Thêm một ngày vào toDate để bao gồm cả ngày kết thúc
                    DateTime endDate = toDate.Value.AddDays(1).AddSeconds(-1);
                    danhSachGiaoDich = danhSachGiaoDich.Where(gd =>
                        gd.timespent.HasValue &&
                        gd.timespent.Value <= endDate);
                }

                var danhSachLoc = danhSachGiaoDich.ToList();

                // Phần còn lại của mã giữ nguyên
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("GiaoDich");
                    // Style cho header
                    var headerStyle = worksheet.Cells[1, 1, 1, 5].Style;
                    headerStyle.Font.Bold = true;
                    headerStyle.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerStyle.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    // Thêm tiêu đề cột
                    worksheet.Cells[1, 1].Value = "ID giao dich";
                    // worksheet.Cells[1, 2].Value = "User ID";
                    worksheet.Cells[1, 3].Value = "Số Tiền";
                    worksheet.Cells[1, 2].Value = "Ngày Giao Dịch";
                    // Thêm dữ liệu vào Excel
                    for (int i = 0; i < danhSachLoc.Count; i++)
                    {
                        var item = danhSachLoc[i];
                        var rowIndex = i + 2;
                        worksheet.Cells[rowIndex, 1].Value = item.salesID;
                        // worksheet.Cells[rowIndex, 2].Value = item.UserID;
                        if (item.timespent.HasValue)
                        {
                            worksheet.Cells[rowIndex, 2].Style.Numberformat.Format = "dd/MM/yyyy";
                            worksheet.Cells[rowIndex, 2].Value = item.timespent.Value;
                        }
                        //worksheet.Cells[rowIndex, 4].Value = item.TransactionCode;
                        worksheet.Cells[rowIndex, 3].Value = item.spenttotal;
                        // Style cho cột số tiền
                        worksheet.Cells[rowIndex, 3].Style.Numberformat.Format = "#,##0";
                    }
                    // Cấu hình các ô
                    worksheet.Cells.AutoFitColumns();
                    // Thêm border cho tất cả các ô có dữ liệu
                    var dataRange = worksheet.Cells[1, 1, danhSachLoc.Count + 1, 3];
                    dataRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    // Tạo tên file với timestamp
                    string fileName = $"GiaoDich_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    // Lấy file Excel dưới dạng byte array
                    var fileContents = package.GetAsByteArray();
                    // Trả về file Excel cho người dùng
                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // Log the error
                // You might want to implement proper logging here
                return Content($"Có lỗi xảy ra khi xuất Excel: {ex.Message}");
            }
        }
        // revenue
        public ActionResult Revenue(int month = 0, int year = 0, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Lấy danh sách giao dịch
            var danhSachGiaoDich = db.sales.AsQueryable();

            // Nếu có yêu cầu lọc theo tháng và năm
            if (month > 0 && year > 0)
            {
                danhSachGiaoDich = danhSachGiaoDich.Where(gd =>
                    gd.timespent.HasValue &&
                    gd.timespent.Value.Month == month &&
                    gd.timespent.Value.Year == year);
            }

            // Lọc theo khoảng thời gian từ ngày đến ngày
            if (fromDate.HasValue)
            {
                danhSachGiaoDich = danhSachGiaoDich.Where(gd =>
                    gd.timespent.HasValue &&
                    gd.timespent.Value >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                // Thêm một ngày vào toDate để bao gồm cả ngày kết thúc
                DateTime endDate = toDate.Value.AddDays(1).AddSeconds(-1);
                danhSachGiaoDich = danhSachGiaoDich.Where(gd =>
                    gd.timespent.HasValue &&
                    gd.timespent.Value <= endDate);
            }

            var danhSachGiaoDich1 = db.sales.ToList();
            // Tính tổng tiền
            var tongTien = danhSachGiaoDich.Where(s => s.spenttotal != null).Sum(s => s.spenttotal.Value);
            // Lưu giá trị lọc vào ViewBag để hiển thị lại trong form
            ViewBag.Month = month;
            ViewBag.Year = year;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TongTien = tongTien;
            ViewBag.SoGiaoDich = danhSachGiaoDich1.Count;
            ViewBag.TrungBinhGiaoDich = danhSachGiaoDich1.Any() ? tongTien / danhSachGiaoDich1.Count : 0;

            // Chuyển danh sách thành List
            var danhSachLoc = danhSachGiaoDich.ToList();

            return View(danhSachLoc);
        }
        //column chart 
        public ActionResult ColumnChart()
        {
            var danhSachGiaoDich = db.sales.ToList();

            // Tính tổng tiền
            var tongTien = danhSachGiaoDich.Where(s => s.spenttotal != null).Sum(s => s.spenttotal.Value);

            // Thống kê theo tháng
            var doanhThuTheoThang = danhSachGiaoDich
                .Where(s => s.spenttotal != null && s.timespent != null)
                .GroupBy(s => new {
                    Thang = s.timespent.Value.Month,
                    Nam = s.timespent.Value.Year
                })
                .Select(g => new {
                    Thang = $"{g.Key.Thang:D2}/{g.Key.Nam}",
                    TongTien = g.Sum(s => s.spenttotal.Value),
                    SoGiaoDich = g.Count()
                })
                .OrderBy(x => x.Thang)
                .ToList();

            // Chuẩn bị dữ liệu cho biểu đồ
            ViewBag.Labels = JsonConvert.SerializeObject(doanhThuTheoThang.Select(x => x.Thang));
            ViewBag.Data = JsonConvert.SerializeObject(doanhThuTheoThang.Select(x => x.TongTien));
            ViewBag.TongTien = tongTien;
            ViewBag.SoGiaoDich = danhSachGiaoDich.Count;
            ViewBag.TrungBinhGiaoDich = danhSachGiaoDich.Any() ? tongTien / danhSachGiaoDich.Count : 0;

            return View(danhSachGiaoDich);
        }
        //circular expression
        public ActionResult CircularExpression()
        {
            return View();
        }
        //DetailsSales
        public ActionResult DetailsSales(string id)
        {
            var list = db.Orders.Where(s=>s.OrderID == id).FirstOrDefault();
            return View(list);
        }
        public ActionResult DetailsSales1(String id)
        {
            var checkid = db.OrderDetails.Where(s => s.OrderID == id).ToList();
            return View(checkid);
        }
        //confirm
        public ActionResult ConfirmOrder(string id)
        {
            var Confirmorder = db.Orders.Where(s => s.OrderID == id).FirstOrDefault();

            if(Confirmorder == null)
            {
                return HttpNotFound();
            }

            Confirmorder.Status = "Confirm";

            db.SaveChanges();

            return RedirectToAction("Order");
        }
        //Manage Review
        public ActionResult ManageReview()
        {
            var product = db.Products.ToList();
            return View(product);
        }
        //Manage Review Details
        public ActionResult ManageReviewDetails(string id)
        {
            var product = db.ProductReviews.Where(s => s.ProductID == id).AsNoTracking().ToList();
            return View(product);
        }
        // lấy ID người dùng
        private string GetCurrentUserId()
        {
            return Session["id"] as string ?? "DefaultUserId";
        }
    }
}