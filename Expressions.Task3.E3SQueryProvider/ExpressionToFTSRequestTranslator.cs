using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }
            else if (node.Method.Name == "Equals" && node.Object != null)
            {
                Visit(node.Object); // Visits the member expression to append the property name
                _resultStringBuilder.Append("(");
                Visit(node.Arguments[0]); // Visits the constant
                _resultStringBuilder.Append(")");
                return node;
            }
            else if (node.Method.Name == "StartsWith" && node.Object != null)
            {
                Visit(node.Object); // Visits the member expression to append the property name
                _resultStringBuilder.Append("(");
                Visit(node.Arguments[0]); // Visits the constant
                _resultStringBuilder.Append("*)");
                return node;
            }
            else if (node.Method.Name == "EndsWith" && node.Object != null)
            {
                Visit(node.Object); // Visits the member expression to append the property name
                _resultStringBuilder.Append("(*");
                Visit(node.Arguments[0]); // Visits the constant
                _resultStringBuilder.Append(")");
                return node;
            }
            else if (node.Method.Name == "Contains" && node.Object != null)
            {
                Visit(node.Object); // Visits the member expression to append the property name
                _resultStringBuilder.Append("(*");
                Visit(node.Arguments[0]); // Visits the constant
                _resultStringBuilder.Append("*)");
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:

                    Expression member;
                    Expression constant;

                    if (node.Left.NodeType == node.Right.NodeType)
                        throw new NotSupportedException($"One operand should be property or field and the other should be constant");


                    if (node.Left.NodeType == ExpressionType.MemberAccess)
                    {
                        member = node.Left;
                        constant = node.Right;
                    }
                    else
                    {
                        member = node.Right;
                        constant = node.Left;
                    }

                    Visit(member);
                    _resultStringBuilder.Append("(");
                    Visit(constant);
                    _resultStringBuilder.Append(")");

                    break;

                case ExpressionType.AndAlso:
                    
                    bool isFirstCondition = _resultStringBuilder.Length == 0;

                    if (isFirstCondition)
                        _resultStringBuilder.Append("\"statements\": [{\"query\":\"");

                    Visit(node.Left);

                    if (node.Left.NodeType != ExpressionType.AndAlso)
                    _resultStringBuilder.Append("\"}");

                    _resultStringBuilder.Append(",{\"query\":\"");

                    Visit(node.Right);

                    _resultStringBuilder.Append("\"}");

                    if (isFirstCondition)
                        _resultStringBuilder.Append("]");

                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
