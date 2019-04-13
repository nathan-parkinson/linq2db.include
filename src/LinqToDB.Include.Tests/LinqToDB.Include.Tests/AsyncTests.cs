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
using System.Threading.Tasks;

namespace Tests
{
    public class AsyncTests
    {
        [SetUp]
        public void Setup()
        {
            DataConnection.DefaultSettings = DataConnection.DefaultSettings ?? new DBConnection();
        }

        [Test]
        public async Task ToListAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.ToListAsync();

                Assert.IsTrue(p is List<Person>);
                Assert.AreEqual(p.Count, 1);
                Assert.AreEqual(p.First().Orders.Count(), 49);
            }
        }

        [Test]
        public async Task ToArrayAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.ToArrayAsync();

                Assert.IsTrue(p is Person[]);
                Assert.AreEqual(p.Length, 1);
                Assert.AreEqual(p.First().Orders.Count(), 49);
            }
        }

        [Test]
        public async Task ToDictionaryAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.ToDictionaryAsync(x => x.PersonId);

                Assert.IsTrue(p is Dictionary<int, Person>);
                Assert.AreEqual(p.Count, 1);
                Assert.AreEqual(p.First().Value.Orders.Count(), 49);
            }
        }



        [Test]
        public async Task FirstAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.FirstAsync();

                Assert.IsTrue(p is Person);
                Assert.AreEqual(p.Orders.Count(), 49);
            }
        }


        [Test]
        public async Task FirstOrDefaultAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.FirstOrDefaultAsync();

                Assert.IsTrue(p is Person);
                Assert.AreEqual(p.Orders.Count(), 49);
            }
        }


        [Test]
        public async Task FirstOrDefaultAsyncWhereNullTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.FirstOrDefaultAsync();

                Assert.IsNull(p);
            }
        }

        [Test]
        public async Task SingleAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.PersonId == 1
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.SingleAsync();

                Assert.IsTrue(p is Person);
                Assert.AreEqual(p.Orders.Count(), 49);
            }
        }


        [Test]
        public async Task SingleOrDefaultAsyncTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.PersonId == 1
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.SingleOrDefaultAsync();

                Assert.IsTrue(p is Person);
                Assert.AreEqual(p.Orders.Count(), 49);
            }
        }


        [Test]
        public async Task SingleOrDefaultAsyncWhereNullTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                var query = from q in db.People
                            where
                                q.PersonId == 1
                            select q;

                query = query.Include(x => x.Orders);
                var p = await query.SingleOrDefaultAsync();

                Assert.IsNull(p);
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

            var orders = Enumerable.Range(1, 200).Select(x => new Order
            {
                PersonId = x % 2 == 0 ? person.PersonId : personOther.PersonId,
                OrderNumber = x.ToString().PadLeft(5, '0'),
                OrderedOn = DateTime.Now

            });

            db.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows }, orders);
        }

        private Action<FluentMappingBuilder> _mapping1 = builder =>
        {
            builder.Entity<Person>()
                .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
                .Property(x => x.FirstName).HasLength(100).IsNullable(false)
                .Property(x => x.LastName).HasLength(100).IsNullable(false)
                .Property(x => x.Dob).IsNullable(false)
                .Property(x => x.Salary).IsNullable(false)
                .Property(x => x.Weight).IsNullable(false)
                .Property(x => x.SpouseId).IsNullable()
                .Property(x => x.Spouse).IsNotColumn()
                .Property(x => x.Orders).IsNotColumn()
                .HasAttribute(new AssociationAttribute { ExpressionPredicate = nameof(Person.ExtraJoinOptions), ThisKey = nameof(Person.PersonId), OtherKey = nameof(Order.PersonId), CanBeNull = true });

            builder.Entity<Order>()
                        .Property(x => x.ProductLines).IsNotColumn()
                        .Property(x => x.OrderId).IsIdentity().IsPrimaryKey().IsNullable(false)
                        .Property(x => x.OrderNumber).HasLength(100).IsNullable(false)
                        .Property(x => x.OrderedOn).IsNullable(false).HasSkipOnUpdate()
                        .Property(x => x.PersonId).IsNullable(false)
                        .Property(x => x.Person).IsNotColumn()
                        .HasAttribute(new AssociationAttribute { ExpressionPredicate = nameof(Order.ExtraJoinOptions), ThisKey = nameof(Order.PersonId), OtherKey = nameof(Person.PersonId), CanBeNull = false });
        };

        private Action<FluentMappingBuilder> _mapping4 = builder =>
        {
            builder.Entity<Person>()
                .Association(x => x.Orders, (p, o) => p.PersonId <= o.PersonId && o.OrderId < 99)
                .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
                .Property(x => x.FirstName).HasLength(100).IsNullable(false)
                .Property(x => x.LastName).HasLength(100).IsNullable(false)
                .Property(x => x.Dob).IsNullable(false)
                .Property(x => x.Salary).IsNullable(false)
                .Property(x => x.Weight).IsNullable(false)
                .Property(x => x.SpouseId).IsNullable()
                .Property(x => x.Spouse).IsNotColumn()
                .Property(x => x.Orders).IsNotColumn();

            builder.Entity<Order>()
                        .Association(x => x.Person, (o, p) => p.PersonId <= o.PersonId && o.OrderId < 99)
                        .Property(x => x.ProductLines).IsNotColumn()
                        .Property(x => x.OrderId).IsIdentity().IsPrimaryKey().IsNullable(false)
                        .Property(x => x.OrderNumber).HasLength(100).IsNullable(false)
                        .Property(x => x.OrderedOn).IsNullable(false).HasSkipOnUpdate()
                        .Property(x => x.PersonId).IsNullable(false)
                        .Property(x => x.Person).IsNotColumn();
        };


        public class DBContext : DataConnection
        {
            public DBContext(Action<FluentMappingBuilder> mappings) : base("DBConn")
            {
                var schema = new MappingSchema();
                var builder = schema.GetFluentMappingBuilder();

                mappings?.Invoke(builder);

                this.AddMappingSchema(schema);

                this.CreateTable<Person>();
                this.CreateTable<Order>();
            }

            public ITable<Person> People => GetTable<Person>();
            public ITable<Order> Orders => GetTable<Order>();
        }
    }
}