﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UIForia.Attributes;
using UIForia.Bindings;
using UIForia.Extensions;
using UIForia.Compilers.AliasSource;
using UIForia.Exceptions;
using UIForia.Expressions;
using UIForia.Parsing.Expression;
using UIForia.Util;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using ElementCallback = System.Action<UIForia.Elements.UIElement, string>;
using Expression = UIForia.Expressions.Expression;

namespace UIForia.Compilers {

    public class PropertyBindingCompiler {

        private static readonly Dictionary<Type, List<IAliasSource>> aliasMap = new Dictionary<Type, List<IAliasSource>>();

        private static readonly Dictionary<Type, Dictionary<string, LightList<object>>> m_TypeMap = new Dictionary<Type, Dictionary<string, LightList<object>>>();

        public const string k_Enable = "enabled";
        public const string k_Read = "read";
        public const string k_Write = "write";

        private Type rootType;
        private Type elementType;

        private ExpressionCompiler compiler;

        public PropertyBindingCompiler() {
            this.compiler = new ExpressionCompiler();
        }

        // todo -- maybe move this to the compiler itself so it can be used per-expression 
        static PropertyBindingCompiler() {
            AddTypedAliasSource(typeof(Color), new ColorAliasSource());
            AddTypedAliasSource(typeof(Color), new MethodAliasSource("rgb", ColorAliasSource.ColorConstructor));
            AddTypedAliasSource(typeof(Color), new MethodAliasSource("rgba", ColorAliasSource.ColorConstructorAlpha));
        }

        public void SetCompiler(ExpressionCompiler compiler) {
            this.compiler = compiler;
        }

        public static void AddTypedAliasSource(Type type, IAliasSource aliasSource) {
            if (type == null || aliasSource == null) return;
            List<IAliasSource> list = aliasMap.GetOrDefault(type);
            if (list == null) {
                list = new List<IAliasSource>();
                aliasMap[type] = list;
            }

            list.Add(aliasSource);
        }
      
        // todo ensure each binding has at most one .read and one .write and that both are different values
        public void CompileAttribute(Type rootType, Type elementType, AttributeDefinition attributeDefinition, LightList<Binding> output) {
            this.rootType = rootType;
            this.elementType = elementType;
            string attrKey = attributeDefinition.key;
            string attrValue = attributeDefinition.value;
            
            EventInfo eventInfo = elementType.GetEvent(attrKey);

            if (eventInfo != null) {
                output.Add(CompileEventBinding(attrKey, attrValue, eventInfo));
                return;
            }

            if (attrKey.IndexOf(".", StringComparison.Ordinal) != -1) {
                // todo -- don't allocate, use span or something similar
                string[] parts = attrKey.Split('.');
                string property = parts[0];

                bool hasRead = false;
                bool hasWrite = false;
                bool hasEnabled = false;

                for (int i = 1; i < parts.Length; i++) {
                    string modifier = parts[i];
                    Binding binding = null;
                    switch (modifier) {
                        case k_Enable: {
                            if (hasEnabled) continue;
                            hasEnabled = true;
                            binding = CompileBinding(property, attrValue);
                            if (binding != null) {
                                binding.bindingType = BindingType.OnEnable;
                            }

                            break;
                        }

                        case k_Read: {
                            if (hasRead) continue;
                            hasRead = true;
                            binding = CompileBinding(parts[0], attrValue);
                            break;
                        }

                        case k_Write:
                            if (hasWrite) continue;
                            hasWrite = true;
                            binding = CompileWriteBinding(parts[0], attrValue);
                            if (binding != null) binding.bindingType = BindingType.Write;
                            break;
                        
                        default:
                            throw new ParseException($"Unsupported attribute binding extension: '{modifier}' in attribute chain: {attrKey}");
                    }

                    if (binding != null) {
                        if (binding.IsConstant()) {
                            binding.bindingType = BindingType.Constant;
                        }

                        output.Add(binding);
                    }
                }
            }

            else {
                Binding binding = CompileBinding(attrKey, attrValue);
                if(binding == null) return;
                
                if (binding.IsConstant()) {
                    binding.bindingType = BindingType.Constant;
                }
                output.Add(binding);
            }
        }

        private Binding CompileWriteBinding(string attrKey, string attrValue) {
            if (ReflectionUtil.IsField(elementType, attrKey, out FieldInfo fieldInfo)) {
                ReflectionUtil.LinqAccessor accessor = ReflectionUtil.GetLinqFieldAccessors(elementType, fieldInfo.FieldType, attrKey);

                WriteTargetExpression expression = compiler.CompileWriteTarget(rootType, fieldInfo.FieldType, attrValue);

                
                try {
                    Binding writeBinding = (Binding) ReflectionUtil.CreateGenericInstanceFromOpenType(typeof(WriteBinding<,>),
                        new GenericArguments(elementType, fieldInfo.FieldType),
                        new ConstructorArguments(attrKey, expression, accessor.getter)
                    );

                    return writeBinding;
                }
                catch (Exception) {
                    // todo improve error message
                    UnityEngine.Debug.Log("Unable to create a write binding for expression: " + attrKey + ".write='" + attrValue + "'. Ensure that the expression is a valid property path");
                    throw;
                }
            }

            if (ReflectionUtil.IsProperty(elementType, attrKey, out PropertyInfo propertyInfo)) {
                ReflectionUtil.LinqAccessor accessor = ReflectionUtil.GetLinqPropertyAccessors(elementType, propertyInfo.PropertyType, attrKey);

                WriteTargetExpression expression = compiler.CompileWriteTarget(rootType, propertyInfo.PropertyType, attrValue);

                try {
                    Binding writeBinding = (Binding) ReflectionUtil.CreateGenericInstanceFromOpenType(typeof(WriteBinding<,>),
                        new GenericArguments(elementType, propertyInfo.PropertyType),
                        new ConstructorArguments(attrKey, expression, accessor.getter)
                    );

                    return writeBinding;
                }
                catch (Exception) {
                    // todo improve error message
                    UnityEngine.Debug.Log("Unable to create a write binding for expression: " + attrKey + ".write='" + attrValue + "'. Ensure that the expression is a valid property path");
                    throw;
                }
            }

            throw new ParseException(attrKey + " is a not a field or property on type " + elementType + " that can be written to with a .write binding");
        }

        private Binding CompileBinding(string attrKey, string attrValue) {
            // todo -- statics?
            FieldInfo fieldInfo = ReflectionUtil.GetFieldInfo(elementType, attrKey);

            if (fieldInfo != null) {
                return CompileFieldAttribute(fieldInfo, attrKey, attrValue);
            }

            PropertyInfo propertyInfo = ReflectionUtil.GetPropertyInfo(elementType, attrKey);
            if (propertyInfo != null) {
                return CompilePropertyAttribute(propertyInfo, elementType, attrKey, attrValue);
            }

            if (attrKey == "if") {
                Expressions.Expression<bool> ifExpression = compiler.Compile<bool>(rootType, elementType, attrValue);
                return new EnabledBinding(ifExpression);
            }

            throw new ParseException(attrKey + " is a not a field or property on type " + elementType);
        }

        private Binding CompilePropertyAttribute(PropertyInfo propertyInfo, Type elementType, string attrKey, string attrValue) {
            Expression expression = compiler.Compile(rootType, elementType, attrValue, propertyInfo.PropertyType);

            ReflectionUtil.LinqAccessor accessor = ReflectionUtil.GetLinqPropertyAccessors(elementType, propertyInfo.PropertyType, attrKey);

            ReflectionUtil.TypeArray2[0] = elementType;
            ReflectionUtil.TypeArray2[1] = propertyInfo.PropertyType;

            if (!propertyInfo.PropertyType.IsAssignableFrom(expression.YieldedType)) {
                UnityEngine.Debug.Log($"Error compiling binding: {attrKey}={attrValue}, Type {propertyInfo.PropertyType} is not assignable from {expression.YieldedType}");
                return null;
            }

            // todo -- with callbacks

            ReflectionUtil.ObjectArray4[0] = attrKey;
            ReflectionUtil.ObjectArray4[1] = expression;
            ReflectionUtil.ObjectArray4[2] = accessor.getter;
            ReflectionUtil.ObjectArray4[3] = accessor.setter;
            return (Binding) ReflectionUtil.CreateGenericInstanceFromOpenType(
                typeof(PropertySetterBinding<,>),
                ReflectionUtil.TypeArray2,
                ReflectionUtil.ObjectArray4
            );
        }

        private Binding CompileFieldAttribute(FieldInfo fieldInfo, string attrKey, string attrValue) {
            Expression expression = compiler.Compile(rootType, elementType, attrValue, fieldInfo.FieldType);

            ReflectionUtil.LinqAccessor accessor = ReflectionUtil.GetLinqFieldAccessors(elementType, fieldInfo.FieldType, attrKey);

            ReflectionUtil.TypeArray2[0] = elementType;
            ReflectionUtil.TypeArray2[1] = fieldInfo.FieldType;

            if (!fieldInfo.FieldType.IsAssignableFrom(expression.YieldedType)) {
                UnityEngine.Debug.Log($"Error compiling binding: {attrKey}={attrValue}, Type {fieldInfo.FieldType} is not assignable from {expression.YieldedType}");
                return null;
            }

            Dictionary<string, LightList<object>> actionMap = GetActionMap(elementType);

            LightList<object> list = actionMap?.GetOrDefault(attrKey);
            if (list != null) {
                ReflectionUtil.ObjectArray5[0] = attrKey;
                ReflectionUtil.ObjectArray5[1] = expression;
                ReflectionUtil.ObjectArray5[2] = accessor.getter;
                ReflectionUtil.ObjectArray5[3] = accessor.setter;
                ReflectionUtil.ObjectArray5[4] = list;

                return (Binding) ReflectionUtil.CreateGenericInstanceFromOpenType(
                    typeof(FieldSetterBinding_WithCallbacks<,>),
                    ReflectionUtil.TypeArray2,
                    ReflectionUtil.ObjectArray5
                );
            }

            ReflectionUtil.ObjectArray4[0] = attrKey;
            ReflectionUtil.ObjectArray4[1] = expression;
            ReflectionUtil.ObjectArray4[2] = accessor.getter;
            ReflectionUtil.ObjectArray4[3] = accessor.setter;
            return (Binding) ReflectionUtil.CreateGenericInstanceFromOpenType(
                typeof(FieldSetterBinding<,>),
                ReflectionUtil.TypeArray2,
                ReflectionUtil.ObjectArray4
            );
        }

        private static LightList<object> GetHandlerList(Dictionary<string, LightList<object>> map, string name) {
            LightList<object> list = map.GetOrDefault(name);
            if (list == null) {
                list = new LightList<object>();
                map[name] = list;
            }

            return list;
        }

        private static Dictionary<string, LightList<object>> GetActionMap(Type elementType) {
            Dictionary<string, LightList<object>> actionMap;
            
            if (m_TypeMap.TryGetValue(elementType, out actionMap)) {
                return actionMap;
            }
            
            MethodInfo[] methods = elementType.GetMethods(ReflectionUtil.InstanceBindFlags);

            for (int i = 0; i < methods.Length; i++) {
                MethodInfo info = methods[i];
                object[] customAttributes = info.GetCustomAttributes(typeof(OnPropertyChanged), true);

                if (customAttributes.Length == 0) continue;

                ParameterInfo[] parameterInfos = info.GetParameters();

                if (!info.IsStatic && parameterInfos.Length == 1 && parameterInfos[0].ParameterType == typeof(string)) {
                    if (actionMap == null) {
                        actionMap = m_TypeMap.GetOrDefault(elementType);
                    }

                    if (actionMap == null) {
                        actionMap = new Dictionary<string, LightList<object>>();
                        m_TypeMap.Add(elementType, actionMap);
                    }

                    for (int j = 0; j < customAttributes.Length; j++) {
                        OnPropertyChanged attr = (OnPropertyChanged) customAttributes[j];
                        Type a = ReflectionUtil.GetOpenDelegateType(info);
                        GetHandlerList(actionMap, attr.propertyName).Add(ReflectionUtil.GetDelegate(a, info));
                    }
                }
                else {
                    UnityEngine.Debug.LogWarning($"Trying to compile 'OnPropertyChanged' attribute on method {info.Name} of type {info.DeclaringType} but the method did not have the required signature of 1 parameter of type string");
                }
            }

            // use null as a marker in the dictionary regardless of whether or not we have actions registered
            m_TypeMap[elementType] = actionMap;

            return actionMap;
        }

        private Binding CompileEventBinding(string attrKey, string attrValue, EventInfo eventInfo) {
            MethodInfo info = eventInfo.EventHandlerType.GetMethod("Invoke");
            Debug.Assert(info != null, nameof(info) + " != null");

            ParameterInfo[] delegateParameters = info.GetParameters();

            Type[] argTypes = new Type[delegateParameters.Length];
            for (int i = 0; i < argTypes.Length; i++) {
                argTypes[i] = delegateParameters[i].ParameterType;
            }

            Expression expression = compiler.Compile(rootType, attrValue, eventInfo.EventHandlerType);
            
            // todo -- this only works for the root type, if we have $item.xxx it will not work yet
            ReflectionUtil.LinqAccessor accessor;
            try {
                accessor = ReflectionUtil.GetLinqFieldAccessors(elementType, eventInfo.EventHandlerType, attrKey);
            }
            catch {
                accessor = ReflectionUtil.GetLinqPropertyAccessors(elementType, eventInfo.EventHandlerType, attrKey);
            }

            ReflectionUtil.ObjectArray3[0] = eventInfo;
            ReflectionUtil.ObjectArray3[1] = expression;
            ReflectionUtil.ObjectArray3[2] = accessor.getter;
            
            return (Binding) ReflectionUtil.CreateGenericInstanceFromOpenType(
                typeof(EventSetterBinding_Delegate<,>),
                new GenericArguments(elementType, eventInfo.EventHandlerType),
                new ConstructorArguments(eventInfo, expression, accessor.getter)
            );
     
        }

    }

}