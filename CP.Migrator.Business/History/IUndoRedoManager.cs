using CP.Migrator.Models.Csv;

namespace CP.Migrator.Business.History
{
	/// <summary>
	/// Tracks manual edits made to CSV rows in the UI grid so the user can
	/// step backwards and forwards through their changes before committing ingestion.
	/// Each Push records a before/after snapshot of a single row edit.
	/// </summary>
	public interface IUndoRedoManager<TRow> where TRow : CsvRow
	{
		bool CanUndo { get; }
		bool CanRedo { get; }

		/// <summary>Records an edit: <paramref name="before"/> is the original, <paramref name="after"/> the modified row.</summary>
		void Push(TRow before, TRow after);

		/// <summary>Reverts the most recent edit and returns the <c>before</c> snapshot.</summary>
		TRow Undo();

		/// <summary>Re-applies the most recently undone edit and returns the <c>after</c> snapshot.</summary>
		TRow Redo();

		/// <summary>Clears all history (e.g. when a new file is loaded).</summary>
		void Clear();
	}
}
