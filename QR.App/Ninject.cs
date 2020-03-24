using Framework.Generic.EntityFramework;
using log4net;
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
            var token = ConfigurationManager.AppSettings["IEXCloudToken"];
            
            var context = new EfContext(new SMAContext());

            Bind<IEfRepository<Company>>().ToMethod(_ => new EfRepository<Company>(context));
            Bind<IEfRepository<Quote>>().ToMethod(_ => new EfRepository<Quote>(context));

            Bind<ICompanyService<Company>>().ToConstructor(_ => new CompanyService<Company>(Kernel.Get<IEfRepository<Company>>())).InThreadScope();
            Bind<IQuoteService<Quote>>().ToConstructor(_ => new QuoteService<Quote>(Kernel.Get<IEfRepository<Quote>>())).InThreadScope();
            Bind<IMarketDownloader<Company, Quote>>().ToConstructor(_ => new EXCloudWrapper(token)).InThreadScope();

            Bind<IMarketService<Company, Quote>>().ToConstructor(_ => new MarketService<Company, Quote>(Kernel.Get<ICompanyService<Company>>(), Kernel.Get<IQuoteService<Quote>>(), Kernel.Get<IMarketDownloader<Company, Quote>>()));

            Bind<ILog>().ToMethod(_ => LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType));
        }
    }
}
