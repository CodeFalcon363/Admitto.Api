namespace Admitto.Core.Models.Requests
{
    public class RegisterUserRequest : LoginRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

    }
}