using BCrypt.Net;
using BCryptNet = BCrypt.Net.BCrypt;

namespace BTAPLON.Utilities
{
    public static class PasswordHelper
    {
        private const int WorkFactor = 12;

        public static bool IsBcryptHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$");
        }

        public static string HashPassword(string password)
        {
            return BCryptNet.HashPassword(password, workFactor: WorkFactor);
        }

        public static bool TryVerify(string password, string? storedHash, out bool shouldUpgradeHash)
        {

            shouldUpgradeHash = false;
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            if (IsBcryptHash(storedHash))
            {

                try
                {
                    var verified = BCryptNet.Verify(password, storedHash);
                    shouldUpgradeHash = verified && BCryptNet.PasswordNeedsRehash(storedHash, WorkFactor);
                    return verified;
                }
                catch (SaltParseException)
                {
                    shouldUpgradeHash = true;
                    return false;
                }
            }

            if (storedHash == password)
            {
                shouldUpgradeHash = true;
                return true;
            }

            return false;
        }
    }
}