using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using ApiAngular.Models;
using ApiAngular.Models.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ApiAngular.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly Fualumni_demoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        string bucketName = "fualumni";
        public string Key { get; private set; }
        private const string _tableName = "client-storeData";

        public PostController(HttpClient httpClient, Fualumni_demoContext context, IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client)
        {
            _httpClient = httpClient;
            _context = context;
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
        }

        public class AddPostRequestDTO
        {
            public IFormFile File { get; set; }
            public string Content { get; set; }
            public int UserId { get; set; }
            public string SystemToken { get; set; }
        }

        [HttpPost("create-post")]
        public async Task<IActionResult> CreatePostAsync([FromForm] AddPostRequestDTO request)
        {
            var externalApiUrl = "http://fal-dev.eba-55qpmvbp.ap-southeast-1.elasticbeanstalk.com/api/detect";

            // Validate the uploaded file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (!request.File.ContentType.StartsWith("image/"))
            {
                return BadRequest("Only image files are supported.");
            }

            var imageName = Path.GetFileNameWithoutExtension(request.File.FileName);
            var originalFileName = request.File.FileName;
            var createdAt = DateTime.UtcNow;

            var post = new Post
            {
                UserId = request.UserId,
                Content = request.Content,
                ImageName = imageName,
                CreatedAt = createdAt
            };

            // Save the file to a temporary location
            var tempFilePath = Path.Combine(Path.GetTempPath(), request.File.FileName);
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            try
            {
                // Read the file content into memory
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);

                // Prepare the request to the external API
                using var externalApiRequest = new HttpRequestMessage(HttpMethod.Post, externalApiUrl);
                using var externalApiContent = new MultipartFormDataContent();

                // Create StreamContent from byte array
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);
                externalApiContent.Add(fileContent, "file", request.File.FileName);

                // Set the Authorization header
                externalApiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.SystemToken);

                // Attach the content to the request
                externalApiRequest.Content = externalApiContent;

                // Send the request
                var response = await _httpClient.SendAsync(externalApiRequest);

                // Handle the response
                if (!response.IsSuccessStatusCode)
                {
                    return Unauthorized("Server error: Unable to authenticate with external service.");
                }

                // Store the post in the database (optional)
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
            }
            finally
            {
                // Ensure the temporary file is deleted after the request completes
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch (IOException ioEx)
                    {
                        // Log the exception or handle it as needed
                        Console.WriteLine($"Error deleting temporary file: {ioEx.Message}");
                    }
                }
            }


            // Return a success response
            return Ok(new
            {
                Content = request.Content,
                CreatedAt = createdAt
            });
        }
        [HttpGet("Detail")]
        public async Task<IActionResult> Detail(int postId)
        {
            var post = _context.Posts.FirstOrDefault(p => p.PostId == postId);
            if(post == null)
            {
                return BadRequest();
            }

            NotificationDetailResponse ndr = new NotificationDetailResponse();

            var response = await FindExactMatchByBaseFileNameAsync(post.ImageName);
            ProcessResponse(response, ndr);
            await GetUrlImageFromS3(ndr);

            return Ok(ndr);
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

        private async Task<ScanResponse> FindExactMatchByBaseFileNameAsync(string baseFileName)
        {
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "contains(FileName, :v_partialFileName)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_partialFileName", new AttributeValue { S = baseFileName } }
        }
            };

            var scanResponse = await _dynamoDbClient.ScanAsync(scanRequest);

           

            return scanResponse;
        }

        private void ProcessResponse(ScanResponse response, NotificationDetailResponse ndr)
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
