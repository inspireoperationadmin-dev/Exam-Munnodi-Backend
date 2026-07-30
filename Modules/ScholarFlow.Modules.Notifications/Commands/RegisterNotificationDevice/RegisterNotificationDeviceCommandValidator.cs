using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Notifications.Commands.RegisterNotificationDevice;

public sealed class RegisterNotificationDeviceCommandValidator : AbstractValidator<RegisterNotificationDeviceCommand>
{
    public RegisterNotificationDeviceCommandValidator()
    {
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.DeviceName).MaximumLength(120);
        RuleFor(x => x.UserAgent).MaximumLength(512);
        RuleFor(x => x.AppVersion).MaximumLength(40);

        When(x => x.Provider == NotificationProvider.WebPush, () =>
        {
            RuleFor(x => x.Platform)
                .Equal(NotificationPlatform.WebPwa)
                .WithMessage("WebPush provider must use WebPwa platform.");
            RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(2048);
            RuleFor(x => x.P256dh).NotEmpty().MaximumLength(512);
            RuleFor(x => x.Auth).NotEmpty().MaximumLength(512);
        });

        When(x => x.Provider is NotificationProvider.Fcm or NotificationProvider.Apns, () =>
        {
            RuleFor(x => x.Platform)
                .Must(platform => platform is NotificationPlatform.Android or NotificationPlatform.Ios)
                .WithMessage("Native providers must use Android or iOS platform.");
            RuleFor(x => x.PushToken).NotEmpty().MaximumLength(4000);
        });

        When(x => x.Provider == NotificationProvider.Apns, () =>
        {
            RuleFor(x => x.Platform)
                .Equal(NotificationPlatform.Ios)
                .WithMessage("APNS provider must use iOS platform.");
        });
    }
}
