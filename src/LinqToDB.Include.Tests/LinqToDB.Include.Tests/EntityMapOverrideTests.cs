using App.Domain.Repository.Mapping;
using LinqToDB.Data;
using LinqToDB.Include.Tests.TestModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Include;
using System.Linq.Expressions;

namespace Tests
{
    public class EntityMapOverrideTests
    {
        [SetUp]
        public void Setup()
        {
            DataConnection.DefaultSettings = DataConnection.DefaultSettings ?? new DBConnection();
        }

        [Test]
        public void TestCustomSelectClause()
        {
            using (var db = new DBContext())
            {
                AddData(db);
                
                Expression<Func<Person, Person>> exp2 = person => BuildPerson(person, person.Spouse);
                EntityMapOverride.Set(exp2);

                var query = from p2 in db.People select p2;                
                query = query.AsIncludable();                
                var p = query.ToList();

                Assert.IsTrue(p is List<Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }


        [Test]
        public void TestRemovingCustomSelectClause()
        {
            using (var db = new DBContext())
            {
                AddData(db);

                Expression<Func<Person, Person>> exp2 = person => BuildPerson(person, person.Spouse);
                EntityMapOverride.Set(exp2);
                EntityMapOverride.Set<Person>(null);

                var query = from p2 in db.People select p2;
                query = query.AsIncludable();
                var p = query.ToList();

                Assert.IsTrue(p is List<Person>);
                Assert.AreEqual(p.Count, 2);
                Assert.AreEqual(p.Count(x => x.Spouse != null), 1);
            }
        }


        public static Person BuildPerson(Person p, Person spouse)
        {
            p.Spouse = spouse;

            return p;
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
                
                this.CreateTable<Person>();                
            }

            public ITable<Person> People => GetTable<Person>();            
        }
    }
}
