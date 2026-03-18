namespace RateYourSchool.Endpoints;

internal interface IHandler<T1, T2>
{
    Task<T2> HandleAsync(T1 request, CancellationToken cancellationToken);
}