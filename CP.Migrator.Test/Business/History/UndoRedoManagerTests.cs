using CP.Migrator.Business.History;
using CP.Migrator.Models.Csv;

namespace CP.Migrator.Test.Business.History;

public class UndoRedoManagerTests
{
    // PatientCsvRow is a concrete CsvRow subclass available in the solution.
    private static PatientCsvRow Row(string firstName) => new() { FirstName = firstName };

    private readonly UndoRedoManager<PatientCsvRow> _sut = new();

    [Fact]
    public void InitialState_CannotUndoOrRedo()
    {
        Assert.False(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Push_SetsCanUndo()
    {
        _sut.Push(Row("Before"), Row("After"));
        Assert.True(_sut.CanUndo);
    }

    [Fact]
    public void Push_DoesNotSetCanRedo()
    {
        _sut.Push(Row("Before"), Row("After"));
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Undo_ReturnsBefore()
    {
        _sut.Push(Row("Before"), Row("After"));
        var result = _sut.Undo();
        Assert.Equal("Before", result.FirstName);
    }

    [Fact]
    public void Undo_SetsCanRedo()
    {
        _sut.Push(Row("Before"), Row("After"));
        _sut.Undo();
        Assert.True(_sut.CanRedo);
    }

    [Fact]
    public void Undo_WhenEmpty_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _sut.Undo());
    }

    [Fact]
    public void Redo_ReturnsAfter()
    {
        _sut.Push(Row("Before"), Row("After"));
        _sut.Undo();

        var result = _sut.Redo();

        Assert.Equal("After", result.FirstName);
    }

    [Fact]
    public void Redo_WhenEmpty_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _sut.Redo());
    }

    [Fact]
    public void Push_AfterUndo_ClearsRedoStack()
    {
        _sut.Push(Row("A"), Row("B"));
        _sut.Undo();
        Assert.True(_sut.CanRedo);

        _sut.Push(Row("C"), Row("D"));

        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void MultipleUndoRedo_FollowsLIFOOrder()
    {
        _sut.Push(Row("A"), Row("B"));
        _sut.Push(Row("C"), Row("D"));

        Assert.Equal("C", _sut.Undo().FirstName); // pops second
        Assert.Equal("A", _sut.Undo().FirstName); // pops first
    }

    [Fact]
    public void Clear_ResetsAllStacks()
    {
        _sut.Push(Row("Before"), Row("After"));
        _sut.Undo();

        _sut.Clear();

        Assert.False(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Redo_AfterClear_ThrowsInvalidOperationException()
    {
        _sut.Push(Row("Before"), Row("After"));
        _sut.Undo();
        _sut.Clear();

        Assert.Throws<InvalidOperationException>(() => _sut.Redo());
    }
}
