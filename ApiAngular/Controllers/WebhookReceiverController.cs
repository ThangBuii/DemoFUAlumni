using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using ApiAngular.Models;
using ApiAngular.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace ApiAngular.Controllers
{
    public class WebhookReceiverController : ControllerBase
    {
        private readonly string _secretKey;
        private readonly IAmazonDynamoDB _dynamoDBService;


        // Inject IConfiguration trực tiếp vào constructor
        public WebhookReceiverController(IConfiguration configuration, IAmazonDynamoDB dynamoDBService)
        {
            // Lấy giá trị SecretKey từ appsettings.json
            _secretKey = "your-secret-key";
            _dynamoDBService = dynamoDBService;
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

                // Trả về phản hồi cho client
                return Ok(payloadString);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
