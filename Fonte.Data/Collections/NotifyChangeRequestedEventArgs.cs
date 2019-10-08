// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is similar to NotifyCollectionChangedEventArgs, but using List<T> instead of List
// and with a more fitting name for ObserverList.

namespace Fonte.Data.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;

    public enum NotifyChangeRequestedAction
    {
        Add = 0,
        Remove = 1,
        Replace = 2,
        Move = 3,  // TODO: drop this?
        Reset = 4,
    }

    /// <summary>
    /// Arguments for the NotifyChangeRequested event.
    /// A collection that supports INotifyChangeRequestedThis raises this event
    /// whenever an item is added or removed, or when the contents of the collection
    /// changes dramatically.
    /// </summary>
    public class NotifyChangeRequestedEventArgs<T> : EventArgs
    {
        private NotifyChangeRequestedAction _action;
        private IList<T> _newItems;
        private IList<T> _oldItems;
        private int _newStartingIndex = -1;
        private int _oldStartingIndex = -1;

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a reset change.
        /// </summary>
        /// <param name="action">The action that caused the event (must be Reset).</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action)
        {
            if (action != NotifyChangeRequestedAction.Reset)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Reset), nameof(action));
            }

            InitializeAdd(action, null, -1);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event; can only be Reset, Add or Remove action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, T changedItem)
        {
            if ((action != NotifyChangeRequestedAction.Add) && (action != NotifyChangeRequestedAction.Remove)
                    && (action != NotifyChangeRequestedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyChangeRequestedAction.Reset)
            {
                if (changedItem != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                InitializeAddOrRemove(action, new T[] { changedItem }, -1);
            }
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The index where the change occurred.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, T changedItem, int index)
        {
            if ((action != NotifyChangeRequestedAction.Add) && (action != NotifyChangeRequestedAction.Remove)
                    && (action != NotifyChangeRequestedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyChangeRequestedAction.Reset)
            {
                if (changedItem != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }
                if (index != -1)
                {
                    throw new ArgumentException(SR.ResetActionRequiresIndexMinus1, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                InitializeAddOrRemove(action, new T[] { changedItem }, index);
            }
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a multi-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, IList<T> changedItems)
        {
            if ((action != NotifyChangeRequestedAction.Add) && (action != NotifyChangeRequestedAction.Remove)
                    && (action != NotifyChangeRequestedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyChangeRequestedAction.Reset)
            {
                if (changedItems != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                if (changedItems == null)
                {
                    throw new ArgumentNullException(nameof(changedItems));
                }

                InitializeAddOrRemove(action, changedItems, -1);
            }
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a multi-item change (or a reset).
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="startingIndex">The index where the change occurred.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, IList<T> changedItems, int startingIndex)
        {
            if ((action != NotifyChangeRequestedAction.Add) && (action != NotifyChangeRequestedAction.Remove)
                    && (action != NotifyChangeRequestedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyChangeRequestedAction.Reset)
            {
                if (changedItems != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }
                if (startingIndex != -1)
                {
                    throw new ArgumentException(SR.ResetActionRequiresIndexMinus1, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                if (changedItems == null)
                {
                    throw new ArgumentNullException(nameof(changedItems));
                }
                if (startingIndex < -1)
                {
                    throw new ArgumentException(SR.IndexCannotBeNegative, nameof(startingIndex));
                }

                InitializeAddOrRemove(action, changedItems, startingIndex);
            }
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, T newItem, T oldItem)
        {
            if (action != NotifyChangeRequestedAction.Replace)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Replace), nameof(action));
            }

            InitializeMoveOrReplace(action, new T[] { newItem }, new T[] { oldItem }, -1, -1);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        /// <param name="index">The index of the item being replaced.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, T newItem, T oldItem, int index)
        {
            if (action != NotifyChangeRequestedAction.Replace)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Replace), nameof(action));
            }

            InitializeMoveOrReplace(action, new T[] { newItem }, new T[] { oldItem }, index, index);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, IList<T> newItems, IList<T> oldItems)
        {
            if (action != NotifyChangeRequestedAction.Replace)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Replace), nameof(action));
            }
            if (newItems == null)
            {
                throw new ArgumentNullException(nameof(newItems));
            }
            if (oldItems == null)
            {
                throw new ArgumentNullException(nameof(oldItems));
            }

            InitializeMoveOrReplace(action, newItems, oldItems, -1, -1);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        /// <param name="startingIndex">The starting index of the items being replaced.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, IList<T> newItems, IList<T> oldItems, int startingIndex)
        {
            if (action != NotifyChangeRequestedAction.Replace)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Replace), nameof(action));
            }
            if (newItems == null)
            {
                throw new ArgumentNullException(nameof(newItems));
            }
            if (oldItems == null)
            {
                throw new ArgumentNullException(nameof(oldItems));
            }

            InitializeMoveOrReplace(action, newItems, oldItems, startingIndex, startingIndex);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a one-item Move event.
        /// </summary>
        /// <param name="action">Can only be a Move action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The new index for the changed item.</param>
        /// <param name="oldIndex">The old index for the changed item.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, T changedItem, int index, int oldIndex)
        {
            if (action != NotifyChangeRequestedAction.Move)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Move), nameof(action));
            }
            if (index < 0)
            {
                throw new ArgumentException(SR.IndexCannotBeNegative, nameof(index));
            }

            T[] changedItems = new T[] { changedItem };
            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs that describes a multi-item Move event.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="index">The new index for the changed items.</param>
        /// <param name="oldIndex">The old index for the changed items.</param>
        public NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, IList<T> changedItems, int index, int oldIndex)
        {
            if (action != NotifyChangeRequestedAction.Move)
            {
                throw new ArgumentException(string.Format(SR.WrongActionForCtor, NotifyChangeRequestedAction.Move), nameof(action));
            }
            if (index < 0)
            {
                throw new ArgumentException(SR.IndexCannotBeNegative, nameof(index));
            }

            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        /// <summary>
        /// Construct a NotifyChangeRequestedEventArgs with given fields (no validation). Used by WinRT marshaling.
        /// </summary>
        internal NotifyChangeRequestedEventArgs(NotifyChangeRequestedAction action, IList<T> newItems, IList<T> oldItems, int newIndex, int oldIndex)
        {
            _action = action;
            _newItems = (newItems == null) ? null : new ReadOnlyList<T>(newItems);
            _oldItems = (oldItems == null) ? null : new ReadOnlyList<T>(oldItems);
            _newStartingIndex = newIndex;
            _oldStartingIndex = oldIndex;
        }

        private void InitializeAddOrRemove(NotifyChangeRequestedAction action, IList<T> changedItems, int startingIndex)
        {
            if (action == NotifyChangeRequestedAction.Add)
            {
                InitializeAdd(action, changedItems, startingIndex);
            }
            else
            {
                Debug.Assert(action == NotifyChangeRequestedAction.Remove, $"Unsupported action: {action}");
                InitializeRemove(action, changedItems, startingIndex);
            }
        }

        private void InitializeAdd(NotifyChangeRequestedAction action, IList<T> newItems, int newStartingIndex)
        {
            _action = action;
            _newItems = (newItems == null) ? null : new ReadOnlyList<T>(newItems);
            _newStartingIndex = newStartingIndex;
        }

        private void InitializeRemove(NotifyChangeRequestedAction action, IList<T> oldItems, int oldStartingIndex)
        {
            _action = action;
            _oldItems = (oldItems == null) ? null : new ReadOnlyList<T>(oldItems);
            _oldStartingIndex = oldStartingIndex;
        }

        private void InitializeMoveOrReplace(NotifyChangeRequestedAction action, IList<T> newItems, IList<T> oldItems, int startingIndex, int oldStartingIndex)
        {
            InitializeAdd(action, newItems, startingIndex);
            InitializeRemove(action, oldItems, oldStartingIndex);
        }

        /// <summary>
        /// The action that caused the event.
        /// </summary>
        public NotifyChangeRequestedAction Action => _action;

        /// <summary>
        /// The items affected by the change.
        /// </summary>
        public IList<T> NewItems => _newItems;

        /// <summary>
        /// The old items affected by the change (for Replace events).
        /// </summary>
        public IList<T> OldItems => _oldItems;

        /// <summary>
        /// The index where the change occurred.
        /// </summary>
        public int NewStartingIndex => _newStartingIndex;

        /// <summary>
        /// The old index where the change occurred (for Move events).
        /// </summary>
        public int OldStartingIndex => _oldStartingIndex;
    }

    /// <summary>
    /// The delegate to use for handlers that receive the NotifyChangeRequested event.
    /// </summary>
    public delegate void NotifyChangeRequestedEventHandler<T>(object sender, NotifyChangeRequestedEventArgs<T> args);

    internal sealed class ReadOnlyList<T> : IList<T>
    {
        private readonly IList<T> _list;

        internal ReadOnlyList(IList<T> list)
        {
            Debug.Assert(list != null);
            _list = list;
        }

        public T this[int index] { get => _list[index]; set => throw new NotSupportedException("Collection is read-only."); }

        public int Count => _list.Count;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        public void Clear()
        {
            throw new NotSupportedException("Collection is read-only.");
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
            throw new NotSupportedException("Collection is read-only.");
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    internal static class SR
    {
        internal const string WrongActionForCtor = "Constructor supports only the '{0}' action.";
        internal const string MustBeResetAddOrRemoveActionForCtor = "Constructor only supports either a Reset, Add, or Remove action.";
        internal const string ResetActionRequiresNullItem = "Reset action must be initialized with no changed items.";
        internal const string ResetActionRequiresIndexMinus1 = "Reset action must be initialized with index -1.";
        internal const string IndexCannotBeNegative = "Index cannot be negative.";
    }
}