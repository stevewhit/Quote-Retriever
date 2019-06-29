using Framework.Generic.EntityFramework;
using log4net;
using QR.Business.Services;
using StockMarket.Generic.Downloaders;
using StockMarket.DataModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using StockMarket.Generic.Downloaders.IEXCloud;
using StockMarket.Generic.Downloaders.IEXCloud.JSON_Objects;
using Ninject;
using System.Reflection;

namespace QR.App
{
    public class Program
    {
        private static readonly DateTime DEFAULT_START_DATE = new DateTime(2000, 1, 1);
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            IKernel kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
            
            var companyService = kernel.Get<ICompanyService<Company>>();
            var quoteService = kernel.Get<IQuoteService<Quote>>();
            var marketService = kernel.Get<IMarketService<Company, Quote>>();

            var company = marketService.DownloadCompany("AAPL");
            if (companyService.FindCompany(company.Id) == null)
                companyService.Add(company);

            //marketService.UpdateCompanyWithLatestQuotes(company.Id);

            try
            {
                // Download data for each active company and update the database.
                //var activeCompanies = companyService.GetCompaniesForQuoteDownload();
                //foreach (var company in activeCompanies)
                {
                    // possibly fire up multiple tasks to download all data. This will require async updating of database..
                    // Might be possible by NOT saving changes to db 
                    
                    // Get the latest quote date that has been stored for this company; otherwise a default value
                    //var lastStoredQuoteDate = companyService.GetMostRecentQuoteDate(company) ?? DEFAULT_START_DATE;

                    //var downloadedQuotes = downloader.DownloadQuotes(company.Symbol, lastStoredQuoteDate.AddDays(1));

                    // Add all quotes to database
                    //quoteService.Add(downloadedQuotes);
                }
            }
            catch
            {
                // Exception thrown during download
            }
            finally
            {
                marketService.Dispose();
            }
        }
    }
}