using System;

public static class SWICLITestClass
{
    static SWICLITestClass()
    {
        Console.WriteLine("SWICLITestClass::SWICLITestClass");
    }
    public static void install()
    {
        Console.WriteLine("SWICLITestClass::install press ctrol-D to leave CSharp");
        //System.Reflection.Assembly.Load("csharp").EntryPoint.DeclaringType.GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { new String[0] });
    }}