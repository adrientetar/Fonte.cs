
namespace Fonte.Data.Interfaces
{
    using System;

    interface IChange
    {
        bool ClearSelection { get; }
        bool IsShallow { get; }

        void Apply();
    }

    // Note: IChangeGroup is never shallow
    // if you have only shallow changes, don't group them.
    public interface IChangeGroup : IDisposable
    {
        int Count { get; }
        bool IsTopLevel { get; }
        
        void Clear();
    }
}
