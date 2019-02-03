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

namespace Tests
{
    public class ExecuteTests
    {
        [SetUp]
        public void Setup()
        {
            DataConnection.DefaultSettings = DataConnection.DefaultSettings ?? new DBConnection();
        }

        [Test]
        public void FirstMethodAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            where q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.First();

                Assert.IsNotNull(p.Spouse);
            }
        }

        [Test]
        public void FirstMethodWithOverloadAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from x in db.People
                            select x;

                query = query.Include(x => x.Spouse);
                var p = query.First(x => x.FirstName == "Jim");

                Assert.IsNotNull(p.Spouse);
            }
        }

        [Test]
        public void FirstOrDefaultMethodAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            where q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.FirstOrDefault();

                Assert.IsNotNull(p.Spouse);
            }
        }

        [Test]
        public void FirstOrDefaultMethodWithOverloadAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from x in db.People
                            select x;

                query = query.Include(x => x.Spouse);
                var p = query.FirstOrDefault(x => x.FirstName == "Jim");

                Assert.IsNotNull(p.Spouse);
            }
        }


        [Test]
        public void TestCallingExecuteDoesNotChangeQuery()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from x in db.People
                            select x;

                query = query.Include(x => x.Spouse);
                var p1 = query.ElementAt(1);

                var p = query.FirstOrDefault(x => x.FirstName == "Jim");

                Assert.AreEqual(p1.PersonId, p.PersonId);
                Assert.Greater(query.ToList().Count, 1);
                Assert.IsNotNull(p.Spouse);
            }
        }


        [Test]
        public void SingleMethodAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            where q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.Single();

                Assert.IsNotNull(p.Spouse);
            }
        }

        [Test]
        public void SingleMethodWithOverloadAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from x in db.People
                            select x;

                query = query.Include(x => x.Spouse);
                var p = query.Single(x => x.FirstName == "Jim");

                Assert.IsNotNull(p.Spouse);
            }
        }

        [Test]
        public void SingleOrDefaultMethodAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            where q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.SingleOrDefault();

                Assert.IsNotNull(p.Spouse);
            }
        }

        [Test]
        public void SingleOrDefaultMethodWithOverloadAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from x in db.People
                            select x;

                query = query.Include(x => x.Spouse);
                var p = query.FirstOrDefault(x => x.FirstName == "Jim");

                Assert.IsNotNull(p.Spouse);
            }
        }





        [Test]
        public void ElementAtMethodAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            where q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ElementAt(0);

                Assert.IsNotNull(p.Spouse);
                Assert.AreEqual(p.PersonId, 2);
            }
        }



        [Test]
        public void ElementAtWithSkipMethodAddsIncludedEntities()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                var query = from q in db.People
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.ElementAt(1);

                Assert.IsNotNull(p.Spouse);
                Assert.AreEqual(p.PersonId, 2);
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