﻿using AccountingUI.Core.Models;
using AccountingUI.Core.Service;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AccountingUI.Core.Services
{
    public class AccountingJournalEndpoint : IAccountingJournalEndpoint
    {
        private readonly IApiService _apiService;

        public AccountingJournalEndpoint(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<bool> Post(List<AccountingJournalModel> list)
        {
            using (HttpResponseMessage response = await _apiService.ApiClient.PostAsJsonAsync("/api/Journal", list))
            {
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = response.ReasonPhrase;
                    return false;
                }
            }
        }

        public async Task<List<AccountingJournalModel>> LoadUnprocessedJournals()
        {
            using (HttpResponseMessage response = await _apiService.ApiClient.GetAsync("/api/Journal/Unprocessed"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<AccountingJournalModel>>();
                    return result;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }

        public async Task<List<AccountingJournalModel>> LoadJournalDetails(AccountingJournalModel model)
        {
            using (HttpResponseMessage response = await _apiService.ApiClient.PostAsJsonAsync("/api/Journal/Details", model))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<AccountingJournalModel>>();
                    return result;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
    }
}
