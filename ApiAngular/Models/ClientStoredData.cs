namespace ApiAngular.Models
{
    public class ClientStoredData
    {
        public string? FileName { get; set; } = null;
        public FaceDetectionResult? Data { get; set; } = null;
        public string CreateDate { get; set; } = null!;
    }
}
