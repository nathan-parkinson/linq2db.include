using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include
{
    public class EntityMatchWalker : ExpressionVisitor
    {
        private bool _ignore = false;
        private bool _isNot = false;

        private Expression _expression;

        private readonly List<Expression> _thisKey = new List<Expression>();
        private readonly List<Expression> _otherKey = new List<Expression>();

        private readonly ParameterExpression _thisParam;
        private readonly ParameterExpression _otherParam;

        private EntityMatchWalker(ParameterExpression thisParameter, ParameterExpression otherParameter)
        {
            _thisParam = thisParameter;
            _otherParam = otherParameter;
        }
        
        public static Tuple<List<Expression>, List<Expression>> ExtractKeyNodes(Expression expression, ParameterExpression thisParameter, ParameterExpression otherParameter)
        {
            var walker = new EntityMatchWalker(thisParameter, otherParameter);
            walker.Visit(expression);
            return Tuple.Create(walker._thisKey, walker._otherKey);
        }

        private static ExpressionType[] _validAccessors = { ExpressionType.MemberAccess, ExpressionType.Constant };


        private void AddKeysToList(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal)
            {
                bool? isLeftThisKey = null;
                switch (node.Left)
                {
                    case MemberExpression property:
                        isLeftThisKey = property.Expression == _thisParam;
                        break;
                    case ConstantExpression constant:
                        break;
                    default:
                        throw new ArgumentException($"Only Expressions of type '{nameof(ExpressionType.MemberAccess)}'" +
                            $" or '{nameof(ExpressionType.Constant)}' can be used as keys");
                }

                switch (node.Right)
                {
                    case MemberExpression property:
                        isLeftThisKey = property.Expression != _thisParam;
                        break;
                    case ConstantExpression constant:
                        break;
                    default:
                        throw new ArgumentException($"Only Expressions of type '{nameof(ExpressionType.MemberAccess)}'" +
                            $" or '{nameof(ExpressionType.Constant)}' can be used as keys");
                }

                if (!isLeftThisKey.HasValue)
                {
                    throw new ArgumentException($"At least one part of '{nameof(BinaryExpression)}' must " +
                        $"relate to table field");
                }

                if (isLeftThisKey.HasValue)
                {
                    _thisKey.Add(node.Left);
                    _otherKey.Add(node.Right);
                }
                else
                {
                    _thisKey.Add(node.Right);
                    _otherKey.Add(node.Left);
                }
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var ignoreAtStart = _ignore;

            _ignore = _ignore || node.NodeType == ExpressionType.OrElse;

            if(!_ignore && !_isNot)
            {
                if(node.NodeType == ExpressionType.Equal && 
                    _validAccessors.Contains(node.Right.NodeType) &&
                    _validAccessors.Contains(node.Left.NodeType))
                {
                    AddKeysToList(node);
                    _expression = _expression == null ? node : Expression.AndAlso(_expression, node);
                }
            }
            
            var returnVal = base.VisitBinary(node);

            if(!ignoreAtStart && _ignore)
            {
                _ignore = false;
            }

            return returnVal;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var isNotAtStart = _isNot;

            _isNot = _isNot || node.NodeType == ExpressionType.Not;

            var returnVal = base.VisitUnary(node);

            if (!isNotAtStart && _isNot)
            {
                _isNot = false;
            }

            return returnVal;
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return base.VisitMember(node);
        }
        
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return base.VisitConditional(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return base.VisitMemberAssignment(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return base.VisitParameter(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            return base.VisitTypeBinary(node);
        }
    }

}
