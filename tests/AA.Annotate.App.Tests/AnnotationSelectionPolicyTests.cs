using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.Tests;

public sealed class AnnotationSelectionPolicyTests
{
    [Fact]
    public void SelectAtPointChoosesTopmostHitWhenRequestedIsNotSelected()
    {
        var bottom = CreateAnnotation(1, new RectInt(10, 10, 100, 100), isSelected: false);
        var top = CreateAnnotation(2, new RectInt(20, 20, 100, 100), isSelected: false);

        var selected = AnnotationSelectionPolicy.SelectAtPoint([bottom, top], null, bottom, new PointInt(30, 30));

        Assert.Same(top, selected);
    }

    [Fact]
    public void SelectAtPointCyclesToAnnotationUnderCurrentlySelectedHit()
    {
        var bottom = CreateAnnotation(1, new RectInt(10, 10, 100, 100), isSelected: false);
        var top = CreateAnnotation(2, new RectInt(20, 20, 100, 100), isSelected: true);

        var selected = AnnotationSelectionPolicy.SelectAtPoint([bottom, top], top, top, new PointInt(30, 30));

        Assert.Same(bottom, selected);
    }

    [Fact]
    public void SelectAtPointCyclesFromCurrentSelectionEvenWhenTopVisualReceivesClick()
    {
        var bottom = CreateAnnotation(1, new RectInt(10, 10, 100, 100), isSelected: false);
        var middle = CreateAnnotation(2, new RectInt(20, 20, 80, 80), isSelected: true);
        var top = CreateAnnotation(3, new RectInt(30, 30, 60, 60), isSelected: false);

        var selected = AnnotationSelectionPolicy.SelectAtPoint([bottom, middle, top], middle, top, new PointInt(40, 40));

        Assert.Same(bottom, selected);
    }

    private static AnnotationViewModel CreateAnnotation(int number, RectInt rect, bool isSelected)
    {
        return new AnnotationViewModel(number.ToString(), number, rect, string.Empty)
        {
            IsSelected = isSelected
        };
    }
}
