using HomeBudgetServer.Data;
using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;

namespace HomeBudgetServer.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionsController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // GET: api/transactions
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transactions = await _context.GetFilteredAsync<Transaction>(
                t => t.UserId == user.Id);

            var result = new List<TransactionResponse>();

            foreach (Transaction t in transactions)
            {
                result.Add(new TransactionResponse
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    Type = t.Type,
                    TotalAmount = t.TotalAmount,
                    CurrencyId = t.CurrencyId,
                    Comment = t.Comment,
                    Date = t.Date,
                    CreatedAt = t.CreatedAt,
                    SyncedAt = t.SyncedAt,
                    IsDeleted = t.IsDeleted
                });
            }

            return Ok(result);
        }

        // GET: api/transactions/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transactions = await _context.GetFilteredAsync<Transaction>(
                t => t.UserId == user.Id &&
                     !t.IsDeleted);

            var result = new List<TransactionResponse>();

            foreach (Transaction t in transactions)
            {
                result.Add(new TransactionResponse
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    Type = t.Type,
                    TotalAmount = t.TotalAmount,
                    CurrencyId = t.CurrencyId,
                    Comment = t.Comment,
                    Date = t.Date,
                    CreatedAt = t.CreatedAt,
                    SyncedAt = t.SyncedAt,
                    IsDeleted = t.IsDeleted
                });
            }

            return Ok(result);
        }

        // GET: api/transactions/deleted
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transactions = await _context.GetFilteredAsync<Transaction>(
                t => t.UserId == user.Id &&
                     t.IsDeleted);

            var result = new List<TransactionResponse>();

            foreach (Transaction t in transactions)
            {
                result.Add(new TransactionResponse
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    Type = t.Type,
                    TotalAmount = t.TotalAmount,
                    CurrencyId = t.CurrencyId,
                    Comment = t.Comment,
                    Date = t.Date,
                    CreatedAt = t.CreatedAt,
                    SyncedAt = t.SyncedAt,
                    IsDeleted = t.IsDeleted
                });
            }

            return Ok(result);
        }

        // GET: api/transactions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Currency)
                .Include(t => t.Items)
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();

            if (transaction == null) return NotFound();

            if (transaction.UserId != user.Id) return BadRequest(
                "Permission denied.");

            var transactionItems = new List<TransactionItemResponse>();

            foreach (TransactionItem t in transaction.Items)
            {
                transactionItems.Add(new TransactionItemResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Quantity = t.Quantity,
                    UnitPrice = t.UnitPrice,
                    TotalPrice = t.TotalPrice,
                });
            }

            return Ok(new DetailedTransactionResponse
            {
                Id = transaction.Id,
                CategoryId = transaction.CategoryId,
                Type = transaction.Type,
                TotalAmount = transaction.TotalAmount,
                CurrencyId = transaction.CurrencyId,
                Comment = transaction.Comment,
                Date = transaction.Date,
                CreatedAt = transaction.CreatedAt,
                SyncedAt = transaction.SyncedAt,
                IsDeleted = transaction.IsDeleted,
                Items = transactionItems
            });
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTransactionRequest request)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            Guid currencyId;

            if (request.CurrencyId.HasValue)
            {
                var currency = await _context.Currencies
                    .FindAsync(request.CurrencyId.Value);

                if (currency is null) return BadRequest(
                    $"Invalid currency id \"" +
                    $"{request.CurrencyId.Value}\"");

                currencyId = request.CurrencyId.Value;
            }
            else currencyId = user.CurrencyId;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = request.Type,
                Date = request.Date,
                TotalAmount = request.TotalAmount,
                CurrencyId = currencyId,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedAt = null,
            };

            var (isValid, errorMessage) = transaction.Validate();
            if (!isValid)
                return BadRequest(errorMessage);

            if (transaction.CategoryId != Guid.Empty) 
            {
                Category? category = await _context.Categories
                    .FindAsync(transaction.CategoryId);

                if (category is null)
                    return BadRequest(
                        $"Invalid category id \"" +
                        $"{transaction.CategoryId}\"");
                
                transaction.Category = category;
            }

            transaction.Currency = (await 
                _context.Currencies.FindAsync(currencyId))!;

            transaction.User = user;

            _context.Transactions.Add(transaction);

            var transactionItemsResponse = 
                new List<TransactionItemResponse>();

            if (request.Items != null && request.Items.Count != 0)
            {
                var items = new List<TransactionItem>();

                foreach (var itemDto in request.Items)
                {
                    var item = new TransactionItem
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transaction.Id,
                        Name = itemDto.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TotalPrice = itemDto.TotalPrice,
                        CreatedAt = DateTime.UtcNow
                    };

                    var (itemIsValid, itemError) = item.Validate();
                    if (!itemIsValid)
                        return BadRequest(itemError);

                    transactionItemsResponse.Add(new TransactionItemResponse
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    });

                    items.Add(item);
                }

                _context.TransactionItems.AddRange(items);
                transaction.Items.AddRange(items);
            }

            await _context.SaveChangesAsync();

            return Ok(new DetailedTransactionResponse
            {
                Id = transaction.Id,
                CategoryId = transaction.CategoryId,
                Type = transaction.Type,
                TotalAmount = transaction.TotalAmount,
                CurrencyId = transaction.CurrencyId,
                Comment = transaction.Comment,
                Date = transaction.Date,
                CreatedAt = transaction.CreatedAt,
                SyncedAt = transaction.SyncedAt,
                IsDeleted = transaction.IsDeleted,
                Items = transactionItemsResponse
            });
        }

        // PUT: api/transaction
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id,
    [FromBody] CreateTransactionRequest request)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var existing = await _context.Transactions
                .Include(t => t.Items)
                .Include(t => t.Currency)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existing == null)
                return NotFound();

            if (existing.UserId != user.Id)
                return Forbid("Permission denied.");

            existing.Type = request.Type;
            existing.Date = request.Date;
            existing.TotalAmount = request.TotalAmount;
            existing.Comment = request.Comment;
            existing.UpdatedAt = DateTime.UtcNow;

            if (request.CurrencyId.HasValue && 
                existing.CurrencyId != request.CurrencyId.Value)
            {
                var currency = await _context.Currencies.
                    FindAsync(request.CurrencyId.Value);
                if (currency == null)
                    return BadRequest($"Invalid currency id \"" +
                        $"{request.CurrencyId.Value}\".");
                existing.CurrencyId = currency.Id;
                existing.Currency = currency;
            }

            if (existing.CategoryId != request.CategoryId)
            {
                if (request.CategoryId != Guid.Empty)
                {
                    var category = await _context.Categories
                        .FindAsync(request.CategoryId);
                    if (category == null)
                        return BadRequest(
                            $"Invalid category id \"" +
                            $"{request.CategoryId}\".");
                    existing.CategoryId = category.Id;
                    existing.Category = category;
                }
                else
                {
                    existing.CategoryId = null;
                    existing.Category = null;
                }
            }

            if (request.Items != null)
            {
                _context.TransactionItems.RemoveRange(existing.Items);
                existing.Items.Clear();

                var newItems = new List<TransactionItem>();
                foreach (var itemDto in request.Items)
                {
                    var item = new TransactionItem
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = existing.Id,
                        Name = itemDto.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TotalPrice = itemDto.TotalPrice,
                        CreatedAt = DateTime.UtcNow
                    };

                    var (itemIsValid, itemError) = item.Validate();
                    if (!itemIsValid)
                        return BadRequest(itemError);

                    newItems.Add(item);
                }

                _context.TransactionItems.AddRange(newItems);
                existing.Items = newItems;
            }

            await _context.SaveChangesAsync();

            var response = new DetailedTransactionResponse
            {
                Id = existing.Id,
                CategoryId = existing.CategoryId,
                Type = existing.Type,
                TotalAmount = existing.TotalAmount,
                CurrencyId = existing.CurrencyId,
                Comment = existing.Comment,
                Date = existing.Date,
                CreatedAt = existing.CreatedAt,
                SyncedAt = existing.SyncedAt,
                IsDeleted = existing.IsDeleted,
                Items = existing.Items.Select(i => new TransactionItemResponse
                {
                    Id = i.Id,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };

            return Ok(response);
        }

        // PUT: api/transactions/recover/{id}
        [HttpPut("recover/{id:guid}")]
        public async Task<IActionResult> Recover([FromRoute] Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transaction = (await _context.GetFilteredAsync<Transaction>(
                t => t.UserId == user.Id &&
                     t.Id == id)).FirstOrDefault<Transaction>();

            if (transaction is null) return NotFound();

            transaction.IsDeleted = false;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/transactions/soft-delete/{id}
        [HttpDelete("soft-delete/{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transaction = (await _context.GetFilteredAsync<Transaction>(
                t => t.UserId == user.Id &&
                     t.Id == id)).FirstOrDefault<Transaction>();

            if (transaction is null) return NotFound();

            transaction.IsDeleted = true;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/transactions/hard-delete/{id}
        [HttpDelete("hard-delete/{id:guid}")]
        public async Task<IActionResult> HardDelete([FromRoute] Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transaction = _context.Transactions
                .Where(t => t.UserId == user.Id &&
                            t.Id == id)
                .FirstOrDefault();

            if (transaction is null) return NotFound();

            await _context.DeleteItemAsync(transaction);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
