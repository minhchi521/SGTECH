using cellphones.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace cellphones.Controllers
{
    public class ProductController : Controller
    {
        SGTechEntities db = new SGTechEntities();
        // GET: Product
        public ActionResult IndexUser(String id)
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
        //Detail
        public ActionResult DetailProduct(String id)
        {
            var userId = GetCurrentUserId();
            var checkid = db.Products.Where(s => s.ProductID == id).FirstOrDefault();
            var comment = db.ProductReviews.Where(s => s.ProductID == id).ToList();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            bool hasPurchased = false;
            if (userId != null)
            {
                hasPurchased = db.Orders
                    .Where(o => o.UserID == userId)
                    .SelectMany(o => o.OrderDetails)
                    .Any(od => od.ProductID == id);
            }

            var viewmodel = new ProductWithComment
            {
                product = checkid,
                DanhGia = comment,
                HasPurchased = hasPurchased
            };

            return View(viewmodel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddComment(string id, ProductWithComment model)
        {
            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();

                bool hasPurchased = db.Orders
               .Where(o => o.UserID == userId)
               .SelectMany(o => o.OrderDetails)
               .Any(od => od.ProductID == id);

                if (!hasPurchased)
                {
                    // User hasn't purchased the product, redirect back
                    TempData["ErrorMessage"] = "Bạn cần mua sản phẩm trước khi đánh giá.";
                    return RedirectToAction("DetailProduct", new { id = id });
                }

                var newComment = new ProductReview
                {
                    ReviewID = Guid.NewGuid().ToString("N").Substring(0, 8),
                    ProductID = id,
                    Comment = model.NewComment,
                    Rating = model.Rating,
                    ReviewDate = DateTime.Now
                };

                db.ProductReviews.Add(newComment);
                db.SaveChanges();

                return RedirectToAction("DetailProduct", new { id = id });
            }


            return RedirectToAction("DetailProduct", new { id = id });
        }
        //Cart
        public ActionResult Cart()
        {
           
            var iduser = GetCurrentUserId();
            if(iduser == "DefaultUserId")
            {
                string idUser = Request.Cookies["idUser12"]?.Value;

                var list1 = from cart in db.Carts
                           join product in db.Products on cart.ProductID equals product.ProductID
                           select new Cartshop
                           {
                               cartid = cart.CartID,
                               nameproduct = product.ProductName,
                               quantity = cart.Quantity,
                               userid = cart.UserID,
                               image = product.Image,
                               price = product.Price
                           };

                var listcart1 = list1.Where(s => s.userid == idUser).ToList();
                return View(listcart1);
            }

            var list = from cart in db.Carts
                       join product in db.Products on cart.ProductID equals product.ProductID
                       select new Cartshop
                       {
                           cartid = cart.CartID,
                           nameproduct = product.ProductName,
                           quantity = cart.Quantity,
                           userid =cart.UserID,
                           image = product.Image,
                           price = product.Price
                       };

            var listcart = list.Where(s => s.userid == iduser).ToList();
            return View(listcart);
        }

        public ActionResult UpdateQuantity1(string id)
        {
            try
            {
                var cart = db.Carts.Find(id);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                if (cart.Quantity <= 1)
                {
                    return Json(new { success = false, message = "Không thể giảm thêm" });
                }

                cart.Quantity -= 1;

                var product = db.Products.Find(cart.ProductID);
                product.StockQuantity +=1;
                decimal newTotal = (decimal)(cart.Quantity * product.Price);

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    newQuantity = cart.Quantity,
                    newTotal = newTotal
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        public ActionResult UpdateQuantity2(string id)
        {
            try
            {
                var cart = db.Carts.Find(id);
                var product = db.Products.Find(cart.ProductID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                if (cart.Quantity >= 5)
                {
                    return Json(new { success = false, message = "Không thể tăng thêm" });
                }

                if(product.StockQuantity == 0)
                {
                    return Json(new { success = false, message = "Sản phẩm đã hết" });
                }
                 
                cart.Quantity += 1;
                decimal newTotal = (decimal)(cart.Quantity * product.Price);

                if(product.StockQuantity > 0)
                {
                    product.StockQuantity -= 1;
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    newQuantity = cart.Quantity,
                    newTotal = newTotal
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }


        public ActionResult Delete(string id)
        {
            var cart = db.Carts.Find(id);
           
            if (cart != null)
            {
                db.Carts.Remove(cart);
                db.SaveChanges();
            }
            return RedirectToAction("Cart");
        }
        //BuyNoLogin
        public ActionResult BuyNoLogin(String id)
        {

            // Gọi UserBuyNew() nhưng đừng gọi lại SaveChanges() ở đó
            UserBuyNew();
           
            // Lấy cookie đã được tạo từ UserBuyNew()
            string idUser = Request.Cookies["idUser12"]?.Value;

                // Kiểm tra xem idUser có giá trị không
                if (string.IsNullOrEmpty(idUser))
                {
                    // Xử lý trường hợp không có idUser
                    return RedirectToAction("Error");
                }

                var cartnouser = new Cart
                {
                    CartID = Guid.NewGuid().ToString("N").Substring(0, 8),
                    UserID = idUser,
                    ProductID = id,
                    Quantity = 1,
                    AddedAt = DateTime.Now
                };

                db.Carts.Add(cartnouser);
                db.SaveChanges();
                return RedirectToAction("Cart");
       
        }

        public void UserBuyNew()
        {
            // Tạo user ID mới
            var usernew = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            // Tạo cookie lưu idUser trong 5 phút
            HttpCookie userCookie = new HttpCookie("idUser12", usernew);
            userCookie.Expires = DateTime.Now.AddMinutes(5);
            Response.Cookies.Add(userCookie);


            var userid = new User
            {
                UserID = usernew,
                Username=Guid.NewGuid().ToString("N").Substring(0,8),
                Email = Guid.NewGuid().ToString("N").Substring(0,8),
                Role="nologin"
            };

            db.Users.Add(userid);
            db.SaveChanges();
        }
        //BooksNoLogin
        public ActionResult BooksNoLogin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult BooksNoLogin(decimal totalAmount1,User user1)
        {
            string idUser = Request.Cookies["idUser12"]?.Value;
            var cartItems = db.Carts.Where(c => c.UserID == idUser).ToList();
            var cartItems1 = db.Users.Where(c => c.UserID == idUser).FirstOrDefault();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("IndexUser");
            }

            try
            {
                cartItems1.Phone = user1.Phone;
                cartItems1.Address = user1.Address;
                int maxSoThuTu = db.Orders.Max(c => (int?)c.number) ?? 0;
                var orderId = Guid.NewGuid().ToString("N").Substring(0, 8);

                var order = new Order
                {
                    OrderID = orderId,
                    UserID = idUser,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount1,
                    Status = "Pending",
                    PaymentStatus = "Pending",
                    number = maxSoThuTu + 1,
                    PaymentMethod = null,
                    ShippingAddress = null
                };

                var sales1 = new sale
                {
                    salesID = Guid.NewGuid().ToString("N").Substring(0, 8),
                    OrderID = orderId,
                    spenttotal = (int?)totalAmount1,
                    timespent = DateTime.Now
                };

                db.sales.Add(sales1);
                db.Orders.Add(order);

                foreach (var cartItem in cartItems)
                {
                    var product = db.Products.Find(cartItem.ProductID);

                    if (product != null)
                    {

                        var orderDetail = new OrderDetail
                        {
                            OrderDetailID = Guid.NewGuid().ToString("N").Substring(0, 8),
                            OrderID = orderId,
                            ProductID = cartItem.ProductID,
                            Quantity = (int)cartItem.Quantity,
                            UnitPrice = (decimal)(product.Price * cartItem.Quantity)
                        };
                        db.OrderDetails.Add(orderDetail);
                    }
                    product.StockQuantity -= 1;
                }

                db.Carts.RemoveRange(cartItems);
                db.SaveChanges();
                return RedirectToAction("BuyPremium");
            }
            catch (Exception ex)
            {
                // Log lỗi hoặc xử lý lỗi
                return RedirectToAction("IndexUser");
            }
        }
        //Buy
        public ActionResult buyproduct(String id)
        {
            try
            {
                // Kiểm tra sản phẩm tồn tại
               var checkid = db.Products.Where(s => s.ProductID == id).FirstOrDefault();

               if (checkid.StockQuantity > 0)
               {
                    if (checkid == null)
                    {
                        return HttpNotFound();
                    }
                    // Lấy và kiểm tra UserID
                    var iduser = GetCurrentUserId();
                    var user = db.Users.Find(iduser);
                    if (user == null)
                    {
                        // Xử lý khi user không tồn tại - có thể redirect về trang login
                        return RedirectToAction("Index", "Account");
                    }

                    var cart = new Cart
                    {
                        CartID = Guid.NewGuid().ToString("N").Substring(0, 8),
                        UserID = iduser,
                        ProductID = id,
                        Quantity = 1,
                        AddedAt = DateTime.Now
                    };
                    checkid.StockQuantity -=1;
                    db.Carts.Add(cart);
                    db.SaveChanges();
                    return RedirectToAction("Cart");

               }
               else
               {
                    ModelState.AddModelError("", "Sản phẩm hiện đang hết hàng");
                    return View();
               }  
            }
            catch (Exception ex)
            {
                // Xử lý lỗi - có thể log lỗi và hiển thị thông báo
                ModelState.AddModelError("", "Không thể thêm vào giỏ hàng. Vui lòng thử lại sau.");
                return RedirectToAction("DetailProduct", new { id = id });
            }
        }
        //Dat hàng 
        public ActionResult Books(decimal totalAmount)
        {
            string userId = GetCurrentUserId();
            string idUser = Request.Cookies["idUser12"]?.Value;

            var cartItems = db.Carts.Where(c => c.UserID == userId).ToList();
            var cartItems1 = db.Carts.Where(c => c.UserID == idUser).ToList();

            if (cartItems.Count != 0 )
            {
                var user = db.Users.Where(s => s.UserID == userId).FirstOrDefault();

                if (user != null)
                {
                    user.spent += (int)totalAmount;
                    if(user.spent >= 10000)
                    {
                        user.RankUser = "silver";
                    }
                    else if (user.spent >= 30000)
                    {
                        user.RankUser = "Gold";
                    }
                    else if (user.spent >= 50000)
                    {
                        user.RankUser = "diamond";
                    }
                }

                int maxSoThuTu = db.Orders.Max(c => (int?)c.number) ?? 0;
                var orderId = Guid.NewGuid().ToString("N").Substring(0, 8);

                var order = new Order
                {
                    OrderID = orderId,
                    UserID = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentStatus = "Pending",
                    number = maxSoThuTu + 1,
                    PaymentMethod = null,
                    ShippingAddress = null
                };

                db.Orders.Add(order);

                foreach (var cartItem in cartItems)
                {
                    var product = db.Products.Find(cartItem.ProductID);
                    if (product != null)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderDetailID = Guid.NewGuid().ToString("N").Substring(0, 8),
                            OrderID = orderId,
                            ProductID = cartItem.ProductID,
                            Quantity = (int)cartItem.Quantity,
                            UnitPrice = (decimal)(product.Price * cartItem.Quantity)
                        };

                        db.OrderDetails.Add(orderDetail);
                    }
                }

                var sales1 = new sale
                {
                    salesID = Guid.NewGuid().ToString("N").Substring(0, 8),
                    OrderID = orderId,
                    spenttotal = (int?)totalAmount,
                    timespent = DateTime.Now
                };

                db.sales.Add(sales1);
                db.Carts.RemoveRange(cartItems);
                db.SaveChanges();

                return RedirectToAction("BuyPremium");
            }
            else if(cartItems1.Count != 0)
            {
                return RedirectToAction("BooksNoLogin", new { totalAmount1 = totalAmount });
            }

            return RedirectToAction("IndexUser");
        }
        //Buy
        [HttpGet]
        public ActionResult BuyPremium()
        {
            var userid = GetCurrentUserId();

            if(userid == "DefaultUserId")
            {
                string idUser = Request.Cookies["idUser12"]?.Value;
                var checkid1 = db.Orders.Where(s => s.UserID == idUser).ToList();
                return View(checkid1);
            }

            var checkid = db.Orders.Where(s => s.UserID == userid).ToList();

            return View(checkid);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessBuyPremium(string paymentMethod = "", string promoCode = "", decimal discountAmount = 0, string orderId = "")
        {
            try
            {
                var userid = GetCurrentUserId();
                Order orderToProcess = null;

                // Find the specific order to process
                if (!string.IsNullOrEmpty(orderId))
                {
                    // If order ID is provided, use it (this is from the form)
                    orderToProcess = db.Orders.FirstOrDefault(o => o.OrderID == orderId);
                }
                else if (userid == "DefaultUserId")
                {
                    // Handle case for non-logged in users
                    string idUser = Request.Cookies["idUser12"]?.Value;
                    orderToProcess = db.Orders.Where(s => s.UserID == idUser).FirstOrDefault();
                }
                else
                {
                    // Otherwise find the first pending order for the user
                    orderToProcess = db.Orders.Where(s => s.UserID == userid && s.PaymentStatus == "Pending")
                                            .OrderByDescending(o => o.OrderDate)
                                            .FirstOrDefault();
                }

                if (orderToProcess == null)
                {
                    ViewBag.ErrorMessage = "No orders found for processing.";
                    var emptyOrders = new List<Order>();
                    return View("BuyPremium", emptyOrders);
                }

                // Apply promotion code if provided
                decimal totalAmount = orderToProcess.TotalAmount;
                if (!string.IsNullOrEmpty(promoCode))
                {
                    var promotion = db.Promotions.FirstOrDefault(p =>
                        p.PromotionID == promoCode &&
                        p.StartDate <= DateTime.Now &&
                        p.EndDate >= DateTime.Now);

                    if (promotion != null)
                    {
                        // Apply discount
                        orderToProcess.PromotionApplied = promoCode;
                        if (discountAmount <= 0)
                        {
                            // Calculate discount if not provided
                            discountAmount = Math.Round((decimal)((orderToProcess.TotalAmount * promotion.DiscountPercent) / 100), 2);
                        }
                        totalAmount = orderToProcess.TotalAmount - discountAmount;

                        // Save the discount information
                        await db.SaveChangesAsync();
                    }
                }
                if(paymentMethod == "PAY")
                {
                    return RedirectToAction("PayShip", new {id = orderToProcess.OrderID });
                }
                var paymentService = new PaymentService();
                string orderInfo = $"Buy Subscription - {orderToProcess.OrderID}";
                string redirectUrl = Url.Action("ReturnBuy", "Product", new { id = orderToProcess.OrderID }, Request.Url.Scheme);
                string callbackUrl = Url.Action("PremiumFailure", "truyen", null, Request.Url.Scheme);

                string paymentUrl = await paymentService.CreateMoMoPaymentAsync(totalAmount, orderInfo, redirectUrl, callbackUrl);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    throw new Exception("Failed to create MoMo payment URL");
                }

                // Save payment method
                orderToProcess.PaymentMethod = paymentMethod;
                await db.SaveChangesAsync();

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (db.Orders.Any(g => g.PaymentStatus == "Pending"))
                {
                    var failedTransaction = db.Orders.First(g => g.PaymentStatus == "Pending");
                    failedTransaction.PaymentStatus = "Failed";
                    await db.SaveChangesAsync();
                }

                ViewBag.ErrorMessage = "An error occurred while processing your payment. Please try again later.";
                var orders = new List<Order>();
                string currentUserId = GetCurrentUserId();

                if (currentUserId == "DefaultUserId")
                {
                    string idUser = Request.Cookies["idUser12"]?.Value;
                    orders = db.Orders.Where(s => s.UserID == idUser).ToList();
                }
                else
                {
                    orders = db.Orders.Where(s => s.UserID == currentUserId).ToList();
                }

                return View("BuyPremium", orders);
            }
        }
        //Tìm kiếm Prodcut
        public ActionResult Search(string searchString)
        {
            var productList = db.Products.OrderByDescending(x => x.ProductName);

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = (IOrderedQueryable<Product>)productList.Where(x => x.ProductName.Contains(searchString));
            }
            return View("IndexUser", productList.ToList());
        }
        //ReturnIndexAfterBuy
        public ActionResult ReturnBuy(string id)
        {
            var checkd = id;
            if (Request.Cookies["idUser12"] != null)
            {
                HttpCookie userCookie = new HttpCookie("idUser12");
                userCookie.Expires = DateTime.Now.AddDays(-1); // Đặt thời gian hết hạn về quá khứ
                Response.Cookies.Add(userCookie);
            }

            return RedirectToAction("IndexUser");
        }
        //ManageProfile
        public ActionResult profile()
        {
            var manguoidung = GetCurrentUserId();
            var checkid = db.Users.Find(manguoidung);

            if (checkid == null)
            {
                return HttpNotFound();
            }

            return View(checkid);
        }
        [HttpPost]
        public ActionResult profile(User user, HttpPostedFileBase UploadImage)
        {
            var manguoidung = GetCurrentUserId();
            var checkid = db.Users.FirstOrDefault(s => s.UserID == manguoidung);

            if (checkid == null)
            {
                return HttpNotFound();
            }


            if (UploadImage != null && UploadImage.ContentLength > 0)
            {
                string filename = Path.GetFileNameWithoutExtension(UploadImage.FileName);
                string extension = Path.GetExtension(UploadImage.FileName);
                filename = filename + extension;
                string imagePath = "~/Content/image/" + filename;

                checkid.imageUser = imagePath;

                UploadImage.SaveAs(Path.Combine(Server.MapPath("~/Content/image/"), filename));
            }

            db.SaveChanges();

            return RedirectToAction("profile");
        }
         //EditProfile
        public ActionResult EditProfile()
        {
            var userid = GetCurrentUserId();
            var checkid = db.Users.Where(s => s.UserID == userid).FirstOrDefault();
            if(userid == null)
            {
                RedirectToAction("IndexUser");
            }
            return View(checkid);
        }
        [HttpPost]
        public ActionResult EditProfile(User user)
        {
            var userid = GetCurrentUserId();
            var checkid = db.Users.Where(s => s.UserID == userid).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            checkid.Username = user.Username;
            checkid.Phone = user.Phone;
            checkid.Address = user.Address;

            db.SaveChanges();
            return View();
        }
        //ChangePassword
        public ActionResult ChangePassword(string password)
        {
            var id = GetCurrentUserId();
            var checkid = db.Users.Where(s => s.UserID == id).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            return View(checkid);
        }
        [HttpPost]
        public ActionResult ChangePassword(User user, string password)
        {
            var checkid = db.Users.Where(s => s.UserID == user.UserID).FirstOrDefault();

            if(checkid == null)
            {
                return HttpNotFound();
            }

            if (password == user.Password)
            {
                checkid.Password = user.Password;
                db.SaveChanges();
            }

            return View();
        }
        //HistorySearch notview
        public ActionResult HistorySearch()
        {
            return View();
        }
        //HistoryBuyProduct notview
        public ActionResult HistoryBuyProduct()
        {
            var userid = GetCurrentUserId();
            var checkid = db.Orders.Where(s => s.UserID == userid).OrderByDescending(s=>s.number).ToList();

            return View(checkid);
        }
        //SearchCartPhone
        public ActionResult SearchCartPhone(string sdt)
        {
            if (string.IsNullOrEmpty(sdt) )
            {
                return View(new List<Order>());
            }

            var query = from ord in db.Orders
                        join user in db.Users on ord.UserID equals user.UserID
                        where user.Phone == sdt 
                        select ord;

            var results = query.ToList();
            return View(results);
        }
        //DetailsHistoryBuy notview
        public ActionResult DetailsHistory(String id)
        {
            var checkid = db.OrderDetails.Where(s => s.OrderID == id).ToList();
            return View(checkid);
        }
        //cancel order
        public ActionResult CancelOrder(String id)
        {
            var orderCancel = db.Orders.Where(s => s.OrderID == id).FirstOrDefault();
            var detailsOrder = db.OrderDetails.Where(s => s.OrderID == id).ToList();

            if (orderCancel == null)
            {
                return HttpNotFound();
            }

            orderCancel.Status = "Cancel";

            foreach (var orderDetail in detailsOrder)
            {
              
                var product = db.Products.Find(orderDetail.ProductID);

                if (product != null)
                {
                    product.StockQuantity += orderDetail.Quantity;
                }
            }

            db.SaveChanges();
            return RedirectToAction("HistoryBuyProduct");
        }
        //offers
        public ActionResult Offers()
        {
            try
            {
                var userId = GetCurrentUserId();

           
                var user = db.Users.Find(userId);
                if (user == null)
                {
                    return HttpNotFound();
                }

                // Chuyển logic lọc promotions ra khỏi database query để giảm tải cho database
                var now = DateTime.Now;
                var activePromotions = db.Promotions
                    .Where(p =>
                                p.DiscountPercent.HasValue)
                    .AsNoTracking() // Tăng tốc độ truy vấn nếu không cần theo dõi thay đổi
                    .ToList();

                // Sử dụng dictionary để map rank với khoảng discount nhanh hơn switch
                var rankDiscountRanges = new Dictionary<string, (int Min, int Max)>
                {
                    ["silver"] = (4, 9),
                    ["gold"] = (10, 15),
                    ["diamond"] = (16, 19)
                };

                // Lọc promotions nhanh hơn
                var userRank = user.RankUser?.ToLower() ?? string.Empty;
                var eligiblePromotions = rankDiscountRanges.TryGetValue(userRank, out var range)
                    ? activePromotions.Where(p =>
                        p.DiscountPercent >= range.Min &&
                        p.DiscountPercent <= range.Max)
                    .ToList()
                    : new List<Promotion>();

                // Sử dụng ThreadLocal để tạo Random thread-safe
                var selectedPromotion = eligiblePromotions.Any()
                    ? eligiblePromotions[ThreadLocalRandom.Next(eligiblePromotions.Count)]
                    : null;

                return View(new OffersViewModel
                {
                    User = user,
                    SelectedPromotion = selectedPromotion
                });
            }
            catch (Exception)
            {
                // Log exception trước khi chuyển hướng
                return RedirectToAction("Error", "Home");
            }
        }

        // Lớp tiện ích để tạo Random thread-safe
        public static class ThreadLocalRandom
        {
            private static int seed = Environment.TickCount;

            private static readonly ThreadLocal<Random> threadLocal = new ThreadLocal<Random>(() =>
                new Random(Interlocked.Increment(ref seed))
            );

            public static Random Instance => threadLocal.Value;

            public static int Next(int maxValue) => Instance.Next(maxValue);
        }
        // Nhập giảm giá
        [HttpPost]
        public ActionResult ApplyDiscount(string codeDiscount, string orderId)
        {
            // Find the promotion code in the database
            var promotion = db.Promotions.FirstOrDefault(p => p.PromotionID == codeDiscount);

            // Find the order
            var order = db.Orders.FirstOrDefault(o => o.OrderID == orderId);

            // Validate promotion and order
            if (promotion == null)
            {
                return Json(new { success = false, message = "Mã không tồn tại hoặc hết hạn" });
            }

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            // Calculate the discount amount
            decimal discountAmount = (decimal)(order.TotalAmount * (promotion.DiscountPercent / 100m));
            decimal newTotal = order.TotalAmount - discountAmount;

            // Update the order with the new total
            order.TotalAmount = newTotal;
            order.PromotionApplied = codeDiscount; // Track which promotion was used

            // Save changes to database
            db.SaveChanges();

            // Return JSON response with discount information
            return Json(new
            {
                success = true,
                message = "Áp dụng mã giảm giá thành công",
                discountAmount = discountAmount,
                newTotal = newTotal,
                discountPercent = promotion.DiscountPercent
            });
        }
        //Pay
        public ActionResult PayShip(string id)
        {
            var checkid = db.Orders.Where(S => S.OrderID == id).FirstOrDefault();
            if(checkid == null)
            {
                return HttpNotFound();
            }
            return View(checkid);
        }
        [HttpPost]
        public ActionResult PayShip(Order order)
        {
            var checkid = db.Orders.Where(S => S.OrderID == order.OrderID).FirstOrDefault();
            if (checkid == null)
            {
                return HttpNotFound();
            }
            checkid.PaymentMethod = "Pay";
            checkid.ShippingAddress = order.ShippingAddress;
            db.SaveChanges();
            return RedirectToAction("IndexUser");
        }
        // lấy ID người dùng
        private string GetCurrentUserId()
        {
            return Session["id"] as string ?? "DefaultUserId";
        }
    }
}