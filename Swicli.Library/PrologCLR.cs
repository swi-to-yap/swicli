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
using PlTerm = SbsSW.SwiPlCs.PlTerm;

namespace Swicli.Library
{
    public partial class PrologCLR
    {
        protected string ClientPrefix { get; set; }
        private string _clientModule = null;
        protected string ClientModule
        {
            get { return _clientModule; }
            set { if (value != "user") _clientModule = value; }
        }

        private static PrologCLR _singleInstance;
        public static PrologCLR SingleInstance
        {
            get
            {
                if (_singleInstance == null) _singleInstance = new PrologCLR();
                return _singleInstance;
            }
        }

        public PrologCLR()
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
       
        public static BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.FlattenHierarchy;

        public static BindingFlags BindingFlagsALLNC = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                     BindingFlags.Instance | BindingFlags.IgnoreReturn
                                                     | BindingFlags.FlattenHierarchy;

        public static BindingFlags BindingFlagsALL3 = BindingFlags.InvokeMethod | BindingFlags.GetField |
                                                      BindingFlags.GetProperty | BindingFlags.SetField |
                                                      BindingFlags.SetProperty;
        public static BindingFlags ICASE = BindingFlags.IgnoreCase;

        private static readonly BindingFlags[] BindingFlags_SEARCHIS = new[]
                                                                {
                                                                    BindingFlagsALL3 | BindingFlagsInstance,
                                                                    BindingFlagsALL3 | BindingFlagsJustStatic,
                                                                    BindingFlagsALL3 | BindingFlagsInstance | ICASE,
                                                                    BindingFlagsALL3 | BindingFlagsJustStatic | ICASE,
                                                                };
        private static readonly BindingFlags[] BindingFlags_SEARCHS = new[]
                                                                {
                                                                    BindingFlagsALL3 | BindingFlagsJustStatic,
                                                                    BindingFlagsALL3 | BindingFlagsJustStatic | ICASE,
                                                                };

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
            Trace();
            return WarnMissing(ToString(ex)) || true;
        }
        private static void Trace()
        {
            //throw new NotImplementedException();
        }

        private static object ToFort(object o)
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
                            ps[i] = PrologCLR.ExceptionString((Exception)o);
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

        private static bool CheckBound(params PlTerm[] terms)
        {
            foreach (PlTerm term in terms)
            {
                if (term.IsVar)
                {
                    return Error("Is var {0}", term);
                }
            }
            return true;
        }

        private static MemberInfo findMember(PlTerm memberSpec, Type c)
        {
            return findMember(memberSpec, c, InstanceFields) ??
                   findMember(memberSpec, c, InstanceFields | BindingFlags.IgnoreCase);
        }
        private static MemberInfo findMember(PlTerm memberSpec, Type c, BindingFlags searchFlags)
        {
            if (IsTaggedObject(memberSpec))
            {
                var r = GetInstance(memberSpec) as MemberInfo;
                if (r != null) return r;
            }
            Type[] paramz = GetParamSpec(memberSpec) ?? ZERO_TYPES;
            return findField(memberSpec, c, searchFlags) ??
                   findPropertyInfo(memberSpec, c, true, true, ref paramz, searchFlags) ??
                   findMethodInfo(memberSpec, -1, c, ref paramz, searchFlags) ??
                   (MemberInfo)findPropertyInfo(memberSpec, c, false, false, ref paramz, searchFlags);
            //findConstructor(memberSpec, c));
        }

        private static FieldInfo findField(PlTerm memberSpec, Type c, BindingFlags searchFlags)
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
                return findField(memberSpec.Arg(0), c, searchFlags);
            }
            string fn = memberSpec.Name;
            if (fn == "[]") fn = "Get";
            FieldInfo fi = c.GetField(fn, searchFlags);
            return fi;
        }


        private static PropertyInfo findPropertyInfo(PlTerm memberSpec, Type c, bool mustHaveP, bool assumeParamTypes, ref Type[] paramz, BindingFlags searchFlags)
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
                    return findPropertyInfo(memberSpec.Arg(0), c, false, assumeParamTypes, ref paramzN, searchFlags);
                }
                if (mustHaveP) return null;
            }
            if (paramz == null)
            {
              //  Warn("using paramSpec {0}", ToString(memberSpec));
                paramz = GetParamSpec(memberSpec) ?? ZERO_TYPES;
            }
            string fn = memberSpec.Name;
            if (fn == "[]") fn = "Item";
            if (paramz == null || paramz.Length == 0)
                return c.GetProperty(fn, searchFlags) ?? c.GetProperty("Is" + fn, searchFlags);
            var ps = c.GetProperties(searchFlags);
            int len = paramz.Length;
            PropertyInfo nameMatched = null;
            bool ignoreCase0 = (BindingFlags.IgnoreCase & searchFlags) != 0;
            if (ignoreCase0)
            {
                fn = fn.ToLower();   
            }
            foreach (PropertyInfo info in ps)
            {
                if (info.Name == fn || (ignoreCase0 && info.Name.ToLower() == fn))
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
            return c.GetProperty(fn, searchFlags) ?? c.GetProperty("Is" + fn, searchFlags) ?? nameMatched;
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

        private static bool GetInstanceAndType(PlTerm clazzOrInstance, out object getInstance, out Type c)
        {
            if (clazzOrInstance.IsVar)
            {
                c = null;
                getInstance = null;
                return Error("Cant find instance {0}", clazzOrInstance);
            }
            getInstance = GetInstance(clazzOrInstance);
            c = GetTypeFromInstance(getInstance, clazzOrInstance);
            if (getInstance == null && c == null)
            {
                return Error("Cant find instance or type {0}", clazzOrInstance);
            }
            return true;
        }

        [PrologVisible]
        static public bool cliGetRaw(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetRaw(clazzOrInstance, memberSpec, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance;
            Type c;
            if (!GetInstanceAndType(clazzOrInstance, out getInstance, out c)) return false;
            if (!CheckBound(memberSpec)) return false;
            bool found;
            foreach (var searchFlags in getInstance == null ? BindingFlags_SEARCHS : BindingFlags_SEARCHIS)
            {
                object cliGet01 = cliGet0(getInstance, memberSpec, c, out found, searchFlags);

                if (found)
                {
                    return valueOut.FromObject(cliGet01);
                }
            }
            return false;
        }

        static public object cliGet0(object getInstance, PlTerm memberSpec, Type c, out bool found, BindingFlags icbf)
        {
            Type[] paramz = null;
            paramz = GetParamSpec(memberSpec) ?? ZERO_TYPES;
            if ((icbf & BindingFlags.GetProperty) != 0)
            {
                var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz, icbf);
                if (pi != null && pi.CanRead)
                {
                    var mi = pi.GetGetMethod();
                    if (mi != null)
                    {
                        found = true;
                        return ((InvokeCaught(mi, mi.IsStatic ? null : getInstance, ZERO_OBJECTS) ?? VoidOrNull(mi)));
                    }
                    Warn("Cant find getter for property " + memberSpec + " on " + c + " for " + pi);
                    found = false;
                    return null;
                }
            }
            if ((icbf & BindingFlags.GetField) != 0)
            {
                FieldInfo fi = findField(memberSpec, c, icbf);
                if (fi != null)
                {
                    object fiGetValue = fi.GetValue(fi.IsStatic ? null : getInstance);
                    found = true;
                    return (fiGetValue);
                }
            }
            if ((icbf & BindingFlags.InvokeMethod) != 0)
            {
                if (memberSpec.IsVar)
                {
                    Warn("cliGet0 on IsVar={0} on {1} for {2}", memberSpec, c, getInstance);
                    found = false;
                    return getInstance;
                }
                string fn = memberSpec.Name;
                MethodInfo mi = findMethodInfo(memberSpec, -1, c, ref paramz,icbf) ??
                                GetMethod(c, fn, icbf) ??
                                GetMethod(c, "get_" + fn, icbf) ??
                                GetMethod(c, "Get" + fn, icbf) ??
                                GetMethod(c, "Is" + fn, icbf) ??
                                GetMethod(c, "To" + fn, icbf);
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
            else
            {
                found = false;
                return null;
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
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetProperty(clazzOrInstance, memberSpec, indexValues, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance;
            Type c;
            if (!GetInstanceAndType(clazzOrInstance, out getInstance, out c)) return false;
            Type[] paramz = null;
            BindingFlags searchFlags = BindingFlagsALL;
            if (!CheckBound(memberSpec, indexValues)) return false;
            var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz, searchFlags);
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
            if (!valueIn.IsVar)
            {
                return Error("Cant set property with a var {0}", valueIn);
            }
            object getInstance;
            Type c;
            if (!GetInstanceAndType(clazzOrInstance, out getInstance, out c)) return false;
            Type[] paramz = null;
            if (!CheckBound(memberSpec, indexValues, valueIn)) return false;
            BindingFlags searchFlags = BindingFlagsALL;
            var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz, searchFlags);
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
            object getInstance;
            Type c;
            if (!GetInstanceAndType(clazzOrInstance, out getInstance, out c)) return false;
            foreach (var searchFlags in getInstance == null ? BindingFlags_SEARCHS : BindingFlags_SEARCHIS)
            {
                if (cliSet0(getInstance, memberSpec, paramIn, c, searchFlags)) return true;
            }
            return false;
        }

        static public bool cliSet0(object getInstance, PlTerm memberSpec, PlTerm paramIn, Type c, BindingFlags searchFlags)
        {
            Type[] paramz = null;
            if ((searchFlags & BindingFlags.SetProperty) != 0)
            {
                var pi = findPropertyInfo(memberSpec, c, false, true, ref paramz, searchFlags);

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
            }
            if ((searchFlags & BindingFlags.SetField) != 0)
            {
                FieldInfo fi = findField(memberSpec, c ,searchFlags);
                if (fi != null)
                {
                    object value = CastTerm(paramIn, fi.FieldType);
                    object target = fi.IsStatic ? null : getInstance;
                    fi.SetValue(target, value);
                    return true;
                }

            }
            if ((searchFlags & BindingFlags.InvokeMethod) != 0)
            {
                string fn = memberSpec.Name;
                MethodInfo mi = findMethodInfo(memberSpec, -1, c, ref paramz, searchFlags) ??
                                GetMethod(c, "set_" + fn, searchFlags) ??
                                GetMethod(c, "Set" + fn, searchFlags) ??
                                GetMethod(c, "from" + fn, searchFlags);
                if (mi == null)
                {
                    WarnMissing("Cant find setter " + memberSpec + " on " + c);
                    return false;
                }
                Action postCallHook;
                object[] value = PlListToCastedArray(paramIn, mi.GetParameters(), out postCallHook);
                object target = mi.IsStatic ? null : getInstance;
                object retval = InvokeCaught(mi, target, value, postCallHook);
                return true; // valueOut.FromObject(retval);
            }
            WarnMissing("Cant find setter " + memberSpec + " on " + c);
            return false;
        }

    }
}