using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class CommentEditCancelPolicyTests
{
    [Fact]
    public void PendingCommentCancelDeletesAnnotation()
    {
        Assert.Equal(CommentEditCancelAction.DeleteAnnotation, CommentEditCancelPolicy.SelectAction(isPendingComment: true));
    }

    [Fact]
    public void AcceptedCommentCancelOnlyClosesEditor()
    {
        Assert.Equal(CommentEditCancelAction.CancelEdit, CommentEditCancelPolicy.SelectAction(isPendingComment: false));
    }
}
