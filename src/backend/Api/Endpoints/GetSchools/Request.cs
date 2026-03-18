namespace RateYourSchool.Endpoints.GetSchools;

internal sealed record Request
{
    public int Page { get; init; }
    public int PageSize { get; init; }
}