using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace S3.Demo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BucketController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        public BucketController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }
        [HttpGet]
        public async Task<IActionResult> GetBucketsAsync()
        {
            var response = await _s3Client.ListBucketsAsync();
            return Ok(response.Buckets);
        }
        [HttpPost]
        public async Task<IActionResult> CreateBucketAsync(string bucketName)
        {
            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if(bucketExists)
            {
                // Return a 400 Bad Request if the bucket already exists
                // You can also return a different status code or message if you prefer
                // For example, you could return a 409 Conflict status code
                // return Conflict($"Bucket {bucketName} already exists.");
                    return BadRequest($"Bucket {bucketName} already exists.");
            }

            //var request = new PutBucketRequest
            //{
            //    BucketName = bucketName,
            //    UseClientRegion = true
            //};
            //var response = await _s3Client.PutBucketAsync(request);
            await _s3Client.PutBucketAsync(bucketName);
            return Created("buckets", $"bucket {bucketName} created.");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBucketAsync(string bucketName)
        {
            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if(!bucketExists)
            {
                return NotFound($"Bucket {bucketName} does not exist.");
            }
            await _s3Client.DeleteBucketAsync(bucketName);
            return Ok($"Bucket {bucketName} deleted.");
        }
    }
}
