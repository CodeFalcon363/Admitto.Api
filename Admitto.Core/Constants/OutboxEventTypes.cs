namespace Admitto.Core.Constants
{
    public static class OutboxEventTypes
    {
        public const string BookingConfirmation = "BookingConfirmation";
        public const string BookingCancellation  = "BookingCancellation";
        public const string EventCreated         = "EventCreated";
        public const string EventUpdated         = "EventUpdated";
        public const string EventDeleted         = "EventDeleted";
        public const string RoleChanged          = "RoleChanged";
        public const string ProfileUpdated       = "ProfileUpdated";
    }
}
