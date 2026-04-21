using CP.Migrator.Models.Csv;

namespace CP.Migrator.Business.History
{
	/// <summary>
	/// Stack-based undo/redo manager for manual edits made to CSV rows in the UI grid.
	/// Each <see cref="Push"/> records a before/after snapshot; <see cref="Undo"/> and
	/// <see cref="Redo"/> step through the history without touching any database state.
	/// Pushing a new edit clears the redo stack (standard undo/redo semantics).
	/// </summary>
	internal class UndoRedoManager<TRow> : IUndoRedoManager<TRow> where TRow : CsvRow
	{
		private readonly Stack<(TRow Before, TRow After)> _undoStack = new();
		private readonly Stack<(TRow Before, TRow After)> _redoStack = new();

		public bool CanUndo => _undoStack.Count > 0;
		public bool CanRedo => _redoStack.Count > 0;

		public void Push(TRow before, TRow after)
		{
			_undoStack.Push((before, after));
			_redoStack.Clear(); // a new edit always invalidates redo history
		}

		public TRow Undo()
		{
			if (!CanUndo)
				throw new InvalidOperationException("Nothing to undo.");

			var (before, after) = _undoStack.Pop();
			_redoStack.Push((before, after));
			return before;
		}

		public TRow Redo()
		{
			if (!CanRedo)
				throw new InvalidOperationException("Nothing to redo.");

			var (before, after) = _redoStack.Pop();
			_undoStack.Push((before, after));
			return after;
		}

		public void Clear()
		{
			_undoStack.Clear();
			_redoStack.Clear();
		}
	}
}

