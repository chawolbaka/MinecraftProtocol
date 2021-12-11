using System;
using System.Linq.Expressions;

namespace MinecraftProtocol.Utils
{
    /// <summary>
    /// 使用表达式树创建委托来执行私有方法或获取私有字段
    /// </summary>
    public class ExpressionTreeUtils
    { 
        //AOT编译下可能不能使用

        /// <summary>
        /// 创建一个委托来执行对应的方法
        /// </summary>
        /// <typeparam name="TInstance">含有该的方法实例</typeparam>
        /// <typeparam name="TArg1">方法的参数1</typeparam>
        /// <typeparam name="TArg2">方法的参数2</typeparam>
        /// <typeparam name="TArg3">方法的参数2</typeparam>
        /// <param name="methodName">方法名称</param>
        public static Action<TInstance, TArg1, TArg2, TArg3> CreateMethodFormInstance<TInstance, TArg1, TArg2, TArg3>(string methodName)
        {
            ParameterExpression expressionInstance = Expression.Parameter(typeof(TInstance), "x");
            ParameterExpression expressionArg1 = Expression.Parameter(typeof(TArg1), "a");
            ParameterExpression expressionArg2 = Expression.Parameter(typeof(TArg2), "b");
            ParameterExpression expressionArg3 = Expression.Parameter(typeof(TArg3), "c");
            return Expression.Lambda<Action<TInstance, TArg1, TArg2, TArg3>>(
                Expression.Call(
                    expressionInstance,
                    typeof(TInstance).GetMethod(methodName, new Type[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) }), expressionArg1, expressionArg2, expressionArg3),
                expressionInstance, expressionArg1, expressionArg2, expressionArg3).Compile();
        }

        /// <summary>
        /// 创建一个委托来执行对应的方法
        /// </summary>
        /// <typeparam name="TInstance">含有该的方法实例</typeparam>
        /// <typeparam name="TArg1">方法的参数1</typeparam>
        /// <typeparam name="TArg2">方法的参数2</typeparam>
        /// <param name="methodName">方法名称</param>
        public static Action<TInstance, TArg1, TArg2> CreateMethodFormInstance<TInstance, TArg1, TArg2>(string methodName)
        {
            ParameterExpression expressionInstance = Expression.Parameter(typeof(TInstance), "x");
            ParameterExpression expressionArg1 = Expression.Parameter(typeof(TArg1), "a");
            ParameterExpression expressionArg2 = Expression.Parameter(typeof(TArg2), "b");
            return Expression.Lambda<Action<TInstance, TArg1, TArg2>>(
                Expression.Call(
                    expressionInstance,
                    typeof(TInstance).GetMethod(methodName, new Type[] { typeof(TArg1), typeof(TArg2) }), expressionArg1, expressionArg2),
                expressionInstance, expressionArg1, expressionArg2).Compile();
        }

        /// <summary>
        /// 创建一个委托来执行对应的方法
        /// </summary>
        /// <typeparam name="TInstance">含有该的方法实例</typeparam>
        /// <typeparam name="TArg">方法的参数</typeparam>
        /// <param name="methodName">方法名称</param>
        public static Action<TInstance, TArg> CreateMethodFormInstance<TInstance, TArg>(string methodName)
        {
            ParameterExpression expressionInstance = Expression.Parameter(typeof(TInstance), "x");
            ParameterExpression expressionArg = Expression.Parameter(typeof(TArg), "a");
            return Expression.Lambda<Action<TInstance, TArg>>(
                Expression.Call(
                    expressionInstance,
                    typeof(TInstance).GetMethod(methodName, new Type[] { typeof(TArg) }), expressionArg),
                expressionInstance, expressionArg).Compile();
        }

        /// <summary>
        /// 创建一个委托来执行对应的方法
        /// </summary>
        /// <typeparam name="TInstance">含有该的方法实例</typeparam>
        /// <param name="methodName">方法名称</param>
        public static Action<TInstance> CreateMethodFormInstance<TInstance>(string methodName)
        {
            ParameterExpression expression = Expression.Parameter(typeof(TInstance), "x");
            return Expression.Lambda<Action<TInstance>>(
                Expression.Call(
                    expression, typeof(TInstance).GetMethod(methodName)), expression).Compile();
        }

        /// <summary>
        /// 创建一个委托来获取对应实例内的字段
        /// </summary>
        /// <typeparam name="TInstance">含有该字段的实例</typeparam>
        /// <typeparam name="TResult">字段的值</typeparam>
        /// <param name="fieldName">字段名称</param>
        public static Func<TInstance, TResult> CreateGetFieldMethodFormInstance<TInstance, TResult>(string fieldName)
        {
            ParameterExpression expression = Expression.Parameter(typeof(TInstance), "x");
            return Expression.Lambda<Func<TInstance, TResult>>(Expression.PropertyOrField(expression, fieldName), expression).Compile();
        }
    }
}
