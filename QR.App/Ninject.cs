﻿using Framework.Generic.EntityFramework;
using Ninject;
using Ninject.Modules;
using QR.Business.Services;
using StockMarket.DataModel;
using StockMarket.Generic.Downloaders;
using StockMarket.Generic.Downloaders.IEXCloud;
using System.Configuration;

namespace QR.App
{
    public class NinjectBindings : NinjectModule
    {
        public override void Load()
        {
            var token = ConfigurationManager.AppSettings["IEXCloudTokenTest"];

            Bind<IEfContext>().ToConstructor(_ => new EfContext(new SMAContext()));
            Bind<IEfRepository<Company>>().ToConstructor(_ => new EfRepository<Company>(Kernel.Get<IEfContext>()));
            Bind<IEfRepository<Quote>>().ToConstructor(_ => new EfRepository<Quote>(Kernel.Get<IEfContext>()));

            Bind<ICompanyService<Company>>().ToConstructor(_ => new CompanyService<Company>(Kernel.Get <IEfRepository<Company>>()));
            Bind<IQuoteService<Quote>>().ToConstructor(_ => new QuoteService<Quote>(Kernel.Get<IEfRepository<Quote>>()));
            Bind<IMarketDownloader<Company, Quote>>().ToConstructor(_ => new EXCloudWrapper(token));

            Bind<IMarketService<Company, Quote>>().ToConstructor(_ => new MarketService<Company, Quote>(Kernel.Get<ICompanyService<Company>>(), Kernel.Get<IQuoteService<Quote>>(), Kernel.Get<IMarketDownloader<Company, Quote>>()));
        }
    }
}
