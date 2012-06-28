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
using ikvm.extensions;
using IKVM.Internal;
using ikvm.runtime;
using java.net;
using java.util;
//using jpl;
using jpl;
using Hashtable = java.util.Hashtable;
using ClassLoader = java.lang.ClassLoader;
using Class = java.lang.Class;
using sun.reflect.misc;
using Util = ikvm.runtime.Util;
#else
using SbsSW.SwiPlCs.Callback;
using Class = System.Type;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using SbsSW.SwiPlCs;
using SbsSW.SwiPlCs.Exceptions;
using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = Swicli.Library.PrologClient;

namespace Swicli.Library
{
    public partial class PrologClient
    {
        [PrologVisible(Name = "cli_load_type", Arity = 1, TypeOf = null)]
        private static void LoadType(Class t)
        {
            lock (TypesLoaded)
            {
                if (TypesLoaded.Contains(t) || TypesLoading.Contains(t)) return;
                TypesLoading.Add(t);
                AddForeignMethods(t);
                TypesLoading.Remove(t);
                TypesLoaded.Add(t);
            }
        }

        public static Class GetTypeThrowIfMissing(CycFort clazzSpec)
        {
            Type fi = GetType(clazzSpec);
            if (fi == null)
            {
                throw new PlException("cant find class" + clazzSpec);
            }
            return fi;
        }
        public static Class GetType(CycFort clazzSpec)
        {
            return GetType(clazzSpec, false);
        }
        public static Class GetType(CycFort clazzSpec, bool canBeObjects)
        {
            if (clazzSpec.IsVar)
            {
                Error("GetType IsVar {0}", clazzSpec);
                return null;
            }
            if (IsTaggedObject(clazzSpec))
            {
                object tagObj = tag_to_object(clazzSpec[1].Name);
                var r = tagObj as Type;
                if (r != null) return r;
                if (!canBeObjects)
                {
                    Warn("cant find tagged object as class: {0}=>{1}", clazzSpec, tagObj);
                }
                if (tagObj != null)
                {
                    return tagObj.GetType();
                }
                return null;
            }
            Type type = null;
            if (clazzSpec.IsAtom || clazzSpec.IsString)
            {
                if (canBeObjects) return typeof (string);
                string name = (string)clazzSpec;
                type = ResolveType(name);
                if (type != null) return type;
                if (!canBeObjects)
                {
                    Warn("cant find atom/string as class: {0}", clazzSpec);
                    type = ResolveType(name);
                }
                return null;
            }
            if (clazzSpec.IsCompound)
            {
                string clazzName = clazzSpec.Name;
                int arity = clazzSpec.Arity;
                if (clazzName == "arrayOf")
                {
                    if (arity != 1)
                    {
                        return GetType(clazzSpec[1]).MakeArrayType(clazzSpec[2].intValue());
                    }
                    return GetType(clazzSpec[1]).MakeArrayType();
                }
                if (clazzName == "type")
                {
                    return (GetInstance(clazzSpec[1]) ?? NEW_OBJECTFORTYPE).GetType();
                }
                if (clazzName == "static" || clazzName == "typeof")
                {
                    return GetType(clazzSpec[1]);
                }
                if (clazzName == "{}")
                {
                    return typeof (CycFort);
                }
                if (clazzName == "pointer")
                {
                    return GetType(clazzSpec[1]).MakePointerType();
                }
                if (clazzName == "byref")
                {
                    return GetType(clazzSpec[1]).MakeByRefType();
                }
                if (clazzName == "nullable")
                {
                    return typeof(Nullable<>).MakeGenericType(new[] { GetType(clazzSpec[1]) });
                }
                type = ResolveType(clazzName + "`" + arity);
                if (type != null)
                {
                    // 'Dictionary'('Int32','string').
                    if (type.IsGenericType)
                    {
                        Type[] genr = type.GetGenericArguments();
                        Type[] genrc = null;
                        Type genrb = null;
                        try
                        {
                            if (type.IsGenericParameter)
                            {
                                genrc = type.GetGenericParameterConstraints();
                            }
                        }
                        catch (Exception e)
                        {
                            Warn("GetGenericParameterConstraints: {0}", e);
                        }
                        try
                        {
                            genrb = type.GetGenericTypeDefinition();
                        }
                        catch (Exception e)
                        {
                            Warn("GetGenericTypeDefinition: {0}", e);
                        }

                        if (arity == genr.Length)
                        {
                            var vt = GetParamSpec(clazzSpec, false);
                            return type.MakeGenericType(vt);
                        }
                    }
                    //  return type;
                }
                string key = clazzName + "/" + arity;
                lock (FunctorToLayout)
                {
                    PrologTermLayout pltl;
                    if (FunctorToLayout.TryGetValue(key, out pltl))
                    {
                        return pltl.ObjectType;
                    }
                }
                lock (FunctorToRecomposer)
                {
                    PrologTermRecomposer layout;
                    if (FunctorToRecomposer.TryGetValue(key, out layout))
                    {
                        return layout.ToType;
                    }
                }
                WarnMissing("cant find compound as class: " + clazzSpec);
            }
            object toObject = GetInstance(clazzSpec);
            if (toObject is Type) return (Type)toObject;
            if (toObject != null)
            {
                return toObject.GetType();
            }
            Warn("@TODO cant figure type from {0}", clazzSpec);
            return typeof(object);
            //return null;
        }

        [PrologVisible(ModuleName = ExportModule)]
        public static bool cliFindType(CycFort clazzSpec, CycFort classRef)
        {
            //            if (term1.IsAtom)
            {
                string className = (string)clazzSpec;//.Name;
                Type s1 = GetType(clazzSpec);
                if (s1 != null)
                {
                    var c = s1;// ikvm.runtime.Util.getFriendlyClassFromType(s1);
                    if (c != null)
                    {
                       // ConsoleTrace("name:" + className + " type:" + s1.FullName + " class:" + c);
                        return UnifyTagged(c, classRef);
                    }
                    ConsoleTrace("cant getFriendlyClassFromType " + s1.FullName);
                    return false;
                }
                ConsoleTrace("cant ResolveType " + className);
                return false;
            }
            ConsoleTrace("cant IsAtom " + clazzSpec);
            return false;
        }

        public static void ConsoleTrace(object s)
        {
            try
            {
                Console.WriteLine(s);
            }
            catch (Exception)
            {
            }  
        }

        [PrologVisible(ModuleName = ExportModule)]
        public static bool cliFindClass(CycFort clazzName, CycFort clazzObjectOut)
        {
            if (clazzName.IsAtom)
            {
                string className = clazzName.Name;
                Type c = ResolveClass(className);
                if (c != null)
                {
                    ConsoleTrace("cliFindClass:" + className + " class:" + c);
                    string tag = object_to_tag(c);
                    return AddTagged(clazzObjectOut.TermRef, tag) != 0;
                }
                ConsoleTrace("cant ResolveClass " + className);
                return false;
            }
            Type t = GetType(clazzName);
            if (t != null)
            {
                Type c = null;
#if USE_IKVM
                c = ikvm.runtime.Util.getFriendlyClassFromType(t);
#else
                c = t;
#endif
                string tag = object_to_tag(c);
                return AddTagged(clazzObjectOut.TermRef, tag) != 0;
            }
            return false;
        }

        private static IDictionary<string, Class> ShortNameType;
        private static readonly Dictionary<Class, string> TypeShortName = new Dictionary<Class, string>();
        private static object NEW_OBJECTFORTYPE = new object();

        private static CycFort typeToSpec(Class type)
        {
            if (type == null) return PLNULL;
            if (type.IsArray && type.HasElementType)
            {
                if (type.GetArrayRank() != 1)
                {
                    return PlC("arrayOf", typeToSpec(type.GetElementType()), ToProlog(type.GetArrayRank()));
                }
                return PlC("arrayOf", typeToSpec(type.GetElementType()));
            }
            if (type.IsGenericParameter)
            {
                Type[] gt = type.GetGenericParameterConstraints();
                return PlC("<" + type.FullName ?? type.Name + ">", ToPlTermVSpecs(gt));
            }
            if (type.IsPointer)
            {
                Type gt = type.GetElementType();
                return PlC("pointer", typeToSpec(gt));
            }
            if (type.IsByRef)
            {
                Type gt = type.GetElementType();
                return PlC("byref", typeToSpec(gt));
            }
            // @todo if false , use IsGenericType
            if (false) if (typeof(Nullable<>).IsAssignableFrom(type))
            {
                Error("@todo Not Implemented NULLABLE");
                Type gt = type.GetElementType();
                return PlC("nullable", typeToSpec(gt));
            }

            if (type.IsGenericType )
            {
                Type gt = type.GetGenericTypeDefinition();
                Type[] gtp = type.GetGenericArguments();
                PlTermV vt = ToPlTermVSpecs(gtp);
                string typeName = type.FullName ?? type.Name;
                int gtpLength = gtp.Length;
                int indexOf = typeName.IndexOf("`" + gtpLength);
                if (indexOf > 0)
                {
                    typeName = typeName.Substring(0, indexOf);
                }
                else
                {
                    Debug("cant chop arity " + gtpLength + " off string '" + typeName + "' ");
                }
                return PlC(typeName, vt);
            }
            if (type.HasElementType)
            {
                string named = typeToName(type);
                Error("@todo Not Implemented " + named);
                Type gt = type.GetElementType();
                if (gt == type) gt = typeof(object);
                return PlC("elementType", PlTerm.PlAtom(named), typeToSpec(gt));
            }
            if (type.IsSpecialName || string.IsNullOrEmpty(type.Name) || string.IsNullOrEmpty(type.FullName) || string.IsNullOrEmpty(type.Namespace))
            {
                string named = typeToName(type);
                Error("@todo Not Implemented " + named);
                Type gt = type.UnderlyingSystemType;
                if (gt == type) gt = typeof (object);
                return PlC("static", PlTerm.PlAtom(named), typeToSpec(gt));
            }
            return PlTerm.PlAtom(typeToName(type));
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliGetType(CycFort valueIn, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetType(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            object val = GetInstance(valueIn);
            if (val == null)
            {
                Error("Cannot get object for {0}", valueIn);
                return true;
            }
            return valueOut.FromObject((val.GetType()));
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliGetClass(CycFort valueIn, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetClass(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            object val = GetInstance(valueIn);
            // extension method
#if USE_IKVM
            return valueOut.FromObject((val.getClass()));
#else
            return valueOut.FromObject((val.GetType()));
#endif
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliClassFromType(CycFort valueIn, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliClassFromType(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type val = GetType(valueIn);
            if (val == null) return false;
#if USE_IKVM
            Class c = ikvm.runtime.Util.getFriendlyClassFromType(val);
            return valueOut.FromObject((c));
#else
            return valueOut.FromObject((val));
#endif
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTypeFromClass(CycFort valueIn, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliTypeFromClass(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type val = GetType(valueIn);
            if (val == null) return false;
#if USE_IKVM
            Type c = ikvm.runtime.Util.getInstanceTypeFromClass(val);
            return valueOut.FromObject((c));
#else
            return valueOut.FromObject(val);
#endif
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliShorttype(CycFort valueName, CycFort valueIn)
        {
            if (!valueName.IsString && !valueName.IsAtom) return Warn("valueName must be string or atom {0}", valueName);
            string name = valueName.Name;
            Type otherType;
            lock (ShortNameType)
            {
                if (ShortNameType.TryGetValue(name, out otherType))
                {
                    if (valueIn.IsNumber)
                    {
                        ShortNameType.Remove(name);
                        TypeShortName.Remove(otherType);
                        return true;
                    }
                    if (valueIn.IsVar)
                    {
                        return valueIn.UnifyAtom(otherType.FullName);
                    }
                    Type val = GetType(valueIn);
                    if (val == otherType) return true;
                    return false;
                }
                else
                {
                    if (valueIn.IsNumber)
                    {
                        return true;
                    }
                    if (valueIn.IsVar)
                    {
                        return true;
                    }
                    Type val = GetType(valueIn);
                    if (val == null) return false;
                    ShortNameType[name] = val;
                    TypeShortName[val] = name;
                    return true;
                }
            }
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliGetClassname(CycFort valueIn, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetClassname(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type val = CastTerm(valueIn, typeof(Type)) as Type;
            if (val == null) return false;

#if USE_IKVM
            return valueOut.Unify(val.getName());
#else
            return valueOut.Unify(val.GetType().Name);
#endif
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliGetTypeFullname(CycFort valueIn, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliGetTypeFullname(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            Type val = CastTerm(valueIn, typeof(Type)) as Type;
            if (val == null) return false;
            return valueOut.Unify(val.FullName);
        }

        private static string typeToName(Class type)
        {
            if (type.IsArray && type.HasElementType)
            {
                return typeToSpec(type.GetElementType()) + "[]";
            }
            lock (ShortNameType)
            {
                string shortName;
                if (TypeShortName.TryGetValue(type, out shortName))
                {
                    return shortName;
                }
                string typeName = type.Name;
                Type otherType;
                if (ShortNameType.TryGetValue(type.Name, out otherType))
                {
                    if (type == otherType)
                    {
                        return typeName;
                    }
                    return type.FullName;
                }
                ShortNameType[typeName] = type;
                TypeShortName[type] = typeName;
                return typeName;
            }
        }

        private static Class ResolveClass(string name)
        {
            if (name == "@" || name == "$cli_object" || name == "array" || name == null) return null;
            Type t = ResolveClassAsType(name);
#if USE_IKVM
            Class c = ikvm.runtime.Util.getFriendlyClassFromType((Type)t);
            return c;
#else
            return t;
#endif

        }
        private static Class ResolveClassAsType(string name)
        {
            Type s1 = ResolveType(name);
            if (s1 != null) return s1;
            if (name.EndsWith("[]"))
            {
                Type t1 = ResolveClassAsType(name.Substring(0, name.Length - 2));
                return t1.MakeArrayType();
            }
            var name2 = name.Replace("/", ".");
            if (name2 != name)
            {
                s1 = ResolveType(name2);
                if (s1 != null) return s1;
            }
            name2 = name.Replace("cli.", "");
            if (name2 != name)
            {
                s1 = ResolveType(name2);
                if (s1 != null) return s1;
            }
            return null;
        }

        static readonly private Dictionary<string, Class> typeCache = new Dictionary<string, Class>();
        private static Class ResolveType(string name)
        {
            lock (typeCache)
            {
                Type type;
                if (!typeCache.TryGetValue(name, out type))
                {
                    return typeCache[name] = ResolveType0(name);
                }
                return type;
            }
        }

        private static Class ResolveType0(string name)
        {
            if (name == "@" || name == "[]" || name == "$cli_object" || name == "array" || name == null) return null;
            if (name.EndsWith("[]"))
            {
                Type t = ResolveType(name.Substring(0, name.Length - 2));
                return t.MakeArrayType();
            }
            if (name.EndsWith("?"))
            {
                return typeof(Nullable<>).MakeGenericType(new[] { ResolveType(name.Substring(0, name.Length - 1)) });
            }
            if (name.EndsWith("&"))
            {
                Type t = ResolveType(name.Substring(0, name.Length - 1));
                return t.MakeByRefType();
            }
            if (name.EndsWith("*"))
            {
                Type t = ResolveType(name.Substring(0, name.Length - 1));
                return t.MakePointerType();
            }
            var s1 = ResolveType1(name);
            if (s1 != null) return s1;
            var name2 = name.Replace("/", ".");
            if (name2 != name)
            {
                s1 = ResolveType1(name2);
                if (s1 != null) return s1;
            }
            name2 = name.Replace("cli.", "");
            if (name2 != name)
            {
                s1 = ResolveType1(name2);
                if (s1 != null) return s1;
            }
            return null;
        }

        public static Class ResolveType1(string typeName)
        {
            Type type = null;
            if (!typeName.Contains("."))
            {
                lock (ShortNameType)
                {
                    if (ShortNameType.TryGetValue(typeName, out type))
                    {
                        return type;
                    }
                }
            }
            type = type ?? Type.GetType(typeName, false, false) ?? Type.GetType(typeName, false, true);
            if (type == null)
            {
                foreach (Assembly loaded in AssembliesLoaded)
                {
                    Type t = loaded.GetType(typeName, false);
                    if (t != null) return t;
                }
                Type obj = null;
#if USE_IKVM
                try
                {
                    obj = Class.forName(typeName);
                }
                catch (java.lang.ClassNotFoundException e)
                {
                }
                catch (Exception e)
                {
                }
                if (obj != null)
                {
                    type = ikvm.runtime.Util.getInstanceTypeFromClass((Class)obj);
                }
#endif
                if (type == null)
                {
                    type = getPrimitiveType(typeName);
                }
                if (type == null)
                {
                    type = Type.GetTypeFromProgID(typeName);
                } if (type == null)
                {
                    try
                    {
                        type = Type.GetTypeFromCLSID(new Guid(typeName));
                    }
                    catch (FormatException)
                    {
                    }
                }
            }
            return type;
        }

        public static Class getPrimitiveType(String name)
        {
            if (name.StartsWith("["))
            {
                Type t = ResolveType(name.Substring(1));
                return t.MakeArrayType();
            }
            switch (name)
            {
                case "byte":
                case "B":
                case "uint8":
                case "ubyte":
                    return typeof(byte);
                case "int16":
                    return typeof(Int16);
                case "int":
                case "int32":
                case "I":
                    return typeof(int);
                case "long":
                case "int64":
                case "J":
                    return typeof(long);
                case "short":
                case "S":
                    return typeof(short);
                case "sbyte":
                case "int8":
                    return typeof(sbyte);
                case "uint":
                case "uint32":
                    return typeof(uint);
                case "uint16":
                    return typeof(UInt16);
                case "uint64":
                case "ulong":
                    return typeof(ulong);
                case "ushort":
                    return typeof(ushort);
                case "decimal":
                    return typeof(decimal);
                case "double":
                case "D":
                    return typeof(double);
                case "float":
                case "F":
                    return typeof(float);
                case "object":
                    return typeof(object);
                case "string":
                    return typeof(string);
                case "void":
                case "V":
                    return typeof(void);
                case "char":
                case "C":
                    return typeof(char);
                case "bool":
                case "boolean":
                case "bit":
                case "Z":
                    return typeof(bool);
                default:
                    return null;
            }
        }
    }
}