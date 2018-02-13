using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AopSugar
{
    /// <summary>
    /// 植入AOP
    /// </summary>
    public class Initialize
    {
        /// <summary>
        /// 如果存在AOP标记，则开始初始化上下文对象
        /// </summary>
        /// <param name="il"></param>
        /// <param name="paramTypes"></param>
        /// <param name="obj_arr"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static LocalBuilder Context(ILGenerator il, Type[] paramTypes, ref LocalBuilder obj_arr, MethodInfo method, FieldBuilder agent)
        {
            obj_arr = ParameterArray(il, paramTypes);

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

            var setMethodInfoMethod = aspectType.GetMethod("set_MethodInfo");
            var getMethodType = typeof(MethodHelper).GetMethod("GetMethod");
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldarg_0); //加载类本身
            il.Emit(OpCodes.Ldfld, agent); //加载代理成员
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Call, getMethodType);
            il.Emit(OpCodes.Call, setMethodInfoMethod);
            il.Emit(OpCodes.Nop);


            var setAttrsMethod = aspectType.GetMethod("set_Attributes");
            var getCusAttributesType = typeof(MethodHelper).GetMethod("GetCustomAttributes");
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldarg_0); //加载类本身
            il.Emit(OpCodes.Ldfld, agent); //加载代理成员
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Call, getCusAttributesType);
            il.Emit(OpCodes.Call, setAttrsMethod);
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
        public static LocalBuilder[] ExecutingBasics(ILGenerator il, Type[] basicTypes, LocalBuilder context)
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
        public static void Authentication(ILGenerator il, Type authType, LocalBuilder context, ref Label? lbl)
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
        /// <summary>
        /// 生成代理+属性的私有成员
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="implementType"></param>
        /// <param name="pis"></param>
        /// <param name="agent"></param>
        /// <param name="members"></param>
        public static void Members(TypeBuilder typeBuilder, Type implementType, PropertyInfo[] pis, ref FieldBuilder agent, ref FieldBuilder[] members)
        {
            //生成内部的私有成员
            agent = typeBuilder.DefineField("m_Agent", implementType, FieldAttributes.Private);

            //生成默认的构造函数
            var cb = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, Type.EmptyTypes);
            var il = cb.GetILGenerator();

            //利用空构造函数实例化m_Agent
            var info = implementType.GetConstructor(Type.EmptyTypes);

            //加载类本身
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, info);
            il.Emit(OpCodes.Stfld, agent);

            int len = pis.Length;
            members = new FieldBuilder[len];

            //初始化属性的各个成员
            for (int i = 0; i < len; i++)
            {
                var pi = pis[i];
                var fb = typeBuilder.DefineField(string.Format("m_{0}", pi.Name), pi.PropertyType, FieldAttributes.Private);

                if (pi.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldflda, fb);
                    il.Emit(OpCodes.Initobj, pi.PropertyType);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stfld, fb);
                }

                members[i] = fb;
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);
        }

        public static LocalBuilder ParameterArray(ILGenerator il, Type[] paramTypes)
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
       
        /// <summary>
        /// 利用成员代理和当前的调用参数，获取真正执行的函数结果
        /// </summary>
        /// <param name="il"></param>
        /// <param name="agent"></param>
        /// <param name="method"></param>
        /// <param name="pis"></param>
        /// <param name="paramTypes"></param>
        /// <param name="result"></param>
        /// <param name="is_void"></param>
        public static void CallResult(ILGenerator il, FieldBuilder agent, MethodInfo method, ParameterInfo[] pis, Type[] paramTypes, LocalBuilder result, bool is_void)
        {
            il.Emit(OpCodes.Ldarg_0); //加载类本身
            il.Emit(OpCodes.Ldfld, agent); //加载代理成员

            //各个参数的入栈
            for (int i = 0; i < pis.Length; i++)
                il.Emit(OpCodes.Ldarg, i + 1);

            //调用实际的代理成员方法
            var impl_method = agent.FieldType.GetMethod(method.Name, paramTypes);
            il.Emit(OpCodes.Callvirt, impl_method);

            if (!is_void)
                il.Emit(OpCodes.Stloc, result);
        }

        /// <summary>
        /// 将本次执行的结果附加到当前的上下文环境中
        /// </summary>
        /// <param name="il"></param>
        /// <param name="context"></param>
        /// <param name="result"></param>
        public static void AppendContextResult(ILGenerator il, LocalBuilder context, LocalBuilder result)
        {
            if (context == null || result == null)
                return;

            var obj = il.DeclareLocal(typeof(object));
            var method = typeof(AspectContext).GetMethod("set_Result");

            //对值类型的返回值进行装箱
            il.Emit(OpCodes.Ldloc, result);
            if (result.LocalType.IsValueType)
                il.Emit(OpCodes.Box, result.LocalType);
            else
                il.Emit(OpCodes.Castclass, typeof(object));

            il.Emit(OpCodes.Stloc, obj);

            //进行属性的赋值
            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldloc, obj);
            il.Emit(OpCodes.Callvirt, method);
        }

        /// <summary>
        /// 开始植入基本（执行后）的AOP代码
        /// </summary>
        /// <param name="il"></param>
        /// <param name="basics"></param>
        /// <param name="basicTypes"></param>
        /// <param name="context"></param>
        public static void ImplantExecutedBasics(ILGenerator il, LocalBuilder[] basics, Type[] basicTypes, LocalBuilder context)
        {
            if (basics == null || basicTypes == null || basicTypes.Length == 0 || basicTypes.Length != basics.Length)
                return;

            int len = basics.Length;
            for (int i = 0; i < len; i++)
            {
                il.Emit(OpCodes.Ldloc, basics[i]);
                il.Emit(OpCodes.Ldloc, context);
                il.Emit(OpCodes.Callvirt, basicTypes[i].GetMethod("OnExecuted"));
            }
        }
    }
}
