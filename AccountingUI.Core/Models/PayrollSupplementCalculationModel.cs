﻿namespace AccountingUI.Core.Models
{
    public class PayrollSupplementCalculationModel : PayrollSupplementEmployeeModel
    {
        private int _accountingId;
        public int AccountingId
        {
            get { return _accountingId; }
            set { SetProperty(ref _accountingId, value); }
        }
    }
}
