using System;
namespace Example4SWICLI
{
    public static class Example4SWICLIClass
    {
        static Example4SWICLIClass()
        {
            Message("Example4SWICLI::SWICLITestClass.<clinit>()");
        }
        public static void install()
        {
            Message("Example4SWICLI::SWICLITestClass.install()");
            //Message("SWICLITestClass::install press ctrol-D to leave CSharp");
            //System.Reflection.Assembly.Load("csharp").EntryPoint.DeclaringType.GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { new String[0] });
        }
        public static void Main(string[] args0)
        {
            Message("Example4SWICLI::SWICLITestClass.install()");
        }

        public static void Message(string p)
        {
            System.Windows.Forms.MessageBox.Show(p);
            Console.WriteLine(p);
        }

    }
}
