// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Fonte.Data.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    public class ObserverList<T> : IList<T>, INotifyCollectionChanged
    {
        private List<T> _list;

        public ObserverList(List<T> items)
        {
            _list = items;
        }

        #region IList<T> Members

        public void Add(T item)
        {
            Insert(Count, item);
        }

        public void Clear()
        {
            if (_list.Count > 0)
            {
                //List.Clear();
                OnCollectionChanged();
            }
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            //List.Insert(index, item);

            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;
            //List.RemoveAt(index);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _list[index];
            //List.RemoveAt(index);

            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }

        public T this[int index] {
            get => _list[index];
            set
            {
                T originalItem = this[index];
                //List[index] = value;

                OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, value, index);
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList)_list).IsReadOnly;

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        public void AddRange(List<T> list)
        {
            if (list == null) throw new ArgumentNullException("collection");

            if (list.Count > 0)
            {
                int index = _list.Count;
                //List.AddRange(list);

                OnCollectionChanged(NotifyCollectionChangedAction.Add, list, index);
            }
        }
        public void AddRange(ObserverList<T> list)
        {
            AddRange(list._list);
        }

        public List<T> GetRange(int index, int count)
        {
            return _list.GetRange(index, count);
        }

        public void RemoveRange(int index, int count)
        {
            var removedItems = _list.GetRange(index, count);
            //List.RemoveRange(index, count);

            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems, index);
        }

        public void Reverse()
        {
            var replacedItems = _list.GetRange(0, _list.Count);
            replacedItems.Reverse();
            //List.Reverse();

            OnCollectionChanged(NotifyCollectionChangedAction.Replace, _list, replacedItems, 0);
        }

        public T First()
        {
            if (Count == 0) throw new InvalidOperationException("List is empty");

            return this[0];
        }

        public T Last()
        {
            if (Count == 0) throw new InvalidOperationException("List is empty");

            return this[Count - 1];
        }

        public T Pop()
        {
            if (Count == 0) throw new InvalidOperationException("List is empty");

            return PopAt(Count - 1);
        }

        public T PopAt(int index)
        {
            T r = this[index];
            RemoveAt(index);
            return r;
        }

        void OnCollectionChanged()
        {
            CollectionChanged?.Invoke(this, EventArgsCache.ResetCollectionChanged);
        }

        void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
        }

        void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }

        void OnCollectionChanged(NotifyCollectionChangedAction action, IList changedItems, int index)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems, index));
        }

        void OnCollectionChanged(NotifyCollectionChangedAction action, IList oldItems, IList newItems, int index)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems, oldItems, index));
        }
    }

    /// <remarks>
    /// To be kept outside <see cref="ObserverList{T}"/>, since otherwise, a new instance will be created for each generic type used.
    /// </remarks>
    internal static class EventArgsCache
    {
        internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }
}