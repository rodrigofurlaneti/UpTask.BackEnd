using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpTask.Domain.Enums
{
    public enum NotificationType
    {
        DeadlineApproaching = 1,
        TaskAssigned = 2,
        Comment = 3,
        Mention = 4,
        Completion = 5,
        Reminder = 6,
        ProjectInvite = 7,
        System = 8
    }
}
