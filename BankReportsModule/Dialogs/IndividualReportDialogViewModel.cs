﻿using AccountingUI.Core.Models;
using AccountingUI.Core.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BankkStatementsModule.Dialogs
{
    class IndividualReportDialogViewModel : BindableBase, IDialogAware
    {
        private readonly IDialogService _showDialog;
        private readonly IAccountPairsEndpoint _accoutPairsEndpoint;
        private readonly IBankReportEndpoint _bankReportEndpoint;

        private readonly string _bookName = "Izvodi";

        public IndividualReportDialogViewModel(IDialogService showDialog,
                                               IAccountPairsEndpoint accoutPairsEndpoint,
                                               IBankReportEndpoint bankReportEndpoint)
        {
            _showDialog = showDialog;
            _accoutPairsEndpoint = accoutPairsEndpoint;
            _bankReportEndpoint = bankReportEndpoint;

            AccountsLinkCommand = new DelegateCommand(AddNewPair, CanAddPair);
            CellValueChanged = new DelegateCommand(SumSides);
            SaveDataCommand = new DelegateCommand(SaveReportToDatabase, CanSaveReport);
        }

        public DelegateCommand AccountsLinkCommand { get; private set; }
        public DelegateCommand CellValueChanged { get; private set; }
        public DelegateCommand SaveDataCommand { get; private set; }

        public string Title => "Pojedinačni izvod";

        public event Action<IDialogResult> RequestClose;

        private BankReportModel _reportHeader;
        public BankReportModel ReportHeader
        {
            get { return _reportHeader; }
            set { SetProperty(ref _reportHeader, value); }
        }

        private ObservableCollection<BankReportItemModel> _reportItems = new();
        public ObservableCollection<BankReportItemModel> ReportItems
        {
            get { return _reportItems; }
            set { SetProperty(ref _reportItems, value); }
        }

        private BankReportItemModel _selectedEntry;
        public BankReportItemModel SelectedEntry
        {
            get { return _selectedEntry; }
            set 
            { 
                SetProperty(ref _selectedEntry, value);
                AccountsLinkCommand.RaiseCanExecuteChanged();
                SaveDataCommand.RaiseCanExecuteChanged();
            }
        }

        private decimal _sumDugovna;
        public decimal SumDugovna
        {
            get { return _sumDugovna; }
            set { SetProperty(ref _sumDugovna, value); }
        }

        private decimal _sumPotrazna;
        public decimal SumPotrazna
        {
            get { return _sumPotrazna; }
            set { SetProperty(ref _sumPotrazna, value); }
        }

        private bool _controlFlag;
        public bool ControlFlag
        {
            get { return _controlFlag; }
            set { SetProperty(ref _controlFlag, value); }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            ReportHeader = parameters.GetValue<BankReportModel>("header");
            var list =parameters.GetValue<List<BankReportItemModel>>("itemsList");
            ReportItems = new ObservableCollection<BankReportItemModel>(list);
            LoadAccountPairs();
            SumSides();
        }

        private void SumSides()
        {
            SumDugovna = ReportItems.Sum(x => x.Dugovna);
            SumPotrazna = ReportItems.Sum(x => x.Potrazna);
            ReportChangesControl();
            SaveDataCommand.RaiseCanExecuteChanged();
        }

        private void ReportChangesControl()
        {
            ControlFlag = ReportHeader.StanjePrethodnogIzvoda + SumPotrazna - SumDugovna == ReportHeader.NovoStanje;
        }

        private async void LoadAccountPairs()
        {
            var pairs = await _accoutPairsEndpoint.GetByBookName(_bookName);

            foreach (var reportEntry in ReportItems)
            {
                FindPair(pairs, reportEntry);
            }

            SaveDataCommand.RaiseCanExecuteChanged();
        }

        private void FindPair(List<AccountPairModel> pairs, BankReportItemModel reportEntry)
        {
            List<AccountPairModel> result = new();
            if (pairs.Count != 0)
            {
                result = pairs.Where(
                    p => p.Name == reportEntry.Naziv)
                    .DefaultIfEmpty(new AccountPairModel()).ToList();
            }

            if (result.Count > 1)
            {
                foreach (var item in result)
                {
                    if (reportEntry.Opis.Contains(item.Description))
                    {
                        reportEntry.Konto = item.Account;
                        return;
                    }
                }
            }
            else
            {
                reportEntry.Konto = result.DefaultIfEmpty(new AccountPairModel()).First().Account;
            }
        }

        private bool CanAddPair()
        {
            return SelectedEntry != null;
        }

        private void AddNewPair()
        {
            var param = new DialogParameters();
            param.Add("bankReport", SelectedEntry);
            _showDialog.ShowDialog("AccountPairDialog", param, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    LoadAccountPairs();
                }
            });
        }

        private bool CanSaveReport()
        {
            foreach (var item in ReportItems)
            {
                if (item.Konto == null || item.Konto.Length < 3)
                {
                    return false;
                }
            }

            return true && ControlFlag;
        }

        private async void SaveReportToDatabase()
        {
            bool result = await _bankReportEndpoint.PostHeader(ReportHeader);
            int headerId = 0;
            if (result)
            {
                headerId = await _bankReportEndpoint.GetHeaderId(ReportHeader.RedniBroj);
            }
            //TODO: If report exists, update? replace?
            if(headerId != 0)
            {
                foreach(var item in ReportItems)
                {
                    item.IdIzvod = headerId;
                }
            }

            result = await _bankReportEndpoint.PostItems(ReportItems.ToList());

            if(result)
            {
                var dialogResult = ButtonResult.OK;
                RequestClose?.Invoke(new DialogResult(dialogResult));
            }
        }
    }
}
