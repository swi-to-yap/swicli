/*  $Id$
*  
*  Project: Swicli.Library - Two Way Interface for .NET and MONO to SWI-Prolog
*  Author:        Douglas R. Miles
*                 Uwe Lesta (SbsSW.SwiPlCs classes)
*  E-mail:        logicmoo@gmail.com
*  WWW:           http://www.logicmoo.com
*  Copyright (C): 2008, Uwe Lesta SBS-Softwaresysteme GmbH, 
*     2010-2012 LogicMOO Developement
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
using System.Reflection;
using Class = System.Type;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SbsSW.SwiPlCs;

using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = Swicli.Library.PrologClient;

namespace Swicli.Library
{
    public partial class PrologClient
    {
        private static IEnumerable CreateCollectionOfType(CycFort arrayValue, Type arrayType)
        {
            if (arrayType.IsArray)
            {
                return CreateArrayOfType(arrayValue, arrayType);
            }
            if (!typeof (IEnumerable).IsAssignableFrom(arrayType))
            {
                Warn("Return as collection?", arrayValue);
            }
            IEnumerable al = (IEnumerable) MakeDefaultInstance(arrayType);
            Type elementType = typeof (object);
            if (arrayType == null || arrayType.IsGenericTypeDefinition) arrayType = al.GetType();
            bool isGeneric = arrayType.IsGenericType;
            if (isGeneric)
            {
                elementType = arrayType.GetGenericArguments()[0];
            }
            if (arrayType.GetArrayRank() != 1)
            {
                Warn("Non rank==1 " + arrayType);
            }
            PlTerm[] terms = ToTermArray(arrayValue);
            int termsLength = terms.Length;
            MethodInfo[] reflectCache;
            lock (reflectCachesForTypes)
            {
                if (!reflectCachesForTypes.TryGetValue(arrayType, out reflectCache))
                {
                    reflectCache = reflectCachesForTypes[arrayType] = new MethodInfo[6];
                }
            }
            if (!isGeneric)
            {
                for (int i = 0; i < termsLength; i++)
                {
                    PlTerm term = terms[i];
                }
                return al;
            }
            MethodInfo gMethod = reflectCache[6];
            if (gMethod == null)
            {
                gMethod = reflectCache[6] = typeof (PrologClient).GetMethod("cliAddElement", BindingFlagsJustStatic)
                                                .MakeGenericMethod(new[] {elementType});
            }
            for (int i = 0; i < termsLength; i++)
            {
                PlTerm term = terms[i];
                gMethod.Invoke(null, new object[] {al, term, arrayType, elementType, reflectCache});
            }
            return al;
        }

        public static bool cliAddElement<T>(IEnumerable<T> enumerable, PlTerm term, Type type, Type elementType, MethodInfo[] reflectCache)
        {
            if (enumerable is ICollection<T>)
            {
                var al = (ICollection<T>) enumerable;
                al.Add((T) CastTerm(term, elementType));
                return true;
            }
            return cliAddElementFallback(enumerable, term, type, elementType, reflectCache);
        }

        public static bool cliAddElementFallback(IEnumerable enumerable, PlTerm term, Type type, Type elementType, MethodInfo[] reflectCache) {
            if (enumerable is ICollection)
            {
                var al = (ICollection)enumerable;
                return cliSetElementFallback(al, al.Count, term, type, elementType, reflectCache);
            }
            // this should cause an error (thats ok)
            return cliSetElementFallback(enumerable, -1, term, type, elementType, reflectCache);
        }

        public static bool cliSetElement<T>(IEnumerable<T> enumerable, int i, PlTerm term, Type type, Type elementType, MethodInfo[] reflectCache)
        {
            if (enumerable is IList<T>)
            {
                var al = (IList<T>) enumerable;
                al[i] = (T) CastTerm(term, elementType);
                return true;
            }
            return cliSetElementFallback(enumerable, i, term, type, elementType, reflectCache);
        }

        /// <summary>
        /// this is just stuffed with a random MethodInfo
        /// </summary>
        private static MethodInfo MissingMI = null;


        /// <summary>
        /// 0 = SetValue
        /// 1 = Count
        /// 2 = add/append
        /// 3 = GetValue
        /// 4 = removeValue
        /// 5 = removeAll
        /// 6 = cliAddElementGeneric
        /// </summary>
        private static readonly Dictionary<Type,MethodInfo[]> reflectCachesForTypes = new Dictionary<Type, MethodInfo[]>();

        public static bool cliSetElementFallback(IEnumerable enumerable, int i, PlTerm term, Type type, Type elementType, MethodInfo[] reflectCache) {

            if (enumerable is Array)
            {
                var al = enumerable as Array;
                al.SetValue(CastTerm(term, elementType), i);
                return true;
            }
            if (enumerable is IList)
            {
                var al = enumerable as IList;
                al[i] = CastTerm(term, elementType);
                return true;
            }             
            // do it all via bad reflection
            ParameterInfo[] pt = null;

            if (reflectCache[0] == null) foreach (var mname in new[] { "set_Item", "SetValue", "Set" /*, "Insert", "InsertAt"*/})
            {
                MethodInfo mi = type.GetMethod(mname, BindingFlagsInstance);
                if (mi == null) continue;
                pt = mi.GetParameters();
                if (pt.Length != 2) continue;
                reflectCache[0] = mi;
                break;
            }

            MethodInfo reflectCache0 = reflectCache[0];
            if (reflectCache0 != null && reflectCache0 != MissingMI)
            {
                pt = pt ?? reflectCache0.GetParameters();
                bool indexIsFirst = false;
                if (pt[0].ParameterType == typeof(int))
                {
                    indexIsFirst = true;
                }
                if (indexIsFirst)
                {
                    reflectCache0.Invoke(enumerable, new object[] { i, CastTerm(term, elementType) });
                }
                else
                {
                    reflectCache0.Invoke(enumerable, new object[] { CastTerm(term, elementType), i });
                }
                return true;
            }

            // get the Count
            int size = -1;
            if (reflectCache[1] == null) foreach (var mname in new string[] { "Count", "Size", "Length" })
            {
                MethodInfo mi = type.GetMethod(mname, BindingFlagsInstance);
                if (mi != null)
                {
                    reflectCache[1] = mi;
                    break;
                }
            }
            reflectCache0 = reflectCache[1];
            if (reflectCache0 != null && reflectCache0 != MissingMI)
            {
                return Error("Cant even find Count on {0}", enumerable);
            }
            size = (int)reflectCache[1].Invoke(enumerable, ZERO_OBJECTS);
            // append elements
            if (i != size)
            {
                return Error("wrong size for element {0} on {1} with count of {2}", i, enumerable, size);
            }
            if (reflectCache[2] == null) foreach (var mname in new string[] { "Add", "Append" })
            {
                MethodInfo mi = type.GetMethod(mname, BindingFlagsInstance);
                if (mi == null) continue;
                pt = mi.GetParameters();
                if (pt.Length != 1) continue;
                reflectCache[2] = mi;
                break;
            }
            reflectCache0 = reflectCache[2];
            if (reflectCache0 != null && reflectCache0 != MissingMI)
            {
                reflectCache0.Invoke(enumerable, new object[] {CastTerm(term, elementType)});
                return true;
            }
            return Error("No append method on {0}", enumerable);
        }
    }

}
