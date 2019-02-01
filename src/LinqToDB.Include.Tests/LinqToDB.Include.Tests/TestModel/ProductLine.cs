using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Include.Tests.TestModel
{
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
}
