namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Pure hit-testing for a horizontal tab strip. Coordinates are in the same space as the slot edges
/// (screen pixels). RTL layouts place index 0 on the right.
/// </summary>
public static class TabDragMath
{
    public readonly record struct Slot(int Index, double Left, double Right);

    /// <summary>Index to pass to a list move while dragging inside the same strip.</summary>
    public static int ResolveMoveTarget(
        IReadOnlyList<Slot> slots,
        int currentIndex,
        double pointerX,
        bool rightToLeft)
    {
        if (currentIndex < 0 || slots.Count == 0)
            return currentIndex;

        foreach (var slot in slots)
        {
            if (slot.Index == currentIndex)
                continue;
            if (pointerX < slot.Left || pointerX > slot.Right)
                continue;

            var mid = (slot.Left + slot.Right) / 2.0;
            if (!rightToLeft)
            {
                if (currentIndex < slot.Index && pointerX >= mid)
                    return slot.Index;
                if (currentIndex > slot.Index && pointerX <= mid)
                    return slot.Index;
            }
            else
            {
                if (currentIndex < slot.Index && pointerX <= mid)
                    return slot.Index;
                if (currentIndex > slot.Index && pointerX >= mid)
                    return slot.Index;
            }

            return currentIndex;
        }

        if (!rightToLeft)
        {
            if (pointerX < slots.Min(s => s.Left))
                return 0;
            if (pointerX > slots.Max(s => s.Right))
                return slots.Max(s => s.Index);
            return currentIndex;
        }

        if (pointerX > slots.Max(s => s.Right))
            return 0;
        if (pointerX < slots.Min(s => s.Left))
            return slots.Max(s => s.Index);
        return currentIndex;
    }

    /// <summary>Insertion index for a tab that is not yet in this strip.</summary>
    public static int ResolveExternalInsertIndex(
        IReadOnlyList<Slot> slots,
        double pointerX,
        bool rightToLeft)
    {
        if (slots.Count == 0)
            return 0;

        foreach (var slot in slots.OrderBy(s => s.Index))
        {
            var mid = (slot.Left + slot.Right) / 2.0;
            if (!rightToLeft)
            {
                if (pointerX < mid)
                    return slot.Index;
            }
            else if (pointerX > mid)
            {
                return slot.Index;
            }
        }

        return slots.Max(s => s.Index) + 1;
    }
}
