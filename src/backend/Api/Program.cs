using RateYourSchool.Endpoints.GetSchools;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGetSchoolsEndpoint();


var app = builder.Build();

app.MapGet("/", RootEndpoint.GetProjectVersionInfo);

app.MapGetSchoolsEndpoint();

app.Run();

static class RootEndpoint
{
    public static string GetProjectVersionInfo()
    {
        var version = Assembly.GetExecutingAssembly()
                            .GetName()
                            .Version?
                            .ToString();

        return $"RateYourSchool Api v{version}";
    }
}
