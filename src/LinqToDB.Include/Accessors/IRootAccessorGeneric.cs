﻿using LinqToDB.Include.Setters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.Include
{
    interface IRootAccessor<TClass> : IRootAccessor where TClass : class
    {
        HashSet<IPropertyAccessor<TClass>> Properties { get; }
        void LoadMap(List<TClass> entities, IQueryable<TClass> query, Builder builder);
    }
}
