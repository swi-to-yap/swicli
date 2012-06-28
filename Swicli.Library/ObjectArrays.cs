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
        /// <summary>
        /// ?- cliNewArray(long,10,Out),cliToString(Out,Str).
        /// </summary>
        /// <param name="clazzSpec"></param>
        /// <param name="rank"></param>
        /// <param name="valueOut"></param>
        /// <returns></returns>
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliArrayToTerm(CycFort arrayValue, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = CycFort.PlVar();
                return cliArrayToTerm(arrayValue, plvar) && SpecialUnify(valueOut, plvar);
            }
            object getInstance = GetInstance(arrayValue);
            if (getInstance == null) return valueOut.Unify(PLNULL);
            Array value = GetArrayValue(getInstance);
            if (value == null)
            {
                Error("Cant find array from {0} as {1}", arrayValue, getInstance.GetType());
                return false;
            }
            int len = value.Length;
            Type arrayType = value.GetType();
            var termv = NewPlTermV(len);
            int rank = arrayType.GetArrayRank();
            if (rank != 1)
            {
                var indexesv = new PlTermV(rank);
                for (int i = 0; i < rank; i++)
                {
                    indexesv[i].Put(value.GetLength(i));
                }
                var idxIter = new ArrayIndexEnumerator(value);
                int putAt = 0;
                while (idxIter.MoveNext())
                {
                    bool pf = termv[putAt++].FromObject((value.GetValue(idxIter.Current)));
                    if (!pf)
                    {
                        return false;
                    }
                }
                return /// array/3 
                    valueOut.Unify(PlC("array", typeToSpec(arrayType), PlC("indexes", indexesv), PlC("values", termv)));
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    bool pf = termv[i].FromObject((value.GetValue(i)));
                    if (!pf)
                    {
                        return false;
                    }
                }
                return valueOut.Unify(PlC("array", typeToSpec(arrayType.GetElementType()), PlC("values", termv)));
            }
        }

        [PrologVisible]
        [PrologTest]
        static public bool cliTestArrayToTerm1(CycFort valueOut)
        {
            return cliArrayToTerm(ToProlog(new[] { 1, 2, 3, 4, }), valueOut);
        }
        [PrologVisible]
        [PrologTest]
        static public bool cliTestArrayToTerm2(CycFort valueOut)
        {
            return cliArrayToTerm(ToProlog(new[, ,] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } }), valueOut);
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliArrayToTermlist(CycFort arrayValue, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliArrayToTermlist(arrayValue, plvar) && SpecialUnify(valueOut, plvar);
            }

            object getInstance = GetInstance(arrayValue);
            if (getInstance == null) return valueOut.Unify(PLNULL);
            Array value = GetArrayValue(getInstance);
            if (value == null)
            {
                Error("Cant find array from {0} as {1}", arrayValue, getInstance.GetType());
                return false;
            }
            Type arrayType = value.GetType();
            if (arrayType.GetArrayRank() != 1)
            {
                Error("Non rank==1 " + arrayType);
            }
            int len = value.Length;
            var termv = ATOM_NIL;
            for (int i = len - 1; i >= 0; i--)
            {
                termv = PlC(".", ToProlog((value.GetValue(i))), termv);
            }
            //Type et = value.GetType().GetElementType();
            return valueOut.Unify(termv);
        }
        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTermToArray(CycFort arrayValue, CycFort valueOut)
        {
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliTermToArray(arrayValue, plvar) && SpecialUnify(valueOut, plvar);
            }
            if (arrayValue.Name == "array")
            {
                return valueOut.FromObject(GetInstance(arrayValue));
            }
            Type elementType = ResolveType(arrayValue.Name);
            if (elementType == null)
            {
                Error("Cant find vector from {0}", arrayValue);
                return false;
            }
            var value = CreateArrayOfType(arrayValue, elementType.MakeArrayType());
            return valueOut.FromObject((value));
        }

        private static Array GetArrayValue(object getInstance)
        {
            if (getInstance == null) return null;
            lock (getInstance)
            {
                try
                {
                    return GetArrayValue0(getInstance);
                }
                catch (Exception ex)
                {
                    WriteException(ex);
                    throw;
                }
            }
        }
        private static Array GetArrayValue0(object getInstance)
        {
            if (getInstance is Array)
            {
                return (Array)getInstance;
            }
            Type t = getInstance.GetType();
            Type et = typeof(object);
            if (t.IsGenericType)
            {
                Type[] typeArguments = t.GetGenericArguments();
                if (typeArguments.Length == 1)
                {
                    et = typeArguments[0];
                }
            }
            if (getInstance is ArrayList)
            {
                return ((ArrayList)getInstance).ToArray(et);
            }
            if (getInstance is ICollection)
            {
                var collection = ((ICollection)getInstance);
                int count = collection.Count;
                var al = Array.CreateInstance(et, count);
                try
                {
                    collection.CopyTo(al, 0);
                    return al;
                }
                catch (Exception ex)
                {
                    string warn = "CopyTo " + ex;
                    Warn(warn);
                    int count2 = collection.Count;
                    if (count2 != count)
                    {
                        ConsoleWriteLine("Collection Modified while in CopyTo! " + count + "->" + count2 + " of " + collection);
                        throw;
                    }
                    int index = 0;
                    foreach (var e in collection)
                    {
                        al.SetValue(e, index++);
                    }
                    return al;
                }
            }
            if (getInstance is IEnumerable)
            {
                var collection = ((IEnumerable)getInstance).GetEnumerator();
                var al = new ArrayList();
                while (collection.MoveNext())
                {
                    al.Add(collection.Current);
                }
                return al.ToArray(et);
            }
            else if (getInstance is IEnumerator)
            {
                var collection = ((IEnumerator)getInstance);
                var al = new ArrayList();
                while (collection.MoveNext())
                {
                    al.Add(collection.Current);
                }
                return al.ToArray(et);
            }
            else
            {
                // this one is probly null
                return getInstance as Array;
            }
        }
       
        public static CycFort[] ToTermArray(IEnumerable<CycFort> enumerable)
        {
            if (enumerable is PlTerm[]) return (PlTerm[])enumerable;
            if (enumerable is PlTermV)
            {
                PlTermV tv = (PlTermV)enumerable;
                return tv.ToArray();
            }
            if (enumerable is PlTerm)
            {
                // I guess IsList makes a copy
                PlTerm tlist = (PlTerm)enumerable;
                if (tlist.IsVar) return new PlTerm[] { tlist };
                if (tlist.IsList)
                {
                    enumerable = tlist.Copy();
                }
                if (tlist.Name == "{}")
                {
                    var t = tlist.Arg(0);
                    var terms = new System.Collections.Generic.List<PlTerm>();
                    while (t.Arity == 2)
                    {
                        terms.Add(t.Arg(0));
                        t = t.Arg(1);
                    }
                    // last Item
                    terms.Add(t);
                    return terms.ToArray();
                }
                if (tlist.IsAtomic)
                {
                    if (tlist.IsAtom && tlist.Name == "[]") return new PlTerm[0];
                    return new PlTerm[] { tlist };
                } 
            }
            return enumerable.ToArray();
        }
        /// <summary>
        /// Construct an array of some type
        /// </summary>
        /// <param name="arrayValue"></param>
        /// <param name="arrayType">The parent array type .. not the Element type</param>
        /// <returns></returns>
        private static Array CreateArrayOfType(CycFort arrayValue, CycFort indexes, Type arrayType)
        {
            if (!arrayType.IsArray)
            {
                Error("Not Array Type! " + arrayType);
            }
            Type elementType = arrayType.GetElementType();
            int rank = arrayType.GetArrayRank();
            PlTerm[] iterms = ToTermArray(indexes);
            int[] lengths = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                lengths[i] = iterms[i].intValue();
            }
            PlTerm[] terms = ToTermArray(arrayValue);
            int termsLength = terms.Length;
            Array al = Array.CreateInstance(elementType, lengths);
            var idxIter = new ArrayIndexEnumerator(al);
            for (int i = 0; i < termsLength; i++)
            {
                idxIter.MoveNext();
                PlTerm term = terms[i];
                al.SetValue(CastTerm(term, elementType), idxIter.Current);
            }
            return al;
        }
        private static Array CreateArrayOfType(CycFort arrayValue, Type arrayType)
        {
            if (!arrayType.IsArray)
            {
                Error("Not Array Type! " + arrayType);
            }
            Type elementType = arrayType.GetElementType();
            if (arrayType.GetArrayRank() != 1)
            {
                Warn("Non rank==1 " + arrayType);
            }
            PlTerm[] terms = ToTermArray(arrayValue);
            int termsLength = terms.Length;
            Array al = Array.CreateInstance(elementType, termsLength);
            for (int i = 0; i < termsLength; i++)
            {
                PlTerm term = terms[i];
                al.SetValue(CastTerm(term, elementType), i);
            }
            return al;
        }
    }

    public class ArrayIndexEnumerator : IEnumerator<int[]>
    {
        private readonly int rank;
        private readonly int[] lowers;
        private readonly int[] uppers;
        private readonly int[] idx;
        private readonly int len = 1;
        private int at = -1;

        public ArrayIndexEnumerator(Array value)
        {
            Type arrayType = value.GetType();
            rank = arrayType.GetArrayRank();
            uppers = new int[rank];
            lowers = new int[rank];
            idx = new int[rank];

            this.len = 1;
            for (int i = 0; i < rank; i++)
            {
                int high = uppers[i] = value.GetUpperBound(i);
                int low = lowers[i] = value.GetLowerBound(i);
                if (low != 0)
                {
                    PrologClient.Error("LowerBound !=0 in " + arrayType);
                }
                int lenSize = (high - low + 1);
                len *= lenSize;
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {

        }

        #endregion

        #region Implementation of IEnumerator

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. 
        ///                 </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            at++;
            if (at == 0)
            {
                return true;
            }
            if (at >= len) return false;
            for (int i = rank - 1; i >= 0; i--)
            {
                if (idx[i] < uppers[i])
                {
                    idx[i]++;
                    return true;
                }
                idx[i] = lowers[i];
            }
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. 
        ///                 </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            for (int i = 0; i < rank; i++)
            {
                idx[i] = 0;
            }
            at = -1;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public int[] Current
        {
            get
            {
                if (at < 0) throw new InvalidOperationException("forgot MoveNext");
                return idx;
            }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.
        ///                 </exception><filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion
    }

}
