using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoundationHttpClientDemo.Common
{
    public static class AuthTokenUtils
    {
        public static string GenerateAuthToken(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return string.Empty;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(username)) +
                   Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
        }

        public static bool IsTokenValid(string token, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            return GenerateAuthToken(username, password) == token;
        }
    }
}