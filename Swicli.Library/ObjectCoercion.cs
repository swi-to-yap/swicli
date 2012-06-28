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
using jpl;
using Class = java.lang.Class;
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using SbsSW.SwiPlCs;
using Class = System.Type;
#endif
using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = Swicli.Library.PrologClient;

namespace Swicli.Library
{
    public partial class PrologClient
    {
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliGetterm(CycFort valueCol, CycFort valueIn, CycFort valueOut)
        {
            List<object> objs;
            if (valueCol.IsVar)
            {
                objs = new List<object>();
                valueCol.FromObject(objs);
            }
            else
            {
                objs = (List<object>)CastTerm(valueCol, typeof(ICollection));
            }
            if (!valueOut.IsVar)
            {
                var plvar = CycFort.PlVar();
                return cliGetterm(valueCol, valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            if (IsTaggedObject(valueIn))
            {
                object val = GetInstance(valueIn);
                int index = objs.IndexOf(val);
                if (index < 0)
                {
                    index = objs.Count;
                    objs.Add(val);
                    var type = val.GetType();
                    if (type.IsArray)
                    {
                        return valueIn.Unify(valueOut);
                    }
                    return ToFieldLayout("object", typeToName(type), val, type, valueOut, false, false) != libpl.PL_fail;
                }
            }
            return valueIn.Unify(valueOut);
        }


        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliCast(CycFort valueIn, CycFort clazzSpec, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliCast(valueIn, clazzSpec, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type type = GetType(clazzSpec);
            if (type == null)
            {
                return Error("Cant find class {0}", clazzSpec);
            }
            if (valueIn.IsVar)
            {
                return Error("Cant find instance {0}", valueIn);
            }
            object retval = CastTerm(valueIn, type);
            return UnifyTagged(retval, valueOut);
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliCastImmediate(CycFort valueIn, CycFort clazzSpec, CycFort valueOut)
        {
            if (valueIn.IsVar)
            {
                return Warn("Cant find instance {0}", valueIn);
            }
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliCastImmediate(valueIn, clazzSpec, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type type = GetType(clazzSpec);
            object retval = CastTerm(valueIn, type);
            return valueOut.FromObject(retval);
        }

        [PrologVisible(ModuleName = ExportModule)]
        private static object[] PlListToCastedArray(IEnumerable<CycFort> term, ParameterInfo[] paramInfos, out Action todo)
        {
            return PlListToCastedArray(0, term, paramInfos, out todo);
        }
        [PrologVisible(ModuleName = ExportModule)]
        private static object[] PlListToCastedArray(int skip, IEnumerable<CycFort> term, ParameterInfo[] paramInfos, out Action todo)
        {
            todo = Do_NOTHING;
            int len = paramInfos.Length;
            if (len == 0) return ARRAY_OBJECT0;
            MethodInfo methodInfo = paramInfos[0].Member as MethodInfo;
            bool isVarArg = methodInfo != null && (methodInfo.CallingConvention & CallingConventions.VarArgs) != 0;
            object[] ret = new object[len];
            PlTerm[] ta = ToTermArray(term);
            int termLen = ta.Length;
            int lenNeeded = len;
            int termN = skip;
            for (int idx = 0; idx < len; idx++)
            {
                ParameterInfo paramInfo = paramInfos[idx];
                PlTerm arg = ta[termN];
                Type type = GetParameterType(paramInfo);
                bool isByRef = IsByRef(paramInfo);
                bool lastArg = (idx + 1 == len);
                if (lastArg && isVarArg)
                {
                    if (arg.IsList)
                    {
                        ret[idx] = CastTerm(arg, type);
                        termN++;
                        continue;
                    }
                    Type arrayElementType = type.GetElementType();
                    if (termN < termLen)
                    {
                        int slack = termLen - termN;
                        var sa = (object[])Array.CreateInstance(arrayElementType, slack);
                        ret[idx] = sa;
                        for (int i = 0; i < slack; i++)
                        {
                            sa[i] = CastTerm(ta[termN++], arrayElementType);
                        }
                        continue;
                    }
                    ret[idx] = Array.CreateInstance(arrayElementType, 0);
                }
                if (IsOptionalParam(paramInfo))
                {
                    if (termLen < lenNeeded)
                    {
                        lenNeeded--;
                        object paramInfoDefaultValue = paramInfo.DefaultValue;
                        if (type.IsInstanceOfType(paramInfoDefaultValue))
                        {
                            ret[idx] = paramInfoDefaultValue;
                        }
                        else
                        {
                            //paramInfo.ParameterType.IsValueType
                            //default()
                        }
                        //termN stays the same!
                        continue;
                    }
                }
                bool wasOut = paramInfo.IsOut || paramInfo.IsRetval;
                if (wasOut) isByRef = false;
                if (isByRef && !wasOut)
                {
                    var ooo = CastTerm(arg, type);
                    if (ooo == null)
                    {
                        Debug("idx " + idx + " (" + type + ") for " + arg + " is null");
                    }
                    ret[idx] = ooo;
                    wasOut = true;
                }
                if (wasOut)
                {
                    var sofar = todo;
                    int index0 = idx;
                    int termM = termN;
                    PlTerm plTerm = arg;
                    todo = () =>
                    {
                        object ret1 = ret[index0];
                        int ii = termM;
                        UnifySpecialObject(plTerm, ret1);
                        sofar();
                    };
                    if (isByRef)
                    {
                        termN++;
                        continue;
                    }
                }
                if (paramInfo.IsIn)
                {
                    var ooo1 = CastTerm(arg, type);
                    if (ooo1 == null)
                    {
                        Debug("idx " + idx + " (" + type + ") for " + arg + " is null");
                    }
                    ret[idx] = ooo1;
                }
                else
                {
                    if (!wasOut)
                    {
                        var ooo1 = CastTerm(arg, type);
                        if (ooo1 == null)
                        {
                            Debug("idx " + idx + " (" + type + ") for " + arg + " is null");
                        }
                        ret[idx] = ooo1;                        
                    }
                    else
                    {
                        ret[idx] = null;// CastTerm(arg, paramInfo.ParameterType);                        
                    }
                }
                termN++;
            }
            return ret;
        }
        public static Type GetParameterType(ParameterInfo paramInfo)
        {
            Type paramType = paramInfo.ParameterType;
            return paramType.IsByRef ? paramType.GetElementType() : paramType;
        }
        public static bool IsByRef(ParameterInfo paramInfo)
        {
            Type paramType = paramInfo.ParameterType;
            return paramType.IsByRef;
        }


        private static void Do_NOTHING()
        {
        }
        public static Object CastTerm(CycFort o, Type pt)
        {
            if (pt == typeof(object)) pt = null;
            object r = CastTerm0(o, pt);
            if (pt == null || r == null)
                return r;
            Type fr = r.GetType();
            if (pt.IsInstanceOfType(r)) return r;
            return RecastObject(pt, r, fr);
        }

        private static object RecastObject(Type pt, object r, Type fr)
        {
            try
            {
                if (r is double && pt == typeof(float))
                {
                    double d = (double)r;
                    return Convert.ToSingle(d);
                }
                //                if (PrologBinder.CanConvertFrom(r.GetType(), pt))
                {
                    var value = Convert.ChangeType(r, pt);
                    if (pt.IsInstanceOfType(value)) return value;
                }
            }
            catch (InvalidCastException e)
            {
                Debug("conversion " + fr + " to " + pt + " resulted in " + e);
            }
            catch (Exception e)
            {
                Debug("conversion " + fr + " to " + pt + " resulted in " + e);
            }

            try
            {
                ICollection<Func<object, object>> tryAll = new List<Func<object, object>>();
                var fc = findCast(fr, pt, tryAll);
                if (fc != null)
                {
                    try
                    {
                        var value = fc(r);
                        if (pt.IsInstanceOfType(value)) return value;
                    }
                    catch (Exception)
                    {
                    }
                }
                foreach (var ti in tryAll)
                {
                    try
                    {
                        var value = ti(r);
                        if (pt.IsInstanceOfType(value)) return value;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Debug("conversion " + fr + " to " + pt + " resulted in " + e);
            }
            Warn("Having time of it convcerting {0} to {1}", r, pt);


            return r;
        }

        private static readonly List<Type> ConvertorClasses = new List<Type>();

        private static Func<object, object> findCast(Type from, Type to, ICollection<Func<object, object>> allMethods)
        {
            Func<object, object> meth;
            if (to.IsAssignableFrom(from))
            {
                meth = (r) => r;
                if (allMethods != null) allMethods.Add(meth); else return meth;
            }
            if (to.IsEnum)
            {
                meth = (r) =>
                           {
                               if (r == null)
                               {
                                   return null;
                               }
                               return Enum.Parse(to, r.ToString());
                           };
                if (allMethods != null) allMethods.Add(meth); else return meth;
            }
            if (to.IsPrimitive)
            {
                meth = ((r) => Convert.ChangeType(r, to));
                if (allMethods != null) allMethods.Add(meth); else return meth;
            }
            if (to.IsArray && from.IsArray)
            {
                var eto = to.GetElementType();
                var efrom = from.GetElementType();
                meth = ((r) =>
                            {
                                Array ar = ((Array)r);
                                int len = ar.Length;
                                Array ret = Array.CreateInstance(eto, len);
                                for (int i = 0; i < len; i++)
                                {
                                    ret.SetValue(RecastObject(eto, ar.GetValue(i), efrom), i);
                                }
                                return ret;
                            });
                if (allMethods != null) allMethods.Add(meth); else return meth;
            }
            ConstructorInfo ci = to.GetConstructor(new Type[] { from });
            if (ci != null)
            {
                meth = (r) => ci.Invoke(new object[] { r });
                if (allMethods != null) allMethods.Add(meth); else return meth;
            }
            ConstructorInfo pc = null;
            foreach (ConstructorInfo mi in to.GetConstructors(BindingFlagsALL))
            {
                var ps = mi.GetParameters();
                if (ps.Length == 0 && !mi.IsStatic)
                {
                    pc = mi;
                    continue;
                }
                if (ps.Length == 1)
                {
                    Type pt = ps[0].ParameterType;
                    if (pt.IsAssignableFrom(from))
                    {
                        ConstructorInfo info = mi;
                        meth = (r) => info.Invoke(new object[] { r });
                        if (allMethods != null) allMethods.Add(meth); else return meth;
                    }
                }
            }
            var someStatic = SomeConversionStaticMethod(to, to, from, allMethods, false);
            if (someStatic != null) return someStatic;
            someStatic = SomeConversionStaticMethod(to, from, from, allMethods, false);
            if (someStatic != null) return someStatic;
            foreach (MethodInfo mi in from.GetMethods(BindingFlagsInstance))
            {
                if (!mi.IsStatic)
                {
                    var ps = mi.GetParameters();
                    if (ps.Length == 0)
                    {
                        if (!to.IsAssignableFrom(mi.ReturnType)) continue;
                        Type pt = ps[0].ParameterType;
                        if (pt.IsAssignableFrom(from))
                        {
                            MethodInfo info = mi;
                            meth = (r) => info.Invoke(null, new object[] { r });
                            if (allMethods != null) allMethods.Add(meth); else return meth;
                        }
                    }
                }
            }
            if (pc != null)
            {
                foreach (var f in to.GetFields(BindingFlagsInstance))
                {
                    FieldInfo info = f;
                    meth = (r) =>
                               {
                                   var ret = pc.Invoke(null);
                                   info.SetValue(ret, r);
                                   return ret;
                               };
                    if (allMethods != null) allMethods.Add(meth); else return meth;
                }
            }
            if (ConvertorClasses.Count == 0)
            {
                ConvertorClasses.Add(typeof(Convert));
                ConvertorClasses.Add(typeof(PrologConvert));
                //ConvertorClasses.Add(typeof(PrologClient));
            }
            foreach (Type convertorClasse in ConvertorClasses)
            {
                var someStaticM = SomeConversionStaticMethod(to, convertorClasse, from, allMethods, false);
                if (someStaticM != null) return someStatic;
            }
            return null;
        }
        private static Func<object, object> SomeConversionStaticMethod(Type to, Type srch, Type from, ICollection<Func<object, object>> allMethods, bool onlyConverionAttribute)
        {
            Func<object, object> meth;
            foreach (MethodInfo mi in srch.GetMethods(BindingFlagsJustStatic))
            {
                if (mi.IsStatic)
                {
                    var ps = mi.GetParameters();
                    if (ps.Length == 1)
                    {
                        if (!to.IsAssignableFrom(mi.ReturnType)) continue;
                        Type pt = ps[0].ParameterType;
                        if (pt.IsAssignableFrom(from))
                        {
                            MethodInfo info = mi;
                            meth = (r) => info.Invoke(null, new object[] { r });
                            if (allMethods != null) allMethods.Add(meth); else return meth;
                        }
                    }
                }
            }
            return null;
        }

        private static readonly Type[] arrayOfStringType = new Type[] { typeof(string) };
        private static uint _enum2;
        private static uint _obj1;
        private static readonly object[] ARRAY_OBJECT0 = new object[0];

        static object ToBigInteger(string value)
        {
            Type t;
            // Just Mono
            t = Type.GetType("Mono.Math.BigInteger");
            if (t != null)
            {
                var m = t.GetMethod("Parse", arrayOfStringType);
                if (m != null) return m.Invoke(null, new object[] {value});
            }
            // .net 4.0 and Mono
            t = ResolveType("System.Numerics.BigInteger");
            if (t != null)
            {
                var m = t.GetMethod("Parse", arrayOfStringType);
                if (m != null) return m.Invoke(null, new object[] { value });
            }
            // Just Mono Android
            t = ResolveType("Java.Math.BigInteger");
            if (t != null)
            {
                var m = t.GetMethod("Parse", arrayOfStringType);
                if (m != null) return m.Invoke(null, new object[] { value });
            }

            // IKVM         
            t = ResolveType("java.math.BigInteger");
            if (t != null)
            {
                var m = t.GetConstructor(arrayOfStringType);
                if (m != null) return m.Invoke(new object[] { value });
            }             
#if USE_IKVM
            return new java.math.BigInteger(value);
#else
            if (!value.StartsWith("-")) return ulong.Parse(value);
            return long.Parse(value);
#endif
        }
        static object ToBigDecimal(string value)
        {
            Type t;
            // Just Mono
            t = Type.GetType("Mono.Math.BigDecimal");
            if (t != null)
            {
                var m = t.GetMethod("Parse", arrayOfStringType);
                if (m != null) return m.Invoke(null, new object[] { value });
            }
            // .net 4.0 and Mono
            t = ResolveType("System.Numerics.BigDecimal");
            if (t != null)
            {
                var m = t.GetMethod("Parse", arrayOfStringType);
                if (m != null) return m.Invoke(null, new object[] { value });
            }
            // Just Mono Android
            t = ResolveType("Java.Math.BigDecimal");
            if (t != null)
            {
                var m = t.GetMethod("Parse", arrayOfStringType);
                if (m != null) return m.Invoke(null, new object[] { value });
            }
            // IKVM   
            t = ResolveType("java.math.BigDecimal");
            if (t != null)
            {
                var m = t.GetConstructor(arrayOfStringType);
                if (m != null) return m.Invoke(new object[] {value});
            }
#if USE_IKVM
            return new java.math.BigDecimal(value);
#else
            return double.Parse(value);
#endif        
        }
        public static Object CastTerm0(CycFort o, Type pt)
        {
            if (pt == typeof(PlTerm)) return o;
            if (pt == typeof(string))
            {
                if (IsTaggedObject(o))
                {
                    return "" + GetInstance(o);
                }
                return (string)o;
            }
            if (pt != null && pt.IsSubclassOf(typeof(Delegate)))
            {
                return cliDelegateTerm(pt, o, false);
            }
            if (pt == typeof(Type))
            {
                return GetType(o);
            }
            switch (o.PlType)
            {
                case PlType.PlUnknown:
                    {
                        return (string)o;
                    }
                    break;
                case PlType.PlVariable:
                    {
                        return o;
                    }
                    break;
                case PlType.PlInteger:
                    {
                        int i = 0;
                        if (0 != libpl.PL_get_integer(o.TermRef, ref i))
                            return i;
                        try
                        {
                            return (long)o;
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            return (ulong)o;
                        }
                        catch (Exception)
                        {
                            return ToBigInteger((string)o);
                        }
                    }
                    break;
                case PlType.PlFloat:
                    {
                        try
                        {
                            return (double)o;
                        }
                        catch (Exception)
                        {
                            return ToBigDecimal((string)o);
                        }
                    }
                    break;
                case PlType.PlAtom:
                case PlType.PlString:
                    {
                        if (pt != null && pt.IsArray)
                        {
                            if (o.Name == "[]")
                            {
                                return Array.CreateInstance(pt.GetElementType(), 0);
                            }
                        }
                        string s = (string)o;
                        if (pt == null) return s;
                        var constructor = pt.GetConstructor(new[] { typeof(string) });
                        if (constructor != null)
                        {
                            return constructor.Invoke(new object[] { s });
                        }
                        foreach (var m in pt.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {

                            ParameterInfo[] mGetParameters = m.GetParameters();
                            if (pt.IsAssignableFrom(m.ReturnType) && mGetParameters.Length == 1 &&
                                mGetParameters[0].ParameterType.IsAssignableFrom(typeof(string)))
                            {
                                WarnMissing("using " + m);
                                try
                                {
                                    return m.Invoke(null, new object[] { s });
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                        return s;
                    }
                    break;
                case PlType.PlTerm:
                    {
                        lock (ToFromConvertLock)
                        {
                            return CastCompoundTerm(o.Name, o.Arity, o[1], o, pt);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsOptionalParam(ParameterInfo info)
        {
            if ((info.Attributes & ParameterAttributes.Optional) != 0)
            {
                return true;
            }
            if ((info.Attributes & ParameterAttributes.HasDefault) != 0)
            {
                return true;
            }
            return info.IsOptional || (info.Name != null && info.Name.ToLower().StartsWith("optional"));
        }

        private static MemberInfo[] GetStructFormat(Type t)
        {
            if (false)
            {
                lock (FunctorToLayout)
                {
                    PrologTermLayout layout;
                    if (TypeToLayout.TryGetValue(t, out layout))
                    {
                        return layout.FieldInfos;
                    }
                }
            }
            bool specialXMLType = false;
            if (!t.IsEnum)
            {
                var ta = t.GetCustomAttributes(typeof(XmlTypeAttribute), false);
                if (ta != null && ta.Length > 0)
                {
                    XmlTypeAttribute xta = (XmlTypeAttribute)ta[0];
                    specialXMLType = true;
                }
            }
            MemberInfo[] tGetFields = null;
            if (specialXMLType)
            {
                List<MemberInfo> fis = new List<MemberInfo>();
                foreach (var e in t.GetFields(InstanceFields))
                {
                    var use = e.GetCustomAttributes(typeof(XmlArrayItemAttribute), false);
                    if (use == null || use.Length < 1)
                    {
                        continue;
                    }
                    fis.Add(e);
                }
                foreach (var e in t.GetProperties(InstanceFields))
                {
                    var use = e.GetCustomAttributes(typeof(XmlArrayItemAttribute), false);
                    if (use == null || use.Length < 1)
                    {
                        continue;
                    }
                    fis.Add(e);
                }
                tGetFields = fis.ToArray();
            }
            else
            {
                // look for [StructLayout(LayoutKind.Sequential)]

                var ta = t.GetCustomAttributes(typeof(StructLayoutAttribute), false);
                if (ta != null && ta.Length > 0)
                {
                    StructLayoutAttribute xta = (StructLayoutAttribute)ta[0];
                    // ReSharper disable ConditionIsAlwaysTrueOrFalse
                    if (xta.Value == LayoutKind.Sequential || true /* all sequential layouts*/)
                    // ReSharper restore ConditionIsAlwaysTrueOrFalse
                    {
                        tGetFields = t.GetFields(InstanceFields);
                    }
                }
                if (tGetFields == null)
                    tGetFields = t.GetFields(InstanceFields);
            }
            if (tGetFields.Length == 0)
            {
                Warn("No fields in {0}", t);
            }
            return tGetFields;
        }


        public static int ToFieldLayout(string named, string arg1, object o, Type t, CycFort term, bool childs, bool addNames)
        {

            MemberInfo[] tGetFields = GetStructFormat(t);

            int len = tGetFields.Length;
            PlTermV tv = NewPlTermV(len + 1);
            tv[0].UnifyAtom(arg1);
            int tvi = 1;
            for (int i = 0; i < len; i++)
            {
                object v = GetMemberValue(tGetFields[i], o);
                if (v is IList)
                {
                    v.GetType();
                }
                tv[tvi++].FromObject((v));
            }
            if (true)
            {
                return PlSucceedOrFail(term.Unify(PlC(named, tv)));
            }
            uint termTermRef = term.TermRef;

            uint temp = libpl.PL_new_term_ref();
            libpl.PL_cons_functor_v(temp,
                                    libpl.PL_new_functor(libpl.PL_new_atom(named), tv.Size),
                                    tv.A0);
            return libpl.PL_unify(termTermRef, temp);
        }

        private static int ToVMNumber(object o, CycFort term)
        {
            if (o is int)
                return libpl.PL_unify_integer(term.TermRef, (int)Convert.ToInt32(o));
            if (PreserveObjectType)
            {
                return PlSucceedOrFail(UnifyTagged(o, term));
            }
            // signed types
            if (o is short || o is sbyte)
                return libpl.PL_unify_integer(term.TermRef, (int)Convert.ToInt32(o));
            if (o is long)
                return libpl.PL_unify_integer(term.TermRef, (long)Convert.ToInt64(o));
            if (o is decimal || o is Single || o is float || o is double)
                return libpl.PL_unify_float(term.TermRef, (double)Convert.ToDouble(o));
            // unsigned types
            if (o is ushort || o is byte)
                return libpl.PL_unify_integer(term.TermRef, (int)Convert.ToInt32(o));
            if (o is UInt32)
                return libpl.PL_unify_integer(term.TermRef, (long)Convert.ToInt64(o));
            // potentually too big?!
            if (o is ulong)
            {
                ulong u64 = (ulong)o;
                if (u64 <= Int64.MaxValue)
                {
                    return libpl.PL_unify_integer(term.TermRef, (long)Convert.ToInt64(o));
                }
                return PlSucceedOrFail(term.Unify(u64));
                //return libpl.PL_unify_float(term.TermRef, (double)Convert.ToDouble(o));
            }
            if (o is IntPtr)
            {
                return libpl.PL_unify_intptr(term.TermRef, (IntPtr)o);
            }
            if (o is UIntPtr)
            {
                return libpl.PL_unify_intptr(term.TermRef, (IntPtr)o);
            }
            return -1;
        }

        /*
         
  jpl_is_ref(@(Y)) :-
	atom(Y),        % presumably a (garbage-collectable) tag
	Y \== void,     % not a ref
	Y \== false,    % not a ref
	Y \== true.     % not a ref
         
         */
        private static object CastCompoundTerm(string name, int arity, CycFort arg1, CycFort orig, Type pt)
        {
            string key = name + "/" + arity;
            lock (FunctorToLayout)
            {
                PrologTermLayout pltl;
                if (FunctorToLayout.TryGetValue(key, out pltl))
                {
                    Type type = pltl.ObjectType;
                    MemberInfo[] fis = pltl.FieldInfos;
                    MemberInfo toType = pltl.ToType;
                    if (toType != null)
                    {
                        return GetMemberValue(toType, CastTerm(arg1, argOneType(toType)));
                    }
                    return CreateInstance(type, fis, orig, 1);
                }
            }
            lock (FunctorToRecomposer)
            {
                PrologTermRecomposer layout;
                if (FunctorToRecomposer.TryGetValue(key, out layout))
                {
                    Type type = layout.ToType;
                    uint newref = libpl.PL_new_term_ref();
                    PlTerm outto = new PlTerm(newref);
                    var ret = PlQuery.PlCall(layout.module, layout.r2obj, new PlTermV(orig, outto));
                    if (ret)
                    {
                        object o = CastTerm(outto, type);
                        if (!pt.IsInstanceOfType(o))
                        {
                            Warn(type + " (" + o + ") is not " + pt);
                        }
                        return o;
                    }
                }
            }
            if (key == "static/1")
            {
                return null;
            }
            if (key == "delegate/1")
            {
                return CastTerm0(arg1, pt);
            }
            if (key == "delegate/2")
            {
                return cliDelegateTerm(pt, orig, false);
            }
            if (key == "{}/1")
            {
                return arg1;
            }
            if (pt == typeof(object)) pt = null;
            //{T}
            //@(_Tag)
            if (key == "@/1" && arg1.IsAtom)
            {
                name = arg1.Name;
                switch (name)
                {
                    case "true":
                        {
                            return true;
                        }
                    case "false":
                        {
                            return false;
                        }
                    case "null":
                        {
                            if (pt != null && pt.IsValueType)
                            {
                                return pt.GetConstructor(ZERO_TYPES).Invoke(ZERO_OBJECTS);
                            }
                            return null;
                        }
                    case "void":
                        {
#if USE_IKVM
                            if (pt == typeof(void)) return JPL.JVOID;
#endif
                            return null;
                        }
                    default:
                        {
                            
                            {
                                object o = tag_to_object(name);
                                if (o == null)
                                {
                                    Warn("Null from tag " + name);
                                }
                                return o;
#if plvar_pins                                
                                lock (ToFromConvertLock) lock (atomToPlRef)
                                {
                                    PlRef oldValue;
                                    if (!atomToPlRef.TryGetValue(name, out oldValue))
                                    {
                                        //Warn("no value for tag=" + name);
                                        if (pt != null && pt.IsInstanceOfType(o))
                                        {
                                            return o;
                                        }
                                        return o;
                                    }
                                    var v = oldValue.Value;
                                    if (pt != null && pt.IsInstanceOfType(v))
                                    {
                                        return v;
                                    }
                                    return v;
                                }
#endif
                            }
                        }
                }
            }
#if plvar_pins
            if (name == "$cli_object")
            {
                lock (ToFromConvertLock)
                {
                    lock (termToObjectPins)
                    {
                        PlRef oldValue;
                        Int64 ohandle = (long)arg1;
                        if (!termToObjectPins.TryGetValue(ohandle, out oldValue))
                        {
                            Warn("no value for ohandle=" + ohandle);
                        }
                        return oldValue.Value;
                    }
                }
            }
#endif
            if (key == "enum/2")
            {
                Type type = GetType(arg1);
                PlTerm arg2 = orig.Arg(1);
                object value = Enum.Parse(type, arg2.Name, true);
                if (value == null) Warn("cant parse enum: {0} for type {1}", arg2, type);
                return value;
            }
            if (key == "array/2")
            {
                Type type = GetType(arg1);
                return CreateArrayOfType(orig.Arg(1), type.MakeArrayType());
            }
            if (key == "array/3")
            {
                Type type = GetType(arg1);
                return CreateArrayOfType(orig.Arg(2), orig.Arg(1), type);
            }
            if (name == "values")
            {
                Warn("Values array");
            }
            if (name == "struct" || name == "event" || name == "object")
            {
                Type type = GetType(arg1);
                MemberInfo[] fis = GetStructFormat(type);
                return CreateInstance(type, fis, orig, 2);
            }
            if (orig.IsList)
            {
                if (pt != null && pt.IsArray)
                {
                    return CreateArrayOfType(orig, pt);
                }
                if (arg1.IsInteger || arg1.IsAtom)
                {
                    Debug("maybe this is a string " + orig);
                }
                else
                {
                    var o1 = GetInstance(arg1);
                    if (o1 != null)
                    {
                        // send a list into cliGet0
                        bool found;
                        var res = cliGet0(arg1, orig.Arg(1), o1.GetType(), out found);
                        if (found) return res;
                    }
                    if (pt == null)
                    {
                        // Return as array?
                        return CreateArrayOfType(orig, typeof(object[]));
                    }
                }
            }
            if (pt != null && pt.IsArray)
            {
                return CreateArrayOfType(orig, pt);
            }
            Type t = ResolveType(name);
            if (t == null)
            {
                WarnMissing(String.Format("Cant GetInstance from {0}", orig));
                return orig;
            }
            if (pt == null || pt.IsAssignableFrom(t))
            {
                foreach (var m in t.GetConstructors())
                {
                    ParameterInfo[] mGetParameters = m.GetParameters();
                    if (mGetParameters.Length == arity)
                    {
                        Warn("using contructor {0}", m);
                        Action postCallHook;
                        var values = PlListToCastedArray(orig, m.GetParameters(), out postCallHook);
                        var retval = m.Invoke(values);
                        postCallHook();
                        return retval;
                    }
                }
            }
            // Debug("Get Instance fallthru");
            MemberInfo[] ofs = GetStructFormat(t);
            return CreateInstance(t, ofs, orig, 1);
        }

        private static Type argOneType(MemberInfo info)
        {
            {
                var mi = info as MethodInfo;
                if (mi != null)
                {

                    ParameterInfo[] miGetParameters = mi.GetParameters();
                    if (miGetParameters.Length > 0)
                    {
                        return miGetParameters[0].ParameterType;
                    }
                }
            }
            {
                var mi = info as ConstructorInfo;
                if (mi != null)
                {

                    ParameterInfo[] miGetParameters = mi.GetParameters();
                    if (miGetParameters.Length > 0)
                    {
                        return miGetParameters[0].ParameterType;
                    }
                }
            }
            var fi = info as FieldInfo;
            if (fi != null)
            {
                return fi.FieldType;
            }
            return typeof(object);
        }

        private static object CreateInstance(Type type, MemberInfo[] fis, CycFort orig, int plarg)
        {
            int fisLength = fis.Length;
            if (orig.Arity < fisLength)
            {
                fisLength = orig.Arity;
                Warn("Struct length mismatch");
            }
            object[] paramz = new object[fisLength];
            for (int i = 0; i < fisLength; i++)
            {
                MemberInfo fi = fis[i];
                PlTerm origArg = orig[plarg];
                paramz[i] = CastTerm(origArg, FieldType(fi, true));
                plarg++;
            }
            object newStruct = null;
            try
            {
                newStruct = Activator.CreateInstance(type);
            }
            catch (System.MissingMethodException)
            {
                foreach (ConstructorInfo ci in type.GetConstructors(BindingFlagsALL))
                {
                    if (ci.GetParameters().Length != paramz.Length) continue;
                    if (ci.IsStatic) continue;
                    newStruct = ci.Invoke(paramz);
                }
                if (newStruct != null) return newStruct;
            }
            for (int i = 0; i < fis.Length; i++)
            {
                MemberInfo fi = fis[i];
                SetMemberValue(fi, newStruct, paramz[i]);
            }


            return newStruct;
        }

        private static object GetMemberValue(MemberInfo field, object o)
        {
            if (field is FieldInfo)
            {
                return ((FieldInfo)field).GetValue(o);
            }
            if (field is PropertyInfo)
            {
                return ((PropertyInfo)field).GetGetMethod(true).Invoke(o, null);
            }
            if (field is MethodInfo)
            {
                MethodInfo mi = (MethodInfo)field;
                if (mi.IsStatic) return mi.Invoke(null, new object[] { o });
                return ((MethodInfo)field).Invoke(o, null);
            }
            if (field is ConstructorInfo)
            {
                return ((ConstructorInfo)field).Invoke(new object[] { o });
            }
            throw new IndexOutOfRangeException("" + field);
        }

        public static void SetMemberValue(MemberInfo field, object o, object value)
        {
            if (field is FieldInfo)
            {
                ((FieldInfo)field).SetValue(o, value);
                return;
            }
            if (field is PropertyInfo)
            {
                MethodInfo setterMethod = ((PropertyInfo)field).GetSetMethod(true);
                if (setterMethod == null)
                {
                    Warn("No setter method on {0}", field);
                    return;
                }
                setterMethod.Invoke(o, new object[] { value });
                return;
            }
            if (field is MethodInfo)
            {
                MethodInfo mi = (MethodInfo)field;
                ParameterInfo[] pms = mi.GetParameters();
                if (mi.IsStatic)
                {
                    if (pms.Length == 1)
                    {
                        mi.Invoke(null, new object[] {value});
                    }
                    mi.Invoke(null, new object[] { o, value });
                    return;
                }
                if (pms.Length == 1)
                {
                    mi.Invoke(o, new object[] { value });
                }
                else
                {
                    
                }
                return;
            }
            throw new IndexOutOfRangeException("" + field);
            if (field is ConstructorInfo)
            {
                ((ConstructorInfo)field).Invoke(new object[] { o, value });
                return;
            }
        }

        private static Type FieldType(MemberInfo field, bool asSetter)
        {
            if (field is FieldInfo)
            {
                return ((FieldInfo)field).FieldType;
            }
            if (field is PropertyInfo)
            {
                return ((PropertyInfo)field).PropertyType;
            }
            if (field is MethodInfo)
            {
                MethodInfo mi = (MethodInfo)field;
                if (asSetter)
                {
                    var pms = mi.GetParameters();
                    if (pms.Length == 0) return mi.ReturnType;
                    return pms[pms.Length - 1].ParameterType;
                }
                return mi.ReturnType;
            }
            if (field is ConstructorInfo)
            {
                return ((ConstructorInfo)field).DeclaringType;
            }
            throw new IndexOutOfRangeException("" + field);
        }

        private static int FillArray(IList fis, Type elementType, CycFort orig, int plarg)
        {
            int elements = 0;
            for (int i = 0; i < fis.Count; i++)
            {
                fis[i] = CastTerm(orig[plarg], elementType);
                plarg++;
                elements++;
            }
            return elements;
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTypespec(CycFort clazzSpec, CycFort valueOut)
        {
            return valueOut.Unify(typeToSpec(GetType(clazzSpec)));
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliToFromLayout(CycFort clazzSpec, CycFort memberSpec, CycFort toSpec)
        {
            Type type = GetType(clazzSpec);
            string name = memberSpec.Name;
            int arity = memberSpec.Arity;
            MemberInfo[] fieldInfos = new MemberInfo[arity];
            for (int i = 0; i < arity; i++)
            {
                var arg = memberSpec.Arg(i);
                fieldInfos[i] = findMember(arg, type);
            }
            var toMemb = findMember(toSpec, type);
            AddPrologTermLayout(type, name, fieldInfos, toMemb);
            return true;
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliAddLayout(CycFort clazzSpec, CycFort memberSpec)
        {
            Type type = GetType(clazzSpec);
            string name = memberSpec.Name;
            int arity = memberSpec.Arity;
            MemberInfo[] fieldInfos = new MemberInfo[arity];
            for (int i = 0; i < arity; i++)
            {
                var arg = memberSpec.Arg(i);
                fieldInfos[i] = findMember(arg, type);
            }
            AddPrologTermLayout(type, name, fieldInfos, null);
            return true;
        }

        readonly private static Dictionary<string, PrologTermLayout> FunctorToLayout = new Dictionary<string, PrologTermLayout>();
        readonly private static Dictionary<Type, PrologTermLayout> TypeToLayout = new Dictionary<Type, PrologTermLayout>();

        static public void AddPrologTermLayout(Type type, string name, MemberInfo[] fieldInfos, MemberInfo toType)
        {
            PrologTermLayout layout = new PrologTermLayout();
            layout.FieldInfos = fieldInfos;
            layout.Name = name;
            layout.ObjectType = type;
            layout.ToType = toType;
            int arity = fieldInfos.Length;
            if (toType != null)
            {
                arity = 1;
            }
            layout.Arity = arity;
            lock (FunctorToLayout)
            {
                FunctorToLayout[name + "/" + arity] = layout;
                TypeToLayout[type] = layout;
            }
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliToFromRecomposer(CycFort clazzSpec, CycFort memberSpec, CycFort obj2r, CycFort r2obj)
        {
            Type type = GetType(clazzSpec);
            string name = memberSpec.Name;
            int arity = memberSpec.Arity;
            AddPrologTermRecomposer(type, "user", obj2r.Name, r2obj.Name, name, arity);
            return true;
        }

        readonly private static Dictionary<string, PrologTermRecomposer> FunctorToRecomposer = new Dictionary<string, PrologTermRecomposer>();
        readonly private static Dictionary<Type, PrologTermRecomposer> TypeToRecomposer = new Dictionary<Type, PrologTermRecomposer>();

        static public void AddPrologTermRecomposer(Type type, string module, string obj2r, string r2obj, string functorWrapper, int arityWrapper)
        {
            PrologTermRecomposer layout = new PrologTermRecomposer();
            layout.obj2r = obj2r;
            layout.module = module;
            layout.r2obj = r2obj;
            layout.Name = functorWrapper;
            layout.Arity = arityWrapper;
            layout.ToType = type;
            lock (FunctorToRecomposer)
            {
                FunctorToRecomposer[functorWrapper + "/" + arityWrapper] = layout;
                TypeToRecomposer[type] = layout;
            }
        }

        public static object GetInstance(CycFort classOrInstance)
        {
            if (classOrInstance.IsVar)
            {
                Warn("GetInstance(PlVar) {0}", classOrInstance);
                return null;
            }
            if (!classOrInstance.IsCompound)
            {
                if (classOrInstance.IsAtom)
                {
                    Type t = GetType(classOrInstance);
                    // we do this for static invokations like: cliGet('java.lang.Integer','MAX_VALUE',...)
                    // the arg1 denotes a type, then return null!
                    if (t != null) return null;
                    Warn("GetInstance(atom) {0}", classOrInstance);
                    // possibly should always return null?!
                }
                else if (classOrInstance.IsString)
                {
                    Debug("GetInstance(string) " + classOrInstance);
                    return (string)classOrInstance;
                }
                else
                {
                    return CastTerm(classOrInstance, null);
                }
                return CastTerm(classOrInstance, null);
            }
            string name = classOrInstance.Name;
            int arity = classOrInstance.Arity;
            return CastCompoundTerm(name, arity, classOrInstance[1], classOrInstance, null);
        }

        /// <summary>
        /// Returns the Type when denoated by a 'namespace.type' (usefull for static instance specification)
        ///    if a @C#234234  the type of the object unless its a a class
        ///    c(a) => System.Char   "sdfsdf" =>  System.String   uint(5) => System.UInt32
        /// 
        ///    instanceMaybe maybe Null.. it is passed in so the method code doesn't have to call GetInstance again
        ///       on classOrInstance
        /// </summary>
        /// <param name="instanceMaybe"></param>
        /// <param name="classOrInstance"></param>
        /// <returns></returns>
        private static Type GetTypeFromInstance(object instanceMaybe, CycFort classOrInstance)
        {
            if (classOrInstance.IsAtom)
            {
                return GetType(classOrInstance);
            }
            if (classOrInstance.IsString)
            {
                if (instanceMaybe != null) return instanceMaybe.GetType();
                return typeof(string);
            }
            if (classOrInstance.IsCompound)
            {
                if (classOrInstance.Name == "static")
                {
                    return GetType(classOrInstance[1]);
                }
            }

            object val = instanceMaybe ?? GetInstance(classOrInstance);
            //if (val is Type) return (Type)val;
            if (val == null)
            {
                Warn("GetTypeFromInstance: {0}", classOrInstance);
                return null;
            }
            return val.GetType();
        }

        static System.Collections.IEnumerable Unfold(object value, out bool unFolded)
        {
            IList<object> results = new List<object>();
            var type = value.GetType();
            var utype = Enum.GetUnderlyingType(type);
            var values = Enum.GetValues(type);
            if (utype == typeof(byte) || utype == typeof(sbyte) || utype == typeof(Int16) || utype == typeof(UInt16) || utype == typeof(Int32))
            {
                unFolded = true;
                var num = (Int32)Convert.ChangeType(value, typeof(Int32));
                if (num == 0)
                {
                    results.Add(value);
                    return results;
                }
                foreach (var val in values)
                {
                    var v = (Int32)Convert.ChangeType(val, typeof(Int32));
                    if (v == 0) continue;
                    if ((v & num) == v)
                    {
                        results.Add(Enum.ToObject(value.GetType(), val));
                    }
                }
            }
            else if (utype == typeof(UInt32))
            {
                unFolded = true;
                var num = (UInt32)value;
                if (num == 0)
                {
                    results.Add(value);
                    return results;
                }
                foreach (var val in values)
                {
                    var v = (UInt32)Convert.ChangeType(val, typeof(UInt32));
                    if ((v & num) == v) results.Add(Enum.ToObject(value.GetType(), val));
                }
            }
            else if (utype == typeof(Int64))
            {
                unFolded = true;
                var num = (Int64)value;
                if (num == 0)
                {
                    results.Add(value);
                    return results;
                }
                foreach (var val in values)
                {
                    var v = (Int64)Convert.ChangeType(val, typeof(Int64));
                    if (v == 0L)
                    {
                        continue;
                    }
                    if ((v & num) == v) results.Add(Enum.ToObject(value.GetType(), val));
                }
            }
            else if (utype == typeof(UInt64))
            {
                unFolded = true;
                var num = (UInt64)value;
                if (num == 0)
                {
                    results.Add(value);
                    return results;
                }
                foreach (var val in values)
                {
                    var v = (UInt64)Convert.ChangeType(val, typeof(UInt64));
                    if (v == 0U)
                    {
                        continue;
                    }
                    if ((v & num) == v) results.Add(Enum.ToObject(value.GetType(), val));
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }

        private static bool SpecialUnify(CycFort valueOut, CycFort plvar)
        {
            bool b = valueOut.Unify(plvar);
            if (b) return true;
            object obj1 = GetInstance(plvar);
            if (ReferenceEquals(obj1, null))
            {
                return false;
            }
            Type t1 = obj1.GetType();
            object obj2 = CastTerm(valueOut, t1);
            if (ReferenceEquals(obj2, null))
            {
                return false;
            }
            Type t2 = obj2.GetType();
            if (obj1.Equals(obj2))
            {
                return true;
            }
            if (t1 == t2)
            {
                return false;
            }
            return false;
        }

        public static Object ToFromConvertLock = new object();
        public static int UnifyToProlog(object o, CycFort term)
        {
            if (!term.IsVar)
            {
                Warn("Not a free var {0}", term);
                return libpl.PL_fail;
            }
            uint TermRef = term.TermRef;
            if (TermRef == 0)
            {
                Warn("Not a allocated term {0}", o);
                return libpl.PL_fail;
            }
            if (o is PlTerm)
            {
                return libpl.PL_unify(TermRef, ((PlTerm)o).TermRef);
            }
#if USE_IKVM
            if (o is Term) return UnifyToProlog(ToPLCS((Term)o), term);
#endif
            if (PreserveObjectType)
            {
                return PlSucceedOrFail(UnifyTagged(o, term));
            }
            if (o is string)
            {
                string s = (string)o;
                switch (VMStringsAsAtoms)
                {
                    case libpl.CVT_STRING:
                        {
                            try
                            {
                                return libpl.PL_unify_string_chars(TermRef, (string)o);
                            }
                            catch (Exception)
                            {

                                return UnifyAtom(TermRef, s);
                            }
                        }
                    case libpl.CVT_ATOM:
                        try
                        {
                            return libpl.PL_unify_atom_chars(TermRef, (string)o);
                        }
                        catch (Exception)
                        {

                            return UnifyAtom(TermRef, s);
                        }
                    case libpl.CVT_LIST:
                        return libpl.PL_unify_list_chars(TermRef, (string)o);
                    default:
                        Warn("UNKNOWN VMStringsAsAtoms {0}", VMStringsAsAtoms);
                        return libpl.PL_fail;
                }
            }
            if (o == null)
            {
                return AddTagged(TermRef, "null");
            }

            if (o is Type || o is Type)
            {
                if (true)
                {
                    //lock (ToFromConvertLock)
                    {
                        var tag = object_to_tag(o);
                        AddTagged(TermRef, tag);
                        return libpl.PL_succeed;
                    }
                }
                return PlSucceedOrFail(term.Unify(typeToSpec((Type)o)));
            }

            Type t = o.GetType();
            if (t == typeof(void))
            {
                return AddTagged(TermRef, "void");
            }
            if (o is ValueType)
            {
                if (o is bool)
                {
                    bool tf = (bool)o;
                    return AddTagged(TermRef, tf ? "true" : "false");
                }
                if (o is char)
                {
                    try
                    {
                        char ch = (char)o;
                        string cs = new string(ch, 1);
                        switch (VMStringsAsAtoms)
                        {
                            case libpl.CVT_STRING:
                                return libpl.PL_unify_atom_chars(TermRef, cs);
                            case libpl.CVT_ATOM:
                                return libpl.PL_unify_atom_chars(TermRef, cs);
                            case libpl.CVT_LIST:
                                return libpl.PL_unify_integer(TermRef, (int)ch);
                            default:
                                Warn("UNKNOWN VMStringsAsAtoms {0}", VMStringsAsAtoms);
                                return libpl.PL_fail;
                        }
                    }
                    catch (Exception e)
                    {
                        Warn("@TODO unmappable errors? {0} type {1}", o, t);
                        //
                    }
                }
                if (t.IsEnum)
                {
                    int res = FromEnum(TermRef, o, t);
                    term.ToString();
                    return res;
                }
                if (t.IsPrimitive)
                {
                    try
                    {
                        int res = ToVMNumber(o, term);
                        if (res == libpl.PL_succeed) return res;
                        if (res == libpl.PL_fail) return res;
                        if (res != -1)
                        {
                            // Warn("@TODO Missing code for ToVmNumber? " + o + " type " + t);
                            return res;
                        }
                        if (t.IsPrimitive)
                        {
                            Warn("@TODO Missing code for primitive? {0} type {1}", o, t);
                        }
                    }
                    catch (Exception e)
                    {
                        Warn("@TODO unmappable errors? {0} type {1}", o, t);
                    }
                }
            }
            lock (FunctorToLayout)
            {
                PrologTermLayout layout;
                if (TypeToLayout.TryGetValue(t, out layout))
                {
                    MemberInfo[] tGetFields = layout.FieldInfos;// GetStructFormat(t);
                    int len = tGetFields.Length;
                    PlTermV tv = NewPlTermV(len);
                    for (int i = 0; i < len; i++)
                    {
                        object v = GetMemberValue(tGetFields[i], o);
                        tv[i].FromObject((v));
                    }
                    return PlSucceedOrFail(term.Unify(PlC(layout.Name, tv)));
                }
            }
            lock (FunctorToRecomposer)
            {
                PrologTermRecomposer layout = GetTypeMap(t, TypeToRecomposer);
                if (layout != null)
                {
                    lock (ToFromConvertLock)
                    {
                        var tag = object_to_tag(o);
                        uint newref = libpl.PL_new_term_refs(2);
                        AddTagged(newref, tag);
                        PlTerm into = new PlTerm(newref);
                        PlTerm outto = new PlTerm(newref + 1);
                        var ret = PlQuery.PlCall(layout.module, layout.obj2r, new PlTermV(into, outto));
                        if (ret)
                        {
                            return term.Unify(outto) ? libpl.PL_succeed
                                   : libpl.PL_fail;

                        }
                    }
                }
            }
            if (o is IList)
            {

            }
            if (IsStructRecomposable(t))
            {
                return ToFieldLayout("struct", typeToName(t), o, t, term, false, false);
            }
            if (o is EventArgs)
            {
                return ToFieldLayout("event", typeToName(t), o, t, term, false, false);
            }
            return PlObject(TermRef, o);
        }

        public static T GetTypeMap<T>(Type t, IDictionary<Type, T> mapped)
        {
            T layout;
            if (mapped.TryGetValue(t, out layout))
            {
                return layout;
            }

            Type bestType = null;
            foreach (KeyValuePair<Type, T> map in mapped)
            {
                Type mapKey = map.Key;
                if (mapKey.IsAssignableFrom(t))
                {
                    if (bestType == null || mapKey.IsAssignableFrom(bestType))
                    {
                        bestType = mapKey;
                        layout = map.Value;
                    }
                }
            }
            return layout;// default(T);
        }

        public static CycFort C(string collection)
        {
            return PlTerm.PlAtom(collection);
        }

        private static int FromEnum(uint TermRef, object o, Type t)
        {
            uint temp = libpl.PL_new_term_ref();
            libpl.PL_cons_functor_v(temp,
                                    ENUM_2,
                                    new PlTermV(typeToSpec(t), PlTerm.PlAtom(o.ToString())).A0);
            return libpl.PL_unify(TermRef, temp);
        }

        protected static uint ENUM_2
        {
            get
            {
                if (_enum2 == 0)
                {
                    _enum2 = libpl.PL_new_functor(libpl.PL_new_atom("enum"), 2);
                }
                return _enum2;
            }
        }

        private static bool IsStructRecomposable(Type t)
        {
            return t.IsValueType && !t.IsEnum && !t.IsPrimitive &&
                   (!t.Namespace.StartsWith("System") || t == typeof(DateTime)) &&
                   !typeof(IEnumerator).IsAssignableFrom(t) &&
                   !typeof(ICloneable).IsAssignableFrom(t) &&
                   !typeof(IEnumerable).IsAssignableFrom(t) &&
                   !typeof(ICollection).IsAssignableFrom(t);
        }

        private delegate void WithEnum(CycFort p);
        private void ForEachEnumValue(WithEnum withValue, object p)
        {
            Type pType = p.GetType();
            if (!CycTypeInfo.IsFlagType(pType))
            {
                PlTerm fort = (PlTerm)ToFort(p);
                withValue(fort);
                return;
            }
            Array pTypeValues = System.Enum.GetValues(pType);
            Array.Reverse(pTypeValues);

            if (p is byte)
            {
                byte b = (byte)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    byte bv = (byte)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is sbyte)
            {
                sbyte b = (sbyte)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    sbyte bv = (sbyte)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is UInt16)
            {
                ushort b = (UInt16)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    ushort bv = (ushort)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is Int16)
            {
                short b = (Int16)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    short bv = (short)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is UInt32)
            {
                uint b = (UInt32)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    uint bv = (uint)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is Int32)
            {
                int b = (Int32)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    int bv = (int)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is UInt64)
            {
                ulong b = (UInt64)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    ulong bv = (ulong)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            if (p is Int64)
            {
                long b = (Int64)p;
                if (b == 0)
                {
                    withValue((PlTerm)ToFort(p));
                    return;
                }
                foreach (object v in pTypeValues)
                {
                    long bv = (long)v;
                    if (bv >= b)
                    {
                        withValue((PlTerm)ToFort(v));
                        b -= bv;
                    }
                    if (b == 0) return;
                }
                return;
            }
            string s = p.ToString();
            bool unfolded;
            foreach (var unfold in Unfold(p, out unfolded))
            {
                withValue((PlTerm)ToFort(unfold));
                //return;
            }
            if (unfolded) return;
            Trace();
            if (p is IConvertible)
            {
                withValue((PlTerm)ToFort(p));
                return;
            }

            if (p is Enum)
            {
                withValue((PlTerm)ToFort(p));
                return;
            }
            withValue((PlTerm)ToFort(p));
        }
    }

    internal class PrologConvert //: OpenMetaverse.UUIDFactory
    {
        static public Guid ToGuid(object from)
        {
            return new Guid("" + from);
        }
        static public String ToStr(object from)
        {
            return "" + from;
        }
        static public Type ToType(CycFort typeSpec)
        {
            return PrologClient.GetType(typeSpec);
        }
    }

    internal class PrologTermLayout
    {
        public string Name;
        public int Arity;
        public Type ObjectType;
        public MemberInfo[] FieldInfos;
        public MemberInfo ToType;
    }

    internal class PrologTermRecomposer
    {
        public string module;
        public string Name;
        public int Arity;
        public String obj2r;
        public String r2obj;
        //public MemberInfo[] FieldInfos;
        public Type ToType;
    }

}
