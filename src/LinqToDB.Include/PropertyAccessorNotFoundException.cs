using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include
{
    public class PropertyAccessorNotFoundException : Exception
    {
        public PropertyAccessorNotFoundException()
        {

        }

        public PropertyAccessorNotFoundException(string message) : base(message)
        {

        }
    }
}
