using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Logging;

namespace EduSync.Backend.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly string _connectionString;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _connectionString = configuration.GetSection("AzureStorage:ConnectionString").Value;
            _containerName = configuration.GetSection("AzureBlobStorage:ContainerName").Value;
            
            _logger = logger;
            
            _logger.LogInformation($"Container name configured as: {_containerName}");
            
            if (string.IsNullOrEmpty(_connectionString) || _connectionString.Contains("REPLACE_WITH_YOUR_CONNECTION_STRING"))
            {
                _logger.LogError("Azure Storage connection string is not properly configured!");
                throw new InvalidOperationException("Azure Storage connection string is not configured");
            }

            try
            {
                _blobServiceClient = new BlobServiceClient(_connectionString);
                _logger.LogInformation("Successfully created BlobServiceClient");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create BlobServiceClient: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureContainerExists()
        {
            try
            {
                _logger.LogInformation($"Ensuring container exists: {_containerName}");
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                _logger.LogInformation("Getting container client successful");
                
                var createResponse = await containerClient.CreateIfNotExistsAsync();
                
                if (createResponse != null && createResponse.Value != null)
                {
                    _logger.LogInformation($"Container '{_containerName}' was created successfully");
                }
                else
                {
                    _logger.LogInformation($"Container '{_containerName}' already exists");
                }

                // Verify container exists
                var exists = await containerClient.ExistsAsync();
                _logger.LogInformation($"Container exists check: {exists.Value}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in EnsureContainerExists: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SaveCourseContentUrlAsync(string courseId, Stream contentUrlStream)
        {
            try
            {
                _logger.LogInformation($"Starting to save course content URL for course {courseId}");
                
                await EnsureContainerExists();
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobPath = $"{courseId}/content-url.txt";
                var blobClient = containerClient.GetBlobClient(blobPath);

                _logger.LogInformation($"Blob path: {blobPath}");
                _logger.LogInformation($"Blob URI: {blobClient.Uri}");

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "text/plain"
                };

                // Read the content for logging
                using var reader = new StreamReader(contentUrlStream, Encoding.UTF8, true, -1, true);
                var content = await reader.ReadToEndAsync();
                _logger.LogInformation($"Content to save: {content}");
                contentUrlStream.Position = 0; // Reset stream position

                await blobClient.UploadAsync(contentUrlStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
                _logger.LogInformation($"Successfully saved course content URL for course {courseId}");

                // Verify the blob exists
                var exists = await blobClient.ExistsAsync();
                _logger.LogInformation($"Blob exists check: {exists.Value}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving course content URL: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SaveMediaUrlAsync(string courseId, Stream mediaUrlStream)
        {
            try
            {
                _logger.LogInformation($"Starting to save media URL for course {courseId}");
                
                await EnsureContainerExists();
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobPath = $"{courseId}/media-url.txt";
                var blobClient = containerClient.GetBlobClient(blobPath);

                _logger.LogInformation($"Blob path: {blobPath}");
                _logger.LogInformation($"Blob URI: {blobClient.Uri}");

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "text/plain"
                };

                // Read the content for logging
                using var reader = new StreamReader(mediaUrlStream, Encoding.UTF8, true, -1, true);
                var content = await reader.ReadToEndAsync();
                _logger.LogInformation($"Content to save: {content}");
                mediaUrlStream.Position = 0; // Reset stream position

                await blobClient.UploadAsync(mediaUrlStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
                _logger.LogInformation($"Successfully saved media URL for course {courseId}");

                // Verify the blob exists
                var exists = await blobClient.ExistsAsync();
                _logger.LogInformation($"Blob exists check: {exists.Value}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving media URL: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<(string courseContentUrl, string mediaUrl)> GetCourseUrlsAsync(string courseId)
        {
            try
            {
                await EnsureContainerExists();
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                string courseContentUrl = null;
                string mediaUrl = null;

                // Get course content URL
                var contentUrlBlob = containerClient.GetBlobClient($"{courseId}/content-url.txt");
                if (await contentUrlBlob.ExistsAsync())
                {
                    var contentResponse = await contentUrlBlob.DownloadAsync();
                    using (var reader = new StreamReader(contentResponse.Value.Content))
                    {
                        courseContentUrl = await reader.ReadToEndAsync();
                    }
                }

                // Get media URL
                var mediaUrlBlob = containerClient.GetBlobClient($"{courseId}/media-url.txt");
                if (await mediaUrlBlob.ExistsAsync())
                {
                    var mediaResponse = await mediaUrlBlob.DownloadAsync();
                    using (var reader = new StreamReader(mediaResponse.Value.Content))
                    {
                        mediaUrl = await reader.ReadToEndAsync();
                    }
                }

                return (courseContentUrl, mediaUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving course URLs: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteCourseUrlsAsync(string courseId)
        {
            try
            {
                await EnsureContainerExists();
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var deleted = false;

                // Delete content URL file
                var contentUrlBlob = containerClient.GetBlobClient($"{courseId}/content-url.txt");
                deleted |= await contentUrlBlob.DeleteIfExistsAsync();

                // Delete media URL file
                var mediaUrlBlob = containerClient.GetBlobClient($"{courseId}/media-url.txt");
                deleted |= await mediaUrlBlob.DeleteIfExistsAsync();

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting course URLs: {ex.Message}");
                throw;
            }
        }
    }
} 