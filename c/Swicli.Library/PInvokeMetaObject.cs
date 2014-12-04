using System;
#if NET40
using System.Dynamic;
#endif
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.IO;
// See http://tirania.org/blog/archive/2009/Aug-11.html
namespace Swicli.Library
{
    // #if NET40
    public class PInvokeMetaObject

#if NET40
        : DynamicMetaObject
#endif
    {
        public PInvokeMetaObject(Expression parameter, object o)
#if NET40

            :            base(parameter, BindingRestrictions.Empty, o) { } 
#else
        {
            i_parameter = parameter;
            i_Obj = o;
        }
        Expression i_parameter;
        Object i_Obj;
#endif
#if NET40
        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {

            var self = this.Expression;
            var pinvoke = (PInvoke)base.Value;

            var arg_types = new Type[args.Length];
            var arg_exps = new Expression[args.Length];
        
            for (int i = 0; i < args.Length; ++i)
            {
                arg_types[i] = args[i].LimitType;
                arg_exps[i] = args[i].Expression;
            }

            var m = pinvoke.GetInvoke(binder.Name, arg_types, binder.ReturnType);
            if (m == null)
                return base.BindInvokeMember(binder, args);
 
            var target = Expression.Block(
                       Expression.Call(m, arg_exps),
                       Expression.Default(typeof(object)));
            var restrictions = BindingRestrictions.GetTypeRestriction(self, typeof(PInvoke));

            var dynamicMetaObject = new DynamicMetaObject(target, restrictions);

            return dynamicMetaObject;
        }
    
#endif
    }
    public partial class PrologCLR
    {
        [PrologVisible]
        static public
#if NET40
         dynamic
#else
 IPInvoke
#endif
 cliGetDll(String dll)
        {
            return new PInvoke(dll);
        }

        [PrologVisible]
        public static void cliDynTest_1()
        {
            Debug("System.Dynamic.DynamicObject=" + Type.GetType("System.Dynamic.DynamicObject"));
        }

        [PrologVisible]
        public static void cliDynTest_2()
        {
            var d = cliGetDll("glibc");

            for (int i = 0; i < 2; ++i)
            {
#if NET40
                d.printf("Hello, World %d\n", i);
#endif
                d.GetInvoke("printf", new Type[] { typeof(String) }, null).Invoke(d, new object[] { "Hello GetInvoke.Invoke: %d\n", i });
            }

        }

        [PrologVisible]
        public static T cliDynTest_3<T>()
        {
            T was = default(T);
            var d = cliGetDll("libc");

            for (int i = 0; i < 2; ++i)
            {
#if NET40
                d.printf("Hello, World %d\n", i);
#else
                was = d.Invoke<T>("printf", new Type[] { typeof(String) }, null, d, new object[] { "Hello GetInvoke.Invoke: %d\n", i });
#endif
                was = d.InvokeDLL<T>("printf", new object[] { "Hello Invoke: %d\n", i });
            }
            return was;
        }
    }
    public class PInvoke : IPInvoke
#if NET40
        , DynamicObject
#endif
    {
        public static void MainLinux(String[] args)
        {
            var d = PrologCLR.cliGetDll("libc");

            for (int i = 0; i < 2; ++i)
            {
#if NET40
                d.printf("Hello, World %d\n", i);
#else
                d.GetInvoke("printf", new Type[] { typeof(String) }, null).Invoke(d, new object[] { "Hello, World %d\n", i });
#endif
                d.GetInvoke("printf", new Type[] { typeof(String) }, null).Invoke(d, new object[] { "Hello GetInvoke.Invoke: %d\n", i });
            }
        }

        public T Invoke<T>(string mspecName, Type[] paramz, Type returnType, Object targetIn, object[] args)
        {
            if (paramz == null) paramz = PrologCLR.GetObjectTypes(args, null);
            var mi = GetInvoke(mspecName, paramz, returnType);
            Object target = mi.IsStatic ? null : (targetIn ?? this);
            return (T)mi.Invoke(target, args);
        }

        string dll;
        static int clid_gen;
        AssemblyBuilder ab;
        ModuleBuilder moduleb;
        int id_gen;

        public PInvoke(string dll)
        {
            this.dll = dll;
        }

#if NET40
        public override DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new PInvokeMetaObject(parameter, this);
        }
#endif
        public MethodInfo GetInvoke(string entry_point, Type[] arg_types, Type returnType)
        {
            if (ab == null)
            {
                AssemblyName aname = new AssemblyName("ctype" + Interlocked.Increment(ref clid_gen));
                ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Run);
                moduleb = ab.DefineDynamicModule("ctype" + Interlocked.Increment(ref clid_gen));
            }

            // Can't use DynamicMethod as they don't support custom attributes
            var tb = moduleb.DefineType("ctype_" + Interlocked.Increment(ref id_gen) + "_" + entry_point);
            String entry_point_invoke = entry_point;
            tb.DefinePInvokeMethod(entry_point_invoke, dll, entry_point,
                       MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                       CallingConventions.Standard, returnType, arg_types,
                       CallingConvention.StdCall, CharSet.Auto);

            var t = tb.CreateType();
            var m = t.GetMethod(entry_point_invoke, BindingFlags.Static | BindingFlags.NonPublic);

            return m;
        }

        #region IPInvoke Members


        public T InvokeDLL<T>(string mspecName, params object[] args)
        {
            return Invoke<T>(mspecName, (Type[])null, null, this, args);
        }

        #endregion


        //
        // Our factory method
        //

        public static T Create<T>(string modulePath) where T : INativeImport
        {
            AssemblyName aName = new AssemblyName(Guid.NewGuid().ToString());
            AssemblyBuilder aBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

            ModuleBuilder mBuilder = aBuilder.DefineDynamicModule(aName.Name);

            Type interfaceType = typeof(T);

            TypeBuilder tBuilder = mBuilder.DefineType(Guid.NewGuid().ToString(),
                TypeAttributes.Public, typeof(NativeImportBase), new Type[] { typeof(T) });

            // Define default constructor
            DefineConstructor(tBuilder);

            // Storage for modules to load
            List<string> moduleNames = new List<string>();

            MethodInfo[] interfaceMethods = interfaceType.GetMethods();

            foreach (MethodInfo methodInfo in interfaceMethods)
            {
                // Get custom attribute
                ImportFunction[] attrs = (ImportFunction[])methodInfo.GetCustomAttributes(typeof(ImportFunction), false);

                if (attrs == null || attrs.Length < 1)
                    throw new InvalidOperationException(
                        String.Format(
                            "ImportFunction attribute not specified for function {0}", methodInfo.Name));

                ImportFunction attr = attrs[0];
                ParameterInfo[] paramInfos = methodInfo.GetParameters();
                Type[] parameters = new Type[paramInfos.Length];

                for (int i = 0; i < paramInfos.Length; ++i)
                    parameters[i] = paramInfos[i].ParameterType;

                //
                // Check if module path is defined
                //
                string moduleName;
                if (!String.Empty.Equals(modulePath))
                    moduleName = Path.Combine(modulePath, attr.ModuleName);
                else
                    moduleName = attr.ModuleName;

                //
                // Define PInvoke method
                //
                MethodBuilder pInvokeMethod = DefinePInvokeMethod(tBuilder, methodInfo.Name, moduleName,
                    attr.CallingConvention, attr.CharSet,
                    methodInfo.ReturnType, parameters);

                //
                // Define proxy method
                //
                MethodBuilder proxyMethod = DefineProxyMethod<T>(tBuilder, methodInfo.Name,
                    methodInfo.ReturnType, parameters, pInvokeMethod);

                //
                // Add module to list if not already added
                //
                if (!moduleNames.Exists(
                    delegate(string str)
                    {
                        return String.Equals(str, moduleName, StringComparison.InvariantCultureIgnoreCase);
                    }))
                {
                    moduleNames.Add(moduleName);
                }

            }

            //
            // Create type
            //
            Type t = tBuilder.CreateType();

            //
            // Create instance using constructor with List<string> parameters
            // Pass module names as parameter
            //
            object o = Activator.CreateInstance(t, new object[] { moduleNames });

            return (T)o;
        }

        static Module aBuilder_ModuleResolve(object sender, ResolveEventArgs e)
        {
            Console.WriteLine("asdASD");
            return null;
        }

        private static void DefineConstructor(TypeBuilder tBuilder)
        {
            // Ctor takes List<string> as module file names to load
            Type[] ctorParameters = new Type[] { typeof(List<string>) };

            ConstructorBuilder ctorBuilder = tBuilder.DefineConstructor(
                MethodAttributes.Public, CallingConventions.Standard, ctorParameters);
            ILGenerator ctorIL = ctorBuilder.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_1);
            ctorIL.Emit(OpCodes.Call, typeof(NativeImportBase).GetConstructor(ctorParameters));
            ctorIL.Emit(OpCodes.Ret);
        }

        private static MethodBuilder DefinePInvokeMethod(TypeBuilder tBuilder, string methodName, string moduleName,
            CallingConvention callingConvention, CharSet charset, Type returnType, Type[] parameters)
        {
            MethodBuilder pInvokeMethod = tBuilder.DefinePInvokeMethod(methodName, moduleName,
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                CallingConventions.Standard,
                returnType,
                parameters,
                callingConvention, charset);

            if (!returnType.Equals(typeof(void)))
                pInvokeMethod.SetImplementationFlags(pInvokeMethod.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            return pInvokeMethod;
        }

        private static MethodBuilder DefineProxyMethod<T>(TypeBuilder tBuilder, string methodName, Type returnType, Type[] parameters, MethodBuilder proxiedMethod)
        {
            MethodBuilder proxyMethod = tBuilder.DefineMethod(methodName,
                MethodAttributes.Public | MethodAttributes.Virtual,
                returnType,
                parameters);

            ILGenerator proxyMethodIL = proxyMethod.GetILGenerator();

            //
            // Pass parameters, since this is proxy for a static method, skip first arg
            //
            for (int i = 1; i <= parameters.Length; ++i)
            {
                switch (i)
                {
                    case 1:
                        proxyMethodIL.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        proxyMethodIL.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        proxyMethodIL.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        proxyMethodIL.Emit(OpCodes.Ldarg, i);
                        break;
                }
            }

            proxyMethodIL.Emit(OpCodes.Call, proxiedMethod);
            proxyMethodIL.Emit(OpCodes.Ret);

            //
            // Override method defined in the interface
            //
            tBuilder.DefineMethodOverride(proxyMethod, typeof(T).GetMethod(methodName));

            return proxyMethod;

        }
    }

}
