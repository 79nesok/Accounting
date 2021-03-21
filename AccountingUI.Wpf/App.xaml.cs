﻿using AccountingUI.Core.Models;
using AccountingUI.Core.Service;
using AccountingUI.Core.Services;
using AccountingUI.Wpf.Views;
using AutoMapper;
using LoginModule;
using PartnersModule;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;

namespace AccountingUI.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILoggedInUserModel, LoggedInUserModel>();

            containerRegistry.RegisterScoped<IApiService, ApiService>();
            containerRegistry.RegisterScoped<IPartnersEndpoint, PartnersEndpoint>();

            containerRegistry.RegisterForNavigation<StartView>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ModuleLogin>();
            moduleCatalog.AddModule<ModulePartners>();
        }

        //private IMapper ConfigureAutoMapper()
        //{
        //    var config = new MapperConfiguration(cfg =>
        //    {
        //    });
        //}
    }
}
