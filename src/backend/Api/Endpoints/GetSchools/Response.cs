namespace RateYourSchool.Endpoints.GetSchools;

internal sealed record Response
{
    public required IEnumerable<SchoolViewModel> Schools { get; init; }
}
