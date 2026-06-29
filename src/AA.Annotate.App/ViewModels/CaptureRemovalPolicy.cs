namespace AA.Annotate.App.ViewModels;

internal static class CaptureRemovalPolicy
{
    public static int SelectReplacementIndex(int countBeforeRemoval, int removedIndex)
    {
        var countAfterRemoval = countBeforeRemoval - 1;
        if (countAfterRemoval <= 0)
        {
            return -1;
        }

        return Math.Clamp(removedIndex, 0, countAfterRemoval - 1);
    }
}
