using RaywattOCTFFR.Models;
using System;

namespace RaywattOCTFFR.Services
{
    public interface IPasswordService
    {
        int PasswordExpiryDays { get; }
        static void ResetPasswordCount() => PasswordService.ResetPasswordCount();

        bool CheckLoginWithRetryCount(string id, string inputPassword);

        /// <summary>
        /// Checks if the input password matches the existing password
        /// </summary>
        bool IsPasswordConfirmed(string beforePassword, string inputPassword, string message1, string message2);

        bool IsPasswordCorrect(string beforePassword, string inputPassword, string message);

        /// <summary>
        /// Checks if the input password does not match the existing password
        /// </summary>
        bool IsNotSamePassword(string beforePassword, string inputPassword, string message1, string message2);

        User? GetAccount(string id);

        /// <summary>
        /// Validates the password and returns an error message if it does not meet the criteria
        /// </summary>
        string? GetPasswordValidationError(string password);

        /// <summary>
        /// Displays an alert dialog with the specified title and message
        /// </summary>
        void ShowAlert(string title, string message);

        public void ShowTimerAlert(string title, string message, bool isShowButton, TimeSpan? timeSpan);

        /// <summary>
        /// Updates the password for a user, resetting it if necessary
        /// </summary>
        bool UpdatePasswordReset(string id, string password, string beforePassowrd);

        string  MessagePasswordChangedSuccessfully { get; }
    }
}
