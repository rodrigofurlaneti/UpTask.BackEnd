namespace UpTask.Domain.Enums;

public enum UserProfile { Admin = 1, Manager = 2, Member = 3 }
public enum UserStatus { Active = 1, Inactive = 2, Suspended = 3 }
public enum ProjectStatus { Draft = 1, Active = 2, Paused = 3, Completed = 4, Cancelled = 5 }
public enum Priority { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum TaskStatus { Pending = 1, InProgress = 2, InReview = 3, Completed = 4, Cancelled = 5 }
public enum RecurrenceType { Daily = 1, Weekly = 2, Biweekly = 3, Monthly = 4, Yearly = 5 }
public enum MemberRole { Viewer = 1, Collaborator = 2, Editor = 3, Admin = 4 }
public enum DependencyType { Blocks = 1, Related = 2, Duplicate = 3 }
public enum NotificationType
{
    DeadlineApproaching = 1, TaskAssigned = 2, Comment = 3,
    Mention = 4, Completion = 5, Reminder = 6, ProjectInvite = 7, System = 8
}
