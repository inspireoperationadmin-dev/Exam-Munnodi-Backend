using MediatR;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery : IRequest<NotificationPreferenceDto>;
