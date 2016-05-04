// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;

#if MERGED_DLL
using Microsoft.Cci.MutableCodeModel;
#endif

#if !NO_METADATA_HELPER
using Microsoft.Cci.UtilityDataStructures;
#endif


namespace Microsoft.Cci 
{
    /// <summary>
    /// Fixed size array wrapped as IReadOnlyList{T}
    /// Construct with known size N, call Add N times, Freeze, and then use as IReadOnlyList{T} or IEnumerable{T}
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>Optimization for List list = new List{T}(); list.Add() list.Add() ...; list.TrimExcess(); list.AsReadOnly() </remarks>
    internal class ReadOnlyList<T> : IReadOnlyList<T>
    {
        T[]  m_data;
        int  m_count; // item count during construction, -1 for frozen state

        /// <summary>
        /// Constructor
        /// </summary>
        public ReadOnlyList(int capacity)
        {
            if (capacity == 0)
            {
                m_data = ArrayT<T>.Empty;
            }
            else
            {
                m_data = new T[capacity];
            }
        }

        /// <summary>
        /// Creation helper
        /// </summary>
        public static ReadOnlyList<T> Create(uint uCount)
        {
            int count = (int) uCount;

            if (count <= 0)
            {
                return null;
            }
            else
            {
                return new ReadOnlyList<T>(count);
            }
        }

        /// <summary>
        /// Creation helper from IEnumerable{T}
        /// </summary>
        public static ReadOnlyList<T> Create(IEnumerable<T> list)
        {
            return Create(IteratorHelper.EnumerableCount<T>(list));
        }

        /// <summary>
        /// Freeze to be read-only
        /// </summary>
        public static IEnumerable<T> Freeze(ReadOnlyList<T> list)
        {
            if (list == null)
            {
                return Enumerable<T>.Empty;
            }
            else
            {
                Debug.Assert(list.m_data.Length == list.m_count);

                list.m_count = -1;

                return list;
            }
        }

        /// <summary>
        /// Append item
        /// </summary>
        public void Add(T item)
        {
            if (m_count < 0)
            {
                throw new NotSupportedException("ReadOnlyList is frozen");
            }

            m_data[m_count ++] = item;
        }

        /// <summary>
        /// Count of total allowed items
        /// </summary>
        public int Count
        {
            get
            {
                return m_data.Length;
            }
        }
      
        /// <summary>
        /// Return an item
        /// </summary>
        public T this[int index]
        {
            get
            {
                return m_data[index];
            }
        }
      
        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable<T> data = m_data;

            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable data = m_data;

            return data.GetEnumerator();
        }
    }

    /// <summary>
    /// Virtual IReadOnlyList + its enumerator
    /// </summary>
    /// <remarks>Borrowed from the internal implementation of "yield return", IEnumerable and IEnumerator are implemented in the 
    /// same class here, to save one extra allocation for the most common usage pattern of single enumerator in the same thread.
    ///
    /// This class is used mostly by SingletonList. There are quite a few CCI objects which store single object inside by needs to return IEnumerable from it.
    /// This is used in super high frequency (e.g. BaseClasses) that we need to reduce memory allocation and CPU cost for it.
    ///
    /// This solution is better replacement for GetSingletonEnumerable which just uses "yield return":
    /// 1) There only needs to be single implementation.
    /// 2) All the source code is here.
    /// 3) IReadOnlyList is implemented so caller can query for Count and this[index] without going through enumerator at all.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal abstract class VirtualReadOnlyList<T> : IReadOnlyList<T>, IEnumerator<T>, IEnumerator
    {
        const int GetEnumeratorNotCalled = -2;
        
        int m_initialThreadId;
        int m_count;
        int m_index;
        
        public VirtualReadOnlyList(int count)
        {
            m_count = count;
            m_index = GetEnumeratorNotCalled;
            m_initialThreadId = Environment.CurrentManagedThreadId;
        }

        public int Count
        {
            get
            {
                return m_count;
            }
        }

        /// <summary>
        /// One method to be implemented in derived classes
        /// </summary>
        public abstract T GetItem(int index);

        public T this[int index]
        {
            get
            {
                return GetItem(index);
            }
        }

        public bool MoveNext()
        {
            if (this.m_index == GetEnumeratorNotCalled)
            {
                throw new InvalidOperationException("GetEnumerator not called");
            }

            m_index ++;

            return (m_index >= 0) && (m_index < Count);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            // First time calling GetEnumerator from the same thread, return itself
            if ((Environment.CurrentManagedThreadId == m_initialThreadId) && (this.m_index == GetEnumeratorNotCalled))
            {
                m_index = -1;
                return this;
            }

            // Need to create a new enumerator
            return new ReadOnlyListEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        void IEnumerator.Reset()
        {
            if (this.m_index == GetEnumeratorNotCalled)
            {
                throw new InvalidOperationException("GetEnumerator not called");
            }

            m_index = -1;
        }

        void IDisposable.Dispose()
        {
        }

        T IEnumerator<T>.Current
        {
            get
            {
                return GetItem(m_index);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return GetItem(m_index);
            }
        }
    }

    /// <summary>
    /// Enumerator for IReadOnlyList
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct ReadOnlyListEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        IReadOnlyList<T> m_list;
        int              m_index;

        public ReadOnlyListEnumerator(IReadOnlyList<T> list)
        {
            m_list  = list;
            m_index = -1;
        }

        public int Count
        {
            get
            {
                return m_list.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return m_list[index];
            }
        }

        public bool MoveNext()
        {
            m_index ++;

            return (m_index >= 0) && (m_index < m_list.Count);
        }

        void IEnumerator.Reset()
        {
            m_index = -1;
        }

        void IDisposable.Dispose()
        {
            m_list = null;
        }

        T IEnumerator<T>.Current
        {
            get
            {
                return m_list[m_index];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return m_list[m_index];
            }
        }
    }

    /// <summary>
    /// IReadOnlyList wrapper for single item, + its enumerator (similar to yield return)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SingletonList<T> : VirtualReadOnlyList<T>
    {
        T m_current;

        public SingletonList(T value) : base(1)
        {
            m_current = value;
        }

        public override T GetItem(int index)
        {
            if (index == 0)
            {
                return m_current;
            }
            else
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }
    }

    /// <summary>
    /// Caching 10 StringBuilders per thread (for nested usage)
    /// </summary>
    internal static class StringBuilderCache
    {
        [ThreadStatic]
        private static StringBuilder[] ts_CachedInstances;

        private static int             s_Capacity = 64;

        /// <summary>
        /// Get StringBuilder array
        /// </summary>
        private static StringBuilder[] GetList()
        {
            StringBuilder[] list = ts_CachedInstances;

            if (list == null)
            {
                list = new StringBuilder[10];

                ts_CachedInstances = list;
            }

            return list;
        }

        /// <summary>
        /// Acquire a StringBuilder
        /// </summary>
        public static StringBuilder Acquire()
        {
            StringBuilder[] list = GetList();

            // Grab from cache
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null)
                {
                    StringBuilder sb = list[i];

                    list[i] = null;
                    sb.Clear();

                    return sb;
                }
            }

            // Create new one
            return new StringBuilder(s_Capacity);
        }

        /// <summary>
        /// Release StringBuilder to cache
        /// </summary>
        public static void Release(StringBuilder sb)
        {
            // If the StringBuilder's capacity is larger than our capacity, then it could be multi-chunk StringBuilder
            // Which is inefficient to use. Reject it, but enlarge our capacity, so that new StringBuilder created here will be larger.
            if (sb.Capacity > s_Capacity)
            {
                s_Capacity = sb.Capacity;
            }
            else // return to cache
            {
                StringBuilder[] list = GetList();

                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i] == null)
                    {
                        list[i] = sb;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Release StringBuilder to cache, after getting string from it
        /// </summary>
        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();

            Release(sb);

            return result;
        }

        public static void FastAppend(this StringBuilder sb, int val)
        {
            if ((val >= 0) && (val < 10))
            {
                sb.Append((char)('0' + val));
            }
            else
            {
                sb.Append(val);
            }
        }
    }

#if !NO_METADATA_HELPER
    /// <summary>
    /// Reusing Containers
    /// </summary>
    internal static class ContainerCache
    {
        internal class Containers
        {
            internal Dictionary<object, object>     m_objectDictionary;
            internal SetOfObjects                   m_setOfObjects;
            internal Hashtable<object, object>      m_hashTable1;
            internal Hashtable<object, object>      m_hashTable2;
            internal Hashtable<IReference, object>  m_refHashTable;
        }

        internal static T Acquire<T>(ref T field) where T : class
        {
            T result = field;

            if (result != null)
            {
                field = null;
            }

            return result;
        }

        internal static T Acquire<T>(ref T field1, ref T field2) where T : class
        {
            T result = field1;

            if (result != null)
            {
                field1 = null;
            }
            else
            {
                result = field2;

                if (result != null)
                {
                    field2 = null;
                }
            }

            return result;
        }

        internal static void Release<T>(ref T field1, ref T field2, T container) where T : class
        {
            if (field1 == null)
            {
                field1 = container;
            }
            else
            {
                field2 = container;
            }
        }

        [ThreadStatic]
        private static Containers ts_CachedContainers;

        internal static Containers GetContainers()
        {
            if (ts_CachedContainers == null)
            {
                ts_CachedContainers = new Containers();
            }

            return ts_CachedContainers;
        }

        /// <summary>
        /// Acquire a Dictionary
        /// </summary>
        public static Dictionary<object, object> AcquireObjectDictionary()
        {
            Dictionary<object, object> result = Acquire(ref GetContainers().m_objectDictionary);

            if (result != null)
            {
                result.Clear();
            }
            else
            {
                result = new Dictionary<object, object>();
            }

            return result;
        }

        /// <summary>
        /// Release Dictionary to cache
        /// </summary>
        public static void Release(Dictionary<object, object> dic)
        {
            if (dic != null)
            {
                dic.Clear();

                GetContainers().m_objectDictionary = dic;
            }
        }

        public static SetOfObjects AcquireSetOfObjects(uint capacity)
        {
            SetOfObjects result = Acquire(ref GetContainers().m_setOfObjects);

            if (result != null)
            {
                result.Clear();
            }
            else
            {
                result = new SetOfObjects(capacity);
            }

            return result;
        }

        public static void ReleaseSetOfObjects(ref SetOfObjects setOfObjects)
        {
            if (setOfObjects != null)
            {
                setOfObjects.Clear();
                GetContainers().m_setOfObjects = setOfObjects;

                setOfObjects = null;
            }
        }


        public static Hashtable<object, object> AcquireHashtable(uint capacity)
        {
            Containers cache = GetContainers();

            Hashtable<object, object> result = Acquire(ref cache.m_hashTable1, ref cache.m_hashTable2);

            if (result != null)
            {
                result.Clear();
            }
            else
            {
                result = new Hashtable<object, object>(capacity);
            }

            return result;
        }

        public static void ReleaseHashtable(ref Hashtable<object, object> hashTable)
        {
            if (hashTable != null)
            {
                Containers cache = GetContainers();
                
                hashTable.Clear();
                Release(ref cache.m_hashTable1, ref cache.m_hashTable2, hashTable);

                hashTable = null;
            }
        }

        public static Hashtable<IReference, object> AcquireRefHashtable(uint capacity)
        {
            Containers cache = GetContainers();

            Hashtable<IReference, object> result = Acquire(ref cache.m_refHashTable);

            if (result != null)
            {
             // Console.WriteLine("AcquireRefHashtable");
                result.Clear();
            }
            else
            {
                result = new Hashtable<IReference, object>(capacity);
            }

            return result;
        }

        public static void ReleaseRefHashtable(ref Hashtable<IReference, object> hashTable)
        {
            if (hashTable != null)
            {
             // Console.WriteLine("ReleaseRefHashTable");

                hashTable.Clear();
                GetContainers().m_refHashTable = hashTable;

                hashTable = null;
            }
        }
    }
#endif

    /// <summary>
    /// Array related helpers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class ArrayT<T>
    {
        readonly static T[] s_empty = new T[0];

        static internal T[] Empty
        {
            get 
            { 
                return s_empty;
            }
        }

        static internal T[] Create(int count)
        {
            if (count == 0)
            {
                return s_empty;
            }
            else
            {
                return new T[count];
            }
        }
    }

    internal static partial class Toolbox
    {
        internal static string GetString(uint n)
        {
            switch (n)
            {
                case 0: return "0";
                case 1: return "1";
                case 2: return "2";
                case 3: return "3";
                case 4: return "4";
                case 5: return "5";
                case 6: return "6";
                case 7: return "7";
                case 8: return "8";
                case 9: return "9";

                default: return n.ToString();
            }
        }

        internal static string GetLocalName(uint n)
        {
            switch (n)
            {
                case 0: return "local_0";
                case 1: return "local_1";
                case 2: return "local_2";
                case 3: return "local_3";
                case 4: return "local_4";
                case 5: return "local_5";
                case 6: return "local_6";
                case 7: return "local_7";
                case 8: return "local_8";
                case 9: return "local_9";

                default: return "local_" + n;
            }
        }

        public static EnumerableAdapter<T> Adapter<T>(this IEnumerable<T> en)
        {
            return new EnumerableAdapter<T>(en);
        }

        public static void IncreaseCapacity<T>(this List<T> list, int count)
        {
            list.Capacity = list.Count + count;
        }

        /// <summary>
        /// Getting read-only IEnumerable{T} from List{T}
        /// Read-only is only enforced in DEBUG build to catch programming errors. In release mode, we just return the original list for performance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        internal static IEnumerable<T> ToReadOnly<T>(this List<T> list)
        {
            if (list == null)
              return Enumerable<T>.Empty;
            else
    #if DEBUG
              return list.AsReadOnly();
    #else
              return list;
    #endif
        }

    }

    /// <summary>
    /// Wrapper around IEnumerable{T}, optimized for IReadOnlyList{T}
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct EnumerableAdapter<T>
    {
        IEnumerator<T> m_enum;
        IReadOnlyList<T> m_list;
        int m_pos;

        public EnumerableAdapter(IEnumerable<T> en)
        {
            m_list = en as IReadOnlyList<T>;
            m_pos = -1;

            if (m_list == null)
            {
                m_enum = en.GetEnumerator();
            }
            else
            {
                m_enum = null;
            }
        }

        public T Current
        {
            get
            {
                if (m_list != null)
                {
                    return m_list[m_pos];
                }
                else
                {
                    return m_enum.Current;
                }
            }
        }

        public bool MoveNext()
        {
            if (m_list != null)
            {
                m_pos++;
                return m_pos < m_list.Count;
            }
            else
            {
                return m_enum.MoveNext();
            }
        }

        public void Dispose()
        {
        }

        public EnumerableAdapter<T> GetEnumerator()
        {
            return this;
        }
    }

#if MERGED_DLL
    /// <summary>
    /// Fast visitor: Assembly -> Module -> Namespace -> TypeDef -> MethodDef
    /// </summary>
    internal class MethodVisitor
    {
        protected IMetadataHost host;

        public MethodVisitor(IMetadataHost host = null)
        {
            this.host = host;
        }

        public virtual void VisitAssembly(Assembly assembly)
        {
            if (assembly.MemberModules != null)
            {
                for (int i = 0; i < assembly.MemberModules.Count; i++)
                {
                    VisitModule(assembly.MemberModules[i] as Module);
                }
            }

            VisitModule(assembly);
        }

        public virtual void VisitModule(Module module)
        {
            VisitNamespace(module.UnitNamespaceRoot as UnitNamespace);
        }

        public virtual void VisitNamespace(UnitNamespace unitNamespace)
        {
            if (unitNamespace.Members == null)
            {
                return;
            }

            for (int i = 0; i < unitNamespace.Members.Count; i++)
            {
                INamespaceMember member = unitNamespace.Members[i];

                MethodDefinition methodDef = member as MethodDefinition;

                if (methodDef != null)
                {
                    VisitMethod(methodDef, null);
                    continue;
                }

                NamespaceTypeDefinition typeDef = member as NamespaceTypeDefinition;

                if (typeDef != null)
                {
                    VisitType(typeDef);
                    continue;
                }

                UnitNamespace ns = member as UnitNamespace;

                if (ns != null)
                {
                    VisitNamespace(ns);
                }
                else
                {
                    throw new InvalidOperationException("INamespaceMember");
                }
            }
        }

        public virtual void VisitType(NamedTypeDefinition typeDef)
        {
            if (typeDef.Methods != null)
            {
                for (int i = 0; i < typeDef.Methods.Count; i++)
                {
                    VisitMethod(typeDef.Methods[i] as MethodDefinition, typeDef);
                }
            }

            if (typeDef.NestedTypes != null)
            {
                for (int i = 0; i < typeDef.NestedTypes.Count; i++)
                {
                    VisitType(typeDef.NestedTypes[i] as NamedTypeDefinition);
                }
            }
        }

        public virtual void VisitMethod(MethodDefinition methodDef, NamedTypeDefinition typeDef)
        {
        }
    }
#endif

    [Flags]
    internal enum OperationValueKind
    {
        None                    = 0x000,
        
        Scalar                  = 0x001,
        String                  = 0x002,
        JumpOffset              = 0x004,
        JumpOffsetArray         = 0x008,
        
        Parameter               = 0x010,
        Local                   = 0x020,
        Field                   = 0x040,
        
        Extra                   = 0x080,

        Type                    = 0x100,
        Method                  = 0x200,
        TypeMember              = 0x400,            // ITypeMemeberReference
        FunctionPointerType     = Type | Extra,     // Treat it asITypeReference
        
        RuntimeHandle           = Type | Field | Method | TypeMember,
        
        Any                     = 0xFFFF
    }

    internal static partial class Toolbox
    {
        internal static bool MaybeType(this OperationValueKind kind)
        {
            return (kind & OperationValueKind.Type) == OperationValueKind.Type;
        }

        internal static bool MaybeField(this OperationValueKind kind)
        {
            return (kind & OperationValueKind.Field) == OperationValueKind.Field;
        }

        internal static bool MaybeMethod(this OperationValueKind kind)
        {
            return (kind & OperationValueKind.Method) == OperationValueKind.Method;
        }

        internal static bool MaybeParameter(this OperationValueKind kind)
        {
            return (kind & OperationValueKind.Parameter) == OperationValueKind.Parameter;
        }

        internal static bool MaybeLocal(this OperationValueKind kind)
        {
            return (kind & OperationValueKind.Local) == OperationValueKind.Local;
        }

        internal static OperationValueKind ValueKind(this OperationCode cilOpCode)
        {
            switch (cilOpCode)
            {
                case OperationCode.Nop:
                case OperationCode.Break:
                    break;
                case OperationCode.Ldarg_0:
                case OperationCode.Ldarg_1:
                case OperationCode.Ldarg_2:
                case OperationCode.Ldarg_3:
                    return OperationValueKind.Parameter;
                case OperationCode.Ldloc_0:
                case OperationCode.Ldloc_1:
                case OperationCode.Ldloc_2:
                case OperationCode.Ldloc_3:
                    return OperationValueKind.Local;
                case OperationCode.Stloc_0:
                case OperationCode.Stloc_1:
                case OperationCode.Stloc_2:
                case OperationCode.Stloc_3:
                    return OperationValueKind.Local;
                case OperationCode.Ldarg_S:
                case OperationCode.Ldarga_S:
                case OperationCode.Starg_S:
                    return OperationValueKind.Parameter;
                case OperationCode.Ldloc_S:
                case OperationCode.Ldloca_S:
                case OperationCode.Stloc_S:
                    return OperationValueKind.Local;
                case OperationCode.Ldnull:
                    break;
                case OperationCode.Ldc_I4_M1:
                case OperationCode.Ldc_I4_0:
                case OperationCode.Ldc_I4_1:
                case OperationCode.Ldc_I4_2:
                case OperationCode.Ldc_I4_3:
                case OperationCode.Ldc_I4_4:
                case OperationCode.Ldc_I4_5:
                case OperationCode.Ldc_I4_6:
                case OperationCode.Ldc_I4_7:
                case OperationCode.Ldc_I4_8:
                    return OperationValueKind.Scalar;
                case OperationCode.Ldc_I4_S:
                    return OperationValueKind.Scalar;
                case OperationCode.Ldc_I4:
                    return OperationValueKind.Scalar;
                case OperationCode.Ldc_I8:
                    return OperationValueKind.Scalar;
                case OperationCode.Ldc_R4:
                    return OperationValueKind.Scalar;
                case OperationCode.Ldc_R8:
                    return OperationValueKind.Scalar;
                case OperationCode.Dup:
                case OperationCode.Pop:
                    break;
                case OperationCode.Jmp:
                    return OperationValueKind.Method;
            
                // For Get(), Set() and Address() on arrays, the runtime provides method implementations.
                // Hence, CCI2 replaces these with pseudo instructions Array_Set, Array_Get and Array_Addr.
                // All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
                case OperationCode.Array_Set:
                case OperationCode.Array_Get:
                case OperationCode.Array_Addr:
                case OperationCode.Array_Create_WithLowerBound:
                case OperationCode.Array_Create:
                    return OperationValueKind.Type;

                case OperationCode.Call:
                    return OperationValueKind.Method;
                case OperationCode.Calli:
                    return OperationValueKind.FunctionPointerType;
                case OperationCode.Ret:
                    break;
                case OperationCode.Br_S:
                case OperationCode.Brfalse_S:
                case OperationCode.Brtrue_S:
                case OperationCode.Beq_S:
                case OperationCode.Bge_S:
                case OperationCode.Bgt_S:
                case OperationCode.Ble_S:
                case OperationCode.Blt_S:
                case OperationCode.Bne_Un_S:
                case OperationCode.Bge_Un_S:
                case OperationCode.Bgt_Un_S:
                case OperationCode.Ble_Un_S:
                case OperationCode.Blt_Un_S:
                    return OperationValueKind.JumpOffset;
                case OperationCode.Br:
                case OperationCode.Brfalse:
                case OperationCode.Brtrue:
                case OperationCode.Beq:
                case OperationCode.Bge:
                case OperationCode.Bgt:
                case OperationCode.Ble:
                case OperationCode.Blt:
                case OperationCode.Bne_Un:
                case OperationCode.Bge_Un:
                case OperationCode.Bgt_Un:
                case OperationCode.Ble_Un:
                case OperationCode.Blt_Un:
                    return OperationValueKind.JumpOffset;
                case OperationCode.Switch:
                    return OperationValueKind.JumpOffsetArray;
                case OperationCode.Ldind_I1:
                case OperationCode.Ldind_U1:
                case OperationCode.Ldind_I2:
                case OperationCode.Ldind_U2:
                case OperationCode.Ldind_I4:
                case OperationCode.Ldind_U4:
                case OperationCode.Ldind_I8:
                case OperationCode.Ldind_I:
                case OperationCode.Ldind_R4:
                case OperationCode.Ldind_R8:
                case OperationCode.Ldind_Ref:
                case OperationCode.Stind_Ref:
                case OperationCode.Stind_I1:
                case OperationCode.Stind_I2:
                case OperationCode.Stind_I4:
                case OperationCode.Stind_I8:
                case OperationCode.Stind_R4:
                case OperationCode.Stind_R8:
                case OperationCode.Add:
                case OperationCode.Sub:
                case OperationCode.Mul:
                case OperationCode.Div:
                case OperationCode.Div_Un:
                case OperationCode.Rem:
                case OperationCode.Rem_Un:
                case OperationCode.And:
                case OperationCode.Or:
                case OperationCode.Xor:
                case OperationCode.Shl:
                case OperationCode.Shr:
                case OperationCode.Shr_Un:
                case OperationCode.Neg:
                case OperationCode.Not:
                case OperationCode.Conv_I1:
                case OperationCode.Conv_I2:
                case OperationCode.Conv_I4:
                case OperationCode.Conv_I8:
                case OperationCode.Conv_R4:
                case OperationCode.Conv_R8:
                case OperationCode.Conv_U4:
                case OperationCode.Conv_U8:
                    break;
                case OperationCode.Callvirt:
                    return OperationValueKind.Method;
                case OperationCode.Cpobj:
                case OperationCode.Ldobj:
                    return OperationValueKind.Type;
                case OperationCode.Ldstr:
                    return OperationValueKind.String;
                case OperationCode.Newobj:
                    return OperationValueKind.Method;
                case OperationCode.Castclass:
                case OperationCode.Isinst:
                    return OperationValueKind.Type;
                case OperationCode.Conv_R_Un:
                    break;
                case OperationCode.Unbox:
                    return OperationValueKind.Type;
                case OperationCode.Throw:
                    break;
                case OperationCode.Ldfld:
                case OperationCode.Ldflda:
                case OperationCode.Stfld:
                    return OperationValueKind.Field;
                case OperationCode.Ldsfld:
                case OperationCode.Ldsflda:
                case OperationCode.Stsfld:
                    return OperationValueKind.Field;
                case OperationCode.Stobj:
                    return OperationValueKind.Type;
                case OperationCode.Conv_Ovf_I1_Un:
                case OperationCode.Conv_Ovf_I2_Un:
                case OperationCode.Conv_Ovf_I4_Un:
                case OperationCode.Conv_Ovf_I8_Un:
                case OperationCode.Conv_Ovf_U1_Un:
                case OperationCode.Conv_Ovf_U2_Un:
                case OperationCode.Conv_Ovf_U4_Un:
                case OperationCode.Conv_Ovf_U8_Un:
                case OperationCode.Conv_Ovf_I_Un:
                case OperationCode.Conv_Ovf_U_Un:
                    break;
                case OperationCode.Box:
                    return OperationValueKind.Type;
                case OperationCode.Newarr:
                    return OperationValueKind.Type;
                case OperationCode.Ldlen:
                    break;
                case OperationCode.Ldelema:
                    return OperationValueKind.Type;
                case OperationCode.Ldelem_I1:
                case OperationCode.Ldelem_U1:
                case OperationCode.Ldelem_I2:
                case OperationCode.Ldelem_U2:
                case OperationCode.Ldelem_I4:
                case OperationCode.Ldelem_U4:
                case OperationCode.Ldelem_I8:
                case OperationCode.Ldelem_I:
                case OperationCode.Ldelem_R4:
                case OperationCode.Ldelem_R8:
                case OperationCode.Ldelem_Ref:
                case OperationCode.Stelem_I:
                case OperationCode.Stelem_I1:
                case OperationCode.Stelem_I2:
                case OperationCode.Stelem_I4:
                case OperationCode.Stelem_I8:
                case OperationCode.Stelem_R4:
                case OperationCode.Stelem_R8:
                case OperationCode.Stelem_Ref:
                    break;
                case OperationCode.Ldelem:
                    return OperationValueKind.Type;
                case OperationCode.Stelem:
                    return OperationValueKind.Type;
                case OperationCode.Unbox_Any:
                    return OperationValueKind.Type;
                case OperationCode.Conv_Ovf_I1:
                case OperationCode.Conv_Ovf_U1:
                case OperationCode.Conv_Ovf_I2:
                case OperationCode.Conv_Ovf_U2:
                case OperationCode.Conv_Ovf_I4:
                case OperationCode.Conv_Ovf_U4:
                case OperationCode.Conv_Ovf_I8:
                case OperationCode.Conv_Ovf_U8:
                    break;
                case OperationCode.Refanyval:
                    return OperationValueKind.Type;
                case OperationCode.Ckfinite:
                    break;
                case OperationCode.Mkrefany:
                    return OperationValueKind.Type;
                case OperationCode.Ldtoken:
                    return OperationValueKind.RuntimeHandle;
                case OperationCode.Conv_U2:
                case OperationCode.Conv_U1:
                case OperationCode.Conv_I:
                case OperationCode.Conv_Ovf_I:
                case OperationCode.Conv_Ovf_U:
                case OperationCode.Add_Ovf:
                case OperationCode.Add_Ovf_Un:
                case OperationCode.Mul_Ovf:
                case OperationCode.Mul_Ovf_Un:
                case OperationCode.Sub_Ovf:
                case OperationCode.Sub_Ovf_Un:
                case OperationCode.Endfinally:
                    break;
                case OperationCode.Leave:
                    return OperationValueKind.JumpOffset;
                case OperationCode.Leave_S:
                    return OperationValueKind.JumpOffset;
                case OperationCode.Stind_I:
                case OperationCode.Conv_U:
                case OperationCode.Arglist:
                case OperationCode.Ceq:
                case OperationCode.Cgt:
                case OperationCode.Cgt_Un:
                case OperationCode.Clt:
                case OperationCode.Clt_Un:
                    break;
                case OperationCode.Ldftn:
                case OperationCode.Ldvirtftn:
                    return OperationValueKind.Method;
                case OperationCode.Ldarg:
                case OperationCode.Ldarga:
                case OperationCode.Starg:
                    return OperationValueKind.Parameter;
                case OperationCode.Ldloc:
                case OperationCode.Ldloca:
                case OperationCode.Stloc:
                    return OperationValueKind.Local;
                case OperationCode.Localloc:
                    break;
                case OperationCode.Endfilter:
                    break;
                case OperationCode.Unaligned_:
                    return OperationValueKind.Scalar;
                case OperationCode.Volatile_:
                case OperationCode.Tail_:
                    break;
                case OperationCode.Initobj:
                    return OperationValueKind.Type;
                case OperationCode.Constrained_:
                    return OperationValueKind.Type;
                case OperationCode.Cpblk:
                case OperationCode.Initblk:
                    break;
                case OperationCode.No_:
                    return OperationValueKind.Scalar;
                case OperationCode.Rethrow:
                    break;
                case OperationCode.Sizeof:
                    return OperationValueKind.Type;
                case OperationCode.Refanytype:
                case OperationCode.Readonly_:
                    break;
                default:
                    return OperationValueKind.Any;
            }

            return OperationValueKind.None;
        }
    }

}