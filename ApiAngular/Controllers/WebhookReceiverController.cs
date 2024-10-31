using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using ApiAngular.Models;
using ApiAngular.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using ApiAngular.Models.Data;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;

namespace ApiAngular.Controllers
{
    public class WebhookReceiverController : ControllerBase
    {
        private readonly string _secretKey;
        private readonly IAmazonDynamoDB _dynamoDBService;
        private readonly Fualumni_demoContext _context;


        // Inject IConfiguration trực tiếp vào constructor
        public WebhookReceiverController(IConfiguration configuration, IAmazonDynamoDB dynamoDBService, Fualumni_demoContext context)
        {
            // Lấy giá trị SecretKey từ appsettings.json
            _secretKey = "your-secret-key";
            _dynamoDBService = dynamoDBService;
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("ReceiveData")]
        public async Task<IActionResult> ReceiveData([FromHeader(Name = "X-Signature")] string signature, [FromBody] FaceDetectionResult payload)
        {
            try
            {
                // Tạo chữ ký HMAC từ payload
                var payloadString = System.Text.Json.JsonSerializer.Serialize(payload);
                var computedSignature = GenerateHMAC(payloadString, _secretKey);

                // Kiểm tra chữ ký
                if (signature != computedSignature)
                {
                    return Unauthorized("Chữ ký không hợp lệ.");
                }

                var dictionary = CreateDictionaryClientStoreResult(payload.FileName, payloadString);

                await CreateNewRecord("client-storeData", dictionary);
                await AddPostNoti(payload);

                // Trả về phản hồi cho client
                return Ok(payloadString);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task AddPostNoti(FaceDetectionResult payload)
        {
            if (payload == null || payload.RegisteredFaces == null || !payload.RegisteredFaces.Any())
            {
                // Log or handle the case where the payload or registered faces are invalid
                return;
            }

            // Find the post using the image name from the payload
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => payload.FileName.Contains(p.ImageName));

            // Check if the post exists
            if (post == null)
            {
                // Log or handle the case where the post is not found
                return;
            }

            try
            {
                var tags = new List<Models.Data.Tag>();
                var notifications = new List<Notification>();

                foreach (var face in payload.RegisteredFaces)
                {
                    // Ensure the UserId is valid before parsing
                    if (int.TryParse(face.UserId, out var userId))
                    {
                        var tag = new Models.Data.Tag
                        {
                            UserId = userId,
                            PostId = post.PostId,
                            TaggedAt = DateTime.Now,
                        };

                        var notification = new Notification
                        {
                            UserId = userId,
                            PostId = post.PostId,
                            CreatedAt = DateTime.Now,
                            Message = "You just got tagged in this post"
                        };

                        tags.Add(tag);
                        notifications.Add(notification);
                    }
                    else
                    {
                        // Log or handle the case where the UserId is invalid
                        // Optionally continue processing other faces
                    }
                }

                // Add tags and notifications in bulk
                if (tags.Any())
                {
                    await _context.Tags.AddRangeAsync(tags);
                }

                if (notifications.Any())
                {
                    await _context.Notifications.AddRangeAsync(notifications);
                }

                // Save changes asynchronously
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the exception (consider using a logging framework)
                Console.WriteLine($"An error occurred while adding notifications: {ex.Message}");
                // Handle the exception accordingly, maybe rethrow or log it
            }
        }

        private static string GenerateHMAC(string payload, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private Dictionary<string, AttributeValue> CreateDictionaryClientStoreResult(string fileName, string data)
        {
            return new Dictionary<string, AttributeValue>
               {
                   {
                       "FileName", new AttributeValue
                       {
                           S = fileName
                       }
                   },
                   {
                       "Data", new AttributeValue
                       {
                           S = data
                       }
                   },
                   {
                       "CreateDate", new AttributeValue
                       {
                           S = DateTimeUtils.GetDateTimeVietNamNow()
                       }
                   }
               };
        }

        private async Task CreateNewRecord(string tableName, Dictionary<string, AttributeValue> dictionary)
        {
            var request = new Amazon.DynamoDBv2.Model.PutItemRequest
            {
                TableName = tableName,
                Item = dictionary,
            };

            await _dynamoDBService.PutItemAsync(request);
        }
    }
}
