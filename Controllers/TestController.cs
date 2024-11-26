using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OrderManagementApp.Data;
using OrderManagementApp.Interfaces;
using OrderManagementApp.Models;
using OrderManagementApp.Repositories;
using AutoMapper;

namespace OrderManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class _TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICategoryRepository _repo;
        private readonly IMapper _mapper;

        public _TestController(ApplicationDbContext context, ICategoryRepository repo, IMapper mapper)
        {
            _context = context;
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategory()
        {
            var cacheData = await _repo.GetCache();

            if (cacheData != null)
            {
                return Ok(cacheData);
            }
            else
            {
                var categories = await _repo.GetCategories();
                await _repo.WriteCache(categories);
                return Ok(categories);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategoryById(int id)
        {
            var category = await _repo.GetCategory(id);
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> PostCategory(Category category)
        {
            await _repo.PostCategory(category);
            await _repo.RemoveCache();
            return CreatedAtAction("GetCategory", new { id = category.CategoryId }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest("Category ID not matches");
            }
            try
            {
                await _repo.PutCategory(id, category);
                await _repo.RemoveCache();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_repo.CategoryExists(id))
                {
                    return NotFound("Do not exist");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {

            var deleted_category = await _repo.GetCategory(id);
            //Delete in database
            if (deleted_category == null)
            {
                return NotFound();
            }
            else
            {
                //Detele in database
                await _repo.DeleteCategory(deleted_category);

                //Detele cache
                await _repo.RemoveCache(id);
                await _repo.RemoveCache();

                return NoContent();
            }
        }
    }
}
