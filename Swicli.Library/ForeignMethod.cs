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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SbsSW.SwiPlCs;
using SbsSW.SwiPlCs.Callback;
using SbsSW.SwiPlCs.Exceptions;
using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = Swicli.Library.PrologClient;

namespace Swicli.Library
{
    /*
         
 5.6.1.1 Non-deterministic Foreign Predicates

By default foreign predicates are deterministic. Using the PL_FA_NONDETERMINISTIC attribute (see PL_register_foreign()) it is possible to register a predicate as a non-deterministic predicate. Writing non-deterministic foreign predicates is slightly more complicated as the foreign function needs context information for generating the next solution. Note that the same foreign function should be prepared to be simultaneously active in more than one goal. Suppose the natural_number_below_n/2 is a non-deterministic foreign predicate, backtracking over all natural numbers lower than the first argument. Now consider the following predicate:

quotient_below_n(Q, N) :- natural_number_below_n(N, N1), natural_number_below_n(N, N2), Q =:= N1 / N2, !.

In this predicate the function natural_number_below_n/2 simultaneously generates solutions for both its invocations.

Non-deterministic foreign functions should be prepared to handle three different calls from Prolog:

* Initial call (PL_FIRST_CALL)
Prolog has just created a frame for the foreign function and asks it to produce the first answer.
* Redo call (PL_REDO)
The previous invocation of the foreign function associated with the current goal indicated it was possible to backtrack. The foreign function should produce the next solution.
* Terminate call (PL_CUTTED)
The choice point left by the foreign function has been destroyed by a cut. The foreign function is given the opportunity to clean the environment. 

Both the context information and the type of call is provided by an argument of type control_t appended to the argument list for deterministic foreign functions. The macro PL_foreign_control() extracts the type of call from the control argument. The foreign function can pass a context handle using the PL_retry*() macros and extract the handle from the extra argument using the PL_foreign_context*() macro.

void PL_retry(long)
The foreign function succeeds while leaving a choice point. On backtracking over this goal the foreign function will be called again, but the control argument now indicates it is a `Redo' call and the macro PL_foreign_context() will return the handle passed via PL_retry(). This handle is a 30 bits signed value (two bits are used for status indication).

void PL_retry_address(void *)
As PL_retry(), but ensures an address as returned by malloc() is correctly recovered by PL_foreign_context_address().

int PL_foreign_control(control_t)
Extracts the type of call from the control argument. The return values are described above. Note that the function should be prepared to handle the PL_CUTTED case and should be aware that the other arguments are not valid in this case.

long PL_foreign_context(control_t)
Extracts the context from the context argument. In the call type is PL_FIRST_CALL the context value is 0L. Otherwise it is the value returned by the last PL_retry() associated with this goal (both if the call type is PL_REDO as PL_CUTTED).

void * PL_foreign_context_address(control_t)
Extracts an address as passed in by PL_retry_address(). 

Note: If a non-deterministic foreign function returns using PL_succeed or PL_fail, Prolog assumes the foreign function has cleaned its environment. No call with control argument PL_CUTTED will follow.

The code of figure 6 shows a skeleton for a non-deterministic foreign predicate definition.

typedef struct // define a context structure  { ... } context; 
 foreign_t my_function(term_t a0, term_t a1, foreign_t handle) { struct context * ctxt; switch( PL_foreign_control(handle) ) { case PL_FIRST_CALL: ctxt = malloc(sizeof(struct context)); ... PL_retry_address(ctxt); case PL_REDO: ctxt = PL_foreign_context_address(handle); ... PL_retry_address(ctxt); case PL_CUTTED: free(ctxt); PL_succeed; } } 
         
 */
    public delegate object AnyMethod(params object[] any);

    public partial class PrologClient
    {

        private static void AddForeignMethods(Type t)
        {
            foreach (var m in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                object[] f = m.GetCustomAttributes(typeof(PrologVisible), false);
                if (f != null && f.Length > 0)
                {
                    PrologVisible f1 = (PrologVisible)f[0];
                    f1.Method = m;
                    try
                    {
                        LoadMethod(m, f1);
                    }
                    catch (Exception e)
                    {
                        Error(m + " caused " + e);
                    }
                }
            }
        }
        public static void LoadMethod(MethodInfo m, PrologVisible pm)
        {
            if (pm.Name == null)
            {
                if (char.IsLower(m.Name[0]))
                {
                    string mName = m.Name;
                    if (ForceJanCase)
                    {
                        pm.Name = ToPrologCase(mName);
                    }
                    else
                    {
                        pm.Name = mName;
                    }
                }
                else
                {
                    string mName = m.Name;
                    pm.Name = ToPrologCase(mName);
                }
            }
            else
            {
                if (ForceJanCase) pm.Name = ToPrologCase(pm.Name);
            }
            if (pm.DelegateType != null)
            {
                PlEngine.RegisterForeign(pm.ModuleName, pm.Name, pm.Arity, pm.Delegate, pm.ForeignSwitches);
                return;
            }
            InternMethod(pm.ModuleName, pm.Name, m);
        }
        public static void InternMethod(string module, string pn, AnyMethod d)
        {
            if (!PlEngine.PinDelegate(module, pn.ToString(), -1, d)) return;
            InternMethod(module, pn, d.Method);
        }
        public static void InternMethod(string module, string pn, MethodInfo list)
        {
            InternMethod(module, pn, list, null);
        }
        public static bool ForceJanCase = true;
        public static void InternMethod(string module, string pn, MethodInfo list, object defaultInstanceWhenMissing)
        {
            if (list == null)
            {
                return;
            }
            Type type = list.DeclaringType;
            pn = pn ?? (type.Name + "." + list.Name);
            if (ForceJanCase)
            {
                var pn2 = ToPrologCase(pn);
                if (pn2 != pn)
                {
                    pn = pn2;
                }
            }
            ParameterInfo[] ps = list.GetParameters();
            Type rt = list.ReturnType;
            int paramlen = ps.Length;
            bool nonvoid = rt != typeof(void);
            bool isbool = rt == typeof(bool);
            bool hasReturnValue = nonvoid && !isbool;
            bool isStatic = list.IsStatic;
            bool isVanilla = true;
            int maxOptionals = 0;
            foreach (ParameterInfo info in ps)
            {
                if (info.ParameterType != typeof(PlTerm))
                {
                    isVanilla = false;
                }
                if (IsOptionalParam(info))
                {
                    isVanilla = false;
                    maxOptionals++;
                }
            }
            if (isbool && isStatic)
            {
                if (isVanilla)
                {
                    RegisterInfo(pn, paramlen, list);
                    Delegate d = null;
                    switch (paramlen)
                    {
                        case 0:
                            {
                                d = new DelegateParameter0(() => (bool)InvokeCaught(list, null, ZERO_OBJECTS));
                                PlEngine.RegisterForeign(module, pn, paramlen, d, PlForeignSwitches.None);
                                return;
                            }
                        case 1:
                            PlEngine.RegisterForeign(module, pn, paramlen,
                                                     new DelegateParameter1(
                                                         (p1) => (bool)InvokeCaught(list, null, new object[] { p1 })),
                                                     PlForeignSwitches.None);
                            return;
                        case 2:
                            PlEngine.RegisterForeign(module, pn, paramlen,
                                                     new DelegateParameter2(
                                                         (p1, p2) =>
                                                         (bool)InvokeCaught(list, null, new object[] { p1, p2 })),
                                                     PlForeignSwitches.None);
                            return;
                        case 3:
                            PlEngine.RegisterForeign(module, pn, paramlen,
                                                     new DelegateParameter3(
                                                         (p1, p2, p3) =>
                                                         (bool)InvokeCaught(list, null, new object[] { p1, p2, p3 })),
                                                     PlForeignSwitches.None);
                            return;
                        case 4:
                            PlEngine.RegisterForeign(module, pn, paramlen,
                                                     new DelegateParameter4(
                                                         (p1, p2, p3, p4) =>
                                                         (bool)InvokeCaught(list, null, new object[] { p1, p2, p3, p4 })),
                                                     PlForeignSwitches.None);
                            return;
                        case -5: // use the default please
                            PlEngine.RegisterForeign(module, pn, paramlen,
                                                     new DelegateParameter5(
                                                         (p1, p2, p3, p4, p5) =>
                                                         (bool)InvokeCaught(list, null, new object[] { p1, p2, p3, p4, p5 })),
                                                     PlForeignSwitches.None);
                            return;
                        default:
                            break;
                    }
                }
            }
            int plarity = paramlen + (hasReturnValue ? 1 : 0) + (isStatic ? 0 : 1);

            RegisterInfo(pn, plarity, list);
            DelegateParameterVarArgs del = GetDelV(list, type, nonvoid, isbool, isStatic, plarity, defaultInstanceWhenMissing);
            PlEngine.RegisterForeign(module, pn, plarity, del, PlForeignSwitches.VarArgs);
            while (maxOptionals > 0)
            {
                RegisterInfo(pn, plarity - maxOptionals, list); 
                del = GetDelV(list, type, nonvoid, isbool, isStatic, plarity - maxOptionals, defaultInstanceWhenMissing);
                PlEngine.RegisterForeign(module, pn, plarity - maxOptionals, del, PlForeignSwitches.VarArgs);
                maxOptionals--;
            }
        }

        public static Dictionary<string, MethodInfo> AutoDocInfos = new Dictionary<string, MethodInfo>();
        public static void RegisterInfo(string pn, int paramlen, MethodInfo info)
        {
            string key = pn + "/" + paramlen;
            MethodInfo minfo;
            lock (AutoDocInfos)
            {
                if (!AutoDocInfos.TryGetValue(key, out minfo))
                {
                    AutoDocInfos[key] = info;
                }
            }
        }

        public static string ToPrologCase(string pn)
        {
            bool cameCased = false;
            foreach (char c in pn)
            {
                if (Char.IsUpper(c) || c == '.' || c == '-')
                {
                    cameCased = true;
                    break;
                }
            }
            if (!cameCased) return pn;
            StringBuilder newname = new StringBuilder();
            bool lastCapped = true;
            bool lastUnderscored = true;
            foreach (char c in pn)
            {

                if (Char.IsUpper(c))
                {
                    if (lastCapped)
                    {
                        newname.Append(CharToLower(c));
                    }
                    else
                    {
                        if (!lastUnderscored) newname.Append('_');
                        newname.Append(CharToLower(c));
                        lastCapped = true;
                    }
                    lastUnderscored = false;
                }
                else
                {
                    if (c == '_' || c == '-')
                    {
                        lastCapped = false;
                        if (lastUnderscored) continue;
                        newname.Append('_');
                        lastUnderscored = true;
                        continue;
                    }
                    newname.Append(CharToLower(c));
                    lastCapped = false;
                    lastUnderscored = false;
                }
            }
            return newname.ToString();
        }

        private static char CharToLower(char c)
        {
            if (c == '-') return '_';
            return Char.ToLower(c);

        }

        private static DelegateParameterVarArgs GetDelV(MethodInfo list, Type type, bool nonvoid, bool isbool, bool isStatic, int plarity, object defaultInstanceWhenMissing)
        {
            DelegateParameterVarArgs d;
            d = (PlTermV termVector) =>
            {
                if (termVector.Size != plarity)
                {
                    //return false;
                    termVector.Resize(plarity);
                }
                object target = isStatic ? null : CastTerm(termVector[0], type) ?? defaultInstanceWhenMissing;
                Action postCallHook;
                int tvargnum = isStatic ? 0 : 1;
                object[] newVariable = PlListToCastedArray(tvargnum, termVector, list.GetParameters(),
                                                           out postCallHook);
                object result = InvokeCaught(list, target, newVariable, postCallHook);

                if (isbool)
                {
                    return (bool)result;
                }
                if (nonvoid)
                {
                    return termVector[plarity - 1].FromObject(result);
                }
                return true;

            };
            return d;
        }

        private static object InvokeCaught(MethodInfo info, object o, object[] os)
        {
            return InvokeCaught(info, o, os, Do_NOTHING);
        }
        private static object InvokeCaught(MethodInfo info, object o, object[] os, Action todo)
        {
            return InvokeCaught0(info, o, os, todo);
        }
        private static object InvokeCaught0(MethodInfo info, object o, object[] os, Action todo)
        {
			if (!ClientReady) return null;
            Thread threadCurrentThread = Thread.CurrentThread;
            bool add1FrameCount = false;
            bool openFFI = false;
            uint fid = 0;
            if (add1FrameCount)
            {
                int fidCount = IncrementUseCount(threadCurrentThread, ForiegnFrameCounts);
                if (SaneThreadWorld) if (fidCount == 1) fid = libpl.PL_open_foreign_frame();
            } else
            {
                if (openFFI) fid = libpl.PL_open_foreign_frame();
            }
            try
            {
                if (info.IsGenericMethodDefinition)
                {
                    Type[] paramTypes = GetObjectTypes(info.GetParameters());
                    Type[] t = GetObjectTypes(os, paramTypes);
                    info = info.MakeGenericMethod(t);
                }
                var ps = info.GetParameters();
                int psLengthM1 = ps.Length - 1;
                bool isVarArg = (info.CallingConvention & CallingConventions.VarArgs) != 0;
                if (isVarArg)
                {
                    int usedUp = 0;
                    object[] ao = new object[psLengthM1 + 1];
                    for (int i = 0; i < psLengthM1; i++)
                    {
                        ao[i] = RecastObject(ps[i].ParameterType, os[i], null);
                        usedUp++;
                    }
                    int slack = os.Length - usedUp;
                    object[] lastArray = new object[slack];
                    int fillAt = 0;
                    while (slack-- > 0)
                    {
                        lastArray[fillAt++] = os[usedUp++];
                    }
                    ao[psLengthM1] = lastArray;
                    os = ao;
                }
                if (ps.Length != os.Length)
                {
                    Warn("ArgCount mismatch " + info + ": call count=" + os.Length);
                }
                object to = o;
                if (!info.DeclaringType.IsInstanceOfType(o)) to = null;
                object ret = info.Invoke(to, os);
                if (todo != null) todo();
                if (ret == null)
                {
                    //return VoidOrNull(info);
                }
                return ret;
            }
            catch (Exception ex)
            {
                var pe = ToPlException(ex);
                var ie = InnerMostException(ex);
                string s = ie.ToString() + "\n" + ie.StackTrace;
                Error("ex: {0}", s);
                //throw pe;
                return false;// pe;
            } finally
            {
                if (add1FrameCount) DecrementUseCount(threadCurrentThread, ForiegnFrameCounts);
                if (fid > 0) libpl.PL_close_foreign_frame(fid);
            }
        }
        private static Type[] GetObjectTypes(ParameterInfo[] parameterInfos)
        {
            int parameterInfosLength = parameterInfos.Length;
            Type[] t = new Type[parameterInfosLength];
            for (int i = 0; i < parameterInfosLength; i++)
            {
                t[i] = parameterInfos[i].ParameterType;
            }
            return t;
        }
        private static Type[] GetObjectTypes(object[] objects, Type[] otherwise)
        {
            Type[] t = new Type[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj != null) t[i] = objects[i].GetType();
                else t[i] = otherwise[i];
            }
            return t;
        }

        private static Exception InnerMostException(Exception ex)
        {
            if (ex is ReflectionTypeLoadException)
            {
                var ile = ((ReflectionTypeLoadException) ex).LoaderExceptions;
                if (ile.Length == 1) return InnerMostException(ile[0]);
            }
            var ie = ex.InnerException;
            if (ie != null && ie != ex)
            {
                return InnerMostException(ie);
            }
            return ex;
        }
        private static PlException ToPlException(Exception ex)
        {
            if (ex is PlException) return (PlException)ex;
            var ie = InnerMostException(ex);
            if (ie != null && ie != ex)
            {
                return ToPlException(ie);
            }
            return new PlException(ex.GetType() + ": " + ex.Message, ex);
        }

        private static Type GetArityType(int paramlen)
        {
            switch (paramlen)
            {
                case 0:
                    return typeof(DelegateParameter0);
                case 1:
                    return typeof(DelegateParameter1);
                case 2:
                    return typeof(DelegateParameter2);
                case 3:
                    return typeof(DelegateParameter3);
                case 4:
                    return typeof(DelegateParameter3);
                default:
                    return null;
            }
        }

        [PrologVisible(ModuleName = ExportModule)]
        private static bool testOut(int incoming, out int outbound)
        {
            outbound = incoming;
            return true;
        }
        [PrologVisible(ModuleName = ExportModule)]
        private static bool testOpt(int incoming, string optionalstr, out int outbound)
        {
            outbound = incoming;
            return true;
        }
        [PrologVisible(ModuleName = ExportModule)]
        private static bool testRef(int incoming, ref string optionalstr, out int outbound)
        {
            outbound = incoming;
            optionalstr = "" + incoming;
            return true;
        }
        [PrologVisible(ModuleName = ExportModule)]
        private static bool testVarArg(out int outbound, params int[] incoming)
        {
            outbound = 0;
            foreach (int i in incoming)
            {
                outbound += i;
            }
            return true;
        }

        public class PinnedObject<T> : IDisposable where T : struct
        {
            public T managedObject;
            protected GCHandle handle;
            protected IntPtr ptr;
            protected bool disposed;

            public T ManangedObject
            {
                get
                {
                    return (T)handle.Target;
                }
                set
                {
                    managedObject = value;
                    Marshal.StructureToPtr(value, ptr, false);
                }
            }

            public IntPtr Pointer
            {
                get { return ptr; }
            }

            public PinnedObject()
            {
                handle = GCHandle.Alloc(managedObject, GCHandleType.Pinned);
                ptr = handle.AddrOfPinnedObject();
            }

            ~PinnedObject()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    handle.Free();
                    ptr = IntPtr.Zero;
                    disposed = true;
                }
            }

            public void Recopy()
            {
                handle = GCHandle.Alloc(managedObject, GCHandleType.Pinned);
                ptr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(managedObject, ptr, false);
            }
        }


        public static T InvokeFromC<T>(Func<T> action, bool discard)
        {
            Thread threadCurrentThread = Thread.CurrentThread;
            int fidCount = IncrementUseCount(threadCurrentThread, ForiegnFrameCounts);
            //lock (SafeThreads)
            {
                try
                {
                    return InvokeFromC0(action, discard, fidCount == 1);
                }
                catch (AccessViolationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                } finally
                {
                    DecrementUseCount(threadCurrentThread, ForiegnFrameCounts);
                }
            } 
        }
        public static T InvokeFromC0<T>(Func<T> action, bool discard, bool useFrame)
        {
            discard = true;
            useFrame = true;
            Thread threadCurrentThread = Thread.CurrentThread;
            RegisterThread(threadCurrentThread);
            uint fid = 0;
            if (useFrame) fid = libpl.PL_open_foreign_frame();

            try
            {
                return action();
            }
            catch (AccessViolationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (discard && useFrame)
                {
                    libpl.PL_close_foreign_frame(fid);
                }
                DeregisterThread(threadCurrentThread);
            }
        }

        private static string PredicateName(CycFort term)
        {
            if (term.Name == "{}")
            {
                if (term.Arity == 1)
                {
                    return PredicateName(term.Arg(0));
                }
            }
            if (term.Name == ":")
            {
                if (term.Arity == 2)
                {
                    return PredicateName(term.Arg(1));
                }
            }
            if (term.Name == "/")
            {
                if (term.Arity == 2)
                {
                    return PredicateName(term.Arg(0));
                }
            }
            return term.Name;
        }

        private static string PredicateModule(CycFort term)
        {
            if (term.Name == "{}")
            {
                if (term.Arity == 1)
                {
                    return PredicateModule(term.Arg(0));
                }
            }
            if (term.Name == ":")
            {
                if (term.Arity == 2)
                {
                    return PredicateName(term.Arg(0));
                }
            }
            return "user";
        }

        private static int PredicateArity(CycFort term)
        {
            if (term.Name == "{}")
            {
                if (term.Arity == 1)
                {
                    return PredicateArity(term.Arg(0));
                }
            } 
            if (term.Name == ":")
            {
                if (term.Arity == 2)
                {
                    return PredicateArity(term.Arg(1));
                }
            }
            if (term.Name == "/")
            {
                if (term.Arity == 2)
                {
                    return term.Arg(1).intValue();
                }
            }
            return term.Arity;
        }
    }

    public delegate int NonDetDelegate(PlTerm term, PlTerm term2);
}