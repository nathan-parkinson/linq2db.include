using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Include.Tests.TestModel;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq.Expressions;
using System.Linq;
using LinqToDB.Include;
using App.Domain.Repository.Mapping;
using System.Collections.Generic;

namespace Tests
{
    public class EnumeratorTests
    {
        [SetUp]
        public void Setup()
        {
            DataConnection.DefaultSettings = DataConnection.DefaultSettings ?? new DBConnection();
        }

        [Test]
        public void ToListAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ToList();

                Assert.IsTrue(p is List<Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }


        [Test]
        public void ToArrayAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ToArray();

                Assert.IsTrue(p is Person[]);
                Assert.AreEqual(p.Length, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }


        [Test]
        public void ToLookupAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ToLookup(x => x.PersonId);

                Assert.IsTrue(p is ILookup<int, Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.IsNotNull(p[2].First().Spouse);
            }
        }

        [Test]
        public void ToDictionaryAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ToDictionary(x => x.PersonId, x => x);

                Assert.IsTrue(p is Dictionary<int, Person>);
                Assert.AreEqual(p.Count, 2);

                Assert.IsNull(p[1].Spouse);
                Assert.IsNotNull(p[2].Spouse);
            }
        }


        [Test]
        public void ToHashSetAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ToHashSet();

                Assert.IsTrue(p is HashSet<Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }

        
        private static void AddData(DBContext db)
        {
            var person = new Person
            {
                FirstName = "Jane",
                LastName = "Jones",
                Dob = new DateTime(2011, 11, 10)
            };

            person.PersonId = db.InsertWithInt32Identity(person);

            var personOther = new Person
            {
                FirstName = "Jim",
                LastName = "Jones",
                Dob = new DateTime(2001, 12, 10),
                SpouseId = person.PersonId
            };

            db.Insert(personOther);


        }

        public class DBContext : DataConnection
        {
            public DBContext() : base("DBConn")
            {
                var builder = MappingSchema.Default.GetFluentMappingBuilder();

                builder.Entity<Person>()
                    .Association(x => x.Orders, (p, o) => p.PersonId == o.PersonId)
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

                this.CreateTable<Person>();
                this.CreateTable<Order>();
                this.CreateTable<ProductLine>();
            }

            public ITable<Person> People => GetTable<Person>();
            public ITable<Order> Orders => GetTable<Order>();
            public ITable<ProductLine> ProductLines => GetTable<ProductLine>();
        }
    }
}