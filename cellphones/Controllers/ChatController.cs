using cellphones.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Configuration;
using System.Web.Caching;
using System.Net.Http.Headers;

namespace cellphones.Controllers
{
    public class ChatController : Controller
    {
        // GET: Chat 
        SGTechEntities db = new SGTechEntities();
        // Constant for chatbot ID
        private const string CHATBOT_ID = "chatbot001";
        // API key for OpenAI - stored in web.config
        private readonly string OPENAI_API_KEY = ConfigurationManager.AppSettings["OpenAIApiKey"];
        // Cache Key for conversation contexts
        private const string CONVERSATION_CONTEXT_CACHE_KEY = "SGTech_ConversationContexts";
        // Cache duration in hours
        private const int CONTEXT_CACHE_DURATION = 24;
        // Maximum conversation history to keep
        private const int MAX_CONVERSATION_HISTORY = 10;

        // Hiển thị danh sách người dùng để chat
        public ActionResult Index()
        {
            var currentUserId = GetCurrentUserId();
            var users = db.Users
                .Where(u => u.UserID != currentUserId && u.Role == "Customer")
                .ToList();

            // Add chatbot to the list of chat options
            ViewBag.ShowChatbot = true;

            return View(users);
        }

        // View for chatting with the chatbot
        public ActionResult ChatBot()
        {
            var currentUserId = GetCurrentUserId();
            var messages = db.Messages
                .Where(m =>
                    (m.SenderID == currentUserId && m.ReceiverID == CHATBOT_ID) ||
                    (m.SenderID == CHATBOT_ID && m.ReceiverID == currentUserId))
                .OrderBy(m => m.Timestamp)
                .ToList();

            ViewBag.ReceiverId = CHATBOT_ID;
            ViewBag.ReceiverName = "SGTech Assistant";

            return View("ChatBot", messages);
        }

        // Send message to chatbot with AJAX support
        [HttpPost]
        public async Task<ActionResult> SendChatBotMessage(string content)
        {
            var currentUserId = GetCurrentUserId();

            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { error = "Vui lòng nhập nội dung tin nhắn" });
                }

                // Save user message
                var userMessage = new Message
                {
                    MessageID = Guid.NewGuid().ToString("N").Substring(0, 8),
                    SenderID = currentUserId,
                    ReceiverID = CHATBOT_ID,
                    Content = content,
                    Timestamp = DateTime.Now
                };

                db.Messages.Add(userMessage);
                db.SaveChanges();

                // Update conversation context
                UpdateConversationContext(currentUserId, "user", content);

                // Generate and save chatbot response
                var botResponse = await GenerateChatbotResponse(currentUserId, content);

                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        userMessage = new
                        {
                            content = userMessage.Content,
                            timestamp = userMessage.Timestamp
                        },
                        botResponse = new
                        {
                            content = botResponse.Content,
                            timestamp = botResponse.Timestamp
                        }
                    });
                }

                return RedirectToAction("ChatBot");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error in SendChatBotMessage: {ex.Message}");

                // Return error message if it's an AJAX request
                if (Request.IsAjaxRequest())
                {
                    return Json(new { error = "Đã xảy ra lỗi khi xử lý tin nhắn" });
                }
                // Otherwise redirect with error message
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xử lý tin nhắn";
                return RedirectToAction("ChatBot");
            }
        }

        // Helper method to generate chatbot responses
        private async Task<Message> GenerateChatbotResponse(string userId, string userMessage)
        {
            string responseContent;

            try
            {
                // First try rule-based responses
                responseContent = GetRuleBasedResponse(userMessage);

                // If rule-based response is empty, use AI
                if (string.IsNullOrEmpty(responseContent))
                {
                    responseContent = await GetAIResponse(userId, userMessage);
                }

                // Add response to conversation context
                UpdateConversationContext(userId, "assistant", responseContent);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error generating chatbot response: {ex.Message}");
                responseContent = "Xin lỗi, tôi đang gặp khó khăn trong việc xử lý câu hỏi của bạn. Vui lòng thử lại sau hoặc liên hệ nhân viên tư vấn để được hỗ trợ.";
            }

            // Create and save the bot message
            var botMessage = new Message
            {
                MessageID = Guid.NewGuid().ToString("N").Substring(0, 8),
                SenderID = CHATBOT_ID,
                ReceiverID = userId,
                Content = responseContent,
                Timestamp = DateTime.Now.AddSeconds(1) // Add a slight delay for natural feel
            };

            db.Messages.Add(botMessage);
            db.SaveChanges();

            return botMessage;
        }

        // Enhanced rule-based response system
        private string GetRuleBasedResponse(string userMessage)
        {
            // Convert input to lowercase for case-insensitive matching
            var normalizedInput = userMessage.ToLower().Trim();

            // Dictionary of patterns and responses
            var responses = new Dictionary<string[], string>
            {
                // Greetings
                { new[] { "hello", "hi", "xin chào", "chào", "hey" },
                  "Xin chào! Tôi là trợ lý của SGTech. Tôi có thể giúp gì cho bạn?" },
                
                // Identity
                { new[] { "bạn là ai", "bạn tên gì", "bạn là gì" },
                  "Tôi là trợ lý ảo của SGTech, có thể hỗ trợ bạn về thông tin sản phẩm, khuyến mãi, và các vấn đề khác." },
                
                // Prices
                { new[] { "giá", "bao nhiêu", "chi phí" },
                  "Để biết thông tin giá cả chi tiết của sản phẩm, vui lòng cho tôi biết bạn quan tâm đến sản phẩm nào?" },
                
                // Phone brands
                { new[] { "iphone", "apple" },
                  "Chúng tôi có nhiều mẫu iPhone từ iPhone 12 đến iPhone 15 Pro Max. Bạn quan tâm đến mẫu nào?" },

                { new[] { "samsung", "galaxy" },
                  "Samsung Galaxy là dòng sản phẩm bán chạy của chúng tôi. Bạn có thể tìm thấy Galaxy S24, Z Fold, và nhiều mẫu khác trên trang web." },

                { new[] { "xiaomi", "redmi" },
                  "Chúng tôi có đầy đủ các sản phẩm Xiaomi với giá cả cạnh tranh. Bạn đang tìm kiếm mẫu nào?" },

                { new[] { "oppo", "realme", "vivo" },
                  "SGTech cung cấp nhiều dòng sản phẩm Oppo, Realme và Vivo. Bạn muốn tìm mẫu nào?" },

                { new[] { "asus", "rog phone" },
                  "Asus ROG Phone là dòng điện thoại gaming mạnh mẽ. Bạn có muốn biết thêm thông tin về ROG Phone không?" },
                
                // Ordering
                { new[] { "đặt hàng", "mua", "order" },
                  "Bạn có thể đặt hàng trực tuyến qua website hoặc đến trực tiếp cửa hàng của chúng tôi. Nếu cần hỗ trợ, vui lòng liên hệ nhân viên tư vấn." },

                { new[] { "thanh toán", "cách trả tiền", "payment" },
                  "Chúng tôi hỗ trợ thanh toán qua tiền mặt, chuyển khoản ngân hàng, và thẻ tín dụng. Bạn muốn biết thêm về phương thức nào?" },
                
                // Promotions
                { new[] { "khuyến mãi", "giảm giá", "ưu đãi", "sale" },
                  "SGTech đang có chương trình giảm giá 10% cho Samsung và 15% cho iPhone khi thanh toán qua ngân hàng đối tác." },

                { new[] { "trả góp", "mua trả góp", "installment" },
                  "Chúng tôi có hỗ trợ trả góp 0% qua thẻ tín dụng. Vui lòng liên hệ nhân viên để biết thêm chi tiết." },
                
                // Warranty
                { new[] { "bảo hành", "hư", "lỗi", "warranty" },
                  "Tất cả sản phẩm đều được bảo hành chính hãng 12 tháng. Một số sản phẩm cao cấp có thời gian bảo hành lên đến 24 tháng." },
                
                // Shipping
                { new[] { "giao hàng", "vận chuyển", "ship" },
                  "Chúng tôi giao hàng miễn phí cho đơn hàng trên 500.000đ. Thời gian giao hàng từ 1-3 ngày tùy khu vực." },
                
                // Returns
                { new[] { "đổi trả", "trả hàng", "return" },
                  "Bạn có thể đổi trả trong vòng 7 ngày nếu sản phẩm bị lỗi do nhà sản xuất. Vui lòng giữ hóa đơn và bao bì sản phẩm." },
                
                // Store info
                { new[] { "cửa hàng", "địa chỉ", "store", "location" },
                  "SGTech có nhiều chi nhánh trên toàn quốc. Bạn đang muốn tìm cửa hàng ở khu vực nào?" },

                { new[] { "giờ mở cửa", "mấy giờ đóng cửa", "open hours" },
                  "SGTech mở cửa từ 8h sáng đến 10h tối tất cả các ngày trong tuần." },
                
                // Thank you
                { new[] { "cảm ơn", "thank" },
                  "Rất vui được hỗ trợ bạn! Nếu có thắc mắc gì thêm, đừng ngần ngại hỏi nhé." },
                
                // Goodbye
                { new[] { "tạm biệt", "bye", "goodbye" },
                  "Tạm biệt bạn! Hẹn gặp lại và cảm ơn bạn đã quan tâm đến SGTech." },
                
                // Specific products
                { new[] { "samsung a05" },
                  "Samsung Galaxy A05 có giá 2.990.000đ. Điện thoại có màn hình 6.7 inch, pin 5000mAh, camera chính 50MP. Bạn muốn biết thêm chi tiết không?" },

                { new[] { "iphone 12" },
                  "iPhone 12 hiện có giá từ 12.990.000đ. Máy có màn hình 6.1 inch OLED, chip A14 Bionic, camera kép 12MP. Bạn quan tâm đến màu nào?" },

                { new[] { "iphone 15" },
                  "iPhone 15 hiện có giá từ 21.990.000đ. Máy có màn hình 6.1 inch OLED, chip A16 Bionic, camera chính 48MP. Bạn quan tâm đến phiên bản bộ nhớ nào?" }
            };

            // Product price section - add more products as needed
            var productPrices = new Dictionary<string, string>
            {
                { "samsung a05", "Samsung Galaxy A05 có giá 2.990.000đ." },
                { "samsung a53", "Samsung Galaxy A53 có giá 9.990.000đ." },
                { "iphone 12", "iPhone 12 có giá từ 12.990.000đ." },
                { "iphone 15", "iPhone 15 có giá từ 21.990.000đ." },
                { "iphone 16 pro", "iPhone 16 Pro có giá từ 29.990.000đ." }
            };

            // Check product prices first
            foreach (var product in productPrices)
            {
                if (normalizedInput.Contains(product.Key))
                {
                    return product.Value + " Nếu bạn muốn biết thêm thông tin về sản phẩm này, vui lòng liên hệ tư vấn viên.";
                }
            }

            // Check other pattern matches
            foreach (var pattern in responses)
            {
                if (pattern.Key.Any(keyword => normalizedInput.Contains(keyword)))
                {
                    return pattern.Value;
                }
            }

            // If no pattern matches, return null to use AI
            return null;
        }

        // Enhanced AI response
        private async Task<string> GetAIResponse(string userId, string userMessage)
        {
            using (var client = new HttpClient())
            {
                // Set authorization header correctly
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OPENAI_API_KEY);

                // Set timeout to avoid long waiting times
                client.Timeout = TimeSpan.FromSeconds(30);

                // Get conversation history
                var conversationContext = GetConversationContext(userId);
                var messages = new List<object>
        {
            new
            {
                role = "system",
                content = "Bạn là trợ lý ảo của SGTech, một cửa hàng điện thoại di động. " +
                         "Bạn có thể giúp khách hàng tìm kiếm thông tin về điện thoại, phụ kiện, và các dịch vụ của cửa hàng. " +
                         "Bạn cần trả lời ngắn gọn, lịch sự và hữu ích. " +
                         "Luôn cung cấp thông tin chính xác và hướng dẫn khách hàng liên hệ với tư vấn viên nếu họ cần thông tin chi tiết hơn. " +
                         "Tránh đưa ra các câu trả lời quá dài hoặc quá phức tạp. " +
                         "Các sản phẩm nổi bật của cửa hàng: iPhone 12-15, Samsung Galaxy A và S series, Xiaomi Redmi Note series."
            }
        };

                // Add conversation history
                foreach (var message in conversationContext.RecentMessages)
                {
                    messages.Add(new
                    {
                        role = message.Role,
                        content = message.Content
                    });
                }

                // Add current user message
                messages.Add(new { role = "user", content = userMessage });

                // Prepare the request data
                var requestData = new
                {
                    model = "gpt-4o-mini",
                    messages,
                    max_tokens = 200,
                    temperature = 0.7
                };

                // Convert to JSON
                var jsonContent = JsonConvert.SerializeObject(requestData);
                System.Diagnostics.Debug.WriteLine($"OpenAI request: {jsonContent}");

                var content = new StringContent(
                    jsonContent,
                    Encoding.UTF8,
                    "application/json"
                );

                try
                {
                    // Send request to OpenAI API
                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

                    // Always log the response for debugging
                    var responseString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"OpenAI API Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"OpenAI API Response: {responseString}");

                    // Check response
                    if (response.IsSuccessStatusCode)
                    {
                        dynamic responseObject = JsonConvert.DeserializeObject(responseString);
                        return responseObject.choices[0].message.content;
                    }
                    else
                    {
                        // Handle error
                        System.Diagnostics.Debug.WriteLine($"OpenAI API Error: {response.StatusCode}, {responseString}");

                        // If unauthorized, provide a specific error message
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            return "Xin lỗi, không thể kết nối với dịch vụ trợ lý ảo do lỗi xác thực. Vui lòng liên hệ với quản trị viên.";
                        }

                        // Return a friendly error message
                        return "Xin lỗi, tôi đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau hoặc liên hệ với nhân viên hỗ trợ.";
                    }
                }
                catch (TaskCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("OpenAI API request timed out");
                    return "Xin lỗi, yêu cầu của bạn đã hết thời gian chờ. Vui lòng thử lại sau.";
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenAI API Network Error: {ex.Message}");
                    return "Xin lỗi, không thể kết nối với dịch vụ trợ lý ảo. Vui lòng kiểm tra kết nối mạng và thử lại.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenAI API Exception: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    return "Xin lỗi, tôi không thể xử lý yêu cầu của bạn. Vui lòng thử lại sau.";
                }
            }
        }

        // Get the current user ID from session
        private string GetCurrentUserId()
        {
            return Session["id"] as string ?? "DefaultUserId";
        }

        // Enhanced conversation context class
        private class ConversationMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class ConversationContext
        {
            public string UserId { get; set; }
            public List<ConversationMessage> RecentMessages { get; set; }
            public DateTime LastInteraction { get; set; }
            public string CurrentTopic { get; set; }

            public ConversationContext()
            {
                RecentMessages = new List<ConversationMessage>();
                LastInteraction = DateTime.Now;
                CurrentTopic = "general";
            }
        }

        // Get conversation context with cache support
        private ConversationContext GetConversationContext(string userId)
        {
            // Try to get conversation contexts from cache
            var contexts = HttpContext.Cache[CONVERSATION_CONTEXT_CACHE_KEY] as Dictionary<string, ConversationContext>;

            // If not in cache, create a new dictionary
            if (contexts == null)
            {
                contexts = new Dictionary<string, ConversationContext>();

                // Add to cache with expiration
                HttpContext.Cache.Insert(
                    CONVERSATION_CONTEXT_CACHE_KEY,
                    contexts,
                    null,
                    Cache.NoAbsoluteExpiration,
                    TimeSpan.FromHours(CONTEXT_CACHE_DURATION)
                );
            }

            // Get or create user context
            if (!contexts.ContainsKey(userId))
            {
                contexts[userId] = new ConversationContext
                {
                    UserId = userId
                };
            }

            // Update last interaction time
            contexts[userId].LastInteraction = DateTime.Now;

            return contexts[userId];
        }

        // Update conversation context
        private void UpdateConversationContext(string userId, string role, string content)
        {
            var context = GetConversationContext(userId);

            // Add new message
            context.RecentMessages.Add(new ConversationMessage
            {
                Role = role,
                Content = content,
                Timestamp = DateTime.Now
            });

            // Keep only the most recent messages
            if (context.RecentMessages.Count > MAX_CONVERSATION_HISTORY)
            {
                context.RecentMessages.RemoveAt(0);
            }

            // Update current topic based on content
            if (content.ToLower().Contains("iphone") || content.ToLower().Contains("apple"))
            {
                context.CurrentTopic = "apple";
            }
            else if (content.ToLower().Contains("samsung") || content.ToLower().Contains("galaxy"))
            {
                context.CurrentTopic = "samsung";
            }
            else if (content.ToLower().Contains("xiaomi") || content.ToLower().Contains("redmi"))
            {
                context.CurrentTopic = "xiaomi";
            }
        }

        // Cleanup old conversation contexts
        private void CleanupOldContexts()
        {
            var contexts = HttpContext.Cache[CONVERSATION_CONTEXT_CACHE_KEY] as Dictionary<string, ConversationContext>;
            if (contexts == null) return;

            var now = DateTime.Now;
            var keysToRemove = contexts
                .Where(kvp => (now - kvp.Value.LastInteraction).TotalHours > CONTEXT_CACHE_DURATION)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                contexts.Remove(key);
            }
        }

        // Other existing methods...

        // Scheduled task to clean up old contexts
        // You can call this method from Application_Start or via a scheduled task
        public static void InitializeContextCleanup()
        {
            // Schedule cleanup every 12 hours
            System.Threading.Timer cleanupTimer = new System.Threading.Timer(
                e => {
                    var controller = new ChatController();
                    controller.CleanupOldContexts();
                },
                null,
                TimeSpan.Zero,
                TimeSpan.FromHours(12)
            );
        }
    }
}