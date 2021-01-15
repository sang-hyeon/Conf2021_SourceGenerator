﻿namespace ToStringGeneratorSample
{
    using ToStringGenerator;

    [ToStringGenerator]
    public partial class User
    {
        public string Name { get; set; }

        public Gender Gender { get; set; }
    }

    public enum Gender
    {
        Male,
        Female
    }
}
