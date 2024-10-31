namespace ApiAngular.Models
{
    public class NotificationDetailResponse
    {
        public string? PictureUrl { get;  set; }
        public int? ImageWidth { get;  set; }
        public int? ImageHeight { get; set; }
        public List<FaceRecognitionResponse>? RegisteredFaces { get; set; } = new List<FaceRecognitionResponse>();
        public List<FaceRecognitionResponse>? UnregisteredFaces { get; set; } = new List<FaceRecognitionResponse>();
    }
}
