using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HomeBudgetServer.Data;
using HomeBudgetServer.Resources;

namespace HomeBudgetServer.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize]
    public class CategoriesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var categories = await _context.GetFilteredAsync<Category>(
                 c => c.UserId == user.Id ||
                      c.UserId == null);

            if (!categories.Any()) return NoContent();

            var categoriesDTO = new List<CategoryResponse>();

            foreach (var category in categories)
            {
                categoriesDTO.Add(new CategoryResponse
                {
                    Id = category.Id,
                    UserId = category.UserId,
                    Name = category.Name,
                    Type = category.Type,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    SyncedAt = category.SyncedAt,
                    IsDeleted = category.IsDeleted
                });
            }

            return Ok(categoriesDTO);
        }

        // GET: api/categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Category>> Get(Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var category = (await _context.GetFilteredAsync<Category>(
                c => (c.UserId == user.Id ||
                      c.UserId == null) &&
                     c.Id == id))
                .FirstOrDefault();

            if (category is null) return NotFound();

            return Ok(new CategoryResponse
            {
                Id = category.Id,
                UserId = category.UserId,
                Name = category.Name,
                Type = category.Type,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                SyncedAt = category.SyncedAt
            });
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] CreateCategoryRequest request)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = request.Name,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedAt = default,
                IsDeleted = false
            };

            var (IsValid, ErrorMessage) = category.Validate();

            if (!IsValid)
                return BadRequest(ErrorMessage);

            var exists = await _context.GetFilteredAsync<Category>(
                c => c.UserId == user.Id && c.Name == request.Name);
            if (exists.FirstOrDefault() is not null)
                return Conflict(String.Format(
                    Messages.Error_UniqueCategoryName,
                    request.Name));

            await _context.AddAsync(category);
            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                Id = category.Id,
                UserId = category.UserId,
                Name = category.Name,
                Type = category.Type,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                SyncedAt = category.SyncedAt,
                IsDeleted = category.IsDeleted
            };

            return CreatedAtAction(nameof(Get), 
                new { id = category.Id }, response);
        }

        // PUT: api/categories/
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put(Guid id,
                                             [FromBody] Category category)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var existing = await _context.FindAsync<Category>(id);
            if (existing == null) return NotFound();

            if (existing.UserId != user.Id) return Forbid();

            existing.Name = category.Name;
            existing.Type = category.Type;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsDeleted = category.IsDeleted;

            var (IsValid, ErrorMessage) = existing.Validate();
            if (!IsValid)
                return BadRequest(ErrorMessage);

            await _context.SaveChangesAsync();
            return Ok(new CategoryResponse
            {
                Id = existing.Id,
                UserId = existing.UserId,
                Name = existing.Name,
                Type = existing.Type,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt,
                SyncedAt = existing.SyncedAt,
                IsDeleted = existing.IsDeleted
            });
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var category = await _context.FindAsync<Category>(id);
            if (category == null) return NoContent();

            if (category.UserId != user.Id) return Forbid();

            category.IsDeleted = true;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
