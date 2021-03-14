﻿using Accounting.DataManager.Models;
using System.Collections.Generic;

namespace Accounting.DataManager.DataAccess
{
    public interface IUserData
    {
        List<UserModel> GetUserById(string Id);
    }
}