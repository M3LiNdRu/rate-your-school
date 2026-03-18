using Domain.Entities;

namespace RateYourSchool.Endpoints.GetSchools;


internal sealed record SchoolInfo(string Name, string Address, string ImageUrl);
internal sealed record UserReview(string UserName, string Comment, decimal Rating);

internal sealed record SchoolViewModel
{
    public required SchoolInfo Info { get; init; }
    public required Score Score { get; init; }
    public required IEnumerable<UserReview> Reviews { get; init; }
}
