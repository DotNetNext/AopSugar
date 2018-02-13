using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace AopSugar
{
    /// <summary>
    /// Emit帮助类
    /// </summary>
    public static class EmitHelper
    {
        public static Func<object> CreateFunc(Type type)
        {
            ConstructorInfo info = type.GetConstructor(Type.EmptyTypes);
            if (info == null) throw new Exception(string.Format("类型[{0}]没有无参构造函数"));

            //使用Emit创建一个代理
            DynamicMethod method = new DynamicMethod("", typeof(object), null);
            ILGenerator il = method.GetILGenerator();

            il.Emit(OpCodes.Newobj, info);
            il.Emit(OpCodes.Castclass, typeof(object));
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Func<object>)) as Func<object>;
        }

        public static Action<object, object[]> CreatePropertiesAction(PropertyInfo[] infos)
        {
            Type classType = GetClassTypeByProperty(infos);
            DynamicMethod method = new DynamicMethod("", null, new Type[] { typeof(object), typeof(object[]) }, true);
            ILGenerator il = method.GetILGenerator();

            LocalBuilder obj = il.DeclareLocal(classType);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Unbox_Any, classType); //对要赋值的对象进行拆箱
            il.Emit(OpCodes.Stloc_0);

            for (int i = 0; i < infos.Length; i++)
            {
                Label lbl_end = il.DefineLabel();
                Type propType = infos[i].PropertyType;

                il.Emit(OpCodes.Ldarg_1);
                Ldc(il, i);
                il.Emit(OpCodes.Ldelem_Ref); //定位i处的value

                il.Emit(OpCodes.Ldnull); //if (arr[i] != null) { } 
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue_S, lbl_end); //判断是否为null，为null则跳过

                il.Emit(OpCodes.Ldloc_0); //对象压栈
                il.Emit(OpCodes.Ldarg_1); //值数组压栈
                Ldc(il, i);               //压入索引
                il.Emit(OpCodes.Ldelem_Ref); //取索引处的值

                if (propType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, propType); //拆箱
                else
                    il.Emit(OpCodes.Castclass, propType);

                il.Emit(OpCodes.Callvirt, infos[i].GetSetMethod(true)); //调用属性的set方法给属性赋值
                il.MarkLabel(lbl_end);
            }

            il.Emit(OpCodes.Ret);
            return method.CreateDelegate(typeof(Action<object, object[]>)) as Action<object, object[]>;
        }

        private static void Ldc(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
            else
                il.Emit(OpCodes.Ldc_I4, value);
        }

        public static Type GetClassTypeByProperty(PropertyInfo[] infos)
        {
            if (infos == null || infos.Length <= 0)
                throw new ArgumentNullException("infos");

            return infos[0].ReflectedType;
        }

        public static void ImplantBeginException(ILGenerator il, Type exType)
        {
            if (exType == null)
                return;

            il.BeginExceptionBlock();
        }

        public static void ImplantCatchException(ILGenerator il, Type exType, LocalBuilder context)
        {
            if (exType == null)
                return;

            Type type = typeof(Exception);
            var ex = il.DeclareLocal(type);

            il.BeginCatchBlock(type);

            //进入catch块后，此时的栈顶就是Exception对象
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Stloc, ex);

            ConstructorInfo info = exType.GetConstructor(Type.EmptyTypes);
            il.Emit(OpCodes.Newobj, info);

            var method = exType.GetMethod("OnException");

            il.Emit(OpCodes.Ldloc, context);
            il.Emit(OpCodes.Ldloc, ex);
            il.Emit(OpCodes.Callvirt, method);

            il.EndExceptionBlock();
        }

        public static void Ldind(ILGenerator il, Type type)
        {
            if (type == CommonConst.I1)
                il.Emit(OpCodes.Ldind_I1);
            else if (type == CommonConst.I2)
                il.Emit(OpCodes.Ldind_I2);
            else if (type == CommonConst.I4)
                il.Emit(OpCodes.Ldind_I4);
            else if (type == CommonConst.I8)
                il.Emit(OpCodes.Ldind_I8);
            else if (type == CommonConst.R4)
                il.Emit(OpCodes.Ldind_R4);
            else if (type == CommonConst.U1)
                il.Emit(OpCodes.Ldind_U1);
            else if (type == CommonConst.U2)
                il.Emit(OpCodes.Ldind_U2);
            else if (type == CommonConst.U4)
                il.Emit(OpCodes.Ldind_U4);
        }

    }
}