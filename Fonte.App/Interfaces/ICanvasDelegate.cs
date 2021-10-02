﻿// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Controls;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Input;

using System;
using Windows.UI.Core;

namespace Fonte.App.Interfaces
{
    public enum ActivationKind
    {
        Other = 0,
        TemporarySwitch,
    };

    public class ActivationEventArgs : EventArgs
    {
        public ActivationKind ActivationKind { get; }

        public ActivationEventArgs(ActivationKind kind)
        {
            ActivationKind = kind;
        }
    }

    public class DrawEventArgs : EventArgs
    {
        public CanvasDrawingSession DrawingSession { get; }
        public float InverseScale { get; }

        public DrawEventArgs(CanvasDrawingSession ds, float inverseScale)
        {
            DrawingSession = ds;
            InverseScale = inverseScale;
        }
    }

    public interface ICanvasDelegate
    {
        CoreCursor Cursor { get; }

        object FindResource(DesignCanvas canvas, string resourceKey);
        void OnActivated(DesignCanvas canvas, ActivationEventArgs args);
        void OnDisabled(DesignCanvas canvas, ActivationEventArgs args);
        void OnDraw(DesignCanvas canvas, DrawEventArgs args);
        void OnDrawCompleted(DesignCanvas canvas, DrawEventArgs args);
        void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args);
        void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs args);
        void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args);
        void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs args);
        void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs args);
    }
}
