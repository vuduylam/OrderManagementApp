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
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public OrdersController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var cacheKey = "all_orders";
            List<Order> orders;
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                //Deserialize cached data
                orders = JsonSerializer.Deserialize<List<Order>>(cachedData) ?? new List<Order>();
            }
            else
            {
                // Fetch data from database
                orders = await _context.Orders.ToListAsync();

                if (orders == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }

                if (orders != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(orders);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(orders);
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var cacheKey = $"order_{id}";
            Order? order;

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                // Deserialize cached data
                order = JsonSerializer.Deserialize<Order>(cachedData) ?? new Order();
            }
            else
            {
                // Fetch data from database
                order = await _context.Orders.FindAsync(id);

                if (order == null)
                {
                    return NotFound();
                }

                if (order != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(order);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(order);
        }

        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.OrderId)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                //UPDATE IN DATABASE
                await _context.SaveChangesAsync();

                //UPDATE IN CACHE
                var cacheKeyId = $"order_{id}";
                var cacheKeyAll = "all_orders";

                var cachedDataId = await _cache.GetStringAsync(cacheKeyId);
                var cachedDataAll = await _cache.GetStringAsync(cacheKeyAll);

                //Cache Id
                if (cachedDataId != null)
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKeyId);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(order);

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
                    var orders = JsonSerializer.Deserialize<List<Order>>(cachedDataAll) ?? new List<Order>();

                    orders.RemoveAll(c => c.OrderId == id);
                    orders.Add(order);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(orders);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
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

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            try
            {
                //DATABASE
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                //CACHE
                var cacheKey = "all_orders";
                var cacheData = await _cache.GetStringAsync(cacheKey);

                if (cacheData != null) //Check if cache exists
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKey);

                    //Make a list from date of old cache and add a new category
                    var orders = JsonSerializer.Deserialize<List<Order>>(cacheData) ?? new List<Order>();

                    orders.Add(order);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(orders);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }

                return CreatedAtAction("GetOrder", new { id = order.OrderId }, order);
            }
            catch
            {
                return BadRequest();
            }

        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            //Delete in database
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            //Delete in cache
            var cacheKeyId = $"order_{id}";
            var cacheDataId = await _cache.GetStringAsync(cacheKeyId);

            var cacheKeyAll = $"all_order";
            var cacheDataAll = await _cache.GetStringAsync(cacheKeyAll);

            if (cacheDataId != null)
            {
                await _cache.RemoveAsync(cacheKeyId);
            }

            if (cacheDataAll != null) 
            {
                await _cache.RemoveAsync(cacheKeyAll);
                
                var orders = JsonSerializer.Deserialize<List<Order>>(cacheDataAll) ?? new List<Order>();
                
                orders.RemoveAll(o => o.OrderId == id);

                var serializedData = JsonSerializer.Serialize(orders);

                var cacheOptions = new DistributedCacheEntryOptions()
                   .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);

            }

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
