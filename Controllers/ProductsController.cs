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
                    return StatusCode(StatusCodes.Status404NotFound);
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
                await _context.SaveChangesAsync();
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
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
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

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
