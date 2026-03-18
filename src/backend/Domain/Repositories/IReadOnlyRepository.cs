namespace RateYourSchool.Endpoints.GetSchools;

public interface IReadOnlyRepository<T>
{
    Task<IEnumerable<T>> GetAsync(int page, int pageSize, CancellationToken cancellationToken);
}