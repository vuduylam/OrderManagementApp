using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Caching.Distributed;
using OrderManagementApp.Data;
using OrderManagementApp.Interfaces;
using OrderManagementApp.Models;
using System.Text.Json;

namespace OrderManagementApp.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public CategoryRepository(ApplicationDbContext context, IDistributedCache cache) 
        {
            _context = context;
            _cache = cache;
        } 

        public async Task<IEnumerable<Category>> GetCategories()
        {
            var categories = await _context.Categories
                .Include(category => category.Products.OrderBy(p => p.ProductId))
                .ToListAsync();
            return categories;
        }

        public async Task<Category> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(category => category.Products.OrderBy(p => p.ProductId))
                .SingleOrDefaultAsync(category => category.CategoryId == id);
            return category;
        }

        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<ActionResult<Category>> PutCategory(int id, Category category)
        {
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return category;
        }
        public async Task DeleteCategory( Category category)
        {     
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCache()
        {
            var cacheKey = "all_categories";
            var cacheData = await _cache.GetStringAsync(cacheKey);

            if (cacheData != null) //Check if cache exists
            {
                //Delete old cache
                await _cache.RemoveAsync(cacheKey);
            }
        }
        public async Task RemoveCache(int id)
        {
            var cacheKeyId = $"category_{id}";
            var cachedDataId = await _cache.GetStringAsync(cacheKeyId);

            if (cachedDataId != null)
            {
                await _cache.RemoveAsync(cacheKeyId);
            }
        }
        public async Task<IEnumerable<Category>> GetCache()
        {
            var cacheKey = "all_categories";
            var cacheData = await _cache.GetStringAsync(cacheKey);
            if (cacheData == null)
            {
                return null;
            }
            var categories = JsonSerializer.Deserialize<List<Category>>(cacheData) ?? new List<Category>(); ;
            
            return categories;
        }
        public async Task<Category> GetCache(int id)
        {
            var cacheKeyId = $"category_{id}";
            var cacheDataId = await _cache.GetStringAsync(cacheKeyId);
            if (cacheDataId == null)
            {
                return null;
            }
            var category = JsonSerializer.Deserialize<Category>(cacheDataId) ?? new Category();

            return category;
        }

        public async Task WriteCache(IEnumerable<Category> categories)
        {
            string cacheKey = "all_categories";

            var serializedData = JsonSerializer.Serialize(categories);

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
        }

        public async Task WriteCache(int id, Category category)
        {
            string cacheKey = $"category_{id}";

            var serializedData = JsonSerializer.Serialize(category);

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
        }
        public bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
