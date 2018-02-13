﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AopSugar
{
    /// <summary>
    /// 执入AOP
    /// </summary>
    public class AopCore
    {
        /// <summary>
        /// 如果存在AOP标记，则开始初始化上下文对象
        /// </summary>
        /// <param name="il"></param>
        /// <param name="paramTypes"></param>
        /// <param name="obj_arr"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static LocalBuilder InitializeAspectContext(ILGenerator il, Type[] paramTypes, ref LocalBuilder obj_arr, MethodInfo method)
        {
            obj_arr = InitializeParameterArray(il, paramTypes);

            //初始化AspectContext
            Type aspectType = typeof(AspectContext);
            ConstructorInfo info = aspectType.GetConstructor(Type.EmptyTypes);

            var context = il.DeclareLocal(aspectType);
            il.Emit(OpCodes.Newobj, info);
            il.Emit(OpCodes.Stloc, context);

            //给AspectContext的参数值属性Args赋值
            var setArgsMethod = aspectType.GetMethod("set_Args");
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldloc, obj_arr);
            il.Emit(OpCodes.Call, setArgsMethod);
            il.Emit(OpCodes.Nop);

            var setMethodMethod = aspectType.GetMethod("set_MethodName");
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Call, setMethodMethod);
            il.Emit(OpCodes.Nop);

            var setClassMethod = aspectType.GetMethod("set_ClassName");
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldstr, method.ReflectedType.Name);
            il.Emit(OpCodes.Call, setClassMethod);
            il.Emit(OpCodes.Nop);

            var setNamespaceMethod = aspectType.GetMethod("set_Namespace");
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldstr, method.ReflectedType.Namespace);
            il.Emit(OpCodes.Call, setNamespaceMethod);
            il.Emit(OpCodes.Nop);
            return context;
        }
        /// <summary>
        /// 开始植入基本（执行前）的AOP代码
        /// </summary>
        /// <param name="il"></param>
        /// <param name="basicTypes"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static LocalBuilder[] ImplantExecutingBasics(ILGenerator il, Type[] basicTypes, LocalBuilder context)
        {
            if (basicTypes == null || basicTypes.Length == 0)
                return null;

            int len = basicTypes.Length;
            LocalBuilder[] array = new LocalBuilder[len];

            for (int i = 0; i < len; i++)
            {
                var type = basicTypes[i];
                var basic = il.DeclareLocal(type);
                ConstructorInfo info = type.GetConstructor(Type.EmptyTypes);

                il.Emit(OpCodes.Newobj, info);
                il.Emit(OpCodes.Stloc, basic);

                var method = type.GetMethod("OnExecuting");

                il.Emit(OpCodes.Ldloc, basic);
                il.Emit(OpCodes.Ldloc, context);
                il.Emit(OpCodes.Callvirt, method);

                array[i] = basic;
            }

            return array;
        }
        /// <summary>
        /// 开始植入认证的AOP代码
        /// </summary>
        /// <param name="il"></param>
        /// <param name="authType"></param>
        /// <param name="context"></param>
        /// <param name="lbl"></param>
        public static void ImplantAuthentication(ILGenerator il, Type authType, LocalBuilder context, ref Label? lbl)
        {
            if (authType == null)
                return;

            lbl = il.DefineLabel();
            var method = authType.GetMethod("OnAuthentication");

            ConstructorInfo info = authType.GetConstructor(Type.EmptyTypes);
            il.Emit(OpCodes.Newobj, info);
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Callvirt, method);

            il.Emit(OpCodes.Brfalse, lbl.Value);
        }

        public static LocalBuilder InitializeParameterArray(ILGenerator il, Type[] paramTypes)
        {
            var obj_arr = il.DeclareLocal(typeof(object[])); //声明数组的局部变量
            il.Emit(OpCodes.Ldc_I4, paramTypes.Length); //数组长度入栈
            il.Emit(OpCodes.Newarr, typeof(object)); //初始化数组
            il.Emit(OpCodes.Stloc, obj_arr); //对数组的局部变量赋值

            //各个参数进数组
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, obj_arr); //数组对象入栈
                il.Emit(OpCodes.Ldc_I4, i); //下标入栈

                var paramType = paramTypes[i];

                //对应下标的参数值入栈，注：OpCodes.Ldarg_0 此时代表类对象本身，参数列表的下标从1开始
                if (!paramType.IsByRef)
                {
                    if (!paramType.IsValueType)
                        il.Emit(OpCodes.Ldarg, i + 1);
                    else
                    {
                        il.Emit(OpCodes.Ldarg, i + 1);
                        il.Emit(OpCodes.Box, paramType);
                    }
                }
                else
                {
                    Type unRefType = TypeHelper.GetUnRefType(paramType);
                    if (unRefType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldarg, i + 1);
                        EmitHelper.Ldind(il, unRefType);
                        il.Emit(OpCodes.Box, unRefType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_S, i + 1);
                        il.Emit(OpCodes.Ldind_Ref);
                    }
                }

                il.Emit(OpCodes.Stelem_Ref); //进行数组的赋值操作
            }

            return obj_arr;
        }


    }
}