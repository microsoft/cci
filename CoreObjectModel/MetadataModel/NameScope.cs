// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.Cci
{
    /// <summary>
    /// MultiMap{Key, TValue} with 'minimum' space requirement
    /// </summary>
    /// <remarks>
    /// This class is intended to store name to definition mapping within a scope (class/struct). Normally a name is mapped to a single definition, 
    /// but 1 to N mapping is possible in the case of overloading.
    /// 
    /// Traditional approach would be using Dictionary{TKey, List{TValue}}, but List{TValue} would waste lots of memory for 1:1 mapping.
    /// 
    /// It's implemented here as Dictionary{TKey, object} where object is either TValue or TValue[] with the exact size. 
    /// For 1:1 mapping, there is no extra per key allocation.
    /// </remarks>
    public class NameScope<TKey, TValue> where TValue : class
    {
        Dictionary<TKey, object> m_map;

        /// <summary />
        public NameScope(int initSize = 0)
        {
            m_map = new Dictionary<TKey, object>(initSize);
        }

        /// <summary>
        /// Key count
        /// </summary>
        public int Count
        {
            get
            {
                return m_map.Count;
            }
        }

        /// <summary>
        /// Add key -> value mapping
        /// </summary>
        /// <returns>true if added, false if duplicate found</returns>
        public bool Add(TKey key, TValue value)
        {
            object values;

            if (m_map.TryGetValue(key, out values))
            {
                TValue[] valueArray = values as TValue[];

                if (valueArray != null)
                {
                    if (Array.IndexOf<TValue>(valueArray, value) >= 0) // duplicate
                    {
                        return false;
                    }

                    TValue[] newArray = new TValue[valueArray.Length + 1]; // N + 1

                    valueArray.CopyTo(newArray, 0);

                    valueArray = newArray;
                }
                else
                {
                    TValue first = values as TValue;

                    Debug.Assert(first != null);

                    if (first == value) // duplicate
                    {
                        return false;
                    }

                    valueArray= new TValue[2]; // Convert single value to two element array
                    valueArray[0] = first;
                }

                m_map[key] = valueArray;

                valueArray[valueArray.Length - 1] = value;
            }
            else
            {
                m_map[key] = value; // single value
            }

            return true;
        }

        /// <summary>
        /// Translate object to IEnumerable{TValue}
        /// </summary>
        static IEnumerable<TValue> Translate(object values, out int count)
        {
            TValue[] valueArray = values as TValue[];

            if (valueArray != null)
            {
                count = valueArray.Length;

                return valueArray;
            }
            else
            {
                Debug.Assert(values is TValue);

                count = 1;

                return new SingletonList<TValue>(values as TValue);
            }
        }

        /// <summary>
        /// Return value or value array, null if key not found. Perf optimization to avoid allocating enumerator
        /// </summary>
        public TValue GetValueArray(TKey key, out TValue[] valueArray)
        {
            object values;

            valueArray = null;

            if (m_map.TryGetValue(key, out values))
            {
                valueArray = values as TValue[];

                if (valueArray != null)
                {
                    return null;
                }
                else
                {
                    return values as TValue;
                }
            }

            return null;
        }

        /// <summary>
        /// Return values associated with key and count, Enumerable{TValue}.Empty if key not found
        /// </summary>
        public IEnumerable<TValue> GetValuesAndCount(TKey key, out int count)
        {
            object values;

            if (m_map.TryGetValue(key, out values))
            {
                return Translate(values, out count);
            }

            count = 0;

            return Enumerable<TValue>.Empty;
        }

        /// <summary>
        /// Return values associated with key, Enumerable{TValue}.Empty if key not found
        /// </summary>
        public IEnumerable<TValue> GetValues(TKey key)
        {
            int count;

            IEnumerable<TValue> result = GetValuesAndCount(key, out count);

            return result;
        }

        /// <summary>
        /// Enumeration of value collection as IEnumerator{IEnumerable{TValue}}
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IEnumerable<TValue>> GetEnumerator()
        { 
            return new ValueEnumerator(m_map.Values.GetEnumerator());
        }

        /// <summary>
        /// Value collection enumerator
        /// </summary>
        struct ValueEnumerator : IEnumerator, IEnumerator<IEnumerable<TValue>>
        {
            IEnumerator<object> m_enum;
            
            public ValueEnumerator(IEnumerator<object> _enum)
            {
                m_enum = _enum;
            }

            public IEnumerable<TValue> Current
            {
                get
                {
                    int count;

                    return Translate(m_enum.Current, out count);
                }
            }

            object IEnumerator.Current
            {
                get 
                { 
                    int count;
                    return Translate(m_enum.Current, out count);
                }
            }

            public bool MoveNext()
            {
                return m_enum.MoveNext();
            }

            public void Reset()
            {
                m_enum.Reset();
            }

            public void Dispose()
            {
                m_enum.Dispose();
            }
        }
    }

}
