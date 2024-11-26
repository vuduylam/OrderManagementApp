using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using OrderManagementApp.Data;
using OrderManagementApp.Models;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.Json.Serialization;
using OrderManagementApp.DTOs;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using static System.Net.Mime.MediaTypeNames;
using Minio;
using Minio.DataModel;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Minio.DataModel.Args;
using System.Security.Cryptography.X509Certificates;
using Minio.Exceptions;
using System.Net;
using System.IO;

namespace OrderManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        public CategoriesController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            List<Category> categories;

            var cacheKey = "all_categories";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            
            if (cachedData != null)
            {
                //Deserialize cached data
                categories = JsonSerializer.Deserialize<List<Category>>(cachedData) ?? new List<Category>();
            }
            else
            {
                // Fetch data from database
                categories = await _context.Categories
                    .Include(category => category.Products.OrderBy(p => p.ProductId))
                    .ToListAsync();

                if (categories != null)
                {
                    var serializedData = JsonSerializer.Serialize(categories);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var cacheKey = $"category_{id}";

            Category? category;

            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                // Deserialize cached data
                category = JsonSerializer.Deserialize<Category>(cachedData) ?? new Category();
            }
            else
            {
                // Fetch data from database
                category = await _context.Categories
                    .Include(category => category.Products.OrderBy(p => p.ProductId))
                    .SingleOrDefaultAsync(category => category.CategoryId == id);

                if (category == null)
                {
                    return NotFound();
                }

                if (category != null)
                {
                    //Serialize data and cache it
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.Preserve
                    };

                    var serializedData = JsonSerializer.Serialize(category, options);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(category);
        }

        // PUT: api/Categories/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest();
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                //UPDATE DATABASE
                await _context.SaveChangesAsync();
                
                //UPDATE CACHE
                var cacheKeyId = $"category_{id}";
                var cacheKeyAll = "all_categories";

                var cachedDataId = await _cache.GetStringAsync(cacheKeyId);
                var cachedDataAll = await _cache.GetStringAsync(cacheKeyAll);

                //Cache Id
                if (cachedDataId != null)
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKeyId);

                }

                //Cache All
                if (cachedDataAll != null)
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKeyAll);

                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpGet("image")]
        public async Task<IActionResult> GetImage(CancellationToken cancellationToken)
        {
            var _minioClient = new MinioClient()
                .WithEndpoint("localhost:9000")
                .WithCredentials("minioadmin", "minioadmin")
                .WithSSL(false)
                .Build();

            try
            {
                // Define the object to retrieve
                var bucketName = "vuduylam";
                var objectName = "image.png";

                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                    .WithBucket(bucketName)
                                    .WithObject(objectName);
                await _minioClient.StatObjectAsync(statObjectArgs);
                var memoryStream = new MemoryStream();
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                .WithBucket("vuduylam")
                                                .WithObject("image.png")
                                                .WithCallbackStream((stream) =>
                                                {
                                                    //stream.CopyTo(fileStream);
                                                    stream.CopyTo(memoryStream);
                                                });
                await _minioClient.GetObjectAsync(getObjectArgs);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return new FileStreamResult(memoryStream, "image/png")
                {
                    FileDownloadName = "test.png"
                };
            }
            catch (MinioException e)
            {
                return BadRequest("Error occurred: " + e.Message);
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult> FileUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var _minioClient = new MinioClient()
                .WithEndpoint("localhost:9000")
                .WithCredentials("minioadmin","minioadmin")
                .WithSSL(false)
                .Build();

            var bucketName = "vuduylam";  // The MinIO bucket name
            var objectName = Path.GetFileName(file.FileName);  // Name of the object to upload
            var contentType = file.ContentType;  // File content type

            // Ensure the bucket exists
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
            if (!bucketExists)
            {
                // If the bucket doesn't exist, create it
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
            }

            try
            {
                // Upload the file to MinIO
                var putObjectArgs = new PutObjectArgs()
                                     .WithBucket(bucketName)
                                     .WithObject(objectName)
                                     .WithStreamData(file.OpenReadStream())  // Use the stream of the file
                                     .WithContentType(contentType)
                                     .WithObjectSize(file.Length);

                await _minioClient.PutObjectAsync(putObjectArgs);

                
                PresignedGetObjectArgs args = new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithExpiry(3600)
                    .WithObject(objectName);

                string url = await _minioClient.PresignedGetObjectAsync(args);


                return Ok(url);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }


        // POST: api/Categories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            try
            {
                //DATABASE
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                //CACHE
                var cacheKey = "all_categories";
                var cacheData = await _cache.GetStringAsync(cacheKey);

                if (cacheData != null) //Check if cache exists
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKey);

                }

                return CreatedAtAction("GetCategory", new { id = category.CategoryId }, category);
            }
            catch
            {
                return BadRequest();
            }
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            //Delete in database
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            //Delete in cache
            var cacheKeyId = $"category_{id}";
            var cacheKeyAll = "all_categories";
 
            await _cache.RemoveAsync(cacheKeyId);
            await _cache.RemoveAsync(cacheKeyAll);

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
