namespace RaywattApp.Services
{
    public interface IPasswordService
    {
        int PasswordExpiryDays { get; }
        void ResetPasswordCount();
        bool CheckLoginWithRetryCount(string id, string inputPassword);

        /// <summary>
        /// Checks if the input password matches the existing password
        /// </summary>
        bool IsSamePassword(string beforePassword, string inputPassword, string message = "");

        /// <summary>
        /// Checks if the input password does not match the existing password
        /// </summary>
        bool IsNotSamePassword(string beforePassword, string inputPassword, string message = ""); 

        /// <summary>
        /// Retrieves the password for a user by their ID
        /// </summary>
        string GetPasswordByUserId(string id); // 

        /// <summary>
        /// Validates the password and returns an error message if it does not meet the criteria
        /// </summary>
        string? GetPasswordValidationError(string password);

        /// <summary>
        /// Displays an alert dialog with the specified title and message
        /// </summary>
        void ShowAlert(string title, string message);

        /// <summary>
        /// Updates the password for a user, resetting it if necessary
        /// </summary>
        bool UpdatePasswordReset(string id, string password, string before_passowrd, bool admin);
    }
}
