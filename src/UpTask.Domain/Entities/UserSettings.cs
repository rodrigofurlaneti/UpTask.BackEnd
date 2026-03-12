using UpTask.Domain.Common;

namespace UpTask.Domain.Entities;

public sealed class UserSettings : BaseEntity
{
    public Guid UserId { get; private set; }
    public bool NotifyByEmail { get; private set; } = true;
    public bool NotifyByPush { get; private set; } = true;
    public bool NotifyDeadline { get; private set; } = true;
    public bool NotifyAssignment { get; private set; } = true;
    public bool NotifyComment { get; private set; } = true;
    public bool NotifyMention { get; private set; } = true;
    public string DefaultView { get; private set; } = "list";
    public string Theme { get; private set; } = "system";
    public string Language { get; private set; } = "pt-BR";
    public int WeekStartsOn { get; private set; } = 0;
    public string DateFormat { get; private set; } = "DD/MM/YYYY";

    private UserSettings() { }

    public static UserSettings CreateDefault(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    public void Update(bool notifyEmail, bool notifyPush, string defaultView, string theme, string language)
    {
        NotifyByEmail = notifyEmail;
        NotifyByPush = notifyPush;
        DefaultView = defaultView;
        Theme = theme;
        Language = language;
        SetUpdatedAt();
    }
}
