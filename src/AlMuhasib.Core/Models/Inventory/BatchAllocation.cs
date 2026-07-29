namespace AlMuhasib.Core.Models.Inventory;

/// <summary>تخصيص كمية من دفعة واحدة ضمن توزيع FEFO.</summary>
public sealed record BatchAllocation(int BatchId, decimal Quantity);
