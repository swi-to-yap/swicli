using System;
namespace SWICFFITests
{
    public static class SWICFFITestsProgram
    {
        static SWICFFITestsProgram()
        {
            Console.WriteLine("SWICLITestDLL::SWICLITestClass.<clinit>()");
        }

        public static void Main(String[] args)
        {
            dynamic d = new PInvoke("libc");

            for (int i = 0; i < 100; ++i)
                d.printf("Hello, World %d\n", i);
        }
        public static void install()
        {
            Console.WriteLine("SWICLITestDLL::SWICLITestClass.install()");    
            //Console.WriteLine("SWICLITestClass::install press ctrol-D to leave CSharp");
            //System.Reflection.Assembly.Load("csharp").EntryPoint.DeclaringType.GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { new String[0] });
        }
    }

}