using HomeBudgetServer.Data;
using HomeBudgetServer.Resources;
using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System.Linq.Expressions;

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
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            return (await Get(t => t.UserId == user.Id));
        }

        // GET: api/transactions/considered
        [HttpGet("considered")]
        public async Task<IActionResult> GetConsidered()
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            //return (await Get(t => t.UserId == user.Id &&
            //                       t.IsConsidered &&
            //                       !t.IsDeleted));
            var transactions = await _context.Transactions
                .Include("User")
                .AsNoTracking()
                .Where(t => t.UserId == user.Id && !t.IsDeleted)
                .ToListAsync();
            //List<Transaction> transactions = [.. (await _context.GetFilteredAsync<Transaction>(t => t.UserId == user.Id))];
            return Ok(transactions);
        }

        // GET: api/transactions/not-considered
        [HttpGet("not-considered")]
        public async Task<IActionResult> GetNotConsidered()
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            return (await Get(t => t.UserId == user.Id &&
                                   !t.IsConsidered &&
                                   !t.IsDeleted));
        }

        // GET: api/transactions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAll([FromRoute] Guid id)
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
                );

            var transactionItems = new List<TransactionItemResponse>();

            foreach (TransactionItem transactionItem in transaction.Items)
            {
                transactionItems.Add(CreateTransactionItemResponse(
                    transactionItem));
            }

            return Ok(CreateDetailedTransactionResponse(transaction,
                user.Id,
                transactionItems));
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

                if (currency is null)
                    return BadRequest(String.Format(
                        Messages.Error_InvalidCurrencyId,
                        request.CurrencyId.Value));

                currencyId = request.CurrencyId.Value;
            }
            else if (user.CurrencyId.HasValue)
                currencyId = (Guid)user.CurrencyId;
            else
            {
                return BadRequest(Messages.Error_InvalidCurrencyId);
            }

            var transaction = new Transaction
            {
                Id = request.Id,
                UserId = user.Id,
                Type = request.Type,
                Date = request.Date,
                TotalAmount = request.TotalAmount,
                CurrencyId = currencyId,
                CategoryId = request.CategoryId,
                IsConsidered = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = request.UpdatedAt,
                SyncedAt = request.SyncedAt,
                IsDeleted = false
            };

            var (isValid, errorMessage) = transaction.Validate();
            if (!isValid)
                return BadRequest(errorMessage);

            if (transaction.CategoryId.HasValue) 
            {
                Category? category = await _context.Categories
                    .FindAsync(transaction.CategoryId);

                if (category is null)
                    return BadRequest(String.Format(
                        Messages.Error_InvalidCategoryId,
                        transaction.CategoryId.Value));
                
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
                    var item = CreateTransactionItem(itemDto,
                        transaction);

                    var (itemIsValid, itemError) = item.Validate();
                    if (!itemIsValid)
                        return BadRequest(itemError);

                    transactionItemsResponse
                        .Add(CreateTransactionItemResponse(item));

                    items.Add(item);
                }

                _context.TransactionItems.AddRange(items);
                transaction.Items.AddRange(items);
            }

            await _context.SaveChangesAsync();

            return Ok(CreateDetailedTransactionResponse(
                transaction,
                user.Id,
                transactionItemsResponse));
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
                return Forbid(Messages.Error_AccessDenied);

            // inserting values
            {
                existing.Type = request.Type;
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
                var currency = await _context.Currencies.
                    FindAsync(request.CurrencyId.Value);

                if (currency == null)
                    return BadRequest(String.Format(
                        Messages.Error_InvalidCurrencyId,
                        request.CurrencyId.Value));

                existing.CurrencyId = currency.Id;
                existing.Currency = currency;
            }

            if (existing.CategoryId != request.CategoryId)
            {
                if (request.CategoryId.HasValue)
                {
                    var category = await _context.Categories
                        .FindAsync(request.CategoryId);

                    if (category == null)
                        return BadRequest(String.Format(
                            Messages.Error_InvalidCategoryId,
                            request.CategoryId.Value));

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
                    var item = CreateTransactionItem(itemDto,
                        existing);

                    var (itemIsValid, itemError) = item.Validate();
                    if (!itemIsValid)
                        return BadRequest(itemError);

                    newItems.Add(item);
                }

                _context.TransactionItems.AddRange(newItems);
                existing.Items = newItems;
            }

            await _context.SaveChangesAsync();

            return Ok(CreateDetailedTransactionResponse(
                existing,
                user.Id,
                [.. existing.Items.Select(i =>
                    CreateTransactionItemResponse(i))]));
        }

        // PUT: api/transactions/recover/{id}
        [HttpPut("recover/{id:guid}")]
        public async Task<IActionResult> Recover([FromRoute] Guid id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            var transaction = (await _context.GetFilteredAsync<Transaction>(
                t => t.UserId == user.Id &&
                     t.Id == id))
                     .FirstOrDefault();

            if (transaction is null) return NotFound();

            transaction.IsConsidered = true;

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
                     t.Id == id))
                     .FirstOrDefault();

            if (transaction is null) return NotFound();

            transaction.IsConsidered = false;

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

            transaction.IsDeleted = true;

            await _context.SaveChangesAsync();

            return Ok();
        }


        private async Task<IActionResult> Get(Expression
            <Func<Transaction, bool>> predicate)
        {
            var transactions = await _context.GetFilteredAsync(predicate);

            var result = new List<TransactionResponse>();

            foreach (Transaction transaction in transactions)
                result.Add(CreateTransactionResponse(transaction));

            return Ok(result);
        }


        private TransactionResponse CreateTransactionResponse(Transaction t)
        {
            return new TransactionResponse
            {
                Id = t.Id,
                UserId = t.UserId,
                CategoryId = t.CategoryId,
                Type = t.Type,
                TotalAmount = t.TotalAmount,
                CurrencyId = t.CurrencyId,
                Comment = t.Comment,
                Date = t.Date,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                SyncedAt = t.SyncedAt,
                IsConsidered = t.IsConsidered,
                IsDeleted = t.IsDeleted
            };
        }

        private TransactionItemResponse CreateTransactionItemResponse(
            TransactionItem ti)
        {
            return new TransactionItemResponse
            {
                Id = ti.Id,
                Name = ti.Name,
                Quantity = ti.Quantity,
                UnitPrice = ti.UnitPrice,
                TotalPrice = ti.TotalPrice
            };
        }

        private DetailedTransactionResponse CreateDetailedTransactionResponse(
            Transaction t,
            Guid ui,
            List<TransactionItemResponse>? ti = null)
        {
            return new DetailedTransactionResponse
            {
                Id = t.Id,
                UserId = ui,
                CategoryId = t.CategoryId,
                Type = t.Type,
                TotalAmount = t.TotalAmount,
                CurrencyId = t.CurrencyId,
                Comment = t.Comment,
                Date = t.Date,
                IsConsidered = t.IsConsidered,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                SyncedAt = t.SyncedAt,
                IsDeleted = t.IsDeleted,
                Items = ti
            };
        }

        private TransactionItem CreateTransactionItem(
            CreateTransactionItemRequest ti,
            Transaction t)
        {
            return new TransactionItem
            {
                Id = ti.Id,
                TransactionId = t.Id,
                Name = ti.Name,
                Quantity = ti.Quantity,
                UnitPrice = ti.UnitPrice,
                TotalPrice = ti.TotalPrice,
                CreatedAt = DateTime.UtcNow,
                Transaction = t
            };
        }
    }
}
