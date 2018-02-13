using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace AopSugar
{
    /// <summary>
    /// 装配工厂
    /// </summary>
    public class AssembleFactory
    {
        #region fields and properies


        //允许的最大装配深度
        private int m_MaxDepth = 1;
        //动态生成的dll保存到本地的名称（保存的路径就是当前程序运行的bin目录）
        private string m_DllName = null;
        private ModuleBuilder m_ModuleBuilder = null;
        private AssemblyBuilder m_AssemblyBuilder = null;

        //单例实例的缓存池
        private Dictionary<string, object> INSTANCE_DICTIONARY = new Dictionary<string, object>();
        //注册类型的缓存池
        private Dictionary<string, BindElement> BIND_DICTIONARY = new Dictionary<string, BindElement>();
        //实例代理的缓存池
        private Dictionary<string, Func<object>> FUNC_DICTIONARY = new Dictionary<string, Func<object>>();
        #endregion

        public AssembleFactory(int maxDepth)
        {
            if (maxDepth < 1)
                throw new ArgumentOutOfRangeException("maxDepth不能小于1");

            this.m_MaxDepth = maxDepth;

            //初始化AssemblyBuilder、ModuleBuilder
            InitializeAssemblyModule();
        }

        public static AssembleFactory Instance
        {
            get
            {
                if (CommonConst.m_Instance != null)
                    return CommonConst.m_Instance;

                lock (CommonConst.m_InstanceLocker)
                {
                    if (CommonConst.m_Instance != null)
                        return CommonConst.m_Instance;

                    //默认实例的最大装配深度为3（我脑子笨，多于3层我就闹不清楚了^_^）
                    CommonConst.m_Instance = new AssembleFactory(3);
                }

                return CommonConst.m_Instance;
            }
        }

        public T CreateInstance<T>(string name = null)
        {
            Type type = typeof(T);
            return (T)CreateInstance(name, type, 1); //调用深度从1开始
        }

        public void Register(string name, Type baseType, object implement)
        {
            if (baseType == null)
                throw new ArgumentNullException("baseType");

            if (implement == null)
                throw new ArgumentNullException("implement");

            //注册基本类型的单例模式时，必须指定name
            if (string.IsNullOrEmpty(name) && (baseType.IsValueType || baseType == CommonConst.S0))
                throw new Exception(string.Format("注册类型[{0}]的实例时，必须指定参数name", baseType.FullName));

            string key = GetCacheName(name, baseType);

            //注册一个单例类型
            Register(name, baseType, implement.GetType(), true, true);

            //把本单例的实例存入缓存
            lock (INSTANCE_DICTIONARY)
            {
                object obj = null;
                if (INSTANCE_DICTIONARY.TryGetValue(key, out obj))
                    INSTANCE_DICTIONARY[key] = implement;
                else
                    INSTANCE_DICTIONARY.Add(key, implement);
            }
        }

        public void Register(string name, Type baseType, Type implementType, bool singleton, bool instance)
        {
            if (baseType == null)
                throw new ArgumentNullException("baseType");

            if (implementType == null)
                throw new ArgumentNullException("implementType");

            //如果指定了节点的名称，则判重
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                int count = BIND_DICTIONARY.Values.Count(c => string.Equals(name, c.Name, StringComparison.OrdinalIgnoreCase));

                if (count > 0)
                    throw new Exception(string.Format("名称为[{0}]的节点已存在", name));
            }

            //根据指定的name和当前注册的基类型，计算缓存的key
            string key = GetCacheName(name, baseType);

            //验证注册类型是否具有继承关系
            if (baseType != implementType && !baseType.IsAssignableFrom(implementType))
                throw new ArgumentOutOfRangeException(string.Format("类型[{0}]和类型[{1}]不具有继承关系", baseType.FullName, implementType.FullName));

            if (BIND_DICTIONARY.ContainsKey(key))
                throw new ArgumentException(string.Format("类型[{0}]已经存在名称为[{1}]的项", baseType.FullName, name));

            lock (BIND_DICTIONARY)
            {
                if (BIND_DICTIONARY.ContainsKey(key))
                    throw new ArgumentException(string.Format("类型[{0}]已经存在名称为[{1}]的项", baseType.FullName, name));

                BindElement reg = new BindElement();
                reg.Name = name;
                reg.BindType = baseType;
                reg.IsInstance = instance;
                reg.Issingleton = singleton;
                reg.ToType = implementType;

                BIND_DICTIONARY.Add(key, reg);
            }
        }

        private object CreateInstance(string name, Type type, int depth)
        {
            //验证装配深度
            if (depth > m_MaxDepth)
                throw new Exception(string.Format("当前的装配深度：{0}，已超过设定值：{1}", depth, m_MaxDepth));

            BindElement bind = null;
            string key = GetCacheName(name, type);

            if (!BIND_DICTIONARY.TryGetValue(key, out bind))
            {
                //未找到注册节点的所有接口类型，抛出异常
                if (type.IsInterface)
                    throw new Exception(string.Format("未找到类型[{0}]名称[{0}]的注册节点", type.FullName, name));

                //未找到注册节点的非接口类型，则主动注册一个（不进行AOP规则适配，仅对可装配的属性进行匹配）
                Register(name, type, type, false, false);

                if (!BIND_DICTIONARY.TryGetValue(key, out bind))
                    throw new Exception(string.Format("未找到类型[{0}]名称[{0}]的注册节点", type.FullName, name));
            }

            object result = null;
            bool singleton = bind.Issingleton;

            //如果是单例，则首先查找单例池
            if (singleton && INSTANCE_DICTIONARY.TryGetValue(key, out result))
                return result;

            result = CreateInstance(key, singleton);
            if (result != null)
                return AppendProperties(result, bind, depth);

            lock (FUNC_DICTIONARY)
            {
                result = CreateInstance(key, singleton);
                if (result != null)
                    return AppendProperties(result, bind, depth);

                Type agentType = null;
                //如果注册的基类是非接口类型，则不适用AOP的规则，直接使用注册的实现类型来创建代理
                if (!bind.BindType.IsInterface || (bind.BindType.IsInterface && !HasAspectAttribute(bind.ToType)))
                    agentType = CreateAgentTypeByClass(bind.BindType); //创建代理类
                else
                {
                    agentType = CreateAgentTypeByInterface(bind.BindType, bind.ToType); //创建代理类

                    //更新BindElement
                    lock (bind)
                        bind.AgentType = agentType;
                }

                //创建生成类实例的委托
                var func = EmitHelper.CreateFunc(agentType);

                //生成本次的新的实例
                result = func();

                //加入缓存
                FUNC_DICTIONARY.Add(key, func);

                //处理单例
                if (singleton)
                {
                    lock (INSTANCE_DICTIONARY)
                    {
                        object instance = null;
                        if (INSTANCE_DICTIONARY.TryGetValue(key, out instance))
                            return instance;
                        else
                            INSTANCE_DICTIONARY.Add(key, result);
                    }
                }
            }

            return AppendProperties(result, bind, depth);
        }


        private object CreateInstance(string key, bool singleton)
        {
            Func<object> func = null;
            if (FUNC_DICTIONARY.TryGetValue(key, out func) && func != null && !singleton)
                return func();

            object result = null;
            if (func != null && singleton)
            {
                lock (INSTANCE_DICTIONARY)
                {
                    if (INSTANCE_DICTIONARY.TryGetValue(key, out result))
                        return result;

                    result = func();
                    INSTANCE_DICTIONARY.Add(key, result);
                }
            }

            return result;
        }

        private object AppendProperties(object result, BindElement bind, int depth)
        {
            //如果是单例，则不在进行各个属性的再次查找，赋值
            if (bind.Issingleton)
                return result;

            //对于注册类型可用于当前对象属性装配的逻辑查找
            CreateAssigment(bind);

            if (bind.Assigment.Action != null)
            {
                //根据匹配的可装配的节点生成实例对象
                IList<object> values = new List<object>();
                foreach (var item in bind.Assigment.RegisterTypes)
                    values.Add(CreateInstance(item.Name, item.BindType, depth + 1)); //注意：这里有递归的调用（为防止死循环的发生，需要对调用深度进行限制）

                //对所有可装配的属性进行赋值
                bind.Assigment.Action(result, values.ToArray());
            }

            return result;
        }

        private void CreateAssigment(BindElement bind)
        {
            if (bind.Assigment != null)
                return;

            lock (bind)
            {
                if (bind.Assigment != null)
                    return;

                IList<BindElement> list_be = new List<BindElement>();
                IList<PropertyInfo> list_pi = new List<PropertyInfo>();

                Type elType = bind.AgentType == null ? bind.ToType : bind.AgentType;
                var pis = elType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var pi in pis)
                {
                    //如果此属性不可写，则直接忽略
                    if (!pi.CanWrite) continue;

                    //查找属性是否有装配标记（没有则直接忽略）
                    var atts = pi.GetCustomAttributes(typeof(AssembleAttribute), false);
                    if (atts == null || atts.Length != 1)
                        continue;

                    var assemble = atts[0] as AssembleAttribute;
                    if (assemble == null)
                        continue;

                    string name = assemble.Name;
                    if (!string.IsNullOrEmpty(name))
                        name = name.Trim();

                    //开始匹配能够适配当前属性的注册节点
                    var be = GetBindElement(pi.PropertyType, name);

                    //如果没找到匹配的注册类型，则抛出异常
                    if (be == null)
                        throw new Exception(string.Format("未能在注册类型中匹配类型[{0}]中的属性[{1}]", bind.BindType.FullName, pi.Name));

                    //记录匹配的结果
                    list_pi.Add(pi);
                    list_be.Add(be);
                }

                PropertiesAction assigment = new PropertiesAction();

                if (list_pi.Count > 0)
                {
                    //记录匹配的节点
                    assigment.RegisterTypes = list_be;

                    //利用得到匹配的属性集合，得到赋值对应属性的委托
                    assigment.Action = EmitHelper.CreatePropertiesAction(list_pi.ToArray());
                }

                bind.Assigment = assigment;
            }
        }

        private BindElement GetBindElement(Type type, string name)
        {
            BindElement bind = null;
            var values = BIND_DICTIONARY.Values;

            if (string.IsNullOrEmpty(name))
            {
                //首先进行属性类型的精确匹配
                bind = values.FirstOrDefault(c => c.BindType == type && string.IsNullOrEmpty(c.Name));
                if (bind != null) return bind;

                //查找能够与当前属性类型匹配的其子类的信息
                foreach (var item in values)
                {
                    //TOTO:这里是否限制在未指定名称的范围内查找
                    if (string.IsNullOrEmpty(item.Name) && type.IsAssignableFrom(item.BindType))
                        return item;
                }

                return null;
            }

            //如果指定了名称，则判断使用该名称的注册类型是否能够生成当前的属性值
            bind = values.FirstOrDefault(c => string.Equals(name, c.Name, StringComparison.OrdinalIgnoreCase));
            if (bind == null)
                throw new Exception(string.Format("未找到名称为[{0}]的注册节点", name));

            if (!type.IsAssignableFrom(bind.BindType))
                throw new Exception(string.Format("节点[{0}]注册的类型[{0}]和当前的属性类型[{1}]不具有继承关系", name, bind.BindType.FullName, type.FullName));

            return bind;
        }

        private bool HasAspectAttribute(Type type)
        {
            object[] atts = null;
            Type aspect = typeof(AspectAttribute);

            //检测类型上的标记
            atts = type.GetCustomAttributes(true);
            if (atts != null && atts.Length > 0)
            {
                foreach (var item in atts)
                {
                    if (aspect.IsInstanceOfType(item))
                        return true;
                }
            }

            //检测方法上的标记
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                atts = method.GetCustomAttributes(true);
                if (atts == null || atts.Length == 0)
                    continue;

                foreach (var item in atts)
                {
                    if (aspect.IsInstanceOfType(item))
                        return true;
                }
            }

            return false;
        }

        private Type CreateAgentTypeByClass(Type bindType)
        {

            string typeName = GetTypeName(bindType);
            TypeAttributes attr = TypeAttributes.Class | TypeAttributes.Public;
            TypeBuilder typeBuilder = m_ModuleBuilder.DefineType(typeName, attr, bindType, Type.EmptyTypes);

            //获取所有的父接口
            //IList<Type> interfaces = new List<Type>();
            //GetAllFatherInterfaces(bindType, interfaces);

            //获取所有属性
            PropertyInfo[] pis = GetAllPropeties(new Type[] { bindType });

            //生成代理+属性的私有成员
            FieldBuilder agent = null;
            FieldBuilder[] members = null;
            AopCore.InitializeMembers(typeBuilder, bindType, pis, ref agent, ref members);

            //识别AOP标记
            Type exType = null;
            Type authType = null;
            Type[] basicTypes = null;

            //识别类上做的标记
            var atts = bindType.GetCustomAttributes(true);
            FilterAspect(atts, ref basicTypes, ref authType, ref exType);

            //处理接口中的所有方法（对方法进行IL植入）
            MethodInfo[] methods = bindType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")) continue;
                ImplementMethodByClass(typeBuilder, agent, method, basicTypes, authType, exType);
            }

            //处理接口中的所有属性（自定义标签的植入）
            //for (int i = 0; i < pis.Length; i++)
            //    ImplementProperty(typeBuilder, pis[i], agent, members[i]);

            Type dynamicType = typeBuilder.CreateType();

            //如果想看动态生成的实际类型，可放开此代码，就可在运行dll的目录找到
            //m_AssemblyBuilder.Save(m_DllName); //保存到本地

            return dynamicType;
        }
        private Type CreateAgentTypeByInterface(Type interfaceType, Type implementType)
        {
            string typeName = GetTypeName(implementType);
            TypeAttributes attr = TypeAttributes.Class | TypeAttributes.Public;
            TypeBuilder typeBuilder = m_ModuleBuilder.DefineType(typeName, attr, null, new Type[] { interfaceType });

            //获取所有的父接口
            IList<Type> interfaces = new List<Type>();
            GetAllFatherInterfaces(interfaceType, interfaces);

            //获取所有属性
            PropertyInfo[] pis = GetAllPropeties(interfaces);

            //生成代理+属性的私有成员
            FieldBuilder agent = null;
            FieldBuilder[] members = null;
            AopCore.InitializeMembers(typeBuilder, implementType, pis, ref agent, ref members);

            //识别AOP标记
            Type exType = null;
            Type authType = null;
            Type[] basicTypes = null;

            //识别类上做的标记
            var atts = implementType.GetCustomAttributes(true);
            FilterAspect(atts, ref basicTypes, ref authType, ref exType);

            //处理接口中的所有方法（对方法进行IL植入）
            MethodInfo[] methods = GetAllMethods(interfaces);
            foreach (MethodInfo method in methods)
            {
                //忽略所有属性的get和set方法
                if (method.IsSpecialName)
                    continue;

                ImplementMethodByInterface(typeBuilder, agent, method, basicTypes, authType, exType);
            }

            //处理接口中的所有属性（自定义标签的植入）
            for (int i = 0; i < pis.Length; i++)
                ImplementProperty(typeBuilder, pis[i], agent, members[i]);

            Type dynamicType = typeBuilder.CreateType();

            //如果想看动态生成的实际类型，可放开此代码，就可在运行dll的目录找到
            //m_AssemblyBuilder.Save(m_DllName); //保存到本地

            return dynamicType;
        }

        private MethodInfo[] GetAllMethods(IList<Type> list)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (var item in list)
                methods.AddRange(item.GetMethods(BindingFlags.Public | BindingFlags.Instance));

            return methods.ToArray();
        }

        private PropertyInfo[] GetAllPropeties(IList<Type> list)
        {
            List<PropertyInfo> props = new List<PropertyInfo>();
            foreach (var item in list)
                props.AddRange(item.GetProperties(BindingFlags.Public | BindingFlags.Instance));

            return props.ToArray();
        }

        private void GetAllFatherInterfaces(Type type, IList<Type> list)
        {
            list.Add(type);
            var types = type.GetInterfaces(); //获取所有的继承的接口

            if (types == null || types.Length == 0)
                return;

            foreach (var item in types)
                GetAllFatherInterfaces(item, list);
        }

        #region Helpers
        private void ImplementMethodByClass(TypeBuilder typeBuilder, FieldBuilder agent, MethodInfo method, Type[] basicTypes, Type authType, Type exType)
        {
            var pis = method.GetParameters();
            if (!method.IsPublic || !method.IsVirtual || MethodHelper.IsObjectMethod(method)) return;

            Type[] paramTypes = MethodHelper.GetParameterTypes(method);
            MethodAttributes attr = MethodAttributes.Public | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            MethodBuilder mb = typeBuilder.DefineMethod(method.Name, attr, method.ReturnType, paramTypes);

            ILGenerator il = mb.GetILGenerator();
            bool is_void = false;
            LocalBuilder result = InitializeResult(il, method.ReturnType, ref is_void);

            //识别方法上的标记（方法上的标记覆盖类上的标记）
            var atts = method.GetCustomAttributes(true);
            FilterAspect(atts, ref basicTypes, ref authType, ref exType);

            if (basicTypes == null) return;


            //初始化上下文对象
            LocalBuilder context = null;
            LocalBuilder obj_arr = null;

            //如果存在AOP标记，则开始初始化上下文对象
            context = AopCore.InitializeAspectContext(il, paramTypes, ref obj_arr, method);


            //开始植入基本（执行前）的AOP代码
            var basics = AopCore.ImplantExecutingBasics(il, basicTypes, context);

            //开始植入认证的AOP代码
            Label? lbl = null;
            AopCore.ImplantAuthentication(il, authType, context, ref lbl);

            //开始植入异常(try)AOP代码
            EmitHelper.ImplantBeginException(il, exType);

            //利用成员代理和当前的调用参数，获取真正执行的函数结果
            CallResult(il, agent, method, pis, paramTypes, result, is_void);

            //开始植入异常(catch)AOP代码
            EmitHelper.ImplantCatchException(il, exType, context);

            //如果有标签，则把标签定位到本位置（认证AOP使用）
            if (lbl.HasValue) il.MarkLabel(lbl.Value);

            //对ref参数的值进行重新赋值
            AopCore.AppendParameterRefValues(il, paramTypes, obj_arr);

            //将本次执行的结果附加到当前的上下文环境中
            AppendContextResult(il, context, result);

            //开始植入基本（执行后）的AOP代码
            ImplantExecutedBasics(il, basics, basicTypes, context);

            //如果有返回值，则结果压栈
            if (!is_void)
                il.Emit(OpCodes.Ldloc, result);

            //返回结果
            il.Emit(OpCodes.Ret);
        }

        private void ImplementMethodByInterface(TypeBuilder typeBuilder, FieldBuilder agent, MethodInfo method, Type[] basicTypes, Type authType, Type exType)
        {
            var pis = method.GetParameters();
            Type[] paramTypes = pis.Select(c => c.ParameterType).ToArray();

            //识别方法上的标记（方法上的标记覆盖类上的标记）
            var atts = agent.FieldType.GetMethod(method.Name, paramTypes).GetCustomAttributes(true);

            //实现接口的方法标记
            MethodAttributes attr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name, attr, method.ReturnType, paramTypes);

            //构建各个参数的描述
            DefineParameters(methodBuilder, pis);

            //开始植入方法体
            ILGenerator il = methodBuilder.GetILGenerator();

            //定义返回值并初始化（如果有）
            bool is_void = false;
            LocalBuilder result = InitializeResult(il, method.ReturnType, ref is_void);

            FilterAspect(atts, ref basicTypes, ref authType, ref exType);

            //初始化上下文对象
            LocalBuilder context = null;
            LocalBuilder obj_arr = null;

            //如果存在AOP标记，则开始初始化上下文对象
            if (basicTypes != null || authType != null || exType != null)
                context = AopCore.InitializeAspectContext(il, paramTypes, ref obj_arr, method);

            //开始植入基本（执行前）的AOP代码
            var basics = AopCore.ImplantExecutingBasics(il, basicTypes, context);

            //开始植入认证的AOP代码
            Label? lbl = null;
            AopCore.ImplantAuthentication(il, authType, context, ref lbl);

            //开始植入异常(try)AOP代码
            EmitHelper.ImplantBeginException(il, exType);

            //利用成员代理和当前的调用参数，获取真正执行的函数结果
            CallResult(il, agent, method, pis, paramTypes, result, is_void);

            //开始植入异常(catch)AOP代码
            EmitHelper.ImplantCatchException(il, exType, context);

            //如果有标签，则把标签定位到本位置（认证AOP使用）
            if (lbl.HasValue) il.MarkLabel(lbl.Value);

            //对ref参数的值进行重新赋值
            AopCore.AppendParameterRefValues(il, paramTypes, obj_arr);

            //将本次执行的结果附加到当前的上下文环境中
            AppendContextResult(il, context, result);

            //开始植入基本（执行后）的AOP代码
            ImplantExecutedBasics(il, basics, basicTypes, context);

            //如果有返回值，则结果压栈
            if (!is_void)
                il.Emit(OpCodes.Ldloc, result);

            //返回结果
            il.Emit(OpCodes.Ret);
        }

        private void ImplementProperty(TypeBuilder typeBuilder, PropertyInfo pi, FieldBuilder agent, FieldBuilder m_Member)
        {
            string name = pi.Name;
            Type type = pi.PropertyType;

            //生成属性
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, null);
            var attr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final;

            //可读
            if (pi.CanRead)
            {
                MethodBuilder getter = typeBuilder.DefineMethod(string.Format("get_{0}", name), attr, type, Type.EmptyTypes);
                ILGenerator il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, m_Member);
                il.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getter);
            }

            //可写
            if (pi.CanWrite)
            {
                MethodBuilder setter = typeBuilder.DefineMethod(string.Format("set_{0}", name), attr, null, new Type[] { type });
                setter.DefineParameter(1, ParameterAttributes.None, "value");
                ILGenerator il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, m_Member);
                il.Emit(OpCodes.Ret);

                propertyBuilder.SetSetMethod(setter);
            }

            var agentProp = agent.FieldType.GetProperty(name);

            //处理本框架能识别的标记，目前仅AssembleAttribute（自动装配标记）
            //其他的标记也不用处理，处理了也没用
            Type assType = typeof(AssembleAttribute);
            var atts = agentProp.GetCustomAttributes(assType, false);
            if (atts == null || atts.Length != 1)
                return;

            var att = atts[0] as AssembleAttribute;
            if (att == null)
                return;

            CustomAttributeBuilder cab = null;
            if (string.IsNullOrEmpty(att.Name))
            {
                var info = assType.GetConstructor(Type.EmptyTypes);
                cab = new CustomAttributeBuilder(info, null);
            }
            else
            {
                var info = assType.GetConstructor(new Type[] { typeof(string) });
                cab = new CustomAttributeBuilder(info, new object[] { att.Name });
            }

            //给属性打标记
            propertyBuilder.SetCustomAttribute(cab);
        }

        private LocalBuilder InitializeResult(ILGenerator il, Type returnType, ref bool is_void)
        {
            is_void = returnType == typeof(void);
            if (is_void) return null;

            LocalBuilder result = il.DeclareLocal(returnType);

            if (returnType.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, result);
                il.Emit(OpCodes.Initobj, returnType);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, result);
            }

            return result;
        }



        private void AppendContextResult(ILGenerator il, LocalBuilder context, LocalBuilder result)
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






        private void ImplantExecutedBasics(ILGenerator il, LocalBuilder[] basics, Type[] basicTypes, LocalBuilder context)
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

        private void FilterAspect(object[] atts, ref Type[] basicTypes, ref Type authType, ref Type exType)
        {
            if (atts == null || atts.Length == 0)
                return;

            var list = new List<Type>();
            foreach (object att in atts)
            {
                if (CommonConst.AT.IsInstanceOfType(att))
                    authType = att.GetType();
                else if (CommonConst.ET.IsInstanceOfType(att))
                    exType = att.GetType();
                else if (CommonConst.BT.IsInstanceOfType(att))
                    list.Add(att.GetType());
            }

            if (list.Count > 0) basicTypes = list.ToArray();
        }

        private void CallResult(ILGenerator il, FieldBuilder agent, MethodInfo method, ParameterInfo[] pis, Type[] paramTypes, LocalBuilder result, bool is_void)
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


        private void DefineParameters(MethodBuilder methodBuilder, ParameterInfo[] pis)
        {
            //构建各个参数的描述
            for (int i = 0; i < pis.Length; i++)
            {
                var pi = pis[i];
                var pb = methodBuilder.DefineParameter(i + 1, pi.Attributes, pi.Name);

                if (pi.IsOptional)
                    pb.SetConstant(pi.DefaultValue);
            }
        }

        private string GetTypeName(Type type)
        {
            //考虑到一个类型可能会实现多个接口，所以这里需要重新生成一个类名
            Random rnd = new Random();
            string name = string.Format("{0}_C{1}", type.FullName, rnd.Next(10000, 99999));

            //在当前程序集里进行类型的排重
            while (m_AssemblyBuilder.GetType(name, false, true) != null)
                name = string.Format("{0}_C{1}", type.FullName, rnd.Next(10000, 99999));

            return name;
        }

        private void InitializeAssemblyModule()
        {
            //创建规则：一个AgentFactory实例一个dll
            Random rnd = new Random();
            string assName = string.Format("DotNet.Dynamic.A{0}", rnd.Next(10000, 99999));
            var assNames = AppDomain.CurrentDomain.GetAssemblies().Select(c => c.GetName(false).Name);

            //在当前应用程序域里进行程序集的排重
            while (assNames.Count(c => string.Equals(c, assName, StringComparison.OrdinalIgnoreCase)) > 0)
                assName = string.Format("DotNet.Dynamic.A{0}", rnd.Next(10000, 99999));

            m_DllName = string.Format("{0}.dll", assName);
            AssemblyName assemblyName = new AssemblyName(assName);
            m_AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            m_ModuleBuilder = m_AssemblyBuilder.DefineDynamicModule(assName, m_DllName);
        }

        private string GetCacheName(string name, Type type)
        {
            if (!string.IsNullOrEmpty(name))
                return name = name.ToLower().Trim();

            return string.Format("!!_{0}_{1}", type.FullName, type.Assembly.FullName);
        }

        #endregion
    }
}