using Framework.Generic.EntityFramework;
using log4net;
using StockMarket.DataModel;
using Ninject;
using System.Reflection;
using QR.Business.Services;
using System;
using System.Collections.Generic;
using Framework.Generic.Utility;
using System.Linq;
using StockMarket.Generic.Downloaders;

namespace QR.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IKernel kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());

            //var fact = kernel.Get<IEfContextFactory>();

            //var context = new EfContext(new SMAContext());
            //var companyRepo = new EfRepository<Company>(context);
            //var quoteRepo = new EfRepository<Quote>(context);

            //var companyService = new CompanyService<Company>(companyRepo);
            //var quoteService = new QuoteService<Quote>(quoteRepo);
            //var marketService = new MarketService<Company, Quote>(companyService, quoteService, kernel.Get<IMarketDownloader<Company, Quote>>());

            //marketService.UpdateAllCompaniesWithLatestQuotes();

            //    quoteService.Save();


            var marketService = kernel.Get<IMarketService<Company, Quote>>();



            //var serc = kernel.Get<ICompanyService<Company>>();
            //ser.Add(marketService.DownloadCompany("AMZN"));

            marketService.UpdateAllCompanyDetails();


            //var ser = kernel.Get<IQuoteService<Quote>>();
            //var quotes = CreateFakeQuotes(serc.GetCompanies().First(), 10, 0);
            //ser.Add(quotes);

            marketService.UpdateAllCompaniesWithLatestQuotes();
            //kernel.Get<IEfRepository<Quote>>().SaveChanges();
            //try
            //{
            //    marketService.UpdateAllCompanyDetails();
            //    marketService.UpdateAllCompaniesWithLatestQuotes();
            //}
            //catch (Exception e)
            //{
            //    kernel.Get<ILog>().Error("An error occurred when updating companies with the latest quotes..", e);
            //}
            //finally
            //{
            //    marketService.Dispose();
            //}
        }

        public static IEnumerable<Quote> CreateFakeQuotes(Company company, int numDays, int numDaysSkipped)
        {
            for (int i = numDaysSkipped + 1; i <= numDays + numDaysSkipped; i++)
            {
                yield return new Quote()
                {
                    Id = i,
                    Company = company,
                    CompanyId = company?.Id ?? 0,
                    Date = DateTime.Now.AddDays((-1 * i)).Date,
                    Close = NumberUtils.GenerateRandomNumber(1, 200),
                    High = NumberUtils.GenerateRandomNumber(1, 200),
                    Low = NumberUtils.GenerateRandomNumber(1, 200),
                    Open = NumberUtils.GenerateRandomNumber(1, 200),
                    Volume = NumberUtils.GenerateRandomNumber(1, 1000000)
                };
            }
        }
    }
}