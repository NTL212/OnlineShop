using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace OnlineShop.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class FirebaseController : ControllerBase
	{
		private readonly IFirebaseConfig config = new FirebaseConfig
		{
			AuthSecret = "5q94n5M3JkB6zq1ZqiKBPbkgGC4swED8cVLbgbBW",
			BasePath = "https://onlineshop-931f7-default-rtdb.asia-southeast1.firebasedatabase.app/"
		};
		private readonly IFirebaseClient _client;
		public FirebaseController()
		{
			_client = new FirebaseClient(config);
		}
		public class MessageModel
		{
			public string UserId { get; set; }
			public string SellerId { get; set; }
			public string Message { get; set; }
			public string SenderId { get; set; }
		}
            public class PeerModel
            {
                public string UserId { get; set; }
                public string PeerId { get; set; }
            }

            [HttpPost("SetMessage")]
        public async Task<IActionResult> SetMessageToFirebase([FromBody] MessageModel messageModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string nodeMessage = $"chats/chat_sellerId_{messageModel.SellerId}_userId_{messageModel.UserId}/messages";
                var chatData = new
                {
                    senderId = messageModel.SenderId,
                    content = messageModel.Message,
                    timeSend = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                PushResponse response = await _client.PushAsync(nodeMessage, chatData);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return Ok("Chat created successfully");
                }
                else
                {
                    return BadRequest("Failed to create chat. Status code: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpGet("GetMessages")]
		public async Task<ActionResult<IEnumerable<object>>> GetMessagesFromFirebase(string userId, string sellerId)
		{
			try
			{
				string nodeMessage = "chats/chat_sellerId_" + sellerId + "_userId_" + userId + "/messages";
				FirebaseResponse response = await _client.GetAsync(nodeMessage);

				if (response.Body != "null")
				{
					Dictionary<string, object> messageDict = response.ResultAs<Dictionary<string, object>>();
					List<object> messages = new List<object>();
					List<JObject> messages2 = new List<JObject>(); // Sử dụng List<JObject> thay vì List<object>

					foreach (var pair in messageDict)
					{
						// Chuyển đổi từ JSON sang đối tượng JObject
						JObject messageJson2 = JObject.Parse(pair.Value.ToString());
						messages2.Add(messageJson2);
					}
					// Sắp xếp danh sách tin nhắn theo trường 'timeSend'
					messages2 = messages2.OrderBy(m => DateTime.Parse((string)m["timeSend"])).ToList();
					foreach (var obj in messages2)
					{
						messages.Add(obj.ToString());

					}
					return messages;
				}
				else
				{
					return NotFound("No messages found");
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, "Internal server error: " + ex.Message);
			}
		}
		[HttpGet("GetChatsBySellerId")]
		public async Task<ActionResult<IEnumerable<object>>> GetChatsBySellerId(string sellerId)
		{
			try
			{
				// Tạo đường dẫn tới node chats
				string nodeChats = "chats";

				// Truy vấn dữ liệu từ Firebase
				FirebaseResponse response = await _client.GetAsync(nodeChats);

				// Kiểm tra xem phản hồi có dữ liệu không
				if (response.Body != "null")
				{
					// Chuyển đổi dữ liệu phản hồi thành một đối tượng Dictionary
					Dictionary<string, object> chatsDict = response.ResultAs<Dictionary<string, object>>();
					List<object> chats = new List<object>();
					// Tạo danh sách để lưu trữ tin nhắn thỏa mãn điều kiện
					string lastMessage = "";
					// Lặp qua từng cặp key-value trong Dictionary
					foreach (var pair in chatsDict)
					{
						// Kiểm tra xem key của node chat có chứa sellerId không
						if (pair.Key.Contains("sellerId_" + sellerId))
						{
							// Lấy ra danh sách các tin nhắn từ node chat
							var chatMessages = pair.Value.ToString();
							
						
							// Phân tích chuỗi JSON
							JObject jsonObject = JObject.Parse(chatMessages);

							// Lấy danh sách tin nhắn từ thuộc tính "messages"
							var messages = jsonObject["messages"];

							// Nếu danh sách tin nhắn không rỗng, sắp xếp theo thời gian gửi và lấy tin nhắn cuối cùng
							if (messages != null)
							{
								var sortedMessages = messages.Values()
									.OrderByDescending(m => DateTime.Parse((string)m["timeSend"]))
									.ToList();

								if (sortedMessages.Any())
								{
									if (sortedMessages.First()["senderId"].ToString() == sellerId)
									{
										lastMessage = sortedMessages.ElementAt(0).ToString();
									}
									
									var mess = new
									{
										userId = ExtractUserId(pair.Key),
										message = lastMessage
									};
									chats.Add(mess);
								}
								else
								{
									return NotFound("No message found");
								}
							}
						}
					}
					return Ok(chats);
				}
				else
				{
					return NotFound("No chats found");
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, "Internal server error: " + ex.Message);
			}
		}
        [HttpPost("SetPeerId")]
        public async Task<IActionResult> SetPeerIdToFirebase([FromBody] PeerModel peerModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                string nodePeer = $"peer/userId_{peerModel.UserId}/peerId";
                SetResponse response = await _client.SetAsync(nodePeer, peerModel.PeerId);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return Ok("peerId created successfully");
                }
                else
                {
                    return BadRequest("Failed to create chat. Status code: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpGet("GetPeerId/{userId}")]
        public async Task<IActionResult> GetPeerIdFromFirebase(int userId)
        {
            try
            {
                string nodePeer = $"peer/userId_{userId}/peerId";
                var snapshot = await _client.GetAsync(nodePeer);

                if (snapshot.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var peerId = snapshot.Body.ToString();
                    string normalizedPeerId = peerId.Replace("\\", "").Replace("\"", "");
                    return Ok(normalizedPeerId);
                }
                else
                {
                    return NotFound("PeerId not found for userId: " + userId);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        private string ExtractUserId(string inputString)
		{
			string[] parts = inputString.Split('_');
			return parts[parts.Length - 1];
		}
		private string timeSend()
		{
			DateTime currentTime = DateTime.Now;
			string dateString = currentTime.ToString("yyyy-MM-dd"); // Ngày tháng năm
			string timeString = currentTime.ToString("HH:mm:ss"); // Giờ phút giây
			return dateString + " " + timeString;
		}
	}
}
