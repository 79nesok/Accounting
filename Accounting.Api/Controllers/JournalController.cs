﻿using Accounting.DataManager.DataAccess;
using Accounting.DataManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Accounting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class JournalController : ControllerBase
    {
        private readonly IAccountingJournalData _data;

        public JournalController(IAccountingJournalData data)
        {
            _data = data;
        }

        [HttpGet]
        public List<AccountingJournalModel> Get()
        {
            return _data.GetHeaders();
        }

        [HttpGet("Unprocessed/")]
        public List<AccountingJournalModel> GetUnprocessed()
        {
            return _data.GetUnprocessedHeaders();
        }
        
        [HttpPost("Details/")]
        public List<AccountingJournalModel> GetDetails([FromBody] AccountingJournalModel model)
        {
            return _data.GetJournalDetails(model);
        }

        // GET api/<JournalController>/5
        [HttpGet("{bookNumber}")]
        public List<AccountingJournalModel> Get(int bookNumber)
        {
            return _data.GetByBookNumber(bookNumber);
        }

        // POST api/<JournalController>
        [HttpPost]
        public void Post([FromBody] List<AccountingJournalModel> list)
        {
            _data.Insert(list);
        }

        // DELETE api/<JournalController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
