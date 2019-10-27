// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Fonte.Data.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ObserverList<T> : IList<T>
    {
        private readonly List<T> _list;

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
                OnChangeRequested();
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

            OnChangeRequested(NotifyChangeRequestedAction.Add, item, index);
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;
            //List.RemoveAt(index);
            OnChangeRequested(NotifyChangeRequestedAction.Remove, item, index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _list[index];
            //List.RemoveAt(index);

            OnChangeRequested(NotifyChangeRequestedAction.Remove, item, index);
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

                OnChangeRequested(NotifyChangeRequestedAction.Replace, originalItem, value, index);
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList)_list).IsReadOnly;

        #endregion

        public event NotifyChangeRequestedEventHandler<T> ChangeRequested;

        public void AddRange(List<T> list)
        {
            InsertRange(_list.Count, list);
        }
        public void AddRange(ObserverList<T> list)
        {
            AddRange(list._list);
        }

        public List<T> GetRange(int index, int count)
        {
            return _list.GetRange(index, count);
        }

        public void InsertRange(int index, List<T> list)
        {
            if (list == null) throw new ArgumentNullException("collection");

            //List.InsertRange(index, list);

            OnChangeRequested(NotifyChangeRequestedAction.Add, list, index);
        }
        public void InsertRange(int index, ObserverList<T> list)
        {
            InsertRange(index, list._list);
        }

        public void RemoveRange(int index, int count)
        {
            var removedItems = _list.GetRange(index, count);
            //List.RemoveRange(index, count);

            OnChangeRequested(NotifyChangeRequestedAction.Remove, removedItems, index);
        }

        public void Reverse()
        {
            var replacedItems = _list.GetRange(0, _list.Count);
            replacedItems.Reverse();
            //List.Reverse();

            OnChangeRequested(NotifyChangeRequestedAction.Replace, _list, replacedItems, 0);
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

        void OnChangeRequested()
        {
            ChangeRequested?.Invoke(this, new NotifyChangeRequestedEventArgs<T>(NotifyChangeRequestedAction.Reset));
        }

        void OnChangeRequested(NotifyChangeRequestedAction action, T item, int index)
        {
            ChangeRequested?.Invoke(this, new NotifyChangeRequestedEventArgs<T>(action, item, index));
        }

        void OnChangeRequested(NotifyChangeRequestedAction action, T oldItem, T newItem, int index)
        {
            ChangeRequested?.Invoke(this, new NotifyChangeRequestedEventArgs<T>(action, newItem, oldItem, index));
        }

        void OnChangeRequested(NotifyChangeRequestedAction action, IList<T> changedItems, int index)
        {
            ChangeRequested?.Invoke(this, new NotifyChangeRequestedEventArgs<T>(action, changedItems, index));
        }

        void OnChangeRequested(NotifyChangeRequestedAction action, IList<T> oldItems, IList<T> newItems, int index)
        {
            ChangeRequested?.Invoke(this, new NotifyChangeRequestedEventArgs<T>(action, newItems, oldItems, index));
        }
    }
}