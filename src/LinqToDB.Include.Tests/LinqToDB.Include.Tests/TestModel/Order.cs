using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Include.Tests.TestModel
{
    public class Order
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public int PersonId { get; set; }
        public DateTime OrderedOn { get; set; } = DateTime.Now;

        public Person Person { get; set; }
        public List<ProductLine> ProductLines { get; set; } = new List<ProductLine>();

        public static Expression<Func<Order, Person, bool>> ExtraJoinOptions = (o, p) => o.PersonId == p.PersonId && p.FirstName != o.OrderNumber;
    }
}
