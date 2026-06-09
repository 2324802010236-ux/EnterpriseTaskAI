namespace EnterpriseTask.Domain.Enums;

public enum NotificationType
{
    TaskCreated = 1,
    TaskAssigned = 2,
    TaskStatusChanged = 3,
    TaskCommented = 4,
    DeadlineReminder = 5,
    ChatMessage = 6,
    System = 7,
    AiSuggestion = 8
}
