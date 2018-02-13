using AopSugar;
using AopTest;
using System;
using System.Reflection;

public class a {
    public MethodInfo GetMethod() {
        return default(MethodInfo);
    }
}
public class UserMamager_C81551 : UserMamager
{
    // Fields
    private UserMamager m_Agent = new UserMamager();
    private DateTime m_Birthday = new DateTime();
    private string m_Name = null;
    private string m_Version = null;

    // Methods
    public override bool ValidateUser(string text1, string text2)
    {
        bool flag = new bool();
        object[] objArray = new object[] { text1, text2 };
        AspectContext context = new AspectContext
        {
            Args = objArray,
            MethodName = "ValidateUser",
            ClassName = "UserMamager",
            Namespace = "AopTest"
        };
        context.MethodInfo = MethodHelper.GetMethod(m_Agent, "ValidateUser");
        context.Attributes = MethodHelper.GetCustomAttributes(m_Agent, "ValidateUser");
        ActionFilter filter = new ActionFilter();
        filter.OnExecuting(context);
        LogFilter filter2 = new LogFilter();
        filter2.OnExecuting(context);
        if (new AuthenticatioFilter().OnAuthentication(context))
        {
            try
            {
                flag = this.m_Agent.ValidateUser(text1, text2);
            }
            catch (Exception exception)
            {
                new ExceptionFilter().OnException(context, exception);
            }
        }
        object obj2 = flag;
        context.Result = obj2;
        filter.OnExecuted(context);
        filter2.OnExecuted(context);
        return flag;
    }

}


 

