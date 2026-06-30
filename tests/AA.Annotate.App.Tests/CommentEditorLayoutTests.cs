using AA.Annotate.App.Views;
using Avalonia.Input;

namespace AA.Annotate.App.Tests;

public sealed class CommentEditorLayoutTests
{
    [Fact]
    public void EmptyCommentUsesMinimumHeight()
    {
        var height = CommentEditorLayout.CalculateTextHostHeight(string.Empty, charactersPerLine: 48);

        Assert.Equal(CommentEditorLayout.MinimumTextHostHeight, height);
    }

    [Fact]
    public void ExplicitLineBreaksIncreaseHeight()
    {
        var height = CommentEditorLayout.CalculateTextHostHeight("one\r\ntwo\r\nthree", charactersPerLine: 48);

        Assert.True(height > CommentEditorLayout.MinimumTextHostHeight);
    }

    [Fact]
    public void LongWrappedCommentCapsHeight()
    {
        var text = string.Join(" ", Enumerable.Repeat("long-comment-text", 80));

        var height = CommentEditorLayout.CalculateTextHostHeight(text, charactersPerLine: 48);

        Assert.Equal(CommentEditorLayout.MaximumTextHostHeight, height);
    }

    [Fact]
    public void EnterCommitsComment()
    {
        Assert.True(CommentEditor.IsCommitKey(Key.Enter, KeyModifiers.None));
    }

    [Fact]
    public void ShiftEnterDoesNotCommitComment()
    {
        Assert.False(CommentEditor.IsCommitKey(Key.Enter, KeyModifiers.Shift));
    }

    [Fact]
    public void EscapeCancelsComment()
    {
        Assert.True(CommentEditor.IsCancelKey(Key.Escape));
    }

    [Fact]
    public void EnterDoesNotCancelComment()
    {
        Assert.False(CommentEditor.IsCancelKey(Key.Enter));
    }
}
