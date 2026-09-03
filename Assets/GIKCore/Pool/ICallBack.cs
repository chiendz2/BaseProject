public class ICallBack
{
    /// <summary>() => {...}</summary>
    public delegate void CallFunc();
    /// <summary>(T) => {...}</summary>
    public delegate void CallFunc2<in T>(T t);
    /// <summary>(T1, T2) => {...}</summary>
    public delegate void CallFunc3<in T1, in T2>(T1 t1, T2 t2);
    /// <summary>(T1, T2, T3) => {...}</summary>
    public delegate void CallFunc4<in T1, in T2, in T3>(T1 t1, T2 t2, T3 t3);
    /// <summary>() => {...return T;}</summary>
    public delegate T CallFunc5<out T>();
    /// <summary>(T2) => {...return T1}</summary>
    public delegate T1 CallFunc6<out T1, in T2>(T2 t2);
    /// <summary>(T2, T3) => {...return T1}</summary>
    public delegate T1 CallFunc7<out T1, in T2, in T3>(T2 t2, T3 t3);
    /// <summary>(T1, T2, T3, T4) => {...}</summary>
    public delegate void CallFunc8<in T1, in T2, in T3, in T4>(T1 t1, T2 t2, T3 t3, T4 t4);
}
