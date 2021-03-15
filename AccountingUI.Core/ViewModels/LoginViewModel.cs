﻿using MvvmCross.Commands;
using MvvmCross.ViewModels;
using AccountingUI.Core.Models;
using System.Collections.ObjectModel;
using AccountingUI.Core.Helpers;
using System;
using System.Threading.Tasks;

namespace AccountingUI.Core.ViewModels
{
    class LoginViewModel : MvxViewModel
    {
        public IMvxCommand AddUserCommand { get; set; }

        private ObservableCollection<UserModel> _user = new();

        private IApiHelper _apiHelper;
        public LoginViewModel(IApiHelper apiHelper)
        {
            _apiHelper = apiHelper;

            AddUserCommand = new MvxCommand(AddUser);
        }

        public ObservableCollection<UserModel> User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set 
            {
                SetProperty(ref _userName, value);
                RaisePropertyChanged(() => UserName);
                RaisePropertyChanged(() => CanAddUser);
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set 
            { 
                SetProperty(ref _password, value);
                RaisePropertyChanged(() => Password);
                RaisePropertyChanged(() => CanAddUser);
            }
        }

        public bool IsErrorVisible
        {
            get 
            {
                bool output = false;
                if(ErrorMessage?.Length > 0)
                {
                    output = true;
                }

                return output;
            }
        }

        private string _errorMessage;

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set 
            {
                SetProperty(ref _errorMessage, value);
                RaisePropertyChanged(() => IsErrorVisible);
                RaisePropertyChanged(() => ErrorMessage);
            }
        }


        public bool CanAddUser => UserName?.Length > 0 && Password?.Length > 0;

        public void AddUser()
        {
            UserModel user = new()
            {
                UserName = UserName,
                Password = Password
            };

            UserName = string.Empty;
            Password = string.Empty;

            User.Add(user);

            _ = LogIn(user);
        }

        private async Task LogIn(UserModel user)
        {
            try
            {
                ErrorMessage = "";
                var result = await _apiHelper.Authenticate(user.UserName, user.Password);

                //capture user info
                await _apiHelper.GetLoggedInUserInfo(result.Access_Token);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
