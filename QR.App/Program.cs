using StockMarket.DataModel;
using Ninject;
using System.Reflection;
using QR.Business.Services;
using System;

namespace QR.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IKernel kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());

            var marketService = kernel.Get<IMarketService<Company, Quote>>();

            try
            {
                marketService.UpdateAllCompanyDetailsAsync().Wait();
                marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            }
            catch (Exception e)
            {
                //throw e;
                kernel.Get<ILog>().Error("An error occurred when updating companies with the latest quotes..", e);
            }
            finally
            {
                marketService.Dispose();
                kernel.Dispose();
            }

            Console.ReadKey();
        }
    }
}