namespace AA.Annotate.App.ViewModels;

public static class CommentEditCancelPolicy
{
    public static CommentEditCancelAction SelectAction(bool isPendingComment)
    {
        return isPendingComment
            ? CommentEditCancelAction.DeleteAnnotation
            : CommentEditCancelAction.CancelEdit;
    }
}
