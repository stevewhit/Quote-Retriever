using Framework.Generic.EntityFramework;
using StockMarket.DataModel;
using StockMarket.Generic.Services;

namespace QR.Business.Services
{
    public interface ICompanyService<T> : ICompanyServiceBase<T> where T : Company
    {
        
    }

    public class CompanyService<T> : CompanyServiceBase<T>, ICompanyService<T> where T : Company
    {
        public CompanyService(IEfRepository<T> repository)
            : base (repository)
        {

        }
    }
}
