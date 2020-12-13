namespace Noname
{
    using ToStringGenerator;

    [ToStringGenerator]
    public partial class User
    {
        public string Name { get; set; }

        private int _age;
    }
}
