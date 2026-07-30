using FluentValidation;

namespace ScholarFlow.Modules.Notifications.Commands.DeactivateNotificationDevice;

public sealed class DeactivateNotificationDeviceCommandValidator : AbstractValidator<DeactivateNotificationDeviceCommand>
{
    public DeactivateNotificationDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();
    }
}
