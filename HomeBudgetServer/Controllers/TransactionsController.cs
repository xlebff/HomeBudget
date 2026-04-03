using HomeBudgetServer.Data;
using HomeBudgetServer.Resources;
using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBudgetServer.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionsController(AppDbContext context)
        : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // GET: api/transactions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            List<Transaction> transactions = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.UserId == user.Id)
                .ToListAsync();

            return Ok(transactions);
        }

        // GET: api/transactions/considered
        [HttpGet("considered")]
        public async Task<IActionResult> GetConsidered()
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            List<Transaction> transactions = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.UserId == user.Id &&
                            t.IsConsidered &&
                            !t.IsDeleted)
                .ToListAsync();

            return Ok(transactions);
        }

        // GET: api/transactions/not-considered
        [HttpGet("not-considered")]
        public async Task<IActionResult> GetNotConsidered()
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            List<Transaction> transactions = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.UserId == user.Id &&
                            !t.IsConsidered &&
                            !t.IsDeleted)
                .ToListAsync();

            return Ok(transactions);
        }

        // GET: api/transactions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAll([FromRoute] Guid id)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Transaction? transaction = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();

            if (transaction is null) return NotFound();

            if (transaction.UserId != user.Id)
                return Forbid(Messages.Error_AccessDenied);

            return Ok(transaction);
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTransactionRequest request)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Guid currencyId;

            if (request.CurrencyId.HasValue)
            {
                Currency? currency = await _context.Currencies
                    .FindAsync(request.CurrencyId.Value);

                if (currency is null)
                    return BadRequest(String.Format(
                        Messages.Error_InvalidCurrencyId,
                        request.CurrencyId.Value));

                currencyId = request.CurrencyId.Value;
            }
            else if (user.CurrencyId.HasValue)
            {
                currencyId = user.CurrencyId.Value;
            }
            else
            {
                return BadRequest(Messages.Error_InvalidCurrencyId);
            }

            Transaction transaction = new()
            {
                Id = request.Id,
                UserId = user.Id,
                Date = request.Date,
                TotalAmount = request.TotalAmount,
                CurrencyId = currencyId,
                CategoryId = request.CategoryId,
                IsConsidered = request.IsConsidered,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = request.UpdatedAt,
                SyncedAt = request.SyncedAt,
                IsDeleted = request.IsDeleted,
            };

            var (isValid, errorMessage) = transaction.Validate();
            if (!isValid)
                return BadRequest(errorMessage);

            await _context.Transactions.AddAsync(transaction);

            if (request.Items != null &&
                request.Items.Count != 0)
            {
                List<TransactionItem> newItems = [];

                foreach (var item in request.Items)
                {
                    TransactionItem newItem = new()
                    {
                        Id = item.Id,
                        TransactionId = transaction.Id,
                        Name = item.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };

                    var (IsValid, ErrorMessage) = newItem.Validate();
                    if (!IsValid)
                        return BadRequest(ErrorMessage);

                    newItems.Add(newItem);
                }

                await _context.TransactionItems.AddRangeAsync(newItems);
                transaction.Items = newItems;
            }

            await _context.SaveChangesAsync();

            return Ok(transaction);
        }

        // PUT: api/transaction
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id,
            [FromBody] CreateTransactionRequest request)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Transaction? existing = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existing is null) return NotFound();

            if (existing.UserId != user.Id)
                return Forbid(Messages.Error_AccessDenied);

            // inserting values
            {
                existing.TotalAmount = request.TotalAmount;
                existing.Comment = request.Comment;
                existing.Date = request.Date;
                existing.UpdatedAt = request.UpdatedAt;
                existing.SyncedAt = request.SyncedAt;
                existing.IsConsidered = request.IsConsidered;
                existing.IsDeleted = request.IsDeleted;
            }

            if (request.CurrencyId.HasValue && 
                existing.CurrencyId != request.CurrencyId.Value)
            {
                Currency? currency = await _context.Currencies.
                    FindAsync(request.CurrencyId.Value);

                if (currency is null)
                    return BadRequest(String.Format(
                        Messages.Error_InvalidCurrencyId,
                        request.CurrencyId.Value));

                existing.CurrencyId = currency.Id;
            }

            if (existing.CategoryId != request.CategoryId)
            {
                if (request.CategoryId.HasValue)
                {
                    Category? category = await _context.Categories
                        .FindAsync(request.CategoryId);

                    if (category is null)
                        return BadRequest(String.Format(
                            Messages.Error_InvalidCategoryId,
                            request.CategoryId.Value));

                    existing.CategoryId = category.Id;
                }
                else
                {
                    existing.CategoryId = null;
                }
            }

            if (request.Items is not null)
            {
                _context.TransactionItems.RemoveRange(existing.Items);
                existing.Items.Clear();

                if (request.Items.Count > 0)
                {
                    List<TransactionItem> newItems = [];

                    foreach (var item in request.Items)
                    {
                        TransactionItem newItem = new()
                        {
                            Id = item.Id,
                            TransactionId = existing.Id,
                            Name = item.Name,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice
                        };

                        var (IsValid, ErrorMessage) = newItem.Validate();
                        if (!IsValid)
                            return BadRequest(ErrorMessage);

                        newItems.Add(newItem);
                    }

                    await _context.TransactionItems.AddRangeAsync(newItems);
                    existing.Items = newItems;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        // PUT: api/transactions/recover/{id}
        [HttpPut("recover/{id:guid}")]
        public async Task<IActionResult> Recover([FromRoute] Guid id)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Transaction? transaction =
                await _context.Transactions.FindAsync(id);

            if (transaction is null) return NotFound();

            if (transaction.UserId != user.Id)
                return Forbid(Messages.Error_AccessDenied);

            transaction.IsConsidered = true;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/transactions/soft-delete/{id}
        [HttpDelete("soft-delete/{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Transaction? transaction =
                await _context.Transactions.FindAsync(id);

            if (transaction is null) return NotFound();

            if (transaction.UserId != user.Id)
                return Forbid(Messages.Error_AccessDenied);

            transaction.IsConsidered = false;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/transactions/hard-delete/{id}
        [HttpDelete("hard-delete/{id:guid}")]
        public async Task<IActionResult> HardDelete([FromRoute] Guid id)
        {
            User? user = await this.GetAuthenticatedUserAsync(_context);
            if (user is null) return Unauthorized();

            Transaction? transaction =
                await _context.Transactions.FindAsync(id);

            if (transaction is null) return NotFound();

            if (transaction.UserId != user.Id)
                return Forbid(Messages.Error_AccessDenied);

            transaction.IsDeleted = true;

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
