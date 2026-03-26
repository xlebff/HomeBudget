using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HomeBudgetServer.Data;

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
                    Type = category.Type
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
                c => c.UserId == user.Id &&
                     c.Id == id)).FirstOrDefault<Category>();

            if (category is null) return NotFound();

            return Ok(new CategoryResponse
            {
                Id = category.Id,
                UserId = category.UserId,
                Name = category.Name,
                Type = category.Type
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
                Type = request.Type
            };

            var (IsValid, ErrorMessage) = category.Validate();

            if (!IsValid)
                return BadRequest(ErrorMessage);

            var exists = await _context.GetFilteredAsync<Category>(
                c => c.UserId == user.Id && c.Name == request.Name);
            if (exists.FirstOrDefault() is not null)
                return Conflict($"Category with name " +
                    $"'{request.Name}' already exists for this user.");

            await _context.AddAsync<Category>(category);
            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                Id = category.Id,
                UserId = category.UserId,
                Name = category.Name,
                Type = category.Type
            };

            return CreatedAtAction(nameof(Get), new { id = category.Id }, response);
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

            await _context.SaveChangesAsync();
            return Ok(new CategoryResponse
            {
                Id = existing.Id,
                UserId = existing.UserId,
                Name = existing.Name,
                Type = existing.Type
            });
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var existing = await _context.FindAsync<Category>(id);
            if (existing == null) return NoContent();

            if (existing.UserId != user.Id) return Forbid();

            _context.Remove<Category>(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
