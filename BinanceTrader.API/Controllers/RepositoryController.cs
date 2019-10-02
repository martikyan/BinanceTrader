using System;
using BinanceTrader.Core.DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace BinanceTrader.API.Controllers
{
    [ApiController]
    [Route("[controller]/{action}")]
    public class RepositoryController : Controller
    {
        private readonly IRepository _repository;

        public RepositoryController(IRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet("{userId}")]
        public ObjectResult GetUserById([FromQuery]string userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(userId);
            }

            var user = _repository.GetUserById(userId);
            if (user == null)
            {
                return NotFound(userId);
            }

            return Ok(user);
        }

        [HttpGet("{tradeId}")]
        public ObjectResult GetTradeById([FromQuery]long tradeId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(tradeId);
            }

            var trade = _repository.GetTradeById(tradeId);
            if (trade == null)
            {
                return NotFound(tradeId);
            }

            return Ok(trade);
        }
    }
}