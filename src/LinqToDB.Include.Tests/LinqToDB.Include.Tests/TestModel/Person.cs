using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Include.Tests.TestModel
{
    public class Person
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Dob { get; set; }

        public decimal Salary { get; set; }
        public double Weight { get; set; }

        public int? SpouseId { get; set; }
        public Person Spouse { get; set; }

        public List<Order> Orders { get; set; } = new List<Order>();

        public static Expression<Func<Person, Order, bool>> ExtraJoinOptions() => (p, o) => o.OrderId < 99;        
    }
}
