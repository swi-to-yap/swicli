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

        [PrologVisible]
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

        [PrologVisible]
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

        public static readonly Dictionary<string, List<Func<object, object>>> convertCache =
            new Dictionary<string, List<Func<object, object>>>();
        private static object RecastObject(Type pt, object r, Type fr)
        {
            Exception ce = null;
            try
            {
                if (r is double && pt == typeof(float))
                {
                    double d = (double)r;
                    return Convert.ToSingle(d);
                }
            }
            catch (InvalidCastException e)
            {
                Debug("conversion {0} to {1} resulted in {2}", fr, pt, e);
                ce = e;
            }
            catch (Exception e)
            {
                Debug("conversion {0} to {1} resulted in {2}", fr, pt, e);
                ce = e;
            }
            try
            {
                var tryAll = GetConvertCache(fr, pt);
                if (tryAll != null && tryAll.Count > 0)
                {
                    ce = null;
                    int wn = 0;
                    bool somethingWorked = false;
                    object retVal = null;
                    foreach (var ti in tryAll)
                    {
                        try
                        {
                            retVal = ti(r);
                            somethingWorked = true;
                            if (pt.IsInstanceOfType(retVal))
                            {
                                if (wn != 0)
                                {
                                    // put this conversion method first in list
                                    tryAll.RemoveAt(wn);
                                    tryAll.Insert(0, ti);
                                }
                                return retVal;
                            }
                        }
                        catch (Exception ee)
                        {
                            ce = ce ?? ee;
                        }
                        wn++;
                    }
                    if (somethingWorked)
                    {
                        // probly was a null->null conversion
                        return retVal;
                    }
                }
            }
            catch (Exception e)
            {
                Debug("conversion {0} to {1} resulted in {2}", fr, pt, e);
                ce = ce ?? e;
            }
            Warn("Having time of it convcerting {0} to {1} why {2}", r, pt, ce);


            return r;
        }

        private static List<Func<object, object>> GetConvertCache(Type from, Type to)
        {
            string key = "" + from + "->" + to;
            List<Func<object, object>> found;
            bool wasNew = true;
            lock (convertCache)
            {
                wasNew = !convertCache.TryGetValue(key, out found);
                if (wasNew)
                {
                    found = convertCache[key] = new List<Func<object, object>>();
                }
            }
            if (wasNew)
            {
                findConversions(from, to, found);
            }
            return found;
        }

        private static readonly List<Type> ConvertorClasses = new List<Type>();
        public static T ReflectiveCast<T>(object o)
        {
            T t =  (T) o;
            return t;
        }

        public static T ReflectiveNull<T>()
        {
            T t = default(T);
            return t;
        }

        private static void CheckMI()
        {
            if (MissingMI == null)
            {
                MissingMI = typeof(PrologClient).GetMethod("ReflectiveCast", BindingFlagsJustStatic);
            }
            if (MakeDefaultViaReflectionInfo == null)
            {
                MakeDefaultViaReflectionInfo = typeof(PrologClient).GetMethod("ReflectiveNull", BindingFlagsJustStatic);
            }
        }

        private static Func<object, object> findConversions(Type from, Type to, ICollection<Func<object, object>> allMethods)
        {
            Func<object, object> meth;
            if (to.IsAssignableFrom(from))
            {
                meth = (r) => r;
                if (allMethods != null) allMethods.Add(meth);
                else return meth;
            }
            Func<object, object> sysmeth = (r) =>
                                               {
                                                   CheckMI();
                                                   if (r == null)
                                                   {
                                                       MethodInfo rc = MakeDefaultViaReflectionInfo.MakeGenericMethod(to);
                                                       return rc.Invoke(null, ZERO_OBJECTS);
                                                   }
                                                   else
                                                   {
                                                       MethodInfo rc = MissingMI.MakeGenericMethod(to);
                                                       return rc.Invoke(null, new[] { r });
                                                   }
                                               };
            if (to.IsValueType)
            {
                if (allMethods != null) allMethods.Add(sysmeth);
                else
                {
                    // dont return .. this is the fallthru at bottem anyhow
                    // return sysmeth;
                }
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
                if (allMethods != null) allMethods.Add(meth);
                else return meth;
            }
            if (to.IsPrimitive)
            {
                meth = ((r) => Convert.ChangeType(r, to));
                if (allMethods != null) allMethods.Add(meth);
                else return meth;
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
                if (allMethods != null) allMethods.Add(meth);
                else return meth;
            }
            ConstructorInfo ci = to.GetConstructor(new Type[] { from });
            if (ci != null)
            {
                meth = (r) => ci.Invoke(new object[] { r });
                if (allMethods != null) allMethods.Add(meth);
                else return meth;
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
                        if (allMethods != null) allMethods.Add(meth);
                        else return meth;
                    }
                }
            }
            // search for op_Implicit/Explicit
            var someStatic = SomeConversionStaticMethod(to, to, from, allMethods, false);
            if (someStatic != null) return someStatic;
            // search for op_Implicit/Explicit
            someStatic = SomeConversionStaticMethod(to, from, from, allMethods, false);
            if (someStatic != null) return someStatic;

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
            //if (PrologBinder.CanConvertFrom(from, to))
            {
                meth = (r) => Convert.ChangeType(r, to);
                if (allMethods != null) allMethods.Add(meth);
                else return meth;
            }
            // search for toWhatnot (very bad should be done last)
            foreach (MethodInfo mi in from.GetMethods(BindingFlagsInstance))
            {
                if (!mi.IsStatic)
                {
                    var ps = mi.GetParameters();
                    if (ps.Length == 0)
                    {
                        if (!to.IsAssignableFrom(mi.ReturnType)) continue;
                        //Type pt = ps[0].ParameterType;
                        //if (pt.IsAssignableFrom(from))
                        {
                            MethodInfo info = mi;
                            meth = (r) => info.Invoke(r, ZERO_OBJECTS);
                            if (allMethods != null) allMethods.Add(meth);
                            else return meth;
                        }
                    }
                }
            }
            // search for to.Whatnot (very very bad should be done last)
            if (pc != null)
            {
                meth = null;
                int fieldCount = 0;
                foreach (var f in to.GetFields(BindingFlagsInstance))
                {
                    fieldCount++;
                    if (fieldCount > 1)
                    {
                        // too many fields
                        break;
                    }
                    FieldInfo info = f;
                    meth = (r) =>
                               {
                                   var ret = pc.Invoke(null);
                                   info.SetValue(ret, r);
                                   return ret;
                               };
                }
                if (fieldCount == 1 && meth != null)
                {
                    if (allMethods != null) allMethods.Add(meth);
                    else return meth;
                }
            }

            return sysmeth;
        }


        /// <summary>
        /// This finds things like op_Implicit/op_Explicition to
        /// </summary>
        /// <param name="to"></param>
        /// <param name="srch"></param>
        /// <param name="from"></param>
        /// <param name="allMethods"></param>
        /// <param name="onlyConverionAttribute"></param>
        /// <returns></returns>
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
            PlType plType = o.PlType;
            switch (plType)
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
                        if (plType == PlType.PlAtom && o.Name == "[]")
                        {
                            return CastCompoundTerm(o.Name, o.Arity, o, o, pt);
                        }
                        string s = (string)o;
                        if (pt == null) return s;
                        var constructor = pt.GetConstructor(ONE_STRING);
                        if (constructor != null)
                        {
                            return constructor.Invoke(new object[] { s });
                        }
                        foreach (var m in pt.GetMethods(BindingFlagsJustStatic))
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
            if (key == "[]/0")
            {
                if (pt != null)
                {
                    if (pt.IsArray)
                    {
                        return Array.CreateInstance(pt.GetElementType(), 0);
                    }
                    return MakeDefaultInstance(pt);
                }
                Warn("Not sure what to convert `[]` too");
                return null;
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
                    Debug("maybe this is a string {0}", orig);
                }
                else
                {
                    if (pt == null)
                    {
                        var o1 = GetInstance(arg1);
                        if (o1 != null)
                        {
                            Warn(" send a list into cliGet0 ", orig);
                            bool found;
                            var res = cliGet0(arg1, orig.Arg(1), o1.GetType(), out found);
                            if (found) return res;
                        }
                        Warn("Return as array of object[]?", orig);
                        return CreateArrayOfType(orig, typeof (object[]));
                    }
                    else
                    {
                        if (!typeof (IEnumerable).IsAssignableFrom(pt))
                        {
                            Warn("Return as collection?", orig);
                        }
                        return CreateCollectionOfType(orig, pt);
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
                        CommitPostCall(postCallHook);
                        return retval;
                    }
                }
            }
            // Debug("Get Instance fallthru");
            MemberInfo[] ofs = GetStructFormat(t);
            return CreateInstance(t, ofs, orig, 1);
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


}
