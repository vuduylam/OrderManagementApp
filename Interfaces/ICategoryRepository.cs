using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementApp.Models;
using System.CodeDom;

namespace OrderManagementApp.Interfaces
{
    public interface ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetCategories();
        public Task<Category> GetCategory(int id);
        public Task<ActionResult<Category>> PutCategory(int id, Category category);
        public Task<ActionResult<Category>> PostCategory(Category category);
        public Task DeleteCategory(Category category);
        public Task RemoveCache();
        public Task RemoveCache(int id);
        public Task<IEnumerable<Category>> GetCache();
        public Task<Category> GetCache(int id);
        public Task WriteCache(IEnumerable<Category> categories);
        public Task WriteCache(int id, Category category);
        public bool CategoryExists(int id);
    }
}
