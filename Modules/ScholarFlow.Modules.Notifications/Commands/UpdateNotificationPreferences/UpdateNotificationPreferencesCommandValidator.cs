using FluentValidation;

namespace ScholarFlow.Modules.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.DailyReminderTime)
            .NotEmpty()
            .Matches(@"^([01]\d|2[0-3]):[0-5]\d$")
            .WithMessage("Daily reminder time must be in HH:mm format.");

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(80);
    }
}
