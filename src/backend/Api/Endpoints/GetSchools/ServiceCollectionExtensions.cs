namespace RateYourSchool.Endpoints.GetSchools;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddGetSchoolsEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IHandler<Request, Response>, Handler>();
        services.AddScoped<IReadOnlyRepository<SchoolViewModel>, ReadonlyRepository>();
        return services;
    }
}
