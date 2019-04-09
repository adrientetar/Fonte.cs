/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using Fonte.Data.Changes;
    using Fonte.Data.Interfaces;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    class UndoStore : IUndoProvider
    {
        enum UndoMode
        {
            AddToUndoClearRedo,
            None
        };

        private int _undoCounter = 0;
        private int _redoCounter = 0;
        private ChangeGroup _undoGroup;
        private int _undoGroupIndex = 0;
        private List<IChange> _undoStack = new List<IChange>();
        private List<IChange> _redoStack = new List<IChange>();
        private UndoMode _undoMode = UndoMode.AddToUndoClearRedo;

        public bool CanRedo => _redoStack.Count > 0 && _redoCounter > 0 && _undoGroupIndex == 0;
        
        public bool CanUndo => _undoStack.Count > 0 && _undoCounter > 0 && _undoGroupIndex == 0;

        public IChangeGroup CreateUndoGroup()
        {
            /* XXX: for multilevel, we only stash the top level one */
            _undoGroup = new ChangeGroup(
                ix => OnUndoGroupDisposed(ix),
                ++_undoGroupIndex
            );
            return _undoGroup;
        }

        public void ProcessChange(IChange change)
        {
            if (_undoMode != UndoMode.None)
            {
                if (_undoGroupIndex > 0)
                {
                    _undoGroup.Add(change);
                }
                else
                {
                    if (!change.IsShallow)
                    {
                        ++_undoCounter;
                        _redoCounter = 0;
                        _redoStack.Clear();
                    }
                    _undoStack.Add(change);
                }
            }
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
                throw new InvalidOperationException("Redo stack is empty");
            if (_undoGroupIndex > 0)
                throw new InvalidOperationException("Cannot redo while in undo group (" + _undoGroupIndex + ")");

            try
            {
                _undoMode = UndoMode.None;

                while (true)
                {
                    var change = _redoStack[_redoStack.Count - 1];
                    _redoStack.RemoveAt(_redoStack.Count - 1);
                    change.Apply();
                    _undoStack.Add(change);

                    if (!change.IsShallow)
                    {
                        --_redoCounter;
                        ++_undoCounter;
                        Debug.Assert(_redoCounter >= 0);
                        break;
                    }
                }
            }
            finally
            {
                _undoMode = UndoMode.AddToUndoClearRedo;
            }
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
                throw new InvalidOperationException("Undo stack is empty");
            if (_undoGroupIndex > 0)
                throw new InvalidOperationException("Cannot undo while in undo group (" + _undoGroupIndex + ")");

            try
            {
                _undoMode = UndoMode.None;

                while (true)
                {
                    var change = _undoStack[_undoStack.Count - 1];
                    _undoStack.RemoveAt(_undoStack.Count - 1);
                    change.Apply();
                    _redoStack.Add(change);

                    if (!change.IsShallow)
                    {
                        --_undoCounter;
                        ++_redoCounter;
                        Debug.Assert(_undoCounter >= 0);
                        break;
                    }
                }
            }
            finally
            {
                _undoMode = UndoMode.AddToUndoClearRedo;
            }
        }

        void OnUndoGroupDisposed(int index)
        {
            if (index != _undoGroupIndex)
                throw new InvalidOperationException(string.Format(
                        "Disposed undo group {0} is not the topmost one {1}",
                        index, _undoGroupIndex));
            --_undoGroupIndex;

            if (_undoGroupIndex == 0)
            {
                ++_undoCounter;
                _redoCounter = 0;
                _redoStack.Clear();
                _undoStack.Add(_undoGroup);
                _undoGroup = null;
            }
        }
    }
}