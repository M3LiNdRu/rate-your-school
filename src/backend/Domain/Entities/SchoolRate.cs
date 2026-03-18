namespace Domain.Entities;

public record SchoolInfo(string Id, string Name);
public class SchoolRate : Entity
{
    private bool _delete;
    public SchoolRate(SchoolInfo school, string description, Score score) : base(Enumerable.Empty<EventLog>())
    {
        Id = Guid.NewGuid().ToString();
        School = school;
        Description = description;
        Score = score;
        _delete = false;
    }

    public SchoolRate(
        string id,
        SchoolInfo school,  
        string description,
        Score score,
        IEnumerable<EventLog> eventLogs) : base(eventLogs)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(school);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(score);

        Id = id;
        School = school;
        Description = description;
        Score = score;
        _delete = false;
    }

    public override string Id { get; }
    public SchoolInfo School { get; }
    public string Description { get; }
    public Score Score { get; }

    public void Create()
    {
        base.AddEventLog($"SchoolRate created for school {this.School.Name}.");
        base.MarkStateChanged();
    }

    public void Delete()
    {
        base.AddEventLog($"SchoolRate deleted for school {this.School.Name}.");
        base.MarkStateChanged();
        _delete = true;
    }
}
