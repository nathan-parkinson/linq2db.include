using LinqToDB.Data;
using LinqToDB.Include;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Tests
{
    public class ThenIncludeTests
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

            public static Expression<Func<Person, Order, bool>> ExtraJoinOptions = (p, o) => p.FirstName != o.OrderNumber;
        }

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

        public class ProductLine
        {
            public int ProductLineId { get; set; }
            public int OrderId { get; set; }

            public ProductLineType ProductLineType { get; set; } = ProductLineType.Normal;

            public int ProductId { get; set; }
            public string ProductCode { get; set; }


            public Order Order { get; set; }
        }

        public enum ProductLineType
        {
            Normal = 0,
            Extended = 1
        }

        public class ExtendedProductLine : ProductLine
        {
            public ExtendedProductLine()
            {
                ProductLineType = ProductLineType.Extended;
            }

            public Person FirstPerson { get; set; }
        }

        
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Test1()
        {
            var schema = new MappingSchema();
            var builder = schema.GetFluentMappingBuilder();
            
                builder.Entity<Person>()
                    .Association(x => x.Orders, p => p.PersonId, o => o.First().PersonId)
                    .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
                    .Property(x => x.FirstName).HasLength(100).IsNullable(false)
                    .Property(x => x.LastName).HasLength(100).IsNullable(false)
                    .Property(x => x.Dob).IsNullable(false)
                    .Property(x => x.Salary).IsNullable(false)
                    .Property(x => x.Weight).IsNullable(false)
                    .Property(x => x.SpouseId).IsNullable()
                    .Property(x => x.Spouse).IsNotColumn().Association(x => x.Spouse, p => p.SpouseId, s => s.PersonId)
                    .Property(x => x.Orders).IsNotColumn();

                
                builder.Entity<Order>()                    
                    .Association(x => x.ProductLines, (o, p) => o.OrderId == p.OrderId, true)
                    .Property(x => x.OrderId).IsIdentity().IsPrimaryKey().IsNullable(false)
                    .Property(x => x.OrderNumber).HasLength(100).IsNullable(false)
                    .Property(x => x.OrderedOn).IsNullable(false).HasSkipOnUpdate()
                    .Property(x => x.PersonId).IsNullable(false)
                    .Property(x => x.Person).IsNotColumn()
                    .HasAttribute(new AssociationAttribute { ThisKey = nameof(Order.PersonId), OtherKey = nameof(Person.PersonId), CanBeNull = false });
                

                builder.Entity<ProductLine>()
                    .Inheritance(x => x.ProductLineType, ProductLineType.Normal, typeof(ProductLine), true)
                    .Inheritance(x => x.ProductLineType, ProductLineType.Extended, typeof(ExtendedProductLine))
                    .Property(x => x.ProductLineId).IsIdentity().IsPrimaryKey().IsNullable(false)
                    .Property(x => x.OrderId).IsNullable(false)
                    .Property(x => x.ProductLineType).IsNullable(false).IsDiscriminator()
                    .Property(x => x.ProductId).IsNullable(false)
                    .Property(x => x.ProductCode).HasLength(20).IsNullable(false)
                    .Association(x => x.Order, p => p.OrderId, o => o.OrderId);

                builder.Entity<ExtendedProductLine>()
                    .HasAttribute(new TableAttribute { Name = nameof(ProductLine), IsColumnAttributeRequired = true })
                    .Association(x => x.FirstPerson, (pl, p) => p.PersonId == 1);

            using (var db = new DataConnection("SQLite", "DataSource=:memory:", schema))
            {
                var query = from p in db.GetTable<Person>()
                            select p;

                var query2 = query
                    .Include(x => x.Orders.First());

              // var query3 = query2.Include(x => x.Orders.First().ProductLines.First());

               query2.ThenInclude(x => x.ProductLines.First());
            }


                Assert.Pass();
        }
    }
}