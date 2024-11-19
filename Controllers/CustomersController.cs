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
using System.Text.Json.Serialization;

namespace OrderManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public CustomersController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            var cacheKey = "all_customers";
            List<Customer> customers;
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                //Deserialize cached data
                customers = JsonSerializer.Deserialize<List<Customer>>(cachedData) ?? new List<Customer>();
            }
            else
            {
                // Fetch data from database
                customers = await _context.Customers
                    .Include(x => x.Orders)
                    .ToListAsync();

                if (customers == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }

                if (customers != null)
                {
                    // Serialize data and cache it
                    var serializedData = JsonSerializer.Serialize(customers);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(customers);
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var cacheKey = $"customer_{id}";
            Customer? customer;

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                // Deserialize cached data
                customer = JsonSerializer.Deserialize<Customer>(cachedData) ?? new Customer();
            }
            else
            {
                // Fetch data from database
                customer = await _context.Customers.FindAsync(id);

                if (customer == null)
                {
                    return NotFound();
                }

                if (customer != null)
                {
                    // Serialize data and cache it

                    customer.Orders = await (from order in _context.Orders
                                               where order.CustomerId == id
                                               select order).ToListAsync();

                    var serializedData = JsonSerializer.Serialize(customer);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            return Ok(customer);
        }

        // PUT: api/Customers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                //UPDATE DATABASE
                await _context.SaveChangesAsync();

                //UPDATE CACHE
                //Read old cache and save in local variable
                var cacheKeyId = $"customer_{id}";
                var cacheKeyAll = "all_customers";

                var cachedDataId = await _cache.GetStringAsync(cacheKeyId);
                var cachedDataAll = await _cache.GetStringAsync(cacheKeyAll);

                //Cache Id
                if (cachedDataId != null) //Check if cache_id exists
                {
                    //Detele old cache
                    await _cache.RemoveAsync(cacheKeyId);

                    //Write new cache
                    var serializedData = JsonSerializer.Serialize(customer);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                    await _cache.SetStringAsync(cacheKeyId, serializedData, cacheOptions);
                }

                //Cache All
                if (cachedDataAll != null) //Check if cache all exists
                {
                    //Delete old cache all
                    await _cache.RemoveAsync(cacheKeyAll);

                    //Make list for new cache all and add new customer
                    var customers = JsonSerializer.Deserialize<List<Customer>>(cachedDataAll) ?? new List<Customer>();

                    customers.RemoveAll(c => c.CustomerId == id);
                    customers.Add(customer);

                    //Write new cache
                    var serializedData = JsonSerializer.Serialize(customer);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
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

        // POST: api/Customers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            try
            {
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                //CACHE
                var cacheKey = "all_customers";
                var cacheData = await _cache.GetStringAsync(cacheKey);

                if (cacheData != null) //Check if cache exists
                {
                    //Delete old cache
                    await _cache.RemoveAsync(cacheKey);

                    //Make a list from date of old cache and add a new category
                    var customers = JsonSerializer.Deserialize<List<Customer>>(cacheData) ?? new List<Customer>();

                    customers.Add(customer);

                    //Create new cache
                    var serializedData = JsonSerializer.Serialize(customers);

                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }

                return CreatedAtAction("GetCustomer", new { id = customer.CustomerId }, customer);
            }
            catch 
            {
                return BadRequest();
            }

        }

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            //Delete in database
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            //Delete in cache
            var cacheKeyId = $"customer_{id}";
            var cacheDataId = await _cache.GetStringAsync(cacheKeyId);

            var cacheKeyAll = "all_customers";
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
                var customers = JsonSerializer.Deserialize<List<Customer>>(cacheDataAll) ?? new List<Customer>();

                customers.RemoveAll(c => c.CustomerId == id);

                var serializedData = JsonSerializer.Serialize(customers);

                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                await _cache.SetStringAsync(cacheKeyAll, serializedData, cacheOptions);
            }

            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}
