using System;

namespace ToStringGeneratorSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var user = new User();
            user.Name = "Damien";
            user.Gender = Gender.Female;

            Console.WriteLine(user.ToString());
            Console.ReadKey();
        }
    }
}
