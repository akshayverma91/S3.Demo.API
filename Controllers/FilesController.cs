using Amazon.S3;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using S3.Demo.API.Models;
using System.Linq;

namespace S3.Demo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;

        public FilesController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string bucketName, string? prefix)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be empty");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded");
            }

            try
            {
                // Use AmazonS3Util to check if the bucket exists
                bool doesBucketExist = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
                if (!doesBucketExist) return NotFound($"Bucket{bucketName} does not exist");

                using (var stream = file.OpenReadStream())
                {
                    var request = new Amazon.S3.Model.PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = string.IsNullOrEmpty(prefix) ? file.FileName : $"{prefix?.TrimEnd('/')}/{file.FileName}",
                        InputStream = stream
                    };

                    await _s3Client.PutObjectAsync(request);
                }

                return Ok(new { Message = "File uploaded successfully to s3 bucket." });
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return StatusCode(403, $"Access denied to bucket '{bucketName}'");
                }
                return StatusCode(500, $"S3 Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFile(string bucketName, string? prefix)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be empty");
            }

            try
            {
                // Check if bucket exists
                bool doesBucketExist = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
                if (!doesBucketExist)
                {
                    return NotFound($"Bucket '{bucketName}' does not exist");
                }
                var result = await _s3Client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix
                });
                // Get the object from S3
                var s3Object = result.S3Objects.Select(s =>
                {
                    var urlRequest = new Amazon.S3.Model.GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = s.Key,
                        Expires = DateTime.UtcNow.AddMinutes(1)
                    };
                    return new S3ObjectDTO
                    {
                        Name = s.Key,
                        PresignedUrl = _s3Client.GetPreSignedURL(urlRequest),
                        Key = s.Key,
                        BucketName = bucketName,
                        ContentType = s.StorageClass.ToString(),
                    };
                });
                return Ok(s3Object);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"File '{prefix}' not found in bucket '{bucketName}'");
                }
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return StatusCode(403, $"Access denied to bucket '{bucketName}'");
                }
                return StatusCode(500, $"S3 Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("preview")]
        public async Task<IActionResult> GetFileByKey(string bucketName, string key)
        {

            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be empty");
            }
            try
            {
                // Check if bucket exists
                bool doesBucketExist = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
                if (!doesBucketExist)
                {
                    return NotFound($"Bucket '{bucketName}' does not exist");
                }
                var s3object = await _s3Client.GetObjectAsync(bucketName, key);
                return File(s3object.ResponseStream, s3object.Headers.ContentType);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"File '{key}' not found in bucket '{bucketName}'");
                }
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return StatusCode(403, $"Access denied to bucket '{bucketName}'");
                }
                return StatusCode(500, $"S3 Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFile(string bucketName, string key)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be empty");
            }

            try
            {
                // Check if bucket exists
                bool doesBucketExist = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
                if (!doesBucketExist)
                {
                    return NotFound($"Bucket '{bucketName}' does not exist");
                }

                await _s3Client.DeleteObjectAsync(bucketName, key);

                return Ok(new { Message = "File deleted successfully" });
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"File '{key}' not found in bucket '{bucketName}'");
                }
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return StatusCode(403, $"Access denied to bucket '{bucketName}'");
                }
                return StatusCode(500, $"S3 Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Optional: Add a method to download the file content
        // to be fixed later
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile(string bucketName, string key)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be empty");
            }

            try
            {
                // Check if bucket exists
                bool doesBucketExist = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client , bucketName);
                if (!doesBucketExist)
                {
                    return NotFound($"Bucket '{bucketName}' does not exist");
                }

                var request = new Amazon.S3.Model.GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectAsync(request);
                
                // Get the file name from the key
                var fileName = Path.GetFileName(key);

                // Set content disposition to attachment to force download
                response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

                // Stream the file content directly to the response
                return new FileStreamResult(response.ResponseStream, response.Headers.ContentType)
                {
                    FileDownloadName = fileName
                };
                // Return the file with the original filename
                //return File(response.ResponseStream, response.Headers.ContentType, Path.GetFileName(key));
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"File '{key}' not found in bucket '{bucketName}'");
                }
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return StatusCode(403, $"Access denied to bucket '{bucketName}'");
                }
                return StatusCode(500, $"S3 Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
