namespace RateYourSchool.Endpoints.GetSchools;

internal sealed class Handler : IHandler<Request, Response>
{
    private readonly IReadOnlyRepository<SchoolViewModel> _repository;

    public Handler(IReadOnlyRepository<SchoolViewModel> repository)
    {
        _repository = repository;
    }

    public async Task<Response> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentPage = request.Page;
        if (currentPage < 1) currentPage = 1;

        var size = request.PageSize;
        size = Math.Clamp(size, 1, 1000);

        var schools = await _repository.GetAsync(currentPage, size, cancellationToken).ConfigureAwait(false);

        return new Response
        {
            Schools = schools
        };
    }
}
