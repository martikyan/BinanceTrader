﻿using System;
using BinanceTrader.Core.AutoTrader;
using Microsoft.AspNetCore.Mvc;

namespace BinanceTrader.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TraderController : Controller
    {
        private readonly IAutoTrader _autoTrader;

        public TraderController(IAutoTrader autoTrader)
        {
            _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        }

        [HttpGet]
        public ObjectResult GetAttachedUser()
        {
            return Ok(_autoTrader.AttachedUser);
        }

        [HttpGet]
        public ObjectResult GetAttachedUserProfit()
        {
            return Ok(_autoTrader.AttachedUserProfit);
        }

        [HttpGet]
        public ObjectResult GetCurrentWallet()
        {
            return Ok(_autoTrader.CurrentWallet);
        }

        [HttpGet]
        public ObjectResult GetWallets()
        {
            return Ok(_autoTrader.Wallets);
        }

        [HttpGet]
        public ObjectResult GetAttachedUserHistory()
        {
            return Ok(_autoTrader.AttachedUsersHistory);
        }

        [HttpPost]
        public IActionResult DetachAttachedUser()
        {
            _autoTrader.DetachAttachedUser();
            return Ok();
        }

        [HttpPost]
        public IActionResult PauseTrading()
        {
            _autoTrader.PauseTrading();
            return Ok();
        }

        [HttpPost]
        public IActionResult ResumeTrading()
        {
            _autoTrader.ResumeTrading();
            return Ok();
        }

        [HttpPost]
        public IActionResult UpdateCurrentWallet()
        {
            _autoTrader.UpdateCurrentWallet();
            return Ok();
        }
    }
}