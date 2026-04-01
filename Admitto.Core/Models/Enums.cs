namespace Admitto.Core.Models
{
    public enum NotificationTrigger
    {
        BookingConfirmation = 0,
        BookingCancellation = 1,
        EventReminder = 2,
        EventCreated = 3,
        EventUpdated = 4,
        EventDeleted = 5,
        RoleChanged = 6,
        ProfileUpdated = 7
    }

    public enum MediaType
    {
        Thumbnail = 0,
        Gallery = 1
    }

    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Canceled = 2,
        Postponed = 3
    }

    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        Failed = 2,
        Canceled = 3,
        Refunded = 4
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Canceled = 3,
        Refunded = 4
    }
}
