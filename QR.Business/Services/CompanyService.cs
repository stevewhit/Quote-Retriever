using Framework.Generic.EntityFramework;
using StockMarket.DataModel;
using StockMarket.Generic.Services;

namespace QR.Business.Services
{
    public interface ICompanyService<T> : IBaseCompanyService<T> where T : Company
    {
        
    }

    public class CompanyService<T> : BaseCompanyService<T>, ICompanyService<T> where T : Company
    {
        public CompanyService(IEfRepository<T> repository)
            : base (repository)
        {

        }
    }
}
