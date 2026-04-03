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
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            List<Category> categories =
                [..
                    await _context.GetFilteredAsync<Category>(
                    c => c.UserId == user.Id ||
                         c.UserId == null)
                ];

            if (categories.Count == 0) return NoContent();

            return Ok(categories);
        }

        // GET: api/categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Category>> Get([FromRoute] Guid id)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Category? category = (await _context.GetFilteredAsync<Category>(
                c => (c.UserId == user.Id ||
                     c.UserId == null) &&
                     c.Id == id))
                     .FirstOrDefault();

            if (category is null) return NotFound();

            return Ok(category);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] CreateCategoryRequest request)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Category? exists = (await _context.GetFilteredAsync<Category>(
                c => c.UserId == user.Id &&
                     c.Name == request.Name))
                     .FirstOrDefault();

            if (exists is not null) return Conflict(String.Format(
                    Messages.Error_UniqueCategoryName,
                    request.Name));

            Category category = new()
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

            await _context.AddAsync(category);
            await _context.SaveChangesAsync();
            
            return Ok(category);
        }

        // PUT: api/categories/
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put([FromRoute] Guid id,
                                             [FromBody] Category category)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Category? existing = await _context.FindAsync<Category>(id);
            if (existing is null) return NotFound();

            if (existing.UserId != user.Id) return Forbid();

            existing.Name = category.Name;
            existing.Type = category.Type;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncedAt = category.SyncedAt;
            existing.IsDeleted = category.IsDeleted;

            var (IsValid, ErrorMessage) = existing.Validate();
            if (!IsValid)
                return BadRequest(ErrorMessage);

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Category? category = await _context.FindAsync<Category>(id);
            if (category is null) return NoContent();

            if (category.UserId != user.Id) return Forbid();

            category.IsDeleted = true;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
