using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UIForia.Exceptions;
using UIForia.Expressions;
using UIForia.Parsing.Expression.AstNodes;
using UIForia.Rendering;
using UIForia.Util;

namespace UIForia.Compilers.ExpressionResolvers {

    public class SizeResolver : ExpressionAliasResolver {

        private static readonly MethodInfo s_SizeInfo1;
        private static readonly MethodInfo s_SizeInfo2;

        static SizeResolver() {
            s_SizeInfo1 = ReflectionUtil.GetMethodInfo(typeof(SizeResolver), nameof(Size1));
            s_SizeInfo2 = ReflectionUtil.GetMethodInfo(typeof(SizeResolver), nameof(Size2));
        }

        public SizeResolver(string aliasName) : base(aliasName) { }

        public override Expression CompileAsMethodExpression(CompilerContext context, LightList<ASTNode> parameters) {
            if (context.targetType != typeof(MeasurementPair)) {
                return null;
            }

            if (parameters.Count == 1) {
                Expression expr0 = context.Visit(typeof(UIMeasurement), parameters[0]);
                if (expr0 == null) {
                    throw new CompileException($"Invalid arguments for {aliasName}");
                }

                return new MethodCallExpression_Static<UIMeasurement, MeasurementPair>(s_SizeInfo1, new[] {expr0});
            }

            if (parameters.Count == 2) {
                Expression expr0 = context.Visit(typeof(UIMeasurement), parameters[0]);
                Expression expr1 = context.Visit(typeof(UIMeasurement), parameters[1]);
                if (expr0 == null || expr1 == null) {
                    throw new CompileException($"Invalid arguments for {aliasName}");
                }

                return new MethodCallExpression_Static<UIMeasurement, UIMeasurement, MeasurementPair>(s_SizeInfo2, new[] {expr0, expr1});
            }

            return null;
        }

        [Pure]
        public static MeasurementPair Size1(UIMeasurement x) {
            return new MeasurementPair(x, x);
        }

        [Pure]
        public static MeasurementPair Size2(UIMeasurement x, UIMeasurement y) {
            return new MeasurementPair(x, y);
        }

    }

}