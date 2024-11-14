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
                await _context.SaveChangesAsync();
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
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.OrderId }, order);
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

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
