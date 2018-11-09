// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.ObjectModel
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;

    public class ObservableList<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private const string CountString = "Count";
        private const string IndexerName = "Item[]";

        internal List<T> List { get; private set; }

        #region Constructors
        public ObservableList()
        {
            List = new List<T>();
        }
        public ObservableList(List<T> list, bool copy = true)
        {
            List = copy ? new List<T>(list) : list;
        }
        public ObservableList(IEnumerable<T> enumerable)
        {
            List = new List<T>(enumerable);
        }
        public ObservableList(int capacity)
        {
            List = new List<T>(capacity);
        }
        #endregion

        #region IList<T> Members

        public void Add(T item)
        {
            Insert(Count, item);
        }

        public void Clear()
        {
            if (List.Count > 0)
            {
                var removedItems = List.GetRange(0, List.Count);
                List.Clear();
                //OnCollectionChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Reset, removedItems);
            }
        }

        public bool Contains(T item)
        {
            return List.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            List.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return List.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            List.Insert(index, item);

            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        public bool Remove(T item)
        {
            int index = List.IndexOf(item);
            if (index < 0) return false;
            List.RemoveAt(index);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = List[index];
            List.RemoveAt(index);

            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)List).GetEnumerator();
        }

        public T this[int index] {
            get => List[index];
            set
            {
                T originalItem = this[index];
                List[index] = value;

                OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, value, index);
            }
        }

        public int Count => List.Count;

        public bool IsReadOnly => ((IList)List).IsReadOnly;

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void AddRange(List<T> list)
        {
            if (list == null) throw new ArgumentNullException("collection");

            if (list.Count > 0)
            {
                int index = List.Count;
                List.AddRange(list);

                OnCollectionChanged(NotifyCollectionChangedAction.Add, list, index);
            }
        }

        public List<T> GetRange(int index, int count)
        {
            return List.GetRange(index, count);
        }

        public void RemoveRange(int index, int count)
        {
            var removedItems = List.GetRange(index, count);
            List.RemoveRange(index, count);

            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems, index);
        }

        public void Reverse()
        {
            List.Reverse();
            OnCollectionChanged();
        }

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);
            PropertyChanged?.Invoke(this, EventArgsCache.IndexerPropertyChanged);
        }

        private void OnCollectionChanged()
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, EventArgsCache.ResetCollectionChanged);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            PropertyChanged?.Invoke(this, EventArgsCache.IndexerPropertyChanged);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList changedItems)
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList changedItems, int index)
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems, index));
        }
    }

    /// <remarks>
    /// To be kept outside <see cref="ObservableList{T}"/>, since otherwise, a new instance will be created for each generic type used.
    /// </remarks>
    internal static class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
        internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
        internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }
}