using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Src.Compilers.CastHandlers;
using UnityEngine;

namespace Src {

    public class ExpressionCompiler {

        private readonly ContextDefinition context;
        private List<ICastHandler> userCastHandlers;

        private static readonly List<ICastHandler> builtInCastHandlers = new List<ICastHandler>() {
            new CastHandler_ColorToVector4(),
            new CastHandler_DoubleToMeasurement(),
            new CastHandler_FloatToInt(),
            new CastHandler_FloatToMeasurement(),
            new CastHandler_IntToDouble(),
            new CastHandler_IntToFloat(),
            new CastHandler_IntToMeasurement(),
            new CastHandler_Vector2ToVector3(),
            new CastHandler_Vector3ToVector2(),
            new CastHandler_Vector4ToColor()
        };

        public ExpressionCompiler(ContextDefinition context) {
            this.context = context;
        }

        public Expression Compile(ExpressionNode root) {
            return Visit(root);
        }

        [PublicAPI]
        public void AddCastHandler(ICastHandler handler) {
            if (userCastHandlers == null) {
                userCastHandlers = new List<ICastHandler>();
            }

            userCastHandlers.Add(handler);
        }

        [PublicAPI]
        public void RemoveCastHandler(ICastHandler handler) {
            userCastHandlers?.Remove(handler);
        }

        private Expression Visit(ExpressionNode node) {
            switch (node.expressionType) {
                case ExpressionNodeType.AliasAccessor:
                    return VisitAliasNode((AliasExpressionNode) node);

                case ExpressionNodeType.Paren:
                    return VisitParenNode((ParenExpressionNode) node);

                case ExpressionNodeType.RootContextAccessor:
                    return VisitRootContextAccessor((RootContextLookupNode) node);

                case ExpressionNodeType.LiteralValue:
                    return VisitConstant((LiteralValueNode) node);

                case ExpressionNodeType.Accessor:
                    return VisitAccessExpression((AccessExpressionNode) node);

                case ExpressionNodeType.Unary:
                    return VisitUnaryExpression((UnaryExpressionNode) node);

                case ExpressionNodeType.Operator:
                    return VisitOperatorExpression((OperatorExpressionNode) node);

                case ExpressionNodeType.MethodCall:
                    return VisitMethodCallExpression((MethodCallNode) node);
            }

            return null;
        }


        private Expression HandleCasting(Type requiredType, Expression expression) {
            Type yieldedType = expression.YieldedType;

            if (yieldedType == requiredType) {
                return expression;
            }

            if (userCastHandlers != null) {
                for (int i = 0; i < userCastHandlers.Count; i++) {
                    if (userCastHandlers[i].CanHandle(requiredType, yieldedType)) {
                        return userCastHandlers[i].Cast(requiredType, expression);
                    }
                }
            }

            for (int i = 0; i < builtInCastHandlers.Count; i++) {
                if (builtInCastHandlers[i].CanHandle(requiredType, yieldedType)) {
                    return builtInCastHandlers[i].Cast(requiredType, expression);
                }
            }

            return expression;
        }

        private void ValidateParameterTypes(ParameterInfo[] parameters, Expression[] arguments) {
            for (int i = 0; i < parameters.Length; i++) {
                if (!parameters[i].ParameterType.IsAssignableFrom(arguments[i].YieldedType)) {
                    throw new Exception(
                        $"Cannot use parameter of type {arguments[i].YieldedType} for parameter of type {parameters[i].ParameterType}");
                }
            }
        }

        private Expression VisitMethodCallExpression_Static(MethodInfo info, MethodCallNode node) {
            string methodName = node.identifierNode.identifier;

            IReadOnlyList<ExpressionNode> signatureParts = node.signatureNode.parts;

            ParameterInfo[] parameters = info.GetParameters();

            if (parameters.Length != signatureParts.Count) {
                throw new Exception("Argument count is wrong");
            }

            Expression[] args = new Expression[signatureParts.Count];
            Type[] genericArguments = new Type[signatureParts.Count + 1];

            for (int i = 0; i < args.Length; i++) {
                Type requiredType = parameters[i].ParameterType;
                ExpressionNode argumentNode = signatureParts[i];
                Expression argumentExpression = Visit(argumentNode);
                args[i] = HandleCasting(requiredType, argumentExpression);

                genericArguments[i] = args[i].YieldedType;
            }

            genericArguments[genericArguments.Length - 1] = info.ReturnType;

            ValidateParameterTypes(parameters, args);

            Type callType;
            switch (args.Length) {
                case 0:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Static<>),
                        genericArguments);
                    break;

                case 1:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Static<,>),
                        genericArguments);
                    break;

                case 2:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Static<,,>),
                        genericArguments);
                    break;

                case 3:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Static<,,,>),
                        genericArguments);
                    break;

                case 4:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Static<,,,,>),
                        genericArguments);
                    break;

                default:
                    throw new Exception(
                        $"Expressions only support functions with up to 4 arguments. {methodName} is supplying {args.Length} ");
            }

            ReflectionUtil.ObjectArray2[0] = info;
            ReflectionUtil.ObjectArray2[1] = args;

            return (Expression) ReflectionUtil.CreateGenericInstance(callType, ReflectionUtil.ObjectArray2);
        }

        private Expression VisitMethodCallExpression(MethodCallNode node) {
            string methodName = node.identifierNode.identifier;

            context.ResolveType(node.identifierNode.identifier);
            MethodInfo info = context.ResolveMethod(methodName);

            if (info == null) {
                throw new Exception(
                    $"Cannot find method {methodName} on type {context.rootType.Name} or any registered aliases");
            }

            if (info.IsStatic) {
                return VisitMethodCallExpression_Static(info, node);
            }

            ParameterInfo[] parameters = info.GetParameters();
            IReadOnlyList<ExpressionNode> signatureParts = node.signatureNode.parts;

            if (parameters.Length != signatureParts.Count) {
                throw new Exception("Argument count is wrong");
            }

            Expression[] args = new Expression[signatureParts.Count];
            Type[] genericArguments = new Type[signatureParts.Count + 2];

            genericArguments[0] = context.rootType; // todo -- this means root functions only, no chaining right now! 

            for (int i = 0; i < args.Length; i++) {
                Type requiredType = parameters[i].ParameterType;
                ExpressionNode argumentNode = signatureParts[i];
                Expression argumentExpression = Visit(argumentNode);
                args[i] = HandleCasting(requiredType, argumentExpression);

                genericArguments[i + 1] = args[i].YieldedType;
            }

            genericArguments[genericArguments.Length - 1] = info.ReturnType;

            ValidateParameterTypes(parameters, args);

            Type callType;
            switch (args.Length) {
                case 0:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Instance<,>), genericArguments);
                    break;

                case 1:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Instance<,,>), genericArguments);
                    break;

                case 2:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Instance<,,,>), genericArguments);
                    break;

                case 3:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Instance<,,,,>), genericArguments);
                    break;

                case 4:
                    callType = ReflectionUtil.CreateGenericType(typeof(MethodCallExpression_Instance<,,,,,>), genericArguments);
                    break;

                default:
                    throw new Exception(
                        $"Expressions only support functions with up to 4 arguments. {methodName} is supplying {args.Length} ");
            }

            ReflectionUtil.ObjectArray2[0] = info;
            ReflectionUtil.ObjectArray2[1] = args;

            return (Expression) ReflectionUtil.CreateGenericInstance(callType, ReflectionUtil.ObjectArray2);
        }

        private Expression VisitAliasNode(AliasExpressionNode node) {
            Type aliasedType = context.ResolveType(node.alias);

            if (aliasedType == typeof(int)) {
                return new ResolveExpression_Alias_Int(node.alias);
            }
            else {
                return new ResolveExpression_Alias_Object(node.alias, aliasedType);
            }
        }

        private Expression VisitRootContextAccessor(RootContextLookupNode node) {
            string fieldName = node.idNode.identifier;
            // todo -- alias is resolved before field access, might be an issue
            object constantAlias = context.ResolveConstantAlias(fieldName);
            if (constantAlias != null) {
                Type aliasType = constantAlias.GetType();

                if (aliasType.IsEnum) {
                    return new LiteralExpression_Enum((Enum) constantAlias);
                }

                if (aliasType == typeof(string)) {
                    return new LiteralExpression_String((string) constantAlias);
                }

                if (aliasType == typeof(int)) {
                    return new LiteralExpression_Int((int) constantAlias);
                }

                if (aliasType == typeof(double)) {
                    return new LiteralExpression_Double((double) constantAlias);
                }

                if (aliasType == typeof(float)) {
                    return new LiteralExpression_Float((float) constantAlias);
                }

                if (aliasType == typeof(bool)) {
                    return new LiteralExpression_Boolean((bool) constantAlias);
                }
            }

            Type type = context.processedType.rawType;
            return new AccessExpression_Root(type, fieldName);
        }

        private Expression VisitParenNode(ParenExpressionNode node) {
            return ParenExpressionFactory.CreateParenExpression(Visit(node.expressionNode));
        }

        private Expression VisitOperatorExpression(OperatorExpressionNode node) {
            Type leftType = node.left.GetYieldedType(context);
            Type rightType = node.right.GetYieldedType(context);

            switch (node.OpType) {
                case OperatorType.Plus:

                    if (leftType == typeof(string) || rightType == typeof(string)) {
                        return OperatorExpression_StringConcat.Create(Visit(node.left), Visit(node.right));
                    }

                    if (ReflectionUtil.AreNumericTypesCompatible(leftType, rightType)) {
                        return OperatorExpression_Arithmetic.Create(OperatorType.Plus, Visit(node.left),
                            Visit(node.right));
                    }

                    break;

                case OperatorType.Minus:
                case OperatorType.Divide:
                case OperatorType.Times:
                case OperatorType.Mod:

                    if (ReflectionUtil.AreNumericTypesCompatible(leftType, rightType)) {
                        return OperatorExpression_Arithmetic.Create(node.OpType, Visit(node.left), Visit(node.right));
                    }

                    break;

                case OperatorType.TernaryCondition:

                    return VisitOperator_TernaryCondition(node);

                case OperatorType.TernarySelection:
                    throw new Exception("Should never visit a TernarySelection operator");

                case OperatorType.GreaterThan:
                case OperatorType.GreaterThanEqualTo:
                case OperatorType.LessThan:
                case OperatorType.LessThanEqualTo:
                    return new OperatorExpression_Comparison(node.OpType, Visit(node.left), Visit(node.right));

                case OperatorType.Equals:
                case OperatorType.NotEquals:
                    return new OperatorExpression_Equality(node.OpType, Visit(node.left), Visit(node.right));
            }

            throw new Exception("Bad operator expression");
        }

        private Expression VisitUnaryExpression(UnaryExpressionNode node) {
            Type yieldType = node.expression.GetYieldedType(context);
            if (yieldType == typeof(bool)) {
                if (node.op == OperatorType.Not) {
                    return new UnaryExpression_Boolean((Expression<bool>) Visit(node.expression));
                }

                throw new Exception("Unary but not boolean operator");
            }
            else if (IsNumericType(yieldType)) {
                switch (node.op) {
                    case OperatorType.Plus:
                        return UnaryExpression_PlusFactory.Create(Visit(node.expression));

                    case OperatorType.Minus:
                        return UnaryExpression_MinusFactory.Create(Visit(node.expression));
                }
            }
            else { }

            return null;
        }

        private static bool IsNumericType(Type type) {
            return type == typeof(int)
                   || type == typeof(float)
                   || type == typeof(double);
        }

        private Expression VisitAccessExpression(AccessExpressionNode node) {
            string contextName = node.identifierNode.identifier;
            Type headType = context.ResolveType(contextName);

            if (headType == null) {
                throw new Exception("Missing field or alias for access on context: " + contextName);
            }

            if (headType.IsPrimitive) {
                throw new Exception(
                    $"Attempting property access on type {headType.Name} on a primitive field {contextName}");
            }

            int startOffset = 0;
            int partCount = node.parts.Count;

            bool isRootContext = !(node.identifierNode is SpecialIdentifierNode);

            if (isRootContext) {
                startOffset++;
                partCount++;
            }

            Type lastType = headType;
            AccessExpressionPart[] parts = new AccessExpressionPart[partCount];

            for (int i = startOffset; i < partCount; i++) {
                AccessExpressionPartNode part = node.parts[i - startOffset];

                PropertyAccessExpressionPartNode propertyPart = part as PropertyAccessExpressionPartNode;

                if (propertyPart != null) {
                    string fieldName = propertyPart.fieldName;
                    lastType = ReflectionUtil.GetFieldInfoOrThrow(lastType, fieldName).FieldType;
                    parts[i] = new AccessExpressionPart_Field(fieldName);
                    continue;
                }

                ArrayAccessExpressionNode arrayPart = part as ArrayAccessExpressionNode;
                if (arrayPart != null) {
                    Expression<int> indexExpression = (Expression<int>) Visit(arrayPart.expressionNode);
                    lastType = ReflectionUtil.GetArrayElementTypeOrThrow(lastType);
                    parts[i] = new AccessExpressionPart_List(indexExpression);
                    continue;
                }

                throw new Exception("Unknown AccessExpression Type: " + part.GetType());
            }

            if (isRootContext) {
                parts[0] = new AccessExpressionPart_Field(contextName);
            }

            AccessExpression retn = new AccessExpression(contextName, lastType, parts);
            return retn;
        }

        private static Expression VisitConstant(LiteralValueNode node) {
            if (node is NumericLiteralNode) {
                return VisitNumericLiteralNode((NumericLiteralNode) node);
            }

            if (node is BooleanLiteralNode) {
                return new LiteralExpression_Boolean(((BooleanLiteralNode) node).value);
            }

            if (node is StringLiteralNode) {
                return new LiteralExpression_String(((StringLiteralNode) node).value);
            }

            return null;
        }

        private static Expression VisitNumericLiteralNode(NumericLiteralNode node) {
            if (node is FloatLiteralNode) {
                return new LiteralExpression_Float(((FloatLiteralNode) node).value);
            }
            else if (node is IntLiteralNode) {
                return new LiteralExpression_Int(((IntLiteralNode) node).value);
            }
            else {
                return new LiteralExpression_Double(((DoubleLiteralNode) node).value);
            }
        }

        private Expression VisitOperator_TernaryCondition(OperatorExpressionNode node) {
            Expression<bool> condition = (Expression<bool>) Visit(node.left);
            OperatorExpressionNode select = (OperatorExpressionNode) node.right;

            if (select.OpType != OperatorType.TernarySelection) {
                throw new Exception("Bad ternary");
            }

            Expression right = Visit(select.right);
            Expression left = Visit(select.left);

            if (right.YieldedType == typeof(int) && left.YieldedType == typeof(int)) {
                return new OperatorExpression_Ternary_Generic<int>(condition, (Expression<int>) left,
                    (Expression<int>) right);
            }
            else if (right.YieldedType == typeof(float) && left.YieldedType == typeof(float)) {
                return new OperatorExpression_Ternary_Generic<float>(condition, (Expression<float>) left,
                    (Expression<float>) right);
            }
            else if (right.YieldedType == typeof(double) && left.YieldedType == typeof(double)) {
                return new OperatorExpression_Ternary_Generic<double>(condition, (Expression<double>) left,
                    (Expression<double>) right);
            }
            else if (right.YieldedType == typeof(string) && left.YieldedType == typeof(string)) {
                return new OperatorExpression_Ternary_Generic<string>(condition, (Expression<string>) left,
                    (Expression<string>) right);
            }
            else if (right.YieldedType == typeof(bool) && left.YieldedType == typeof(bool)) {
                return new OperatorExpression_Ternary_Generic<bool>(condition, (Expression<bool>) left,
                    (Expression<bool>) right);
            }
            else {
                return new OperatorExpression_Ternary(condition, left, right);
            }
        }

    }

}