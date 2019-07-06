using log4net;
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
            }
            catch (AggregateException e)
            {
                LogRecursive(kernel.Get<ILog>(), e, "Error occured downloading stock details");
            }

            try
            {
                marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            }
            catch (AggregateException e)
            {
                LogRecursive(kernel.Get<ILog>(), e, "Error occured downloading stock data");
            }
            
            marketService.Dispose();
            kernel.Dispose();
        }

        /// <summary>
        /// Logs the inner exceptions of an AggregateException separately.
        /// </summary>
        private static void LogRecursive(ILog log, AggregateException e, string message)
        {
            foreach (var innerException in e.InnerExceptions)
            {
                if (innerException is AggregateException)
                    LogRecursive(log, innerException as AggregateException, message);
                else
                    log.Error($"{message}", innerException);
            }
        }
    }
}