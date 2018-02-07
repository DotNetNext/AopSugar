using System;

namespace AopSugar
{
    /// <summary>
    /// 为有一个良好的易读的注册接口而扩展的语法糖
    /// </summary>
    public static class BindExtension
    {
        public static Tuple<AssembleFactory, Type, string> Bind<T>(this AssembleFactory factory)
        {
            return factory.Bind<T>(null);
        }

        public static Tuple<AssembleFactory, Type, string> Bind<T>(this AssembleFactory factory, string name)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            return new Tuple<AssembleFactory, Type, string>(factory, typeof(T), name);
        }

        public static void To(this Tuple<AssembleFactory, Type, string> bind, object element)
        {
            if (bind == null)
                throw new ArgumentNullException("bind");

            if (element == null)
                throw new ArgumentNullException("element");

            if (bind.Item2 == null)
                throw new ArgumentNullException("bind.Type");

            if (bind.Item1 == null)
                throw new ArgumentNullException("bind.Factory");

            bind.Item1.Register(bind.Item3, bind.Item2, element);
        }

        public static void To<T>(this Tuple<AssembleFactory, Type, string> bind)
        {
            bind.To<T>(false);
        }

        public static void To<T>(this Tuple<AssembleFactory, Type, string> bind, bool singleton)
        {
            if (bind == null)
                throw new ArgumentNullException("bind");

            if (bind.Item2 == null)
                throw new ArgumentNullException("bind.Type");

            if (bind.Item1 == null)
                throw new ArgumentNullException("bind.Factory");

            bind.Item1.Register(bind.Item3, bind.Item2, typeof(T), singleton, false);
        }
    }
}