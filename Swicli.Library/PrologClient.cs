/*  $Id$
*  
*  Project: Swicli.Library - Two Way Interface for .NET and MONO to SWI-Prolog
*  Author:        Douglas R. Miles
*  E-mail:        logicmoo@gmail.com
*  WWW:           http://www.logicmoo.com
*  Copyright (C):  2010-2012 LogicMOO Developement
*
*  This library is free software; you can redistribute it and/or
*  modify it under the terms of the GNU Lesser General Public
*  License as published by the Free Software Foundation; either
*  version 2.1 of the License, or (at your option) any later version.
*
*  This library is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
*  Lesser General Public License for more details.
*
*  You should have received a copy of the GNU Lesser General Public
*  License along with this library; if not, write to the Free Software
*  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*
*********************************************************/
#if USE_IKVM
using IKVM.Internal;
using ikvm.runtime;
using java.net;
using jpl;
#endif
#if USE_IKVM
using Hashtable = java.util.Hashtable;
using ClassLoader = java.lang.ClassLoader;
using Class = java.lang.Class;
using sun.reflect.misc;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SbsSW.SwiPlCs;
using SbsSW.SwiPlCs.Callback;
using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = Swicli.Library.PrologClient;

namespace Swicli.Library
{
    public partial class PrologClient
    {

        /// <summary>
        /// the .Net process (Not OS)
        /// </summary>
        /// <returns></returns>
        internal static bool Is64BitRuntime()
        {
            int bits = IntPtr.Size * 8;
            return bits == 64;
        }

        /// <summary>
        /// The OS and not the .Net process
        ///  therefore "Program Files" are either for 64bit or 32bit apps
        /// </summary>
        /// <returns></returns>
        internal static bool Is64BitComputer()
        {
            return Is64BitRuntime() || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
        }
        #if USE_IKVM

        internal static Term[] ToJPL(PlTermV args)
        {
            int UPPER = args.Size;
            Term[] target = new Term[UPPER];
            for (int i = 0; i < UPPER; i++)
            {
                target[i] = ToJPL(args[i]);
            }
            return target;
        }

        internal static jpl.fli.term_t ToFLI(PlTermV args)
        {
            return ToFLI(args.A0);
        }

        internal static jpl.fli.term_t ToFLI(PlTerm args)
        {
            return ToFLI(args.TermRef);
        }

        internal static PlTerm ToPLCS(Term args)
        {
            if (args is Atom) return new PlTerm(args.name());
            if (args is jpl.Variable) return new PlTerm((uint)GetField(args, "term_"));
            if (args is jpl.Float) return new PlTerm(args.doubleValue());
            if (args is jpl.Integer) return new PlTerm(args.longValue());
            if (args is jpl.Compound) return PlTerm.PlCompound(args.name(), ToPLCSV(args.args()));
            if (args is jpl.JRef)
            {
                var jref = (jpl.JRef)args;// return new PlTerm(args.doubleValue());
                var obj = jref.@ref();
                var t = new PlTerm();
                t.FromObject(obj);
                return t;
            }
            throw new ArgumentOutOfRangeException();
        }

        private static PlTermV ToPLCSV(Term[] terms)
        {
            int size = terms.Length;
            PlTermV target = NewPlTermV(size);
            for (int i = 0; i < size; i++)
            {
                target[i] = ToPLCS(terms[i]);
            }
            return target;
        }
#endif

        private static PlTermV ToPLCSV(PlTerm[] terms)
        {
            int size = terms.Length;
            PlTermV target = NewPlTermV(size);
            for (int i = 0; i < size; i++)
            {
                target[i] = terms[i];
            }
            return target;
        }

        private static PlTermV ToPLCSV1(PlTerm a0, PlTerm[] terms)
        {
            int size = terms.Length;
            PlTermV target = NewPlTermV(size + 1);
            int to = 1;
            target[0] = a0;
            for (int i = 0; i < size; i++)
            {
                target[to++] = terms[i];
            }
            return target;
        }

        private static object GetField(object term, string s)
        {
            return term.GetType().GetField(s, PrologClient.BindingFlagsALL).GetValue(term);
        }

#if USE_IKVM
        private static jpl.fli.term_t ToFLI(uint hndle)
        {
            jpl.fli.term_t t = new jpl.fli.term_t();
            t.value = hndle;
            return t;
        }

        internal static Term ToJPL(PlTerm o)
        {
            switch (o.PlType)
            {
                case PlType.PlAtom:
                    {
                        return new Atom((string)o);
                    }
                    break;
                case PlType.PlInteger:
                    {
                        return new jpl.Integer((long)o);
                    }
                    break;
                case PlType.PlFloat:
                    {
                        return new jpl.Float((double)o);
                    }
                    break;
                case PlType.PlString:
                    {
                        return new jpl.Atom((string)o);
                    }
                    break;
                case PlType.PlTerm:
                    {
                        var a = o.Arity;
                        var c = new jpl.Compound(o.Name, a);
                        for (int i = 1; i <= a; i++)
                        {
                            c.setArg(i, ToJPL(o[i]));
                        }
                        return c;
                    }
                    break;
                case PlType.PlVariable:
                    {
                        var v = new jpl.Variable();
                        SetField(v, "term_", o.TermRef);
                        return v;
                    }
                    break;
                case PlType.PlUnknown:
                    {
                        return jpl.Util.textToTerm((string)o);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#endif
        public static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlagsALL);
            //if (!field.IsPublic) field..IsPublic = true;
            field.SetValue(field.IsStatic ? null : target, value);
        }

#if USE_IKVM
        public static jpl.Term InModule(string s, jpl.Term o)
        {
            if (s == null || s == "" || s == "user") return o;
            return new jpl.Compound(":", new Term[] { new jpl.Atom(s), o });
        }
#endif
        ///<summary>
        ///</summary>
        ///<exception cref="NotImplementedException"></exception>
        public void Dispose()
        {

        }

        protected PlTerm ThisClientTerm
        {
            get
            {
                return ToProlog(this);
            }
        }

        public static PlTerm ToProlog(object value)
        {
            PlTerm t = PlTerm.PlVar();
            t.FromObject(value);
            return t;
        }

        private bool ModuleCall0(string s, PlTermV termV)
        {
            return PlQuery.PlCall(ClientModule, ClientPrefix + s, termV);
        }

        private bool ModuleCall(string s, params PlTerm[] terms)
        {
            return PlQuery.PlCall(ClientModule, ClientPrefix + s, ToPLCSV1(ThisClientTerm, terms));
        }
        public static PlTerm PlNamed(string name)
        {
            return PlTerm.PlAtom(name);
        }

        public object Eval(object obj)
        {
            PlTerm termin = PlTerm.PlVar();
            if (obj is PlTerm)
            {
                termin.Unify((PlTerm)obj);
            }
            else
            {
                termin.FromObject(obj);
            }
            PlTerm termout = PlTerm.PlVar();
            if (!ModuleCall("Eval", termin, termout)) return null;
            return PrologClient.CastTerm(termout, typeof(System.Object));
        }

        public void Intern(string varname, object value)
        {
            PlTerm termin = PlTerm.PlVar();
            termin.FromObject(value);
            ModuleCall("Intern", PlNamed(varname), termin);
        }


        public bool IsDefined(string name)
        {
            return ModuleCall("IsDefined", PlNamed(name));
        }

        public object GetSymbol(string name)
        {
            PlTerm termout = PlTerm.PlVar();
            if (!ModuleCall("GetSymbol", PlNamed(name), termout)) return null;
            return PrologClient.CastTerm(termout, typeof(System.Object));
        }

        public object Read(string line, TextWriter @delegate)
        {
            return new Nullable<PlTerm>(PlTerm.PlCompound(line));
        }

        static public void load_swiplcs()
        {

        }

        private static readonly object PrologIsSetupLock = new object();
        private static bool PrologIsSetup;
        public static void SetupProlog()
        {
            lock (PrologIsSetupLock)
            {
                if (PrologIsSetup) return;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                PrologIsSetup = true;
                SafelyRun(SetupProlog0);
                SafelyRun(SetupProlog2);
                RegisterPLCSForeigns();
            }
        }
        public static void SetupProlog0()
        {
            PrologClient.ConsoleTrace("SetupProlog");
          //  SafelyRun(SetupIKVM);
            if (!IsUseableSwiProlog(SwiHomeDir))
            {
                try
                {
                    SwiHomeDir = Application.StartupPath;
                    if (!IsUseableSwiProlog(SwiHomeDir))
                    {
                        SwiHomeDir = null;
                    }
                }
                catch (Exception)
                {
                }
            }
            if (!IsUseableSwiProlog(SwiHomeDir))
            {
                SwiHomeDir = Environment.GetEnvironmentVariable("SWI_HOME_DIR");
                if (!IsUseableSwiProlog(SwiHomeDir))
                {
                    SwiHomeDir = null;
                }
            }
            if (!IsUseableSwiProlog(SwiHomeDir))
            {

                SwiHomeDir = "c:/Program Files/pl";

                if (Is64BitComputer() && !Is64BitRuntime())
                {
                    SwiHomeDir = "c:/Program Files (x86)/pl";
                }
            }
            AltSwiHomeDir = AltSwiHomeDir ?? ".";
            bool copyPlatFormVersions = false;
            if (!IsUseableSwiProlog(SwiHomeDir))
            {
                SwiHomeDir = AltSwiHomeDir;
                copyPlatFormVersions = true;
            }
            SwiHomeDir = SwiHomeDir ?? AltSwiHomeDir;
            if (IsUseableSwiProlog(SwiHomeDir))
            {
                Environment.SetEnvironmentVariable("SWI_HOME_DIR", SwiHomeDir);
            }
            if (!ConfirmRCFile(SwiHomeDir)) ConsoleTrace("RC file missing from " + SwiHomeDir);
            string platformSuffix = Is64BitRuntime() ? "-x64" : "-x86";
            if (copyPlatFormVersions)
            {
                string destination = Path.Combine(SwiHomeDir, "bin");
                CopyFiles(destination + platformSuffix, destination, true, "*.*", true);
                destination = Path.Combine(SwiHomeDir, "lib");
                CopyFiles(destination + platformSuffix, destination, true, "*.*", true);
            }
            if (IsUseableSwiProlog(SwiHomeDir))
            {
                Environment.SetEnvironmentVariable("SWI_HOME_DIR", SwiHomeDir);
            }
            String path = Environment.GetEnvironmentVariable("PATH");
            if (path != null)
                if (!path.ToLower().StartsWith(SwiHomeDir.ToLower()))
                {
                    Environment.SetEnvironmentVariable("PATH", SwiHomeDir + "/bin;" + IKVMHome + ";" + path);
                }
            libpath = "";


            string swiHomeBin = SwiHomeDir + "/bin";
            libpath += swiHomeBin;
            if (swiHomeBin != IKVMHome)
            {
                libpath += ";";
                libpath += IKVMHome;
            }
            libpath += ";";
            libpath += ".";

            string LD_LIBRARY_PATH = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
            if (String.IsNullOrEmpty(LD_LIBRARY_PATH))
            {
                LD_LIBRARY_PATH = libpath;
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", LD_LIBRARY_PATH);
            }
           // SafelyRun(SetupProlog1);
           // SafelyRun(SetupProlog2);
        }
#if USE_IKVM
        public static void SetupProlog1()
        {
            PrologClient.ConsoleTrace("geting lib path");
            CLASSPATH = java.lang.System.getProperty("java.class.path");
            ConsoleTrace("GOT lib path");
            string CLASSPATH0 = Environment.GetEnvironmentVariable("CLASSPATH");

            if (String.IsNullOrEmpty(CLASSPATH))
            {
                CLASSPATH = CLASSPATH0;
            }
            string jplcp = clasPathOf(new jpl.JPL());

            if (!JplDisabled)
                CLASSPATH = IKVMHome + "/SWIJPL.dll" + ";" + IKVMHome + "/SWIJPL.jar;" + CLASSPATH0;

            ConsoleWriteLine("CLASSPATH=" + CLASSPATH);
            if (CLASSPATH != null)
            {
                Environment.SetEnvironmentVariable("CLASSPATH", CLASSPATH);
                java.lang.System.setProperty("java.class.path", CLASSPATH);
            }
            java.lang.System.setProperty("java.library.path", libpath);
        }
#endif
        static string CLASSPATH = null;
        static string libpath = null;
        public static void SetupProlog2()
        {
            try
            {
#if USE_IKVM
                if (!JplDisabled)
                {
                    JPL.setNativeLibraryDir(SwiHomeDir + "/bin");
                    try
                    {
                        JPL.loadNativeLibrary();
                    }
                    catch (Exception e)
                    {
                        WriteException(e);
                        JplDisabled = true;
                    }
                    if (!JplDisabled)
                    {
                        SafelyRun(() => jpl.fli.Prolog.initialise());
                    }
                }
                SafelyRun(TestClassLoader);
#endif
                //if (IsPLWin) return;
                try
                {
                    if (!PlEngine.IsInitialized)
                    {
                        String[] param = { "-q" }; // suppressing informational and banner messages
                        PlEngine.Initialize(param);
                    }
                    if (IsPLWin) return;
                    if (!PlEngine.IsStreamFunctionReadModified) PlEngine.SetStreamReader(Sread);
                    PlQuery.PlCall("nl.");
                }
                catch (Exception e)
                {
                    WriteException(e);
                    PlCsDisabled = true;
                }
                //                PlAssert("jpl:jvm_ready");
                //                PlAssert("module_transparent(jvm_ready)");
            }
            catch (Exception exception)
            {
                WriteException(exception);
                return;
            }
        }

        private static bool IsUseableSwiProlog(string swiHomeDir)
        {
            if (string.IsNullOrEmpty(swiHomeDir)) return false;
            if (!Directory.Exists(swiHomeDir)) return false;
            if (File.Exists(swiHomeDir + "/bin/libpl.dll"))
            {
                ConsoleTrace("SWI too old: " + swiHomeDir + "/bin/libpl.dll");
                return false;
            }
            if (File.Exists(swiHomeDir + "/bin/swipl.dll")) return true;
            if (!ConfirmRCFile(swiHomeDir)) return false;
            return true;
        }

        private static bool ConfirmRCFile(string swiHomeDir)
        {
            if (!File.Exists(swiHomeDir + "/boot32.prc") &&
                !File.Exists(swiHomeDir + "/boot.prc") &&
                !File.Exists(swiHomeDir + "/boot64.prc"))
            {
                return false;
            }
            return true;
        }

        //FileInfo & DirectoryInfo are in System.IO
        //This is something you should be able to tweak to your specific needs.
        static void CopyFiles(string source,
                              string destination,
                              bool overwrite,
                              string searchPattern, bool recurse)
        {
            if (Directory.Exists(source))
                CopyFiles(new DirectoryInfo(source), new DirectoryInfo(destination), overwrite, searchPattern, recurse);
        }

        static void CopyFiles(DirectoryInfo source,
                              DirectoryInfo destination,
                              bool overwrite,
                              string searchPattern, bool recurse)
        {
            FileInfo[] files = source.GetFiles(searchPattern);
            if (!destination.Exists)
            {
                destination.Create();
            }
            foreach (FileInfo file in files)
            {
                string destName = Path.Combine(destination.FullName, file.Name);
                try
                {
                    file.CopyTo(destName, overwrite);
                }
                catch (Exception e0)
                {
                    if (!overwrite)
                    {
                        ConsoleWriteLine("file: " + file + " copy to " + destName + " " + e0);
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(destName))
                            {
                                if (File.Exists(destName + ".dead")) File.Delete(destName + ".dead");
                                File.Move(destName, destName + ".dead");
                                file.CopyTo(destName, false);
                            }
                        }
                        catch (Exception e)
                        {
                            ConsoleWriteLine("file: " + file + " copy to " + destName + " " + e);
                        }
                    }
                }
            }
            if (recurse)
            {
                foreach (var info in source.GetDirectories())
                {
                    string destName = Path.Combine(destination.FullName, info.Name);
                    try
                    {
                        if (!Directory.Exists(destName)) Directory.CreateDirectory(destName);
                        CopyFiles(info, new DirectoryInfo(destName), overwrite, searchPattern, recurse);
                    }
                    catch (Exception e)
                    {
                        ConsoleWriteLine("file: " + info + " copy to " + destName + " " + e);
                    }
                }
            }
        }
#if USE_IKVM

        private static void SetupIKVM()
        {
            if (String.IsNullOrEmpty(IKVMHome)) IKVMHome = Environment.GetEnvironmentVariable("IKVM_BINDIR");
            if (String.IsNullOrEmpty(IKVMHome)) IKVMHome = new FileInfo(typeof(ikvm.runtime.Util).Assembly.Location).DirectoryName;
            if (String.IsNullOrEmpty(IKVMHome)) IKVMHome = Environment.CurrentDirectory;
            Environment.SetEnvironmentVariable("IKVM_BINDIR", IKVMHome);
            DirectoryInfo destination = new DirectoryInfo(IKVMHome);
            DirectoryInfo source;
            if (Is64BitRuntime())
            {
                source = new DirectoryInfo(IKVMHome + "/bin-x64/");
            }
            else
            {
                source = new DirectoryInfo(IKVMHome + "/bin-x86/");
            }
            if (source.Exists) CopyFiles(source, destination, true, "*.*", false);
        }

#pragma warning disable 414, 3021
        [CLSCompliant(false)]
        public class ScriptingClassLoader : URLClassLoader
        {
            readonly IList<ClassLoader> lc = new List<ClassLoader>();
            IList<AppDomainAssemblyClassLoader> AppDomainAssemblyClassLoaders = new List<AppDomainAssemblyClassLoader>();
            IList<AssemblyClassLoader> AssemblyClassLoaders = new List<AssemblyClassLoader>();
            IList<ClassPathAssemblyClassLoader> ClassPathAssemblyClassLoaders = new List<ClassPathAssemblyClassLoader>();
            IList<URLClassLoader> URLClassLoaders = new List<URLClassLoader>();
            IList<MethodUtil> MethodUtils = new List<MethodUtil>();

            public ScriptingClassLoader(ClassLoader cl)
                : base(new URL[0], cl)
            {
                AddLoader(cl);
            }

            public void AddLoader(ClassLoader cl)
            {
                lock (lc)
                {
                    if (cl != null)
                    {
                        if (lc.Contains(cl)) return;
                        lc.Add(cl);
                        bool added = false;
                        if (cl is AppDomainAssemblyClassLoader)
                        {
                            AppDomainAssemblyClassLoaders.Add((AppDomainAssemblyClassLoader)cl);
                            added = true;
                        }
                        if (cl is AssemblyClassLoader)
                        {
                            AssemblyClassLoaders.Add((AssemblyClassLoader)cl);
                            added = true;
                        }
                        if (cl is ClassPathAssemblyClassLoader)
                        {
                            ClassPathAssemblyClassLoaders.Add((ClassPathAssemblyClassLoader)cl);
                            added = true;
                        }
                        if (!added)
                        {
                            if (cl is MethodUtil)
                            {
                                MethodUtils.Add((MethodUtil)cl);
                                added = true;
                            }
                            else
                                if (cl is URLClassLoader)
                                {
                                    URLClassLoaders.Add((URLClassLoader)cl);
                                    added = true;
                                }
                        }
                        AddLoader(cl.getParent());
                    }
                }
            }

            public static void Check()
            {

            }

            public string FindLibrary(string libname)
            {
                return base.findLibrary(libname);
            }
            public Class LoadClass(string name, bool resolve)
            {
                return base.loadClass(name, resolve);
            }
            public java.lang.Class ResolveClass(java.lang.Class clz)
            {
                base.resolveClass(clz);
                return clz;
            }
        }
#pragma warning restore 414, 3021

        private static void TestClassLoader()
        {
            //using java.lang;
            //IKVM.Internal.BootstrapClassLoader()
            ScriptingClassLoader cl = new ScriptingClassLoader(ClassLoader.getSystemClassLoader());

            string s = "jpl.fli.term_t";
            Class c;
            try
            {
                c = cl.loadClass(s);
            }
            catch (java.lang.ClassNotFoundException e)
            {
            }
            catch (java.security.PrivilegedActionException e)
            {

            }

            foreach (var s1 in new Type[] { 1.GetType(), true.GetType(), "".GetType(), typeof(void), 'a'.GetType(), typeof(Type[]), typeof(IComparable<Type>) })
            {
                c = ikvm.runtime.Util.getFriendlyClassFromType(s1);
                if (c != null)
                {
                    ConsoleTrace("class: " + c + " from type " + s1.FullName);
                    continue;
                }
                ConsoleTrace("cant get " + s1.FullName);
            }

            foreach (var s1 in new jpl.JPL().GetType().Assembly.GetTypes())
            {
                c = ikvm.runtime.Util.getFriendlyClassFromType(s1);
                if (c != null)
                {
                    //ConsoleTrace("" + c);
                    continue;
                }
                ConsoleTrace("cant get " + s1.FullName);
            }
            return;
        }

        private static string clasPathOf(jpl.JPL jpl1)
        {
            string s = null;
            var cl = jpl1.getClass().getClassLoader();
            if (cl != null)
            {
                var r = cl.getResource(".");
                if (r != null)
                {
                    s = r.getFile();
                }
                else
                {
                    var a = jpl1.GetType().Assembly;
                    if (a != null)
                    {
                        s = a.Location;
                    }
                }
            }
            return s;
        }
#endif
		
		        //[MTAThread]
        public static void Main0(string[] args0)
        {
            PingThreadFactories();
            bool demo = false;
            SetupProlog();
			libpl.PL_initialise(args0.Length, args0);
			libpl.PL_toplevel();
		}
		
        //[MTAThread]
        public static void Main(string[] args0)
        {
            PingThreadFactories();
            bool demo = false;
            SetupProlog();

            if (demo)
            {
                DoQuery("asserta(fff(1))");
                DoQuery("asserta(fff(9))");
                DoQuery("nl");
                DoQuery("flush");

                PlAssert("father(martin, inka)");
                if (!PlCsDisabled)
                {
                    PlQuery.PlCall("assert(father(uwe, gloria))");
                    PlQuery.PlCall("assert(father(uwe, melanie))");
                    PlQuery.PlCall("assert(father(uwe, ayala))");
                    using (PlQuery q = new PlQuery("father(P, C), atomic_list_concat([P,' is_father_of ',C], L)"))
                    {
                        foreach (PlTermV v in q.Solutions)
                            ConsoleTrace(ToCSString(v));

                        foreach (PlQueryVariables v in q.SolutionVariables)
                            ConsoleTrace(v["L"].ToString());


                        ConsoleTrace("all child's from uwe:");
                        q.Variables["P"].Unify("uwe");
                        foreach (PlQueryVariables v in q.SolutionVariables)
                            ConsoleTrace(v["C"].ToString());
                    }
                    //PlQuery.PlCall("ensure_loaded(library(thread_util))");
                    //Warning: [Thread 2] Thread running "thread_run_interactor" died on exception: thread_util:attach_console/0: Undefined procedure: thread_util:win_open_console/5
                    //PlQuery.PlCall("interactor");
                    //Delegate Foo0 = foo0;
                    RegisterPLCSForeigns();
                }

                PlAssert("tc2:-foo2(X,Y),writeq(f(X,Y)),nl,X=5");
                PlAssert("tc3:-foo3(X,Y,Z),Z,writeln(f(X,Y,Z)),X=5");
            }

#if USE_IKVM
            ClassFile.ThrowFormatErrors = false;
            libpl.NoToString = true;
            //SafelyRun((() => PlCall("jpl0")));            
            //SafelyRun((() => DoQuery(new Query(new jpl.Atom("jpl0")))));
            libpl.NoToString = false;
            ClassFile.ThrowFormatErrors = true;
#endif
            if (args0.Length > 0)
            {
                int i = 0;
                foreach (var s in args0)
                {
                    if (s == "-f")
                    {
                        string s1 = args0[i + 1];
                        args0[i + 1] = "['" + s1 + "']";
                        continue;
                    }
                    PlCall(s);
                    i++;
                }
            }
            if (!JplDisabled)
            {
#if USE_IKVM
                var run = new jpl.Atom("prolog");
                while (!IsHalted) SafelyRun(() => DoQuery(new jpl.Query(run)));
#endif
            }
            else
            {
                if (!PlCsDisabled)
                    // loops on exception
                    while (!SafelyRun(()=>libpl.PL_toplevel())) ;
            }



            ConsoleTrace("press enter to exit");
            Console.ReadLine();
            SafelyRun(()=>PlEngine.PlCleanup());

            ConsoleTrace("finshed!");


        }

        private static bool SafelyRun(MethodInvoker invoker)
        {
            try
            {
                invoker();
                return true;
            }
            catch (Exception e)
            {
                WriteException(e);
                return false;
            }
        }

        public static void RegisterPLCSForeigns()
        {
            CreatorThread = Thread.CurrentThread;
            RegisterMainThread();
            ShortNameType = new Dictionary<string, Type>();
            ShortNameType["string"] = typeof(String);
            ShortNameType["object"] = typeof(Object);
            ShortNameType["sbyte"] = typeof(sbyte);
            //ShortNameType = new PrologBackedDictionary<string, Type>(null, "shortTypeName");
            //PlEngine.RegisterForeign(null, "cliFindClass", 2, new DelegateParameter2(PrologCli.cliFindClass), PlForeignSwitches.None);
            PlEngine.RegisterForeign(ExportModule, "cli_load_assembly", 1, new DelegateParameter1(PrologClient.cliLoadAssembly), PlForeignSwitches.None);
            ConsoleWriteLine("RegisterPLCSForeigns");

            InternMethod(ExportModule, "load_assembly", typeof(PrologClient).GetMethod("LoadAssembly"));
            InternMethod(null, "cwl", typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            RegisterJPLForeigns();
            //RegisterThread(CreatorThread);
            //PLNULL = PlTerm.PlCompound("@", PlTerm.PlAtom("null"));
            //PLVOID = PlTerm.PlCompound("@", PlTerm.PlAtom("void"));
            //PLTRUE = PlTerm.PlCompound("@", PlTerm.PlAtom("true"));
            //PLFALSE = PlTerm.PlCompound("@", PlTerm.PlAtom("false"));
            ConsoleWriteLine("done RegisterPLCSForeigns");
        }


        private static void RegisterJPLForeigns()
        {
            // backup old jpl.pl and copy over it
            if (!JplDisabled)
                SafelyRun(() =>
                              {
                                  if (File.Exists(IKVMHome + "/jpl_for_ikvm.phps"))
                                  {
                                      if (!File.Exists(SwiHomeDir + "/library/jpl.pl.old"))
                                      {
                                          File.Copy(SwiHomeDir + "/library/jpl.pl",
                                                    SwiHomeDir + "/library/jpl.pl.old",
                                                    true);
                                      }
                                      File.Copy(IKVMHome + "/jpl_for_ikvm.phps", SwiHomeDir + "/library/jpl.pl", true);
                                  }
                              });

            PlEngine.RegisterForeign("swicli", "link_swiplcs", 1, new DelegateParameter1(link_swiplcs),
                                     PlForeignSwitches.None);
            //JplSafeNativeMethods.install();
            JplSafeNativeMethodsCalled = true;
            //DoQuery(new Query("ensure_loaded(library(jpl))."));
            /*
                             
             
jpl_jlist_demo :-
	jpl_new( 'javax.swing.JFrame', ['modules'], F),
	jpl_new( 'javax.swing.DefaultListModel', [], DLM),
	jpl_new( 'javax.swing.JList', [DLM], L),
	jpl_call( F, getContentPane, [], CP),
	jpl_call( CP, add, [L], _),
	(	current_module( M),
		jpl_call( DLM, addElement, [M], _),
		fail
	;	true
	),
	jpl_call( F, pack, [], _),
	jpl_call( F, getHeight, [], H),
	jpl_call( F, setSize, [150,H], _),
	jpl_call( F, setVisible, [@(true)], _).


% this directive runs the above demo

:- jpl_jlist_demo.

             */
            return; //we dont need to really do this
            PlCall("use_module(library(jpl)).");
            PlAssert("jpl0 :- jpl_new( 'java.lang.String', ['hi'], DLM),writeln(DLM)");
            PlAssert("jpl1 :- jpl_new( 'javax.swing.DefaultListModel', [], DLM),writeln(DLM)");
        }

        public static bool PlCall(string s)
        {
            try
            {
                if (!JplDisabled)
                {
                    return DoQuery(s);
                }
                if (PlCsDisabled)
                {
                    WriteDebug("Disabled PlCall " + s);
                    return false;
                }
                return PlQuery.PlCall(s);
            }
            catch (Exception e)
            {
                WriteException(e);
                throw e;
            }
        }

        public static bool PlCall(string m, string f, PlTermV args)
        {
            try
            {
                if (!JplDisabled)
                {
#if USE_IKVM
                    return DoQuery(m, f, args);
#endif
                }
                if (PlCsDisabled)
                {
                    WriteDebug("Disabled PlCall " + f);
                    return false;
                }
                return PlQuery.PlCall(m, f, args);
            }
            catch (Exception e)
            {
                WriteException(e);
                throw e;
            }
        }

        private static bool link_swiplcs(PlTerm pathName)
        {
            try
            {
                return true;
                if (JplSafeNativeMethodsCalled)
                {
                    bool enabled = !JplSafeNativeMethodsDisabled;
                    SafelyRun(
                        () => ConsoleTrace("JplSafeNativeMethods called again from " + pathName + " result=" + enabled));
                    return enabled;
                }
                JplSafeNativeMethodsCalled = true;
                SafelyRun(() => ConsoleTrace("JplSafeNativeMethods call first time from " + pathName));
                JplSafeNativeMethods.install();
                //var v = new PlTerm("_1");
                //JplSafeNativeMethods.jpl_c_lib_version_1_plc(v.TermRef);
                return true;
            }
            catch (Exception e)
            {
                JplSafeNativeMethodsDisabled = true;
                WriteException(e);
                return false;
            }
        }

        private static bool DoQuery(string query)
        {
            if (JplDisabled) return PlCall(query);
#if USE_IKVM
            Query q;
            try
            {
                q = new Query(query);
            }
            catch (Exception e)
            {
                WriteException(e);
                return false;
            }
            return DoQuery(q);
#else
            return PlCall(query);
#endif
        }

#if USE_IKVM
        public static bool DoQuery(string m, string f, PlTermV args)
        {
            if (JplDisabled) return PlCall(m, f, args);
            Query q;
            try
            {
                q = new Query(InModule(m, new Compound(f, ToJPL(args))));
            }
            catch (Exception e)
            {
                WriteException(e);
                return false;
            }
            return DoQuery(q);
        }

        private static bool DoQuery(Query query)
        {
            try
            {
                bool any = false;
                //if (!query.isOpen()) query.open();
                while (query.hasMoreSolutions())
                {
                    any = true;
                    Hashtable ht = query.nextSolution();
                    foreach (var list in ToEnumer(ht.elements()))
                    {
                        string s = "" + list;
                        ConsoleTrace(s);
                    }
                }
                return any;
            }
            catch (Exception exception)
            {
                WriteException(exception);
                return false;
            }

        }
#endif
        public static void ConsoleWriteLine(string text)
        {
            Console.Error.WriteLine(text);
        }

        public static void WriteException(Exception exception)
        {
#if USE_IKVM
            java.lang.Exception ex = exception as java.lang.Exception;
            if (ex != null)
            {
                ex.printStackTrace();

            }
#endif
            //else
            {
                Exception inner = exception.InnerException;
                if (inner != null && inner != exception)
                {
                    WriteException(inner);
                }
                ConsoleWriteLine("ST: " + exception.StackTrace);
            }

            ConsoleWriteLine("PrologClient: " + exception);
        }
#if USE_IKVM
        private static IEnumerable ToEnumer(java.util.Enumeration enumeration)
        {
            List<object> list = new List<object>();
            while (enumeration.hasMoreElements())
            {
                list.Add(enumeration.nextElement());
            }
            return list;
        }
        private static IEnumerable ToEnumer(java.util.Iterator enumeration)
        {
            List<object> list = new List<object>();
            while (enumeration.hasNext())
            {
                list.Add(enumeration.next());
            }
            return list;
        }
#endif
        private static void FooMethod(String print)
        {
            //DoQuery(new Query("asserta(jpl:jvm_ready)."));
            //DoQuery(new Query("asserta(jpl:jpl_c_lib_version(3-3-3-3))."));

            //DoQuery(new Query("module(jpl)."));
            //JplSafeNativeMethods.install();
            //DoQuery("ensure_loaded(library(jpl)).");
            //DoQuery("module(user).");
            //DoQuery(new Query("load_foreign_library(foreign(jpl))."));
            // DoQuery(new Query(new jpl.Compound("member", new Term[] { new jpl.Integer(1), new jpl.Variable("H") })));
            //DoQuery(new Query(new jpl.Atom("interactor")));
            //DoQuery(new Query(new jpl.Compound("writeq", new Term[] { new jpl.Integer(1) })));

            ConsoleTrace(print);
        }

        static internal long Sread(IntPtr handle, System.IntPtr buffer, long buffersize)
        {
            int i = Console.Read();
            if (i == -1) return 0;
            string s = "" + (char)i;
            byte[] array = System.Text.Encoding.Unicode.GetBytes(s);
            System.Runtime.InteropServices.Marshal.Copy(array, 0, buffer, array.Length);
            return array.Length;
        }


        //[TestMethod]
        public void StreamRead()
        {
            PlEngine.SetStreamReader(Sread);
            // NOTE: read/1 needs a dot ('.') at the end
            PlQuery.PlCall("assert( (test_read(A) :- read(A)) )");
            PlTerm t = PlQuery.PlCallQuery("test_read(A)");
            //     Assert.AreEqual(ref_string_read, t.ToString() + ".");
        }


        private static string ToCSString(PlTermV termV)
        {
            int s = termV.Size;

            //var a0= termV.A0;
            PlTerm v0 = termV[0];
            PlTerm v1 = termV[1];
            PlQuery.PlCall("write", new PlTermV(v0));
            PlQuery.PlCall("nl");
            PlQuery.PlCall("writeq", new PlTermV(v1));
            PlQuery.PlCall("nl");
            return "";
        }

        private static void PlAssert(string s)
        {
            if (PlCsDisabled)
            {
                WriteDebug("Disabled PlAssert " + s);
                return;
            }
            PlQuery.PlCall("assert((" + s + "))");
        }

        private static void WriteDebug(string s)
        {
            ConsoleTrace(s);
        }


        public static bool JplDisabled = true;
        public static bool PlCsDisabled = false;
        private static string _ikvmHome;
        public static string IKVMHome
        {
            get { return _ikvmHome; }
            set { _ikvmHome = RemoveTrailingPathSeps(value); }
        }

        private static string _swiHomeDir;// = Path.Combine(".", "swiprolog");
        public static string SwiHomeDir
        {
            get { return _swiHomeDir; }
            set
            {
                _swiHomeDir = RemoveTrailingPathSeps(value); ;
            }
        }

        private static string RemoveTrailingPathSeps(string value)
        {
            if (value != null)
            {
                value = value.TrimEnd("\\/".ToCharArray()).Replace("\\", "/");
            }
            return value;
        }


        public static string AltSwiHomeDir = "C:/development/opensim4opencog";// Path.Combine(".", "swiprolog");
        public static bool JplSafeNativeMethodsDisabled = false;
        public static bool JplSafeNativeMethodsCalled = false;
        public static bool IsHalted = false;
        private static Int64 TopOHandle = 6660000;
        private static readonly Dictionary<string, object> SavedDelegates = new Dictionary<string, object>();
        public static bool FailOnMissingInsteadOfError = true;
        public static Thread CreatorThread;
        public static bool IsPLWin;
        public static bool RedirectStreams = false;
        public static int VMStringsAsAtoms = libpl.CVT_STRING;

        public static bool IsLinux = Type.GetType("Mono.Runtime") != null;

        /* static public void WriteLine(string s, params object[] args)
         {
             Console.DebugWriteLine(s, args);
         }
         */

        public bool Consult(string filename)
        {
            // atomic quote the filename
            string replace = "'" + filename.Replace("\\", "/").Replace("'", "\\'") + "'";
            return PlCall("[" + replace + "]");
        }

        public void InitFromUser()
        {
            ConsultIfExists("prolog/cogbot.pl");
        }

        public bool ConsultIfExists(string file)
        {
            return PrologClient.InvokeFromC(() => File.Exists(file) && Consult(file), false);
        }
    }

    [System.Security.SuppressUnmanagedCodeSecurity]
    public static class JplSafeNativeMethods
    {
        //private const string DllFileName = @"D:\Lesta\swi-pl\pl\bin\LibPl.dll";
        private const string DllFileName = @"jpl.dll";//"libpl.dll" for 5.7.8; //was 

        public static string DllFileName1
        {
            get { return DllFileName; }
        }
        [DllImport(DllFileName)]
        public static extern void install();

        //[DllImport(DllFileName)]
        //public static extern java.lang.Thread jni_env();

        //[DllImport(DllFileName)]
        //public static extern int jpl_c_lib_version_1_plc(uint term_t);
    }

}