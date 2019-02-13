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
    public class EntityMatchingTests
    {
        [SetUp]
        public void Setup()
        {
            DataConnection.DefaultSettings = DataConnection.DefaultSettings ?? new DBConnection();
        }

        [Test]
        public void ExpressionPredicateAndKeysTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = query.ToList();

                Assert.IsTrue(p is List<Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }

        [Test]
        public void PredicateAndKeysTest()
        {
            using (var db = new DBContext(_mapping3))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = query.ToList();

                Assert.IsTrue(p is List<Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }

        [Test]
        public void ExpressionAndNoKeysTest()
        {
            using (var db = new DBContext(_mapping2))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.Orders.Any(x => x.OrderNumber == "00004")
                            select q;

                query = query.Include(x => x.Orders);
                var p = query.ToList();

                Assert.IsTrue(p is List<Person>);
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

        private Action<FluentMappingBuilder> _mapping2 = builder =>
        {
            builder.Entity<Person>()
                .Association(x => x.Orders, (p, o) => p.PersonId == o.PersonId && o.OrderId < 99)
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
                        .Association(x => x.Person, (o, p) => p.PersonId == o.PersonId && o.OrderId < 99)
                        .Property(x => x.ProductLines).IsNotColumn()
                        .Property(x => x.OrderId).IsIdentity().IsPrimaryKey().IsNullable(false)
                        .Property(x => x.OrderNumber).HasLength(100).IsNullable(false)
                        .Property(x => x.OrderedOn).IsNullable(false).HasSkipOnUpdate()
                        .Property(x => x.PersonId).IsNullable(false)
                        .Property(x => x.Person).IsNotColumn();
        };


        private Action<FluentMappingBuilder> _mapping3 = builder =>
        {
            Expression<Func<Person, Order, bool>> personPredicate1 = (p, o) => p.PersonId == o.PersonId && o.OrderId < 99;
            Expression<Func<Person, Order, bool>> personPredicate2 = (p, o) => p.PersonId == o.PersonId || o.OrderId < 99;


            Expression<Func<Person, Order, bool>> personPredicate3 = (p, o) => p.PersonId == o.PersonId && (o.OrderId < 99 || o.OrderId == 104);
            Expression<Func<Person, Order, bool>> personPredicate4 = (p, o) => p.PersonId == o.PersonId && (o.OrderId < 99 && p.Salary < 9);

            Expression<Func<Person, Order, bool>> personPredicate5 = (p, o) => (p.PersonId == o.PersonId && p.LastName == "Jim") && (o.OrderId < 99 && p.Salary < 9);
            Expression<Func<Person, Order, bool>> personPredicate6 = (p, o) => (p.PersonId == o.PersonId && p.LastName == "Jim") || (o.OrderId < 99 && p.Salary < 9);

            Expression<Func<Person, Order, bool>> personPredicate = (p, o) => o.OrderId < 99;            
            builder.Entity<Person>()
                .Association(x => x.Orders, (p, o) => p.PersonId == o.PersonId && o.OrderId < 99)
                .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
                .Property(x => x.FirstName).HasLength(100).IsNullable(false)
                .Property(x => x.LastName).HasLength(100).IsNullable(false)
                .Property(x => x.Dob).IsNullable(false)
                .Property(x => x.Salary).IsNullable(false)
                .Property(x => x.Weight).IsNullable(false)
                .Property(x => x.SpouseId).IsNullable()
                .Property(x => x.Spouse).IsNotColumn()
                .Property(x => x.Orders).IsNotColumn()
                .HasAttribute(new AssociationAttribute
                {
                    Predicate = personPredicate,
                    ThisKey = nameof(Person.PersonId),
                    OtherKey = nameof(Order.PersonId),
                    CanBeNull = true
                });

            Expression<Func<Order, Person, bool>> orderPredicate = (o, p) => o.OrderId < 99;
            builder.Entity<Order>()
                        .Association(x => x.Person, (o, p) => p.PersonId == o.PersonId && o.OrderId < 99)
                        .Property(x => x.ProductLines).IsNotColumn()
                        .Property(x => x.OrderId).IsIdentity().IsPrimaryKey().IsNullable(false)
                        .Property(x => x.OrderNumber).HasLength(100).IsNullable(false)
                        .Property(x => x.OrderedOn).IsNullable(false).HasSkipOnUpdate()
                        .Property(x => x.PersonId).IsNullable(false)
                        .Property(x => x.Person).IsNotColumn()
                        .HasAttribute(new AssociationAttribute
                        {
                            Predicate = orderPredicate,
                            ThisKey = nameof(Order.PersonId),
                            OtherKey = nameof(Person.PersonId),
                            CanBeNull = false
                        });
        };




        public class DBContext : DataConnection
        {
            public DBContext(Action<FluentMappingBuilder> mappings) : base("DBConn")
            {
                var builder = MappingSchema.Default.GetFluentMappingBuilder();

                mappings?.Invoke(builder);


                this.CreateTable<Person>();
                this.CreateTable<Order>();                
            }

            public ITable<Person> People => GetTable<Person>();
            public ITable<Order> Orders => GetTable<Order>();
        }
    }
}