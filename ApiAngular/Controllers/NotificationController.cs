using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using ApiAngular.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Amazon.S3.Model;

namespace ApiAngular.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        string bucketName = "fualumni";
        private const string _tableName = "client-storeData";

        public string Key { get; private set; }

        public NotificationController(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            List<ClientStoredData> ListData = new List<ClientStoredData>();

            var tableName = "client-storeData";

            var request = new ScanRequest
            {
                TableName = tableName
            };

            var response = await _dynamoDbClient.ScanAsync(request);

            foreach (var item in response.Items)
            {
                var file = new ClientStoredData
                {
                    FileName = item.ContainsKey("FileName") ? item["FileName"].S : string.Empty,
                    CreateDate = item.ContainsKey("CreateDate") ? item["CreateDate"].S : string.Empty
                };

                ListData.Add(file);
            }

            return Ok(ListData);
        }

        private async Task GetUrlImageFromS3(NotificationDetailResponse ndr)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = Key,
                Expires = DateTime.UtcNow.AddMinutes(30)
            };
            ndr.PictureUrl = await _s3Client.GetPreSignedURLAsync(request);
        }


        [HttpGet]
        [Route("detail")]
        public async Task<IActionResult> Detail([FromQuery] string fileName)
        {
            NotificationDetailResponse ndr = new NotificationDetailResponse();

            var response = await QueryDynamoDBAsync(fileName);
            ProcessResponse(response, ndr);
            await GetUrlImageFromS3(ndr);

            return Ok(ndr);
        }

        private async Task<QueryResponse> QueryDynamoDBAsync(string fileName)
        {
            var queryRequest = new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "FileName = :v_fileName",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_fileName", new AttributeValue { S = fileName } }
                }
            };

            return await _dynamoDbClient.QueryAsync(queryRequest);
        }

        private void ProcessResponse(QueryResponse response, NotificationDetailResponse ndr)
        {
            if (response.Items.Count == 1)
            {
                var item = response.Items[0];

                if (item.TryGetValue("Data", out var dataValue))
                {
                    // Deserialize the Data column into the specified type
                    var data = JsonConvert.DeserializeObject<FaceDetectionResult>(dataValue.S);
                    ndr.ImageWidth = data.Width ?? 0;
                    ndr.ImageHeight = data.Height ?? 0;
                    Key = data.Key;
                    ndr.RegisteredFaces = data.RegisteredFaces ?? new List<FaceRecognitionResponse>();
                    ndr.UnregisteredFaces = data.UnregisteredFaces ?? new List<FaceRecognitionResponse>();
                }
            }
            else if (response.Items.Count == 0)
            {
                throw new Exception("No item found for the given fileName.");
            }
            else
            {
                throw new Exception("Multiple items found for the given fileName. Please ensure fileName is unique.");
            }
        }
    }
}
