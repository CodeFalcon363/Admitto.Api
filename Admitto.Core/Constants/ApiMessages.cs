namespace Admitto.Core.Constants
{
    public static class ApiMessages
    {   //Auth
        public const string LoginSuccess = "Login successful.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string RegistrationSuccess = "Registration successful.";
        public const string UserAlreadyExists = "User with this email already exists.";
        public const string TokenExpired = "Token has expired.";
        public const string TokenInvalid = "Invalid token.";

        //Events
        public const string EventCreated = "Event created successfully.";
        public const string EventUpdated = "Event updated successfully.";
        public const string EventDeleted = "Event deleted successfully.";
        public const string EventNotFound = "Event not found.";
        public const string Unauthorized = "You are not authorized to perform this action.";

        //TicketTypes
        public const string TicketTypeCreated = "Ticket type created successfully.";
        public const string TicketTypeUpdated = "Ticket type updated successfully.";
        public const string TicketTypeDeleted = "Ticket type deleted successfully.";
        public const string TicketTypeNotFound = "Ticket type not found.";
        public const string InsufficientTickets = "Not enough tickets available for the requested quantity.";
        public const string InsufficientCapacity = "This ticket type is sold out.";
        public const string TicketSalesEnded = "Ticket sales have ended for this event.";

        //Bookings
        public const string BookingCreated = "Booking created successfully.";
        public const string BookingCanceled = "Booking canceled successfully.";
        public const string BookingNotFound = "Booking not found.";

        //Payments
        public const string PaymentProcessed = "Payment processed successfully.";
        public const string PaymentFailed = "Payment processing failed.";
        public const string PaymentNotFound = "Payment not found.";
        public const string PaymentAlreadyProcessed = "Payment has already been processed.";

        //General
        public const string InternalServerError = "An unexpected error occurred. Please try again later.";
        public const string BadRequest = "Invalid request. Please check your input and try again.";
        public const string NotFound = "The requested resource was not found.";
        public const string UnauthorizedAccess = "You are not authorized to access this resource.";
    }
}
