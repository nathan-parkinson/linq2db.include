using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include
{
    public class PrimaryKeyNotFoundException : Exception
    {
        public PrimaryKeyNotFoundException()
        {

        }

        public PrimaryKeyNotFoundException(string message) : base(message)
        {

        }
    }
}
