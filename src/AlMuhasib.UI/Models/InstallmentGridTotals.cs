using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class InstallmentGridTotals : ObservableObject
{
    [ObservableProperty] private int _count;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private string _scopeNote = string.Empty;

    public bool HasScopeNote => !string.IsNullOrWhiteSpace(ScopeNote);

    partial void OnScopeNoteChanged(string value) => OnPropertyChanged(nameof(HasScopeNote));

    public void Clear(string scopeNote = "")
    {
        Count = 0;
        TotalAmount = 0;
        PaidAmount = 0;
        RemainingAmount = 0;
        ScopeNote = scopeNote;
    }

    public void SetFromInstallments(IEnumerable<Installment> items, string? scopeNote = null)
    {
        var list = items.ToList();
        Count = list.Count;
        TotalAmount = list.Sum(i => i.Amount);
        PaidAmount = list.Sum(i => i.PaidAmount);
        RemainingAmount = list.Sum(i => i.RemainingAmount);
        if (scopeNote is not null)
            ScopeNote = scopeNote;
    }

    public void SetFromTotals(int count, decimal totalAmount, decimal paidAmount, decimal remainingAmount, string? scopeNote = null)
    {
        Count = count;
        TotalAmount = totalAmount;
        PaidAmount = paidAmount;
        RemainingAmount = remainingAmount;
        if (scopeNote is not null)
            ScopeNote = scopeNote;
    }

    public void SetFromPlans(IEnumerable<InstallmentPlan> plans, string? scopeNote = null)
    {
        var list = plans.ToList();
        Count = list.Count;
        TotalAmount = list.Sum(p => p.TotalAmount);
        var installments = list.SelectMany(p => p.Installments).ToList();
        PaidAmount = installments.Sum(i => i.PaidAmount);
        RemainingAmount = installments.Sum(i => i.RemainingAmount);
        if (scopeNote is not null)
            ScopeNote = scopeNote;
    }

    public IList<string> ToPrintSummary()
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(ScopeNote))
            lines.Add(ScopeNote);
        lines.Add($"عدد السجلات: {Count:N0}");
        lines.Add($"إجمالي المبالغ: {TotalAmount:N0} د.ع");
        lines.Add($"المسدد: {PaidAmount:N0} د.ع");
        lines.Add($"المتبقي: {RemainingAmount:N0} د.ع");
        return lines;
    }
}
