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
    public class EntityPoolTests
    {
        [SetUp]
        public void Setup()
        {
            DataConnection.DefaultSettings = DataConnection.DefaultSettings ?? new DBConnection();
        }
        
        [Test]
        public void DuplicateEntitiesReducedTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                var builder = Enumerable.Range(1, 10).Select(x => new Person
                {
                    Dob = DateTime.MinValue.AddDays(x),
                    FirstName = "FirstName " + x,
                    LastName = "LastName " + x,
                    PersonId = x,
                    Salary = x + 1000,
                    Weight = x + 20
                });

                var entities = builder.ToList();
                entities.AddRange(builder);

                var result = new LinqToDB.Include.Setters.EntityPool()
                    .ProcessEntities<Person, Person>(db, entities);

                Assert.AreEqual(result.ElementAt(0), result.ElementAt(10));
            }
        }



        [Test]
        public void DuplicateEntitiesWithInheritanceReducedTest()
        {
            using (var db = new DBContext(_mapping1))
            {
                var builder = Enumerable.Range(1, 10).Select(x => new ExtendedProductLine
                {
                    OrderId = 1,
                    ProductCode = "ProductCode " + x,
                    ProductLineId = x,
                    ProductId = x % 2 == 0 ? x : x - 1,
                    ProductLineType = ProductLineType.Extended
                });

                var entities = builder.ToList();
                entities.AddRange(builder);

                var result = new LinqToDB.Include.Setters.EntityPool()
                    .ProcessEntities<ExtendedProductLine, ProductLine>(db, entities);

                Assert.AreEqual(result.ElementAt(0), result.ElementAt(10));
            }
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
                .Property(x => x.Orders).IsNotColumn();


            builder.Entity<ProductLine>()
                .Inheritance(x => x.ProductLineType, ProductLineType.Normal, typeof(ProductLine), true)
                .Inheritance(x => x.ProductLineType, ProductLineType.Extended, typeof(ExtendedProductLine))
                .Property(x => x.ProductLineId).IsIdentity().IsPrimaryKey().IsNullable(false)
                .Property(x => x.OrderId).IsNullable(false)
                .Property(x => x.ProductLineType).IsNullable(false).IsDiscriminator()
                .Property(x => x.ProductId).IsNullable(false)
                .Property(x => x.ProductCode).HasLength(20).IsNullable(false);

            builder.Entity<ExtendedProductLine>()
                .HasAttribute(new TableAttribute { Name = nameof(ProductLine), IsColumnAttributeRequired = true })
                .Association(x => x.FirstPerson, (pl, p) => p.PersonId == 1);

        };

        public class DBContext : DataConnection
        {
            public DBContext(Action<FluentMappingBuilder> mappings) : base("DBConn")
            {
                
                var schema = new MappingSchema();
                var builder = schema.GetFluentMappingBuilder();

                mappings?.Invoke(builder);

                this.AddMappingSchema(schema);   
            }
        }
    }
}