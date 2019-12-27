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
        public void ATest()
        {
            using (var db = new DBContext(_mapper1))
            {
                AddData(db);
                MatchUp(db.MappingSchema, typeof(Person));
            }
        }


        private static void MatchUp(MappingSchema schema, Type type)
        {
            foreach(var association in schema.GetEntityDescriptor(type).Associations)
            {
                
            }
        }


        [Test]
        public void FirstMethodAddsIncludedEntities()
        {
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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
            using (var db = new DBContext(_mapper1))
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


        [Test]
        public void FirstMethodAddsIncludedEntitiesWithCompositeKey()
        {
            using (var db = new DBContext(_mapper2))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.First();

                Assert.IsNotNull(p.Spouse);
                Assert.AreEqual(p.PersonId, 2);
            }
        }


        [Test]
        public void FirstMethodAddsIncludedEntitiesWithNoPrimaryKey()
        {
            using (var db = new DBContext(_mapper3))
            {
                AddData(db);

                var query = from q in db.People
                            where
                                q.FirstName == "Jim"
                            select q;

                query = query.Include(x => x.Spouse);
                var p = query.First();

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

        private static Action<FluentMappingBuilder> _mapper1 = builder =>
        {

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
        };



        private static Action<FluentMappingBuilder> _mapper2 = builder =>
        {

            builder.Entity<Person>()
                .Association(x => x.Orders, (p, o) => p.PersonId == o.PersonId)
                .Property(x => x.PersonId).IsIdentity().IsPrimaryKey(1).IsNullable(false)
                .Property(x => x.FirstName).IsPrimaryKey(2).HasLength(100).IsNullable(false)
                .Property(x => x.LastName).HasLength(100).IsNullable(false)
                .Property(x => x.Dob).IsNullable(false)
                .Property(x => x.Salary).IsNullable(false)
                .Property(x => x.Weight).IsNullable(false)
                .Property(x => x.SpouseId).IsNullable()
                .Property(x => x.Spouse).IsNotColumn().Association(x => x.Spouse, p => p.SpouseId, s => s.PersonId)
                .Property(x => x.Orders).IsNotColumn();
        };


        private static Action<FluentMappingBuilder> _mapper3 = builder =>
        {

            builder.Entity<Person>()
                .Association(x => x.Orders, (p, o) => p.PersonId == o.PersonId)
                .Property(x => x.PersonId).IsIdentity().IsNullable(false)
                .Property(x => x.FirstName).HasLength(100).IsNullable(false)
                .Property(x => x.LastName).HasLength(100).IsNullable(false)
                .Property(x => x.Dob).IsNullable(false)
                .Property(x => x.Salary).IsNullable(false)
                .Property(x => x.Weight).IsNullable(false)
                .Property(x => x.SpouseId).IsNullable()
                .Property(x => x.Spouse).IsNotColumn().Association(x => x.Spouse, p => p.SpouseId, s => s.PersonId)
                .Property(x => x.Orders).IsNotColumn();
        };

        public class DBContext : DataConnection
        {
            public DBContext(Action<FluentMappingBuilder> mapperFunc) : base("DBConn")
            {
                var schema = new MappingSchema();
                var builder = schema.GetFluentMappingBuilder();

                mapperFunc?.Invoke(builder);
                this.AddMappingSchema(schema);

                this.CreateTable<Person>();
            }

            public ITable<Person> People => GetTable<Person>();
        }
    }
}