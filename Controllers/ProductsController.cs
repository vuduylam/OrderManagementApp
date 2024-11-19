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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public ProductsController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProduct()
        {
            var cacheKey = "all_products";
            List<Product> products;
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                //Deserialize cached data
                products = JsonSerializer.Deserialize<List<Product>>(cachedData) ?? new List<Product>();
            }
            else
            {
                // Fetch data from database
                products = await _context.Products.ToListAsync();



                if (products == null)
                {
                    return NotFound();
                }

                if (products != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(products);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var cacheKey = $"product_{id}";
            Product? product;

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                // Deserialize cached data
                product = JsonSerializer.Deserialize<Product>(cachedData) ?? new Product();
            }
            else
            {
                // Fetch data from database
                product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound();
                }

                if (product != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(product);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(product);
        }

        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                //UPDATE DATABASE
                await _context.SaveChangesAsync();

                //UPDATE CACHE
                var cacheKeyId = $"product_{id}";
                var cacheKeyAll = "all_products";

                var cachedDataId = await _cache.GetStringAsync(cacheKeyId);
                var cachedDataAll = await _cache.GetStringAsync(cacheKeyAll);

                //Cache Id
                if (cachedDataId != null)
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKeyId);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(product);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKeyId, serializedData, cacheOptions);
                }

                //Cache All
                if (cachedDataAll != null)
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKeyAll);

                    //Make the list to from old cache data and modify
                    var products = JsonSerializer.Deserialize<List<Product>>(cachedDataAll) ?? new List<Product>();

                    products.RemoveAll(c => c.ProductId == id);
                    products.Add(product);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(products);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);

                }

            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
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

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            try
            {
                //Database
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                //CACHE
                var cacheKey = "all_customers";
                var cacheData = await _cache.GetStringAsync(cacheKey);

                if (cacheData != null) //Check if cache exists
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKey);

                    //Make a list from date of old cache and add a new category
                    var products = JsonSerializer.Deserialize<List<Product>>(cacheData) ?? new List<Product>();

                    products.Add(product);

                    var serializedData = JsonSerializer.Serialize(products);
                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }

                return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
            }
            catch
            {
                return BadRequest();
            }
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            //Delete in database
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            //Cache
            var cacheKeyId = $"product_{id}";
            var cacheKeyAll = "all_products";

            var cacheDataAll = await _cache.GetStringAsync(cacheKeyAll);

            //Delete cache ID
            await _cache.RemoveAsync(cacheKeyId);

            if(cacheDataAll != null)
            {
                var products = JsonSerializer.Deserialize<List<Product>>(cacheDataAll) ?? new List<Product>();
                
                products.RemoveAll(p => p.ProductId == id);

                var serializedData = JsonSerializer.Serialize(products);

                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
            }

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
