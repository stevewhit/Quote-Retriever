using Framework.Generic.EntityFramework;
using StockMarket.DataModel;
using StockMarket.Generic.Services;

namespace QR.Business.Services
{
    public interface IQuoteService<T> : IBaseQuoteService<T> where T : Quote
    {

    }

    public class QuoteService<T> : BaseQuoteService<T>, IQuoteService<T> where T : Quote
    {
        public QuoteService(IEfRepository<T> repository)
            : base(repository)
        {
            
        }
    }
}
