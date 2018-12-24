using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include
{
    public class SingleAssociationNotFoundException : Exception
    {
        private const string DEFAULT_MESSAGE = "Single Association Not Found";
        public SingleAssociationNotFoundException() : base(DEFAULT_MESSAGE)
        {

        }
        
        public SingleAssociationNotFoundException(string message) : base(message)
        {

        }

        public SingleAssociationNotFoundException(Exception innerException, string message = DEFAULT_MESSAGE)
            : base(message, innerException)
        {

        }
    }
}
