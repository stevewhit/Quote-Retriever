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

            var marketService = kernel.Get<IMarketService<Company, Quote>>();
            marketService.UpdateAllCompanyDetails();
            marketService.UpdateAllCompaniesWithLatestQuotes();
            
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
    }
}