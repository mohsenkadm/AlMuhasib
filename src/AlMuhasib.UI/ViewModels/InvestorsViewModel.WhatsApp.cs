using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class InvestorsViewModel
{
    private InvestorTransactionPrintModel? _pendingWhatsAppReceipt;
    private List<ProfitPreviewItem> _pendingProfitWhatsApp = [];

    [ObservableProperty]
    private bool _canSendWhatsAppReceipt;

    [ObservableProperty]
    private bool _canSendProfitWhatsApp;

    private void ClearWhatsAppReceiptOption()
    {
        _pendingWhatsAppReceipt = null;
        CanSendWhatsAppReceipt = false;
    }

    private void StageWhatsAppReceipt(InvestorTransactionPrintModel receipt)
    {
        _pendingWhatsAppReceipt = receipt;
        CanSendWhatsAppReceipt = true;
    }

    private void StageProfitWhatsApp(IEnumerable<ProfitPreviewItem> items, DateTime distributionDate)
    {
        _pendingProfitWhatsApp = items
            .Where(p => p.IsIncluded && p.ProfitAmount > 0)
            .Select(p => new ProfitPreviewItem
            {
                InvestorId = p.InvestorId,
                InvestorName = p.InvestorName,
                InvestorPhone = p.InvestorPhone,
                TotalDeposit = p.TotalDeposit,
                EligibleDeposit = p.EligibleDeposit,
                ProfitPercentage = p.ProfitPercentage,
                ProfitAmount = p.ProfitAmount,
                IsIncluded = true
            })
            .ToList();
        _pendingProfitDistributionDate = distributionDate;
        CanSendProfitWhatsApp = _pendingProfitWhatsApp.Count > 0;
    }

    private DateTime _pendingProfitDistributionDate = DateTime.Now;

    [RelayCommand(CanExecute = nameof(CanSendWhatsAppReceipt))]
    private void SendPendingWhatsAppReceipt()
    {
        if (_pendingWhatsAppReceipt is null) return;
        _whatsAppShare.ShareInvestorTransaction(_pendingWhatsAppReceipt);
    }

    [RelayCommand]
    private void SendTransactionWhatsApp(InvestorTransaction? transaction)
    {
        if (transaction is null) return;
        _whatsAppShare.ShareInvestorTransaction(BuildTransactionPrintModel(transaction));
    }

    [RelayCommand]
    private void SendProfitPreviewWhatsApp(ProfitPreviewItem? item)
    {
        if (item is null || item.ProfitAmount <= 0) return;
        _whatsAppShare.ShareInvestorTransaction(BuildProfitPrintModel(item, DistributionDate));
    }

    [RelayCommand(CanExecute = nameof(CanSendProfitWhatsApp))]
    private void SendPendingProfitWhatsApp()
    {
        if (_pendingProfitWhatsApp.Count == 0) return;

        var item = _pendingProfitWhatsApp[0];
        _whatsAppShare.ShareInvestorTransaction(BuildProfitPrintModel(item, _pendingProfitDistributionDate));
        _pendingProfitWhatsApp.RemoveAt(0);
        CanSendProfitWhatsApp = _pendingProfitWhatsApp.Count > 0;
    }

    [RelayCommand]
    private void ShareStatementWhatsApp()
    {
        if (StatementInvestor is null || StatementDetails.Count == 0) return;

        var model = new StatementPrintModel
        {
            Title = $"كشف أرباح — {StatementInvestor.Name}",
            PartyName = StatementInvestor.Name,
            PartyPhone = StatementInvestor.Phone,
            Columns = ["التاريخ", "النسبة %", "المبلغ"],
            Rows = StatementDetails.Select(d => new object[]
            {
                d.ProfitDistribution.Date.ToString("yyyy/MM/dd"),
                d.ProfitPercentage,
                d.Amount
            }).ToList(),
            SummaryLines =
            [
                $"رصيد الإيداع: {StatementCurrentDeposit:N0} د.ع",
                $"إجمالي الأرباح: {StatementTotalProfits:N0} د.ع"
            ]
        };

        _whatsAppShare.ShareStatement(model, StatementInvestor.Phone, StatementInvestor.Name);
    }

    private static InvestorTransactionPrintModel BuildTransactionPrintModel(InvestorTransaction transaction)
    {
        var typeLabel = transaction.Type switch
        {
            InvestorTransactionType.Deposit => "إيداع",
            InvestorTransactionType.Withdrawal => "سحب",
            InvestorTransactionType.ProfitDistribution => "توزيع أرباح",
            InvestorTransactionType.OpeningBalance => "رصيد افتتاحي",
            _ => "حركة مستثمر"
        };

        return new InvestorTransactionPrintModel
        {
            Title = $"إيصال {typeLabel} مستثمر",
            TransactionTypeLabel = typeLabel,
            InvestorName = transaction.Investor?.Name ?? "—",
            InvestorPhone = transaction.Investor?.Phone,
            Date = transaction.Date,
            Amount = transaction.Amount,
            Notes = transaction.Notes,
            BalanceAfter = transaction.Investor?.TotalDeposit
        };
    }

    private InvestorTransactionPrintModel BuildDepositPrintModel(
        Investor investor, decimal amount, DateTime date, string? cashBoxName, string? notes)
    {
        return new InvestorTransactionPrintModel
        {
            Title = "إيصال إيداع مستثمر",
            TransactionTypeLabel = "إيداع",
            InvestorName = investor.Name,
            InvestorPhone = investor.Phone,
            Date = date,
            Amount = amount,
            CashBoxName = cashBoxName,
            Notes = notes,
            BalanceAfter = investor.TotalDeposit + amount
        };
    }

    private InvestorTransactionPrintModel BuildWithdrawalPrintModel(
        Investor investor, decimal amount, DateTime date, string? cashBoxName, string? notes)
    {
        return new InvestorTransactionPrintModel
        {
            Title = "إيصال سحب مستثمر",
            TransactionTypeLabel = "سحب",
            InvestorName = investor.Name,
            InvestorPhone = investor.Phone,
            Date = date,
            Amount = amount,
            CashBoxName = cashBoxName,
            Notes = notes,
            BalanceAfter = Math.Max(0, investor.TotalDeposit - amount)
        };
    }

    private static InvestorTransactionPrintModel BuildProfitPrintModel(ProfitPreviewItem item, DateTime date)
    {
        return new InvestorTransactionPrintModel
        {
            Title = "إيصال توزيع أرباح",
            TransactionTypeLabel = "توزيع أرباح",
            InvestorName = item.InvestorName,
            InvestorPhone = item.InvestorPhone,
            Date = date,
            Amount = item.ProfitAmount,
            Notes = $"نسبة الربح: {item.ProfitPercentage:N2}% — الإيداع المؤهل: {item.EligibleDeposit:N0} د.ع"
        };
    }

    partial void OnCanSendWhatsAppReceiptChanged(bool value) =>
        SendPendingWhatsAppReceiptCommand.NotifyCanExecuteChanged();

    partial void OnCanSendProfitWhatsAppChanged(bool value) =>
        SendPendingProfitWhatsAppCommand.NotifyCanExecuteChanged();
}
