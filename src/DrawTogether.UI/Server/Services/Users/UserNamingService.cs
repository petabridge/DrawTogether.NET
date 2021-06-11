using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Util;

namespace DrawTogether.UI.Server.Services.Users
{
    public static class UserNamingService
    {
        public static readonly string[] SeedNames1 = new[]
        {
            "Icarus", "H3", "himij", "Ruk", "Kristoffer", "Stan",
            "Cheeseburger", "Farmaggedon", "Inflation", "Oracle",
            "Apple", "Dollar", "Big", "Sloppy", "Angry"
        };

        public static readonly string[] SeedNames2 = new[]
        {
            "Ardbeg", "Pappy", "Weller", "TrashCan", "Foo",
            "Bar", "Fuber", "DotNetDrama", "Bulleit", "TheGreat",
            "Pinn", "Swede", "German", "'Merican", "Canadian", "Dane"
        };

        public static string GenerateRandomName()
        {
            var r1 = SeedNames1[ThreadLocalRandom.Current.Next(0, SeedNames1.Length - 1)];
            var r2 = SeedNames2[ThreadLocalRandom.Current.Next(0, SeedNames2.Length - 1)];
            var r3 = ThreadLocalRandom.Current.Next(0, 10000);
            return $"{r1}{r2}{r3}";
        }
    }
}
