using System;

namespace AopSugar
{
    /// <summary>
    /// 对属性是否进行自动装配的标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AssembleAttribute : Attribute
    {
        private string m_Name = null;

        public AssembleAttribute()
        {
 
        }

        public AssembleAttribute(string name)
        {
            this.m_Name = name;
        }

        public string Name
        {
            get { return m_Name; }
        }
    }
}