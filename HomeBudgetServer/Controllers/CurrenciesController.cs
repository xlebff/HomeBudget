using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeBudgetServer.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    public class CurrenciesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var currencies = await _context.GetAllAsync<Currency>();
            return Ok(currencies);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null)
                return NotFound();
            return Ok(currency);
        }

        [HttpGet("{code:length(3)}")]
        public async Task<IActionResult> Get(string code)
        {
            var currency = await _context.GetFilteredAsync<Currency>(
                c => c.Code == code);
            if (!currency.Any())
                return NotFound();
            return Ok(currency);
        }
    }
}
