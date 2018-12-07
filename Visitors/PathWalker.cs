using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Utils
{
    class PathWalker : ExpressionVisitor
    {
        private List<string> _path = new List<string>();

        private PathWalker()
        {

        }

        internal static List<string> GetPath<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expr) 
            where TEntity : class 
            where TProperty : class
        {
            var walker = new PathWalker();
            walker.Visit(expr);
            walker._path.Reverse();

            return walker._path;
        }

        internal static List<string> GetPath(MemberExpression expr)
        {
            var walker = new PathWalker();
            walker.Visit(expr);
            walker._path.Reverse();

            return walker._path;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _path.Add(node.Member.Name);
            return base.VisitMember(node);
        }
    }

}
