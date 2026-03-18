namespace Domain.Entities;

public enum SchoolType
{
    Public,
    Private,
    Charter,
    Magnet
}
public enum EducationGradeType
{
    Elementary,
    Middle,
    High,
    K12
}
public record GeoLocation(double Latitude, double Longitude);
public record EventLog(DateTime Timestamp, string Description);
public record Score(decimal Innovation, decimal Building, decimal Professorate, decimal ManagementTeam)
{
    public decimal Total => (Innovation + Building + Professorate + ManagementTeam) / 4;
}

public class School : Entity
{
    public School(
        string name,
        string address,
        string city,
        SchoolType type,
        IEnumerable<EducationGradeType> grades,
        GeoLocation location,
        IEnumerable<EventLog> eventLogs) : base(eventLogs)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(grades);

        Id = Guid.NewGuid().ToString();
        Name = name;
        Address = address;
        City = city;
        Location = location;
        Type = type;
        Grades = grades.ToList();
        IsPublished = false;
        Score = new Score(0, 0, 0, 0);
    }

    public School(string id, 
        string name, 
        string address, 
        string city, 
        SchoolType type, 
        IEnumerable<EducationGradeType> grades, 
        GeoLocation location,
        bool isPublished,
        Score score,
        IEnumerable<EventLog> eventLogs) : base(eventLogs)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(grades);
        ArgumentNullException.ThrowIfNull(score);

        Id = id ?? Guid.NewGuid().ToString();
        Name = name;
        Address = address;
        City = city;
        Location = location;
        Type = type;
        Grades = grades.ToList();
        IsPublished = isPublished;
        Score = score;
    }

    public override string Id { get; }
    public string Name { get; }
    public string Address { get; }
    public string City { get; }
    public SchoolType Type { get; }
    public IEnumerable<EducationGradeType> Grades { get; } = Enumerable.Empty<EducationGradeType>();
    public bool IsPublished { get; }
    public Score Score { get; }
    public GeoLocation Location { get; }

    public void Create()
    {
        base.AddEventLog($"School {this.Name} created.");
        base.MarkStateChanged();
    }

    public void Publish()
    {
        if (IsPublished)
            return;
        base.AddEventLog($"School {this.Name} activated.");
        base.MarkStateChanged();
    }

    public void Unpublish()
    {
        if (!IsPublished)
            return;
        base.AddEventLog($"School {this.Name} deactivated.");
        base.MarkStateChanged();
    }
}

