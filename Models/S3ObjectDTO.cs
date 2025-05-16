namespace S3.Demo.API.Models
{
    public class S3ObjectDTO
    {
        public string? Name { get; set; }
        public string? PresignedUrl { get; set; }
        public string? Key { get; set; }
        public string? BucketName { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

}
