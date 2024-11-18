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
    public class OrderDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public OrderDetailsController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/OrderDetails
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDetail>>> GetOrderDetails()
        {
            var cacheKey = "all_order_details";
            List<OrderDetail> orderDetails;
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                //Deserialize cached data
                orderDetails = JsonSerializer.Deserialize<List<OrderDetail>>(cachedData) ?? new List<OrderDetail>();
            }
            else
            {
                // Fetch data from database
                orderDetails = await _context.OrderDetails.ToListAsync();

                if (orderDetails == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }

                if (orderDetails != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(orderDetails);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(orderDetails);
        }

        // GET: api/OrderDetails/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetail>> GetOrderDetail(int id)
        {
            var cacheKey = $"order_detail_{id}";
            OrderDetail? orderDetail;

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                // Deserialize cached data
                orderDetail = JsonSerializer.Deserialize<OrderDetail>(cachedData) ?? new OrderDetail();
            }
            else
            {
                // Fetch data from database
                orderDetail = await _context.OrderDetails.FindAsync(id);

                if (orderDetail == null)
                {
                    return NotFound();
                }

                if (orderDetail != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(orderDetail);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(orderDetail);
        }

        // PUT: api/OrderDetails/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrderDetail(int id, OrderDetail orderDetail)
        {
            if (id != orderDetail.OrderDetailId)
            {
                return BadRequest();
            }

            _context.Entry(orderDetail).State = EntityState.Modified;

            try
            {
                //UPDATE DATABASE
                await _context.SaveChangesAsync();

                //UPDATE CACHE
                var cacheKeyId = $"order_detail_{id}";
                var cacheKeyAll = "all_order_details";

                var cachedDataId = await _cache.GetStringAsync(cacheKeyId);
                var cachedDataAll = await _cache.GetStringAsync(cacheKeyAll);

                //Cache Id
                if (cachedDataId != null)
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKeyId);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(orderDetail);

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
                    var orderDetails = JsonSerializer.Deserialize<List<OrderDetail>>(cachedDataAll) ?? new List<OrderDetail>();

                    orderDetails.RemoveAll(c => c.OrderDetailId == id);
                    orderDetails.Add(orderDetail);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(orderDetails);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderDetailExists(id))
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

        // POST: api/OrderDetails
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<OrderDetail>> PostOrderDetail(OrderDetail orderDetail)
        {
            try
            {
                //ADD DATABASE
                _context.OrderDetails.Add(orderDetail);
                await _context.SaveChangesAsync();

                //CACHE
                var cacheKey = "all_order_details";
                var cacheData = await _cache.GetStringAsync(cacheKey);

                if (cacheData != null) //Check if cache exists
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKey);

                    //Make a list from date of old cache and add a new category
                    var orderDetails = JsonSerializer.Deserialize<List<OrderDetail>>(cacheData) ?? new List<OrderDetail>();

                    orderDetails.Add(orderDetail);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(orderDetails);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }

                return CreatedAtAction("GetOrderDetail", new { id = orderDetail.OrderDetailId }, orderDetail);
            }
            catch
            {
                return BadRequest();
            }

        }

        // DELETE: api/OrderDetails/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderDetail(int id)
        {
            var orderDetail = await _context.OrderDetails.FindAsync(id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            //Delete in database
            _context.OrderDetails.Remove(orderDetail);
            await _context.SaveChangesAsync();

            //Delete in cache
            var cacheKeyId = $"order_detail_{id}";
            var cacheDataId = await _cache.GetStringAsync(cacheKeyId);

            var cacheKeyAll = "all_order_details";
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
                var orderDetails = JsonSerializer.Deserialize<List<OrderDetail>>(cacheDataAll) ?? new List<OrderDetail>();

                orderDetails.RemoveAll(c => c.OrderDetailId == id);

                var serializedData = JsonSerializer.Serialize(orderDetails);

                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
            }

            return NoContent();
        }

        private bool OrderDetailExists(int id)
        {
            return _context.OrderDetails.Any(e => e.OrderDetailId == id);
        }
    }
}
