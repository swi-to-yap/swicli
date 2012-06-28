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
        public static void Debug(object plthreadhasdifferntthread)
        {

        }

        [PrologVisible]
        public static bool cliThrow(PlTerm ex)
        {
            throw (Exception) CastTerm(ex, typeof (Exception));
        }
        [PrologVisible]
        public static bool cliBreak(PlTerm ex)
        {
            return WarnMissing((string)ex) || true;
        }

        private static string PlStringFormat(string text, params object[] ps)
        {
            RegisterCurrentThread();
            try
            {
                if (ps != null && ps.Length > 0) text = String.Format(text, ps);
            }
            catch (Exception)            
            {
            }
            DeregisterThread(Thread.CurrentThread);
            return text;
        }

        private static CycFort ToPlList(CycFort[] terms)
        {
            int termLen = terms.Length;
            if (termLen == 0) return ATOM_NIL;
            termLen--;
            PlTerm ret = listOfOne(terms[termLen]);
            while (--termLen >= 0)
            {
                ret = PlTerm.PlCompound(".", terms[termLen], ret);
            }
            return ret;
        }

        private static PlTermV ToPlTermV(PlTerm[] terms)
        {
            var tv = NewPlTermV(terms.Length);
            for (int i = 0; i < terms.Length; i++)
            {
                tv[i] = terms[i];
            }
            return tv;
        }

        private static PlTermV NewPlTermV(int length)
        {
            return new PlTermV(length);
        }

        private static PlTermV ToPlTermVParams(ParameterInfo[] terms)
        {
            var tv = NewPlTermV(terms.Length);
            for (int i = 0; i < terms.Length; i++)
            {
                tv[i] = typeToSpec(terms[i].ParameterType);
            }
            return tv;
        }
        private static PlTerm ToPlListParams(ParameterInfo[] terms)
        {
            PlTerm listOf = ATOM_NIL;
            for (int i = terms.Length - 1; i >= 0; i--)
            {
                PlTerm term = typeToSpec(terms[i].ParameterType);
                listOf = PlTerm.PlCompound(".", term, listOf);
            }
            return listOf;
        }
        private static PlTerm ToPlListTypes(Type[] terms)
        {
            PlTerm listOf = ATOM_NIL;
            for (int i = terms.Length - 1; i >= 0; i--)
            {
                PlTerm term = typeToSpec(terms[i]);
                listOf = PlTerm.PlCompound(".", term, listOf);
            }
            return listOf;
        }
        private static PlTermV ToPlTermVSpecs(Type[] terms)
        {
            var tv = NewPlTermV(terms.Length);
            for (int i = 0; i < terms.Length; i++)
            {
                tv[i] = typeToSpec(terms[i]);
            }
            return tv;
        }


        private static PlTerm listOfOne(PlTerm term)
        {
            return PlTerm.PlCompound(".", term, ATOM_NIL);
        }

        protected static PlTerm ATOM_NIL
        {
            get { return PlTerm.PlAtom("[]"); }
        }

        public static PlTerm PLNULL { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("null")); } }
        public static PlTerm PLVOID { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("void")); } }
        public static PlTerm PLTRUE { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("true")); } }
        public static PlTerm PLFALSE { get { return PlTerm.PlCompound("@", PlTerm.PlAtom("false")); } }

        public static object CallProlog(object target, string module, string name, int arity, object origin, object[] paramz, Type returnType, bool discard)
        {
			if (!ClientReady) {
				return null;
			}
            return InvokeFromC(() =>
            {

                PlTermV args = NewPlTermV(arity);
                int fillAt = 0;
                if (origin != null)
                {
                    args[fillAt++].FromObject(origin);
                }
                for (int i = 0; i < paramz.Length; i++)
                {
                    args[fillAt++].FromObject(paramz[i]);
                }
                bool IsVoid = returnType == typeof (void);
                if (!IsVoid)
                {
                    //args[fillAt] = PlTerm.PlVar();
                }
                if (!PlQuery.PlCall(module, name, args))
                {
                    if (!IsVoid) Warn("Failed Event Handler {0} failed", target);
                }
                if (IsVoid) return null;
                object ret = PrologClient.CastTerm(args[fillAt], returnType);
                return ret;
            }, discard);
        }

        public static int UnifyAtom(uint TermRef, string s)
        {
            uint temp = libpl.PL_new_term_ref();
            libpl.PL_put_atom(temp, libpl.PL_new_atom_wchars(s.Length, s));
            return libpl.PL_unify(temp, TermRef);
        }

        private static bool UnifySpecialObject(PlTerm plTerm, object ret1)
        {
            if (plTerm.IsVar)
            {
                return plTerm.FromObject(ret1);
            }
            else
            {
                var plvar = PlTerm.PlVar();
                return plvar.FromObject(ret1) && SpecialUnify(plTerm, plvar);
            }
        }
        private static Type[] GetParamSpec(PlTerm memberSpec, bool isObjects)
        {
            return GetParamSpec(memberSpec, isObjects, 0, -1);
        }
        private static Type[] GetParamSpec(PlTerm memberSpec, bool isObjects, int start, int length)
        {
            if (memberSpec.IsInteger)
            {
                Type[] lenType = new Type[memberSpec.intValue()];
                for (int i = 0; i < lenType.Length; i++)
                {
                    lenType[i] = null;
                }
                return lenType;
            }
            if (memberSpec.IsAtomic) return ZERO_TYPES;
            if (memberSpec.IsList)
            {
                memberSpec = memberSpec.Copy();
            }
            var specArray = memberSpec.ToArray();
            int arity = specArray.Length - start;
            if (length > -1)
            {
                arity = length - start;
            }
            int end = start + arity;
            Type[] paramz = new Type[arity];
            for (int i = start; i < end; i++)
            {
                PlTerm info = specArray[i];
                if (!info.IsVar)
                {
                    var t = GetType(info, isObjects);
                    if (t == null)
                    {
                        t = typeof (object);
                    }
                    paramz[i] = t;
                }
            }
            return paramz;
        }

        private static EventInfo findEventInfo(PlTerm memberSpec, Type c, ref Type[] paramz)
        {
            if (memberSpec.IsVar)
            {
                WarnMissing("findEventInfo IsVar {0} on type {1}", memberSpec, c);
                return null;
            }
            if (memberSpec.IsInteger)
            {
                int ordinal = memberSpec.intValue();
                var mis = c.GetEvents(BindingFlagsALL);
                if (ordinal < 0 || ordinal >= mis.Length) return null;
                return mis[ordinal];
            }
            if (IsTaggedObject(memberSpec))
            {
                var r = tag_to_object(memberSpec[1].Name) as EventInfo;
                if (r != null) return r;
            }
            if (memberSpec.IsCompound)
            {
                if (memberSpec.Name == "e")
                {
                    var arg1 = memberSpec.Arg(0);
                    if (arg1.IsInteger)
                    {
                        Type[] paramzN = null;
                        return findEventInfo(arg1, c, ref paramzN);
                    }
                }
            }
            if (c == null) return null;
            EventInfo ei = c.GetEvent(memberSpec.Name, BindingFlagsALL);
            if (ei != null) return ei;
            var members = c.GetEvents(BindingFlagsALL);
            int arity = memberSpec.Arity;
            if (paramz == null)
            {
                Warn("using paramSpec {0}", ToString(memberSpec));
                paramz = GetParamSpec(memberSpec, false);
            }
            foreach (var infos in members)
            {
                ParameterInfo[] getParmeters = GetParmeters(infos);
                if (getParmeters != null && getParmeters.Length == arity)
                {
                    return infos;
                }
            }
            return null;
        }
        private static MemberInfo findMember(PlTerm memberSpec, Type c)
        {
            Type[] paramz = null;
            if (IsTaggedObject(memberSpec))
            {
                var r = GetInstance(memberSpec) as MemberInfo;
                if (r != null) return r;
            }
            paramz = GetParamSpec(memberSpec, false);
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

        readonly static Dictionary<int, string> indexTest = new Dictionary<int, string>() { { 1, "one" }, { 2, "two" }, };
        public string this[int v]
        {
            get { return indexTest[v]; }
            set { indexTest[v] = value; }
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
            if (paramz == null)
            {
                Warn("using paramSpec {0}", ToString(memberSpec));
                paramz = GetParamSpec(memberSpec, false);
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

        private static MethodInfo findMethodInfo(PlTerm memberSpec, int arity, Type c, ref Type[] paramz)
        {
            var mi = findMethodInfo0(memberSpec, arity, c, ref paramz);
            if (mi == null)
            {
                return null;
            }
            if (!mi.IsGenericMethodDefinition) return mi;
            var typeparams = mi.GetGenericArguments() ?? new Type[0];
            if (typeparams.Length == 0) return mi;
            if (memberSpec.IsAtom)
            {
                {
                    var ps = mi.GetParameters();
                    bool missingTypePAramInArgs = false;
                    foreach (Type type in typeparams)
                    {
                        Type type1 = type;
                        var pi = Array.Find(ps, p => p.ParameterType == type1);
                        if (pi != null)
                        {
                            continue;
                        }
                        Warn("Trying to find a generic methods without type specifiers {0}",
                             ToString(typeparams));
                    }
                }
                return mi;
            }
            else
            {
                if (memberSpec.IsCompound)
                {

                    Type[] t = GetParamSpec(memberSpec, false, 0, typeparams.Length);
                    if (t.Length == typeparams.Length)
                    {
                        mi = mi.MakeGenericMethod(t);
                    }
                }
                return mi;
            }
        }

        private static object ToString(object o)
        {
            if (o == null) return "null";
            if (o is IConvertible) return o.ToString();
            if (o is System.Collections.IEnumerable)
            {
                var oc = (System.Collections.IEnumerable) o;
                int count = 0;
                string ret = "[";
                foreach (var o1 in (System.Collections.IEnumerable)o)
                {
                    if (count>1) ret += ",";
                    count++;
                    ret += ToString(o1);
                }
                return ret + "]";
            }
            return o.ToString();
        }
        private static MethodInfo findMethodInfo0(PlTerm memberSpec, int arity, Type c, ref Type[] paramz)
        {
            if (c == null)
            {
                Warn("findMethod no class for {0}", memberSpec);
                return null;
            }
            if (memberSpec.IsVar)
            {
                Warn("findMethod IsVar {0} on type {1}", memberSpec, c);
                return null;
            }
            if (memberSpec.IsInteger)
            {
                var mis = c.GetMethods(BindingFlagsALL);
                return mis[memberSpec.intValue()];
            }
            if (IsTaggedObject(memberSpec))
            {
                object o = tag_to_object(memberSpec[1].Name);
                var r = o as MethodInfo;
                if (r != null) return r;
                var d = o as Delegate;
                if (d != null) return d.Method ?? d.GetType().GetMethod("Invoke");
            }
            string fn = memberSpec.Name;
            MethodInfo mi = null;
            if (arity < 1)
            {
                mi = GetMethod(c, fn, BindingFlagsALL);
                if (mi != null) return mi;
            }
            if (paramz == null)
            {
                Warn("using paramSpec {0}", ToString(memberSpec));
                paramz = GetParamSpec(memberSpec, false);
            }
            try
            {
                mi = c.GetMethod(fn, BindingFlagsALL, null, CallingConventions.Any, paramz, null);
                if (mi != null) return mi;
            }
            catch (/*AmbiguousMatch*/ Exception e)
            {
                Debug("AME: " + e + " fn = " + fn);
            }
            MethodInfo[] members = c.GetMethods(BindingFlagsALL);
            if (arity < 0) arity = paramz.Length;// memberSpec.Arity;
            string fnLower = fn.ToLower();
            MethodInfo candidate = null;
            foreach (var infos in members)
            {
                if (infos.GetParameters().Length == arity)
                {
                    if (infos.Name == fn)
                    {
                        return infos;
                    }
                    if (candidate == null && infos.Name.ToLower() == fnLower)
                    {
                        candidate = infos;
                    }
                }
            }
            if (candidate != null) return candidate;
            return null;
        }


        private static ConstructorInfo findConstructorInfo(PlTerm memberSpec, Type c, ref Type[] paramz)
        {
            if (c == null)
            {
                Warn("findConstructor no class for {0}", memberSpec);
                return null;
            }
            if (IsTaggedObject(memberSpec))
            {
                var r = tag_to_object(memberSpec[1].Name) as ConstructorInfo;
                if (r != null) return r;
            }
            if (memberSpec.IsInteger)
            {
                var mis = c.GetConstructors(BindingFlagsALL);
                return mis[memberSpec.intValue()];
            }
            if (paramz == null)
            {
                Warn("using paramSpec {0}", ToString(memberSpec));
                paramz = GetParamSpec(memberSpec, false);
            }
            if (paramz != null)
            {
                var mi = c.GetConstructor(paramz);
                if (mi != null) return mi;
            }
            ConstructorInfo[] members = c.GetConstructors(BindingFlagsALL);
            int arity = memberSpec.Arity;
            ConstructorInfo candidate = null;
            foreach (var infos in members)
            {
                if (infos.GetParameters().Length == arity)
                {
                    if (infos.IsStatic)
                    {
                        if (candidate == null)
                        {
                            candidate = infos;
                        }
                    }
                    else return infos;
                }
            }
            return candidate;
        }

        private static ParameterInfo[] GetParmeters(EventInfo ei)
        {
            ParameterInfo[] parme = null;
            var rm = ei.GetRaiseMethod();
            var erm = ei.EventHandlerType;
            if (rm == null && erm != null)
            {
                rm = erm.GetMethod("Invoke");
            }
            if (rm != null)
            {
                parme = rm.GetParameters();
            }
            return parme;
        }



        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliFindConstructor(PlTerm clazzSpec, PlTerm memberSpec, PlTerm methodOut)
        {
            Type c = GetType(clazzSpec);
            Type[] paramz = null;
            MethodBase mi = findConstructorInfo(memberSpec, c, ref paramz);
            if (mi != null)
            {
                return methodOut.FromObject((mi));
            }
            return false;
        }

        /// <summary>
        /// ?- cliNew('java.lang.Long',[long],[44],Out),cliToString(Out,Str).
        /// </summary>
        /// <param name="memberSpec"></param>
        /// <param name="paramIn"></param>
        /// <param name="valueOut"></param>
        /// <returns></returns>
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliNew(PlTerm clazzSpec, PlTerm memberSpec, PlTerm paramIn, PlTerm valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliNew(clazzSpec, memberSpec, paramIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type c = GetType(clazzSpec);
            if (c == null)
            {
                Error("Cant resolve clazzSpec {0}", clazzSpec);
                return false;
            }
            Type[] paramz = GetParamSpec(memberSpec, false);
            MethodBase mi = findConstructorInfo(memberSpec, c, ref paramz);
            object target = null;
            if (mi == null)
            {
                int arity = paramz.Length;
                if (arity == 1)
                {
                    mi = c.GetMethod("op_Implicit", (BindingFlags.Public | BindingFlags.Static), null, paramz,
                                     new ParameterModifier[0]);
                    if (mi == null)
                    {
                        mi = c.GetMethod("op_Explicit", (BindingFlags.Public | BindingFlags.Static), null, paramz,
                                         new ParameterModifier[0]);
                    }
                    if (mi == null)
                    {
                        if (c.IsPrimitive)
                        {
                            //Warn("Trying to constuct a primitive type");
                            return valueOut.FromObject(Convert.ChangeType(GetInstance(paramIn.Arg(0)), c));
                        }
                    }
                }
                if (mi == null)
                {
                    MethodInfo[] members = c.GetMethods(BindingFlagsJustStatic);
                    mi = BestMethod(paramz, members, c, true);
                }
            }
            if (mi == null)
            {
                Error("Cant find constructor {0} on {1}", memberSpec, c);
                return false;
            }
            Action postCallHook;
            object[] values = PlListToCastedArray(paramIn, mi.GetParameters(), out postCallHook);
            object res;

            // mono doesnt mind..
            //  typeof(System.Text.StringBuilder).GetConstructor(new[]{typeof(System.String)}).Invoke(null,new object[]{"hi there"}).ToString();
            // .NET doesnt
            if (mi is ConstructorInfo)
            {
                res = ((ConstructorInfo)mi).Invoke(values);
            }
            else
            {
                res = mi.Invoke(null, values);
            }
            var ret = valueOut.FromObject(res);
            postCallHook();
            return ret;
        }
        private static MethodBase BestMethod(Type[] paramz, MethodInfo[] members, Type returnType, bool mustStatic)
        {
            MethodBase maybe = null;
            foreach (var infos in members)
            {
                if (mustStatic && !infos.IsStatic) continue;
                ParameterInfo[] testParams = infos.GetParameters();
                if (testParams.Length == paramz.Length)
                {
                    if (returnType.IsAssignableFrom(infos.ReturnType))
                    {
                        if (ParamsMatch(paramz, testParams))
                        {
                            return infos;
                        }
                        if (maybe == null) maybe = infos;
                    }
                }
            }
            return maybe;
        }

        private static bool ParamsMatch(Type[] paramz, ParameterInfo[] paramInfos)
        {
            int i = 0;
            foreach (ParameterInfo info in paramInfos)
            {
                if (!info.ParameterType.IsAssignableFrom(paramz[i])) return false;
                i++;
            }
            return true;
        }

        /// <summary>
        /// ?- cliNewArray(long,10,Out),cliToString(Out,Str).
        /// </summary>
        /// <param name="clazzSpec"></param>
        /// <param name="rank"></param>
        /// <param name="valueOut"></param>
        /// <returns></returns>
        [PrologVisible(ModuleName = ExportModule)]
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

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliLockEnter(PlTerm lockObj)
        {
            object getInstance = GetInstance(lockObj);
            Monitor.Enter(getInstance);
            return true;
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliLockExit(PlTerm lockObj)
        {
            object getInstance = GetInstance(lockObj);
            Monitor.Exit(getInstance);
            return true;
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliFindMethod(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm methodOut)
        {
            if (!methodOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliFindMethod(clazzOrInstance, memberSpec, plvar) && SpecialUnify(methodOut, plvar);
            }
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            Type[] paramz = null;
            var mi = findMethodInfo(memberSpec, -1, c, ref paramz);
            if (mi != null)
            {
                return methodOut.FromObject((mi));
            }
            return false;
        }



        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliCallRaw(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm paramsIn, PlTerm valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliCallRaw(clazzOrInstance, memberSpec, paramsIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            int arity = Arglen(paramsIn);
            Type[] paramz = GetParamSpec(paramsIn, true);
            var mi = findMethodInfo(memberSpec, arity, c, ref paramz);
            if (mi == null)
            {
                var ei = findEventInfo(memberSpec, c, ref paramz);
                if (ei != null) return RaiseEvent(getInstance, memberSpec, paramsIn, valueOut, ei, c);
                if (paramsIn.IsAtom && paramsIn.Name == "[]") return cliGetRaw(clazzOrInstance, memberSpec, valueOut);
                Warn("Cant find method {0} on {1}", memberSpec, c);
                return false;
            }
            Action postCallHook;
            object[] value = PlListToCastedArray(paramsIn, mi.GetParameters(), out postCallHook);
            object target = mi.IsStatic ? null : getInstance;
            object retval = InvokeCaught(mi, target, value, postCallHook);
            return valueOut.FromObject(retval ?? VoidOrNull(mi));
        }

        private static int Arglen(PlTerm paramIn)
        {
            if (paramIn.IsList)
            {
                int len = 0;
                PlTerm each = paramIn;
                while (each.IsList && !each.IsAtom)
                {
                    each = each.Arg(1);
                    len++;
                }
                return len;
            }
            if (paramIn.IsCompound) return paramIn.Arity;
            if (paramIn.IsAtom) return 0;
            return -1;
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliRaiseEventHandler(PlTerm clazzOrInstance, PlTerm memberSpec, PlTerm paramIn, PlTerm valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliRaiseEventHandler(clazzOrInstance, memberSpec, paramIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance = GetInstance(clazzOrInstance);
            Type c = GetTypeFromInstance(getInstance, clazzOrInstance);
            Type[] paramz = null;
            EventInfo evi = findEventInfo(memberSpec, c, ref paramz);
            return RaiseEvent(getInstance, memberSpec, paramIn, valueOut, evi, c);
        }

        public static bool RaiseEvent(object getInstance, PlTerm memberSpec, PlTerm paramIn, PlTerm valueOut, EventInfo evi, Type c)
        {
            if (evi == null)
            {
                return Warn("Cant find event {0} on {1}", memberSpec, c);
            }
            ParameterInfo[] paramInfos = GetParmeters(evi);
            MethodInfo mi = evi.GetRaiseMethod();
            string fn = evi.Name;
            if (mi == null)
            {
                FieldInfo fi = c.GetField(fn, BindingFlagsALL);
                if (fi != null)
                {
                    Delegate del = (Delegate)fi.GetValue(getInstance);
                    if (del != null)
                    {
                        Action postCallHook;
                        var ret = valueOut.FromObject((del.DynamicInvoke(
                                                          PlListToCastedArray(paramIn, paramInfos,
                                                                              out postCallHook))));
                        postCallHook();
                        return ret;
                    }
                }
                string fn1 = fn.Substring(1);
                int len = fn.Length;
                foreach (FieldInfo info in c.GetFields(BindingFlagsALL))
                {
                    if (info.Name.EndsWith(fn1))
                    {
                        if (info.Name.Length - len < 3)
                        {
                            Delegate del = (Delegate)info.GetValue(info.IsStatic ? null : getInstance);
                            if (del != null)
                            {
                                Action postCallHook;
                                var ret = valueOut.FromObject((del.DynamicInvoke(
                                                                  PlListToCastedArray(paramIn, paramInfos,
                                                                                      out postCallHook))));
                                postCallHook();
                                return ret;
                            }
                        }
                    }
                }
            }
            if (mi == null)
            {
                Type eviEventHandlerType = evi.EventHandlerType;
                if (eviEventHandlerType != null) mi = eviEventHandlerType.GetMethod("Invoke");
            }
            if (mi == null)
            {
                Warn("Cant find event raising for  {0} on {1}", evi, c);
                return false;
            }
            Action postCallHook0;
            object[] value = PlListToCastedArray(paramIn, mi.GetParameters(), out postCallHook0);
            object target = mi.IsStatic ? null : getInstance;
            return valueOut.FromObject(InvokeCaught(mi, target, value, postCallHook0) ?? VoidOrNull(mi));
        }

        private static object VoidOrNull(MethodInfo info)
        {
            return info.ReturnType == typeof(void) ? (object)PLVOID : PLNULL;
        }

        [PrologVisible(ModuleName = ExportModule)]
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
            paramz = GetParamSpec(memberSpec, false);
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
        [PrologVisible(ModuleName = ExportModule)]
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
            if (postCallHook != null) postCallHook();
            return valueOut.FromObject(cliGet01);
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

        [PrologVisible(ModuleName = ExportModule)]
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


        /// <summary>
        /// 1 ?- cliToString(-1,X).
        /// X = "4294967295".
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        [PrologVisible(ModuleName = ExportModule)]
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
                return str.FromObject("" + o);
            }
            catch (Exception e)
            {
                Warn("cliToString: {0}", e);
                object o = GetInstance(obj);
                if (o == null) return str.FromObject("" + obj);
                return str.FromObject("" + o);
            }
        }

        [PrologVisible(ModuleName = ExportModule)]
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
    }
}