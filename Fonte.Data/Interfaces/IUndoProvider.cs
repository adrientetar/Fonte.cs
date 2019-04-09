
namespace Fonte.Data.Interfaces
{
    public interface IUndoProvider
    {
        bool CanRedo { get; }
        bool CanUndo { get; }

        IChangeGroup CreateUndoGroup();
        void Redo();
        void Undo();
    }
}
