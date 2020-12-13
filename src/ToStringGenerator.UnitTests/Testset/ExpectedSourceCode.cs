using ToStringGenerator;

namespace Noname
{
    public partial class User
    {
        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            builder.AppendLine("Name:" + Name.ToString());

            return builder.ToString();
        }
    }
}
