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
#if USE_MUSHDLR
using MushDLR223.Utilities;
#endif
#if USE_IKVM
using Class = java.lang.Class;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SbsSW.SwiPlCs;
using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = Swicli.Library.PrologClient;

namespace Swicli.Library
{
    public partial class PrologClient
    {
        protected string ClientPrefix { get; set; }
        private string _clientModule = null;
        protected string ClientModule
        {
            get { return _clientModule; }
            set { if (value != "user") _clientModule = value; }
        }

        private static PrologClient _singleInstance;
        public static PrologClient SingleInstance
        {
            get
            {
                if (_singleInstance == null) _singleInstance = new PrologClient();
                return _singleInstance;
            }
        }

        public PrologClient()
        {
            _singleInstance = this;
            ClientModule = null;
            ClientPrefix = "cli_";
            SetupProlog();
        }

        public readonly static Type[] ZERO_TYPES = new Type[0];

        public readonly static Object[] ZERO_OBJECTS = new Object[0];

        public static readonly Type[] ONE_STRING = new[] {typeof (string)};

        public static BindingFlags BindingFlagsJustStatic = BindingFlags.Public | BindingFlags.NonPublic |
                                                            BindingFlags.Static | BindingFlags.FlattenHierarchy;
        public static BindingFlags BindingFlagsInstance = BindingFlags.Public | BindingFlags.NonPublic |
                                                            BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        public static BindingFlags BindingFlagsALL = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                     BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn 
                                                     | BindingFlags.FlattenHierarchy;
        public static BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;


        const string ExportModule = "swicli";

        public static bool Warn(string text, params object[] ps)
        {
            text = PlStringFormat(text, ps);
            return libpl.PL_warning(text) != 0;
        }
        public static bool Error(string text, params object[] ps)
        {
            text = PlStringFormat(text, ps);
            return libpl.PL_warning(text) != 0;
        }
        private static bool WarnMissing(string text, params object[] ps)
        {
            text = PlStringFormat(text, ps);
            if (true)
            {
                Debug(text);
                return false;
            }
            return Warn(text);
        }
        public static void Debug(string text, params object[] ps)
        {
            text = PlStringFormat(text, ps);
            text.ToString();
        }

        [PrologVisible]
        public static bool cliThrow(PlTerm ex)
        {
            throw (Exception) CastTerm(ex, typeof (Exception));
        }
        [PrologVisible]
        public static bool cliBreak(PlTerm ex)
        {
            return WarnMissing(ToString(ex)) || true;
        }
        private void Trace()
        {
            //throw new NotImplementedException();
        }

        private object ToFort(object o)
        {
            return ToProlog(o);
        }

        public static int PlSucceedOrFail(bool p)
        {
            return p ? libpl.PL_succeed : libpl.PL_fail;
        }

        private static string PlStringFormat(string text, params object[] ps)
        {
            RegisterCurrentThread();
            try
            {
                if (ps != null && ps.Length > 0)
                {
                    for (int i = 0; i < ps.Length; i++)
                    {
                        var o = ps[i];
                        if (o==null)
                        {
                            ps[i] = "NULL";
                        }
                        else if (o is Exception)
                        {
                            ps[i] = PrologClient.ExceptionString((Exception)o);
                        }
                    }
                    text = String.Format(text, ps);
                }
            }
            catch (Exception)            
            {
            }
            DeregisterThread(Thread.CurrentThread);
            return text;
        }

        private static string ToString(object o)
        {
            try
            {
                return ToString0(o);
            }
            catch (Exception)
            {
                return "" + o;
            }
        }
        private static string ToString0(object o)
        {
            if (o == null) return "null";
            if (o is IConvertible) return o.ToString();
            if (o is System.Collections.IEnumerable)
            {
                var oc = (System.Collections.IEnumerable)o;
                int count = 0;
                string ret = "[";
                foreach (var o1 in (System.Collections.IEnumerable)o)
                {
                    if (count > 1) ret += ",";
                    count++;
                    ret += ToString0(o1);
                }
                return ret + "]";
            }
            return o.ToString();
        }
        /// <summary>
        /// 1 ?- cliToString(-1,X).
        /// X = "4294967295".
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        [PrologVisible]
        public static bool cliToStrRaw(PlTerm obj, PlTerm str)
        {
            try
            {
                if (!str.IsVar)
                {
                    var plvar = PlTerm.PlVar();
                    return cliToStrRaw(obj, plvar) && SpecialUnify(str, plvar);
                }
                if (obj.IsString) return str.Unify(obj);
                if (obj.IsVar) return str.Unify((string)obj);
                object o = GetInstance(obj);
                if (o == null) return str.FromObject("" + obj);
                return str.FromObject(ToString(o));
            }
            catch (Exception e)
            {
                Warn("cliToString: {0}", e);
                object o = GetInstance(obj);
                if (o == null) return str.FromObject("" + obj);
                return str.FromObject(ToString(o));
            }
        }
        [IKVMBased]
        [PrologVisible]
        static public bool cliJavaToString(PlTerm paramIn, PlTerm valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliJavaToString(paramIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance = GetInstance(paramIn);
            if (getInstance == null) return valueOut.Unify(PlTerm.PlString("null"));
#if USE_IKVM
            object val = getInstance as java.lang.Object;
            if (val == null)
            {
                Class c = ikvm.runtime.Util.getClassFromObject(getInstance);
                string s = (string)c.getMethod("toString", new Class[0]).invoke(getInstance, ZERO_OBJECTS);
                return valueOut.Unify(PlTerm.PlString(s));
            }
            return valueOut.Unify(PlTerm.PlString(val.toString()));
#else
            object val = getInstance;
            return valueOut.Unify(PlTerm.PlString(val.ToString()));
#endif
        }

        protected static PlTerm ATOM_NIL
        {
            get { return PlTerm.PlAtom("[]"); }
        }

        public static PlTerm PLNULL { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("null")); } }
        public static PlTerm PLVOID { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("void")); } }
        public static PlTerm PLTRUE { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("true")); } }
        public static PlTerm PLFALSE { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("false")); } }

        private static MemberInfo findMember(PlTerm memberSpec, Type c)
        {
            if (IsTaggedObject(memberSpec))
            {
                var r = GetInstance(memberSpec) as MemberInfo;
                if (r != null) return r;
            }
            Type[] paramz = GetParamSpec(memberSpec) ?? ZERO_TYPES;
            return findField(memberSpec, c) ??
                   (MemberInfo)
                   findPropertyInfo(memberSpec, c, true, true, ref paramz) ??
                   (MemberInfo) findMethodInfo(memberSpec, -1, c, ref paramz) ??
                   findPropertyInfo(memberSpec, c, false, false, ref paramz);
            //findConstructor(memberSpec, c));
        }

        private static FieldInfo findField(PlTerm memberSpec, Type c)
        {
            if (c == null)
            {
                Error("findField no class for {0}", memberSpec);
                return null;
            }
            if (memberSpec.IsVar)
            {
                Error("findField IsVar {0} on type {1}", memberSpec, c);
                return null;
            }
            if (memberSpec.IsInteger)
            {
                int ordinal = memberSpec.intValue();
                var mis = c.GetFields(BindingFlagsALL);
                if (ordinal < 0 || ordinal >= mis.Length) return null;
                return mis[ordinal];
            }
            if (IsTaggedObject(memberSpec))
            {
                var r = tag_to_object(memberSpec[1].Name) as FieldInfo;
                if (r != null) return r;
            }
            if (memberSpec.IsCompound)
            {
                if (memberSpec.Name != "f")
                {
                    return null;
                }
                return findField(memberSpec.Arg(0), c);
            }
            string fn = memberSpec.Name;
            if (fn == "[]") fn = "Get";
            FieldInfo fi = c.GetField(fn, BindingFlagsALL);
            return fi;
        }


        private static PropertyInfo findPropertyInfo(PlTerm memberSpec, Type c, bool mustHaveP, bool assumeParamTypes, ref Type[] paramz)
        {
            if (c == null)
            {
                Error("findProperty no class for {0}", memberSpec);
                return null;
            }
            if (memberSpec.IsVar)
            {
                Error("findProperty IsVar {0} on type {1}", memberSpec, c);
                return null;
            }
            if (memberSpec.IsInteger)
            {
                int ordinal = memberSpec.intValue();
                var mis = c.GetProperties(BindingFlagsALL);
                if (ordinal < 0 || ordinal >= mis.Length) return null;
                return mis[ordinal];
            }
            if (IsTaggedObject(memberSpec))
            {
                var r = tag_to_object(memberSpec[1].Name) as PropertyInfo;
                if (r != null) return r;
            }
            if (memberSpec.IsCompound)
            {
                if (memberSpec.Name == "p")
                {
                    Type[] paramzN = null;
                    return findPropertyInfo(memberSpec.Arg(0), c, false, assumeParamTypes, ref paramzN);
                }
                if (mustHaveP) return null;
            }
            if (paramz == null)
            {
                Warn("using paramSpec {0}", ToString(memberSpec));
                paramz = GetParamSpec(memberSpec) ?? ZERO_TYPES;
            }
            string fn = memberSpec.Name;
            if (fn == "[]") fn = "Item";
            if (paramz == null || paramz.Length == 0)
                return c.GetProperty(fn, BindingFlagsALL) ?? c.GetProperty("Is" + fn, BindingFlagsALL);
            var ps = c.GetProperties(BindingFlagsALL);
            int len = paramz.Length;
            PropertyInfo nameMatched = null;
            foreach (PropertyInfo info in ps)
            {
                if (info.Name.ToLower() == fn.ToLower())
                {
                    nameMatched = nameMatched ?? info;
                    ParameterInfo[] indexParameters = info.GetIndexParameters();
                    if (assumeParamTypes)
                    {
                        if (len == indexParameters.Length)
                        {
                            if (IsCompatTypes(paramz, GetObjectTypes(indexParameters)))
                            {
                                return info;
                            }
                            // incompat but ok
                            nameMatched = info;
                        }
                    }
                }
            }
            return c.GetProperty(fn, BindingFlagsALL) ?? c.GetProperty("Is" + fn, BindingFlagsALL) ?? nameMatched;
        }

        private static bool IsCompatTypes(Type[] supplied, Type[] required)
        {
            int len = supplied.Length;
            if (required.Length != len) return false;
            int considered = 0;
            foreach (Type type in required)
            {
                Type consider = supplied[considered];
                if (!IsCompatType(consider,type))
                {                    
                    return false;
                }
                considered++;               
            }
            return true;
        }

        private static bool IsCompatType(Type consider, Type type)
        {
            if (consider == null || type == null) return true;
            if (consider == typeof(object) || type == typeof(object)) return true;
            if (type.IsAssignableFrom(consider)) return true;
            if (typeof(IConvertible).IsAssignableFrom(type)
                && typeof(IConvertible).IsAssignableFrom(consider))
                return true;
            return false;
        }

        /// <summary>
        /// ?- cliNewArray(long,10,Out),cliToString(Out,Str).
        /// </summary>
        /// <param name="clazzSpec"></param>
        /// <param name="rank"></param>
        /// <param name="valueOut"></param>
        /// <returns></returns>
        [PrologVisible]
        static public bool cliNewArray(PlTerm clazzSpec, PlTerm rank, PlTerm valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliNewArray(clazzSpec, rank, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type c = GetType(clazzSpec);
            if (c == null)
            {
                Warn("Cant find type {0}", clazzSpec);
                return false;
            }
            var value = c.MakeArrayType(rank.intValue());
            return valueOut.FromObject((value));
        }

        [PrologVisible]
        static public bool cliLockEnter(PlTerm lockObj)
        {
            object getInstance = GetInstance(lockObj);
            Monitor.Enter(getInstance);
            return true;
        }
        [PrologVisible]
        static public bool cliLockExit(PlTerm lockObj)
        {
            object getInstance = GetInstance(lockObj);
            Monitor.Exit(getInstance);
            return true;
        }

        [PrologVisible]
        static public bool cliGetRaw(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm valueOut)
        {
            if (clazzOrInstance.IsVar)
            {
                return Error("Cant find instance {0}", clazzOrInstance);
            }
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetRaw(clazzOrInstance, memberSpec, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            if (getInstance == null && c == null)
            {
                Error("Cant find instance {0}", clazzOrInstance);
                return false;
            }
            bool found;
            object cliGet01 = cliGet0(getInstance, memberSpec, c, out found);
            if (!found) return false;
            return valueOut.FromObject(cliGet01);
        }
        static public object cliGet0(object getInstance, PlTerm memberSpec, Type c, out bool found)
        {
            Type[] paramz = null;
            FieldInfo fi = findField(memberSpec, c);
            if (fi != null)
            {
                object fiGetValue = fi.GetValue(fi.IsStatic ? null : getInstance);
                found = true;
                return (fiGetValue);
            }
            paramz = GetParamSpec(memberSpec) ?? ZERO_TYPES;
            var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz);
            if (pi != null)
            {
                var mi = pi.GetGetMethod();
                if (mi != null)
                {
                    found = true;
                    return ((InvokeCaught(mi, mi.IsStatic ? null : getInstance, ZERO_OBJECTS) ?? VoidOrNull(mi)));
                }
                WarnMissing("Cant find getter for property " + memberSpec + " on " + c + " for " + pi);
                found = false;
                return null;
            }
            else
            {
                if (memberSpec.IsVar)
                {
                    Warn("cliGet0 on IsVar={0} on {1} for {2}", memberSpec, c, getInstance);
                    found = false;
                    return getInstance;
                }
                string fn = memberSpec.Name;
                MethodInfo mi = findMethodInfo(memberSpec, -1, c, ref paramz) ??
                                GetMethod(c, fn, BindingFlagsALL) ??
                                GetMethod(c, "get_" + fn, BindingFlagsALL) ??
                                GetMethod(c, "Get" + fn, BindingFlagsALL) ??
                                GetMethod(c, "Is" + fn, BindingFlagsALL) ??
                                GetMethod(c, "To" + fn, BindingFlagsALL);
                if (mi == null)
                {
                    WarnMissing("Cant find getter " + memberSpec + " on " + c);
                    found = false;
                    return null;
                }
                Action postCallHook;
                object[] value = PlListToCastedArray(memberSpec, mi.GetParameters(), out postCallHook);
                object target = mi.IsStatic ? null : getInstance;
                object retval = InvokeCaught(mi, target, value, postCallHook) ?? VoidOrNull(mi);
                found = true;
                return retval;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clazzOrInstance"></param>
        /// <param name="memberSpec">[] = 'Item'</param>
        /// <param name="indexValues"></param>
        /// <param name="valueOut"></param>
        /// <returns></returns>
        [PrologVisible]
        static public bool cliGetProperty(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm indexValues, PlTerm valueOut)
        {
            if (clazzOrInstance.IsVar)
            {
                return Error("Cant find instance {0}", clazzOrInstance);
            }
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetProperty(clazzOrInstance, memberSpec, indexValues, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            if (getInstance == null && c == null)
            {
                Error("Cant find instance {0}", clazzOrInstance);
                return false;
            }
            Type[] paramz = null;
            var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz);
            if (pi == null)
            {
                Error("Cant find property {0} on {1}", memberSpec, c);
                return false;
            }
            Action postCallHook;
            var ps = PlListToCastedArray(indexValues, pi.GetIndexParameters(), out postCallHook);
            object cliGet01 = pi.GetValue(getInstance, ps);
            CommitPostCall(postCallHook);
            return valueOut.FromObject(cliGet01);
        }
        [PrologVisible]
        static public bool cliSetProperty(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm indexValues, PlTerm valueIn)
        {
            if (clazzOrInstance.IsVar)
            {
                return Error("Cant find instance {0}", clazzOrInstance);
            }
            if (!valueIn.IsVar)
            {
                return Error("Cant set property with a var {0}", valueIn);
            }
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            if (getInstance == null && c == null)
            {
                Error("Cant find instance {0}", clazzOrInstance);
                return false;
            }
            Type[] paramz = null;
            var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz);
            if (pi == null)
            {
                Error("Cant find property {0} on {1}", memberSpec, c);
                return false;
            }
            Action postCallHook;
            var ps = PlListToCastedArray(indexValues, pi.GetIndexParameters(), out postCallHook);
            pi.SetValue(getInstance, CastTerm(valueIn,pi.PropertyType), ps);
            CommitPostCall(postCallHook);
            return true;
        }

        private static MethodInfo GetMethod(Type type, string s, BindingFlags flags)
        {
            try
            {
                return type.GetMethod(s, flags);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }
            catch (MissingMethodException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [PrologVisible]
        static public bool cliSetRaw(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm paramIn)
        {
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            return cliSet0(getInstance, memberSpec, paramIn, c);
        }

        static public bool cliSet0(object getInstance, PlTerm memberSpec, PlTerm paramIn, Type c)
        {

            FieldInfo fi = findField(memberSpec, c);
            if (fi != null)
            {
                object value = CastTerm(paramIn, fi.FieldType);
                object target = fi.IsStatic ? null : getInstance;
                fi.SetValue(target, value);
                return true;
            }
            Type[] paramz = null;
            var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz);
            if (pi != null)
            {
                var mi = pi.GetSetMethod();
                if (mi != null)
                {
                    object value = CastTerm(paramIn, pi.PropertyType);
                    object target = mi.IsStatic ? null : getInstance;
                    InvokeCaught(mi, target, new[] { value });
                    return true;
                }
                return WarnMissing("Cant find setter for property " + memberSpec + " on " + c);
            }
            else
            {
                string fn = memberSpec.Name;
                MethodInfo mi = findMethodInfo(memberSpec, -1, c, ref paramz) ??
                                GetMethod(c, "set_" + fn, BindingFlagsALL) ??
                                GetMethod(c, "Set" + fn, BindingFlagsALL) ??
                                GetMethod(c, "from" + fn, BindingFlagsALL);
                if (mi == null)
                {
                    WarnMissing("Cant find setter " + memberSpec + " on " + c);
                    return false;
                }
                Action postCallHook;
                object[] value = PlListToCastedArray(paramIn, mi.GetParameters(), out postCallHook);
                object target = mi.IsStatic ? null : getInstance;
                object retval = InvokeCaught(mi, target, value, postCallHook);
                return true;// valueOut.FromObject(retval);
            }
            WarnMissing("Cant find setter " + memberSpec + " on " + c);
            return false;
        }

    }
}