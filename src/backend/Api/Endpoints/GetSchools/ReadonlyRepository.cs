
namespace RateYourSchool.Endpoints.GetSchools;

internal sealed class ReadonlyRepository : IReadOnlyRepository<SchoolViewModel>
{
    private readonly List<SchoolViewModel> _schools;

    public ReadonlyRepository()
    {
        _schools = BuildSchoolList();
    }

    public async Task<IEnumerable<SchoolViewModel>> GetAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await Task.FromResult(_schools.Skip((page - 1) * pageSize).Take(pageSize)).ConfigureAwait(false);
    }

    private static List<SchoolViewModel> BuildSchoolList()
    {
        return new List<SchoolViewModel>
        {
            //new SchoolViewModel { Info = { Name = "Springfield High", Address = "742 Evergreen Terrace" } },
            //new SchoolViewModel { Info = { Name = "Shelbyville High", Address = "123 Main St" } },
            //new SchoolViewModel { Info = { Name = "Capital City High", Address = "456 Elm St" } }
        };
    }
}
