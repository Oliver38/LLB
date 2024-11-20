using System;
using Microsoft.AspNetCore.Identity;

namespace LLB.Helpers
{
	public class PasswordHelper
	{
        public static string GenerateStrongPassword()
        {
            var options = new PasswordOptions
            {
                RequiredLength = 12,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true
            };

            string[] randomChars = new[]
            {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // Uppercase letters
            "abcdefghijkmnopqrstuvwxyz",    // Lowercase letters
            "0123456789",                   // Digits
            "!@$?_-",                       // Non-alphanumeric characters
        };

            Random rand = new Random(Environment.TickCount);
            char[] password = new char[options.RequiredLength];
            int[] positions = new int[password.Length];

            // Ensure each type of character is present
            password[0] = randomChars[0][rand.Next(randomChars[0].Length)];
            password[1] = randomChars[1][rand.Next(randomChars[1].Length)];
            password[2] = randomChars[2][rand.Next(randomChars[2].Length)];
            password[3] = randomChars[3][rand.Next(randomChars[3].Length)];

            // Fill the rest with random characters
            for (int i = 4; i < password.Length; i++)
            {
                string rcs = randomChars[rand.Next(randomChars.Length)];
                password[i] = rcs[rand.Next(rcs.Length)];
            }

            // Shuffle the result
            positions = positions.Select(x => rand.Next(password.Length)).ToArray();
            password = password.OrderBy(x => rand.Next()).ToArray();

            return new string(password);
        }
        }
}

