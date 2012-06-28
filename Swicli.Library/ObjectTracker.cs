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
using Class = java.lang.Class;
#else
using Class = System.Type;
#endif
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SbsSW.SwiPlCs;

namespace Swicli.Library
{
    public partial class PrologClient
    {
        [ThreadStatic]
        public static bool PreserveObjectType;

        [ThreadStatic]
        static ThreadEngineObjectTracker _locallyTrackedObjects;
        static ThreadEngineObjectTracker LocallyTrackedObjects
        {
            get
            {
                if (_locallyTrackedObjects == null)
                {
                    _locallyTrackedObjects = new ThreadEngineObjectTracker();
                }
                return _locallyTrackedObjects;
            }
        }

        public static bool DebugRefs = true;
        public static bool StrictRefs = true;
        readonly static public Dictionary<object, TrackedObject> ObjToTag = new Dictionary<object, TrackedObject>();
        readonly static public Dictionary<string, TrackedObject> TagToObj = new Dictionary<string, TrackedObject>();
        public static object tag_to_object(string s)
        {
            return tag_to_object(s, false);
        }

        [PrologVisible]
        public static bool cliAddTag(PlTerm taggedObj, PlTerm tagString)
        {
            object o = GetInstance(taggedObj);
            string tagname = (string)tagString;
            lock (ObjToTag)
            {

                TrackedObject s;
                GCHandle iptr = PinObject(o);
                long adr = ((IntPtr)iptr).ToInt64();
                var hc = iptr.GetHashCode();

                s = new TrackedObject(o)
                        {
                            TagName = tagname,
                            Pinned = iptr,
                            HashCode = hc,
                            Heaped = true
                        };
                ObjToTag[o] = s;
                TagToObj[tagname] = s;

            }
            return true;
        }
        [PrologVisible]
        public static bool cliRemoveTag(PlTerm tagString)
        {
            string tagname = (string)tagString;
            TrackedObject to;

            lock (ObjToTag)
            {
                if (TagToObj.TryGetValue(tagname, out to))
                {
                    TagToObj.Remove(tagname);
                    ObjToTag.Remove(to.Value);
                    //TODO?? to.RemoveRef();
                }
            }
            return true;
        }

        public static object tag_to_object(string s, bool allowConstants)
        {
            if (s == "true") return true;
            if (s == "false") return true;
            if (s == "null") return null;
            if (string.IsNullOrEmpty(s) || s == "void" /*|| !s.StartsWith("C#")*/)
            {
                Warn("tag_to_object: {0} ", s);
                return null;
            }
            lock (ObjToTag)
            {
                TrackedObject o;
                if (TagToObj.TryGetValue(s, out o))
                {
                    LocallyTrackedObjects.AddTracking(o);
                    return o.Value;
                }
                if (DebugRefs) Warn("tag_to_object: {0}", s);
#if USE_IKVM
                return jpl.fli.Prolog.tag_to_object(s);
#else
                return null;
#endif
            }
        }
        public static string object_to_tag(object o)
        {
            if (o == null)
            {
                Warn("object_to_tag: NULL");
                return null;
            }

            Type t = o.GetType();
            if (IsStructRecomposable(t) || t.IsPrimitive)
            {
                if (DebugRefs) Debug(string.Format("object_to_tag:{0} from {1}", t, o));
            }

            lock (ObjToTag)
            {
                TrackedObject s;
                if (ObjToTag.TryGetValue(o, out s))
                {
                    LocallyTrackedObjects.AddTracking(s);
                    return s.TagName;
                }
                GCHandle iptr = PinObject(o);
                long adr = ((IntPtr)iptr).ToInt64();
                var hc = iptr.GetHashCode();
                string tagname = "C#" + adr;

                s = new TrackedObject(o)
                        {
                            TagName = tagname,
                            Pinned = iptr,
                            HashCode = hc
                        };
                ObjToTag[o] = s;
                TagToObj[tagname] = s;
                LocallyTrackedObjects.AddTracking(s);
                if (DebugRefs && ObjToTag.Count % 10000 == 0)
                {
                    PrologClient.ConsoleTrace("ObjToTag=" + ObjToTag);
                }

                return s.TagName;
            }
            //return jpl.fli.Prolog.object_to_tag(o);
        }

        private static bool IsTaggedObject(PlTerm info)
        {
            return info.IsCompound && info.Name == "@";
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliToTagged(PlTerm obj, PlTerm str)
        {
            if (!str.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliToTagged(obj, plvar) && SpecialUnify(str, plvar);
            }
            //if (obj.IsString) return str.Unify(obj);
            if (obj.IsVar) return str.Unify(obj);
            object o = GetInstance(obj);
            return UnifyTagged(o, str);
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliImmediateObject(PlTerm valueIn, PlTerm valueOut)
        {
            if (valueIn.IsVar)
            {
                if (StrictRefs) return Error("Cant find instance {0}", valueIn);
                return valueIn.Unify(valueOut);
            }
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                return cliImmediateObject(valueIn, plvar) && SpecialUnify(valueOut, plvar);
            }
            object retval = GetInstance(valueIn);
            return valueOut.FromObject(retval);
        }

        public static bool UnifyTagged(object c, PlTerm term2)
        {
            string tag = object_to_tag(c);
            var t1 = term2;
            if (t1.IsCompound)
            {
                t1 = t1[1];
            }
            else if (t1.IsVar)
            {
                return 0 != AddTagged(t1.TermRef, tag);
            }
            //var t2 = new PlTerm(t1.TermRef + 1);

            //libpl.PL_put_atom_chars(t1.TermRef + 1, tag);
            bool ret = t1.UnifyAtom(tag); // = t1;
            return ret;
        }

        private static int AddTagged(uint TermRef, string tag)
        {
            /*
            PlTerm term2 = new PlTerm(TermRef);
            var t1 = term2;
            if (t1.IsCompound)
            {
                t1 = t1[1];
            }
            else if (t1.IsVar)
            {
            }
            //var t2 = new PlTerm(t1.TermRef + 1);

            //libpl.PL_put_atom_chars(t1.TermRef + 1, tag);
            bool ret = t1.Unify(tag); // = t1;*/
            uint fid = 0;// libpl.PL_open_foreign_frame();
            uint nt = libpl.PL_new_term_ref();
            libpl.PL_cons_functor_v(nt,
                                    OBJ_1,
                                    new PlTermV(PlTerm.PlAtom(tag)).A0);
            PlTerm termValue = new PlTerm(nt);
            PlTerm termVar = new PlTerm(TermRef);
            int retcode = libpl.PL_unify(TermRef, nt);
            if (fid > 0) libpl.PL_close_foreign_frame(fid);
            if (retcode != libpl.PL_succeed)
            {
                //libpl.PL_put_term(nt, TermRef);
                if (retcode == libpl.PL_fail)
                {
                    return retcode;
                }
                return retcode;
            }
            return retcode;
        }

        protected static uint OBJ_1
        {
            get
            {
                if (_obj1 == default(uint))
                {
                    _obj1 = libpl.PL_new_functor(libpl.PL_new_atom("@"), 1);
                }
                return _obj1;
            }
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTrackerBegin(PlTerm trackerOut)
        {
            var newTracking = LocallyTrackedObjects.CreateFrame();
            return UnifyTagged(newTracking, trackerOut);
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTrackerFree(PlTerm trackerIn)
        {
            TrackedFrame tc0 = (TrackedFrame)GetInstance(trackerIn);
            if (tc0 != null)
            {
                LocallyTrackedObjects.RemoveFrame(tc0);
                return true;
            }
            return false;
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliFree(PlTerm taggedObject)
        {
            if (taggedObject.IsVar)
            {
                return false;
            }
            string tag;
            if (taggedObject.IsCompound)
            {
                tag = taggedObject[1].Name;
            }
            else if (taggedObject.IsAtom)
            {
                tag = taggedObject.Name;
            }
            else if (taggedObject.IsString)
            {
                tag = taggedObject.Name;
            }
            else
            {
                return true;
            }
            lock (TagToObj)
            {
                TrackedObject oref;
                if (TagToObj.TryGetValue(tag, out oref))
                {
                    oref.Heaped = false;
                    oref.RemoveRef();
                    if (oref.Refs > 0)
                    {
                        return RemoveTaggedObject(tag);
                    }
                    return true;
                }
                return false;
            }
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliHeap(PlTerm taggedObject)
        {
            if (taggedObject.IsVar)
            {
                return false;
            }
            string tag;
            if (taggedObject.IsCompound)
            {
                tag = taggedObject[1].Name;
            }
            else if (taggedObject.IsAtom)
            {
                tag = taggedObject.Name;
            }
            else if (taggedObject.IsString)
            {
                tag = taggedObject.Name;
            }
            else
            {
                return true;
            }
            lock (TagToObj)
            {
                TrackedObject oref;
                if (TagToObj.TryGetValue(tag, out oref))
                {
                    oref.Heaped = true;
                    return true;
                }
                return false;
            }
        }

        public static bool CantPin(object pinme)
        {
            return pinme.GetType().Name.Contains("e");
        }

        public static GCHandle PinObject(object pinme)
        {
            return GCHandle.Alloc(pinme);
#if false
            if (true) return pinme;
            try
            {
                if (CantPin(pinme))
                {
                    GCHandle.Alloc(pinme, GCHandleType.Normal);
                    return pinme;
                }
                if (!Monitor.TryEnter(pinme))
                {
                    return pinme;
                }
                Monitor.Exit(pinme);
                GCHandle gch = GCHandle.Alloc(pinme, GCHandleType.Pinned);
                GCHandle gch2 = GCHandle.Alloc(pinme, GCHandleType.Pinned);
                if (gch != gch2)
                {

                }
            }
            catch (Exception)
            {
                GCHandle gch = GCHandle.Alloc(pinme, GCHandleType.Normal);
            }
            return pinme;
#endif
        }
        public static object UnPinObject(object pinme)
        {
            try
            {
                GCHandle gch = GCHandle.Alloc(pinme, GCHandleType.Pinned);
                gch.Free();
            }
            catch (Exception)
            {
                GCHandle gch = GCHandle.Alloc(pinme, GCHandleType.Normal);
                gch.Free();
            }
            return pinme;
        }

        public static bool RemoveTaggedObject(string tag)
        {
            lock (TagToObj)
            {
                TrackedObject obj;
                if (TagToObj.TryGetValue(tag, out obj))
                {
                    //UnPinObject(obj);
                    TagToObj.Remove(tag);
                    if (obj is IDisposable)
                    {
                        try
                        {
                            ((IDisposable)obj).Dispose();
                        }
                        catch (Exception e)
                        {
                            if (DebugRefs) Warn("Dispose of {0} had problem {1}", obj, e);
                        }
                    }
                    obj.Pinned.Free();
                    return ObjToTag.Remove(obj);
                }
                return false;
            }
        }
        private static int PlObject(uint TermRef, object o)
        {
            var tag = object_to_tag(o);
            AddTagged(TermRef, tag);
            return libpl.PL_succeed;
#if plvar_pins
                PlRef oref;
                if (!objectToPlRef.TryGetValue(o, out oref))
                {
                    objectToPlRef[o] = oref = new PlRef();
                    oref.Value = o;
                    oref.CSType = o.GetType();
                    oref.Tag = tag;
                    lock (atomToPlRef)
                    {
                        PlRef oldValue;
                        if (atomToPlRef.TryGetValue(tag, out oldValue))
                        {
                            Warn("already a value for tag=" + oldValue);
                        }
                        atomToPlRef[tag] = oref;
                    }
#if PLVARBIRTH
                    Term jplTerm = JPL.newJRef(o);
                    oref.JPLRef = jplTerm;

                    Int64 ohandle = TopOHandle++;
                    oref.OHandle = ohandle;
                    // how do we track the birthtime?
                    var plvar = oref.Variable = PlTerm.PlVar();
                    lock (termToObjectPins)
                    {
                        PlRef oldValue;
                        if (termToObjectPins.TryGetValue(ohandle, out oldValue))
                        {
                            Warn("already a value for ohandle=" + oldValue);
                        }
                        termToObjectPins[ohandle] = oref;
                    }
                    //PL_put_integer
                    oref.Term = comp("$cli_object", new PlTerm((long) ohandle), plvar);
#else
                    oref.Term = comp("@", PlTerm.PlAtom(tag));
#endif
                    return -1; // oref.Term;
                }
                else
                {
                    oref.Term = comp("@", PlTerm.PlAtom(tag));
                    return -1; // oref.Term;
                }
#endif
        }
    }

    public class TrackedObject: IComparable<TrackedObject>
    {
        public string TagName;
        public int Refs = 0;
        public object Value;
        public GCHandle Pinned;
        public bool Heaped = false;
        public int HashCode;
        public Thread LastThread;

        public TrackedObject(object value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            TrackedObject other = (TrackedObject) obj;
            return Pinned == other.Pinned;
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public long ToInt64()
        {
            return ((IntPtr) Pinned).ToInt64();
        }

        #region IComparable<ObjectWRefCounts> Members

        public int CompareTo(TrackedObject other)
        {
            return ToInt64().CompareTo(other.ToInt64());
        }

        #endregion

        public void RemoveRef()
        {
            Refs--;
            if (Refs == 0 && !Heaped)
            {
                PrologClient.RemoveTaggedObject(TagName);
            }
        }

        public override string ToString()
        {
            string vs;
            try
            {
                vs = Value.ToString();
            }
            catch (Exception e)
            {
                vs = "" + e;
            }
            return TagName + " for " + vs;
        }

        public void AddRef()
        {
            Refs++;
        }
    }
    public class TrackedFrame
    {
        HashSet<TrackedObject> TrackedObjects;
        public void AddTracking(TrackedObject info)
        {
            if (TrackedObjects == null)
            {
                TrackedObjects = new HashSet<TrackedObject>();
            }
            if (TrackedObjects.Add(info))
            {
                info.AddRef();
            }
        }

        public void RemoveRefs()
        {
            if (TrackedObjects == null) return;
            foreach (var oref in TrackedObjects)
            {
                oref.RemoveRef();
            }
        }
        public TrackedFrame Prev;
    }
    internal class ThreadEngineObjectTracker
    {
        private TrackedFrame CurrentTrackedFrame = null;
        public ThreadEngineObjectTracker()
        {
            CurrentTrackedFrame = new TrackedFrame();
        }

        public TrackedFrame CreateFrame()
        {
            if (CurrentTrackedFrame == null)
            {
                CurrentTrackedFrame = new TrackedFrame();
                return CurrentTrackedFrame;
            }
            TrackedFrame newTrackedFrame = new TrackedFrame {Prev = CurrentTrackedFrame};
            CurrentTrackedFrame = newTrackedFrame;
            return newTrackedFrame;
        }
        public TrackedFrame PopFrame()
        {
            if (CurrentTrackedFrame == null)
            {
                return null;
            }
            TrackedFrame old = CurrentTrackedFrame;
            CurrentTrackedFrame = old.Prev;
            old.RemoveRefs();
            return old;
        }

        public void AddTracking(TrackedObject info)
        {
            if (CurrentTrackedFrame == null)
            {
                return;
            }
            else
            {
                if (CurrentTrackedFrame.Prev == null)
                {
                    Thread lt = info.LastThread;
                    Thread ct = Thread.CurrentThread;
                    if (ct == lt)
                    {
                        return;
                    }
                    info.LastThread = ct;
                }
                CurrentTrackedFrame.AddTracking(info);
            }
        }

        public bool RemoveFrame(TrackedFrame frame)
        {
            if (CurrentTrackedFrame == frame)
            {
                PopFrame();
                return true;
            }
            else
            {
                if (PrologClient.DebugRefs)
                {
                    PrologClient.Debug("Removing wierd frame" + frame);
                }
                frame.RemoveRefs();
                return false;
            }
        }
#if plvar_pins
        public static Dictionary<Int64, PlRef> termToObjectPins = new Dictionary<Int64, PlRef>();
        public static Dictionary<object, PlRef> objectToPlRef = new Dictionary<object, PlRef>();
        public static Dictionary<string, PlRef> atomToPlRef = new Dictionary<string, PlRef>();
#endif
    }
#if plvar_pins
    public class PlRef
    {
        public object Value;
        public PlTerm Term;
        public Int64 OHandle;
        public PlTerm Variable;
        public Type CSType;
        public Term JPLRef;
        public string Tag;
    }
#endif
}
