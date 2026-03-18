namespace RateYourSchool.Endpoints.GetSchools;

internal static class GetSchoolsEndpoint
{
    public static IEndpointRouteBuilder MapGetSchoolsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/v1/schools", (int? page, int? pageSize, CancellationToken cancellationToken, IHandler<Request, Response> handler) => {
            var request = new Request
            {
                Page = page.GetValueOrDefault(1),
                PageSize = pageSize.GetValueOrDefault(20)
            };

            return handler.HandleAsync(request, cancellationToken);
        });

        return endpoints;
    }
}
