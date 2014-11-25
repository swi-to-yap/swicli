using System;
using Swicli.Library;
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
            dynamic d = new PInvoke("glibc");
            PrologCLR.cliDynTest_1();
            PrologCLR.cliDynTest_3();
            PrologCLR.cliDynTest_2();
        }
        public static void install()
        {
            Console.WriteLine("SWICLITestDLL::SWICLITestClass.install()");    
            //Console.WriteLine("SWICLITestClass::install press ctrol-D to leave CSharp");
            //System.Reflection.Assembly.Load("csharp").EntryPoint.DeclaringType.GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { new String[0] });
        }
    }

}