using AspireBoot.Infrastructure.Postgres;

namespace AspireBoot.ApiService.Middlewares;

public sealed class UnitOfWorkMiddleware(RequestDelegate requestDelegate)
{
    public async Task Invoke(HttpContext httpContext, IUnitOfWork unitOfWork)
    {
        try
        {
            await requestDelegate(httpContext).ConfigureAwait(false);
            await unitOfWork.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }
}
