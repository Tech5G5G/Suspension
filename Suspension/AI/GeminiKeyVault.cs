using Windows.Security.Credentials;

namespace Suspension.AI
{
    /// <summary>
    /// Represents a vault that stores a user's Gemini API key.
    /// </summary>
    internal static class GeminiKeyVault
    {
        private const string ResourceName = "2A7417A8-694C-4936-8E0F-5C4C6016E463",
            UserName = "Gemini API Key";

        private static readonly PasswordVault vault = new();

        /// <summary>
        /// Gets or sets the stored Gemini API key.
        /// </summary>
        public static string Key
        {
            get
            {
                try
                {
                    return vault.Retrieve(ResourceName, UserName).Password;
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    vault.Remove(vault.Retrieve(ResourceName, UserName));
                else
                    vault.Add(new(ResourceName, UserName, value));

                KeyChanged?.Invoke(null, value);
            }
        }

        /// <summary>
        /// Occurs after <see cref="Key"/> is changed.
        /// </summary>
        public static event EventHandler<string> KeyChanged;
    }
}
