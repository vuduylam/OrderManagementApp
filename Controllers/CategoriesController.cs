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
            var cacheKey = "all_categories";
            List<Category> categories;
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                //Deserialize cached data
                categories = JsonSerializer.Deserialize<List<Category>>(cachedData) ?? new List<Category>();
            }
            else
            {
                // Fetch data from database
                categories = await _context.Categories.ToListAsync();

                if (categories != null)
                {
                    // Serialize data and cache it
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
                category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound();
                }

                if (category != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(category);

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
                //Update database
                await _context.SaveChangesAsync();
                
                //Update cache
                var cacheKeyId = $"category_{id}";
                var cacheKeyAll = "all_categories";

                var cachedDataId = await _cache.GetStringAsync(cacheKeyId);
                var cachedDataAll = await _cache.GetStringAsync(cacheKeyAll);

                //Read old cache, delete in Redis and add the modified cache
                if (cachedDataId != null)
                {
                    await _cache.RemoveAsync(cacheKeyId);
                    var serializedData = JsonSerializer.Serialize(category);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKeyId, serializedData, cacheOptions);
                }

                if (cachedDataAll != null)
                {
                    await _cache.RemoveAsync(cacheKeyAll);
                    var categories = JsonSerializer.Deserialize<List<Category>>(cachedDataAll) ?? new List<Category>();
                    
                    categories.Add(category);
                    
                    var serializedData = JsonSerializer.Serialize(categories);
                    
                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKeyAll, serializedData,cacheOptions);
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

        // POST: api/Categories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var cacheKey = "all_categories";
                var cacheData = await _cache.GetStringAsync(cacheKey);

                //Read old cache, delete in Redis and add the modified cache
                if (cacheData != null)
                {
                    await _cache.RemoveAsync(cacheKey);
                    var categories = JsonSerializer.Deserialize<List<Category>>(cacheData) ?? new List<Category>();

                    categories.Add(category);

                    var serializedData = JsonSerializer.Serialize(categories);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
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
            var cacheDataId = await _cache.GetStringAsync(cacheKeyId);

            var cacheKeyAll = "all_categories";
            var cacheDataAll = await _cache.GetStringAsync(cacheKeyAll);

            //Delete cache_id if exists
            if (cacheDataId != null)
            {
                await _cache.RemoveAsync(cacheKeyId);
            }

            //Read old cache, delete in Redis and add the modified cache for all
            if (cacheDataAll != null)
            {
                await _cache.RemoveAsync(cacheKeyAll);
                var categories = JsonSerializer.Deserialize<List<Category>>(cacheDataAll) ?? new List<Category>();

                categories.RemoveAll(c => c.CategoryId == id);

                var serializedData = JsonSerializer.Serialize(categories);

                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                
                await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
            }

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
