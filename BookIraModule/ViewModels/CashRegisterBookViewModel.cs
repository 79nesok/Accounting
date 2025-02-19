﻿using AccountingUI.Core.Models;
using AccountingUI.Core.Services;
using AccountingUI.Core.TabControlRegion;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BookIraModule.ViewModels
{
    public class CashRegisterBookViewModel : ViewModelBase
    {
        private readonly IXlsFileReader _xlsFileReader;
        private readonly IDialogService _showDialog;
        private readonly IBookAccountSettingsEndpoint _settingsEndpoint;
        private readonly ICashRegisterBookEndpoint _cashRegisterBookEndpoint;
            
        private readonly string _bookName;
        private bool _loaded = false;
        private int _maxRedniBroj;

        public CashRegisterBookViewModel(IXlsFileReader xlsFileReader,
                                         IDialogService showDialog,
                                         IBookAccountSettingsEndpoint settingsEndpoint,
                                         ICashRegisterBookEndpoint cashRegisterBookEndpoint)
        {
            _xlsFileReader = xlsFileReader;
            _showDialog = showDialog;
            _settingsEndpoint = settingsEndpoint;
            _cashRegisterBookEndpoint = cashRegisterBookEndpoint;

            _bookName = "Blagajna";

            LoadDataCommand = new DelegateCommand(LoadDataFromFile);
            SaveDataCommand = new DelegateCommand(SaveToDatabase, CanSaveItems);
            AccountsSettingsCommand = new DelegateCommand(OpenAccountsSettings);
            FilterDataCommand = new DelegateCommand(FilterItems);
            ProcessItemCommand = new DelegateCommand(ProcessItem, CanProcess);
            CalculationsReportCommand = new DelegateCommand(SumColumns);
        }

        #region Delegate commands
        public DelegateCommand LoadDataCommand { get; private set; }
        public DelegateCommand SaveDataCommand { get; private set; }
        public DelegateCommand AccountsSettingsCommand { get; private set; }
        public DelegateCommand FilterDataCommand { get; private set; }
        public DelegateCommand ProcessItemCommand { get; private set; }
        public DelegateCommand CalculationsReportCommand { get; private set; }
        #endregion

        #region Properties
        private ObservableCollection<CashRegisterModel> _bookitems;
        public ObservableCollection<CashRegisterModel> BookItems
        {
            get { return _bookitems; }
            set
            {
                SetProperty(ref _bookitems, value);
                SaveDataCommand.RaiseCanExecuteChanged();
            }
        }

        private CashRegisterModel _selectedBookItem;
        public CashRegisterModel SelectedBookItem
        {
            get { return _selectedBookItem; }
            set
            {
                SetProperty(ref _selectedBookItem, value);
                ProcessItemCommand.RaiseCanExecuteChanged();
            }
        }

        private ICollectionView _filteredView;
        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get { return _dateFrom; }
            set
            {
                SetProperty(ref _dateFrom, value);
            }
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get { return _dateTo; }
            set
            {
                SetProperty(ref _dateTo, value);
            }
        }

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set { SetProperty(ref _filePath, value); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        private List<BookAccountsSettingsModel> _accountingSettings;
        public List<BookAccountsSettingsModel> AccountingSettings
        {
            get { return _accountingSettings; }
            set { SetProperty(ref _accountingSettings, value); }
        }

        private decimal _cashSum;
        public decimal CashSum
        {
            get { return _cashSum; }
            set { SetProperty(ref _cashSum, value); }
        }

        private decimal _cardsSum;
        public decimal CardsSum
        {
            get { return _cardsSum; }
            set { SetProperty(ref _cardsSum, value); }
        }

        private decimal _totalSum;
        public decimal TotalSum
        {
            get { return _totalSum; }
            set { SetProperty(ref _totalSum, value); }
        }

        private decimal _participationSum;
        public decimal ParticipationSum
        {
            get { return _participationSum; }
            set { SetProperty(ref _participationSum, value); }
        }
        #endregion

        public async void LoadBook()
        {
            StatusMessage = "Učitavam podatke iz baze...";
            var primke = await _cashRegisterBookEndpoint.GetAll();
            StatusMessage = "";
            BookItems = new ObservableCollection<CashRegisterModel>(primke);
            FilterItems();
            LoadAccountingSettings();
        }

        #region Load accounting settings
        private void OpenAccountsSettings()
        {
            var list = new List<string>() { "Gotovina", "Kreditne kartice", "Sveukupno", "Iznos sudjelovanja"};
            var parameters = new DialogParameters();
            parameters.Add("columnsList", list);
            parameters.Add("bookName", _bookName);
            _showDialog.ShowDialog("AccountsLinkDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                }
            });
            LoadAccountingSettings();
        }

        private async void LoadAccountingSettings()
        {
            AccountingSettings = await _settingsEndpoint.GetByBookName(_bookName);
        }
        #endregion

        #region Filtering datagrid
        private void FilterItems()
        {
            _filteredView = CollectionViewSource.GetDefaultView(BookItems);
            _filteredView.Filter = o => FilterData((CashRegisterModel)o);

            SumColumns();
        }

        private bool FilterData(CashRegisterModel o)
        {
            if (DateFrom != null || DateTo != null)
            {
                return o.Datum >= DateFrom && o.Datum <= DateTo;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region Sum currency columns
        private void SumColumns()
        {
            var filteredItems = _filteredView.Cast<CashRegisterModel>().ToList();
            CashSum = filteredItems.Sum(x => x.Gotovina);
            CardsSum = filteredItems.Sum(x => x.KreditneKartice);
            TotalSum = filteredItems.Sum(x => x.Sveukupno);
            ParticipationSum = filteredItems.Sum(x => x.IznosSudjelovanja);
        }
        #endregion

        #region Load data from file
        private void LoadDataFromFile()
        {
            _maxRedniBroj = BookItems.Count > 0 ? BookItems.Max(y => y.Id) : 0;

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Xlsx Files *.xlsx|*.xlsx|Xls Files *.xls|*.xls|Csv files *.csv|*.csv",
                FilterIndex = 1,
                Multiselect = false
            };

            Nullable<bool> result = ofd.ShowDialog();
            if (result != null && result == true)
            {
                FilePath = ofd.FileName;
                var data = _xlsFileReader.Convert(FilePath, "PREGLED PROMETA BLAGAJNE");
                if (data != null)
                {
                    FromStringToList(data);
                    _loaded = true;
                }
            }
        }

        private void FromStringToList(DataSet data)
        {
            BookItems = new ObservableCollection<CashRegisterModel>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                if (!int.TryParse(row[0].ToString(), out _))
                {
                    continue;
                }
                AddDataToList(row);
            }
        }

        private void AddDataToList(DataRow val)
        {
            BookItems.Add(new CashRegisterModel
            {
                RedniBroj = int.Parse(val[0].ToString()),
                Datum = DateTime.Parse(val[1].ToString()),
                Gotovina = decimal.Parse(val[2].ToString()),
                KreditneKartice = decimal.Parse(val[3].ToString()),
                Sveukupno = decimal.Parse(val[6].ToString()),
                IznosSudjelovanja = decimal.Parse(val[8].ToString()),
            });
        }
        #endregion

        #region Save to database
        private bool CanSaveItems()
        {
            return BookItems != null && _loaded;
        }

        private async void SaveToDatabase()
        {
            IEnumerable<CashRegisterModel> primke = BookItems.Where(x => x.RedniBroj > _maxRedniBroj);
            var list = new List<CashRegisterModel>(primke);

            StatusMessage = "Zapisujem u bazu podataka...";
            await _cashRegisterBookEndpoint.PostItems(list);
            StatusMessage = ""; ;

            _loaded = false;
            LoadBook();
        }
        #endregion

        #region Book item processing
        private Dictionary<string, decimal> MapColumnToPropertyValue()
        {
            var bookItem = SelectedBookItem;
            var item = new Dictionary<string, decimal>();
            item.Add("Gotovina", bookItem.Gotovina);
            item.Add("Kreditne kartice", bookItem.KreditneKartice);
            item.Add("Sveukupno", bookItem.Sveukupno);
            item.Add("Iznos sudjelovanja", bookItem.IznosSudjelovanja);

            return item;
        }

        private async Task<List<AccountingJournalModel>> CreateJournalEntries()
        {
            var mappings = MapColumnToPropertyValue();
            var entry = SelectedBookItem;
            var entries = new List<AccountingJournalModel>();
            foreach (var setting in AccountingSettings)
            {
                var value = mappings.GetValueOrDefault(setting.Name);
                value *= setting.Prefix ? (-1) : 1;
                if (value != 0)
                {
                    entries.Add(new AccountingJournalModel
                    {
                        Broj = entry.RedniBroj,
                        Dokument = setting.Name + ": " + entry.Datum?.ToString("dd.MM.yyyy.") ,
                        Datum = entry.Datum,
                        Opis = setting.Name,
                        Konto = setting.Account,
                        Dugovna = setting.Side == "Dugovna" ? value : 0,
                        Potrazna = setting.Side == "Potražna" ? value : 0,
                        Valuta = "HRK",
                        VrstaTemeljnice = _bookName
                    });
                }
            }
            return entries;
        }

        private bool CanProcess()
        {
            return SelectedBookItem != null && !SelectedBookItem.Knjizen;
        }

        private async void ProcessItem()
        {
            var entries = await CreateJournalEntries();
            var parameters = new DialogParameters();
            parameters.Add("entries", entries);
            _showDialog.ShowDialog("ProcessToJournal", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    SelectedBookItem.Knjizen = true;
                    _cashRegisterBookEndpoint.MarkAsProcessed(SelectedBookItem.RedniBroj);
                }
            });
        }
        #endregion
    }
}
