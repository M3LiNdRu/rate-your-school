namespace Domain.Entities;
public abstract class Entity : IEntity
{
    private bool _internalStateChanged;
    private List<EventLog> _eventLogs;
    protected Entity(IEnumerable<EventLog> eventLogs)
    {
        ArgumentNullException.ThrowIfNull(eventLogs);

        _internalStateChanged = false;
        _eventLogs = eventLogs.ToList();
    }

    public abstract string Id { get; }
    public IReadOnlyList<EventLog> EventLogs => _eventLogs.AsReadOnly();

    protected void MarkStateChanged()
    {
        _internalStateChanged = true;
    }
    protected bool IsStateChanged() => _internalStateChanged;

    protected void AddEventLog(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        _eventLogs.Add(new EventLog(DateTime.UtcNow, description));
        MarkStateChanged();
    }
}
