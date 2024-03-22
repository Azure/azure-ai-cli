using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IntegrationTests
{
    /// <summary>
    /// Extensions for the Assert class
    /// </summary>
    public static class AssertExtensions
    {
        /// <summary>
        /// Asserts that the given string contains the given substring
        /// </summary>
        /// <param name="_">The assert object</param>
        /// <param name="contains">The substring to check for</param>
        /// <param name="str">The string to check</param>
        /// <param name="message">The message to display if the assertion fails</param>
        public static void ContainsString(this Assert _, string contains, string? str, string message)
        {
            bool success = str?.Contains(contains) == true;
            if (success)
            {
                return;
            }

            string displayValue = str == null
                ? "<<null>>"
                : str;

            Assert.Fail($"{message} - String did not contain '{contains}'. Got '{displayValue}'");
        }

        /// <summary>
        /// Asserts that the given string has been populated (aka is not null, empty, or whitespace only)
        /// </summary>
        /// <param name="_">The assert object</param>
        /// <param name="str">The string to check</param>
        /// <param name="message">The message to display if the assertion fails</param>
        public static void IsPopulatedString(this Assert _, string? str, string message)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                return;
            }

            string displayValue = str == null
                ? "<<null>>"
                : str;

            Assert.Fail($"{message} - String was not populated. Got '{displayValue}'");
        }

        /// <summary>
        /// Asserts that the given value is not the default value for its type
        /// </summary>
        /// <typeparam name="T">The type of the value type</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="message">The message to display if the assertion fails</param>
        public static void IsNotDefault<T>(this Assert _, T value, string message) where T : struct
        {
            if (!default(T).Equals(value))
            {
                return;
            }

            Assert.Fail($"{message} - Value was the default for its type");
        }

        /// <summary>
        /// Asserts that the given value is the default value for its type
        /// </summary>
        /// <typeparam name="T">The type of the value type</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="message">The message to display if the assertion fails</param>
        public static void IsDefault<T>(this Assert _, T value, string message) where T : struct
        {
            if (default(T).Equals(value))
            {
                return;
            }

            Assert.Fail($"{message} - Value was the default for its type");
        }
    }
}
