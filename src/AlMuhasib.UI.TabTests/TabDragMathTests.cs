using AlMuhasib.UI.Helpers;
using Xunit;

namespace AlMuhasib.UI.TabTests;

public class TabDragMathTests
{
    private static TabDragMath.Slot S(int index, double left, double right) => new(index, left, right);

    [Fact]
    public void Rtl_MoveFromRightEdgeTowardMiddle_WhenPastMidpoint()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(1, TabDragMath.ResolveMoveTarget(slots, currentIndex: 0, pointerX: 300, rightToLeft: true));
    }

    [Fact]
    public void Rtl_DoesNotMove_BeforeCrossingMidpoint()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(0, TabDragMath.ResolveMoveTarget(slots, currentIndex: 0, pointerX: 360, rightToLeft: true));
    }

    [Fact]
    public void Rtl_MoveFromLeftEndToStart_WhenPastRightEdge()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(0, TabDragMath.ResolveMoveTarget(slots, currentIndex: 2, pointerX: 520, rightToLeft: true));
    }

    [Fact]
    public void Rtl_MoveToEnd_WhenPastLeftEdge()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(2, TabDragMath.ResolveMoveTarget(slots, currentIndex: 0, pointerX: 100, rightToLeft: true));
    }

    [Fact]
    public void Ltr_MoveRight_WhenPastMidpoint()
    {
        var slots = new[] { S(0, 10, 110), S(1, 120, 220), S(2, 230, 330) };
        Assert.Equal(1, TabDragMath.ResolveMoveTarget(slots, currentIndex: 0, pointerX: 180, rightToLeft: false));
    }

    [Fact]
    public void Ltr_MoveLeft_WhenPastMidpoint()
    {
        var slots = new[] { S(0, 10, 110), S(1, 120, 220), S(2, 230, 330) };
        Assert.Equal(0, TabDragMath.ResolveMoveTarget(slots, currentIndex: 1, pointerX: 40, rightToLeft: false));
    }

    [Fact]
    public void Rtl_ExternalInsert_OnRightHalf_GoesBeforeThatTab()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(1, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 360, rightToLeft: true));
    }

    [Fact]
    public void Rtl_ExternalInsert_OnLeftHalf_GoesAfterThatTab()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(2, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 310, rightToLeft: true));
    }

    [Fact]
    public void Rtl_ExternalInsert_PastLeftEdge_Appends()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(3, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 100, rightToLeft: true));
    }

    [Fact]
    public void Rtl_ExternalInsert_PastRightEdge_InsertsAtStart()
    {
        var slots = new[] { S(0, 400, 500), S(1, 290, 390), S(2, 180, 280) };
        Assert.Equal(0, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 600, rightToLeft: true));
    }

    [Fact]
    public void Ltr_ExternalInsert_OnLeftHalf_InsertsAtThatIndex()
    {
        var slots = new[] { S(0, 10, 110), S(1, 120, 220), S(2, 230, 330) };
        Assert.Equal(0, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 40, rightToLeft: false));
        Assert.Equal(1, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 90, rightToLeft: false));
        Assert.Equal(3, TabDragMath.ResolveExternalInsertIndex(slots, pointerX: 400, rightToLeft: false));
    }

    [Fact]
    public void EmptyStrip_InsertsAtZero()
    {
        Assert.Equal(0, TabDragMath.ResolveExternalInsertIndex([], 10, rightToLeft: true));
    }
}
