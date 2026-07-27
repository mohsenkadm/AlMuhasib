using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Services;

public sealed class OfflineReminderService : IOfflineReminderService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserPreferencesService _preferences;
    private readonly ISystemProfileService _systemProfile;
    private readonly DispatcherTimer _timer;
    private readonly string _statePath;
    private HashSet<string> _notifiedToday = new(StringComparer.Ordinal);

    public event Action<OfflineReminderEvent>? ReminderRaised;

    public OfflineReminderService(
        IServiceScopeFactory scopeFactory,
        IUserPreferencesService preferences,
        ISystemProfileService systemProfile)
    {
        _scopeFactory = scopeFactory;
        _preferences = preferences;
        _systemProfile = systemProfile;
        _statePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "reminder-state.json");
        LoadState();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (_, _) => await CheckRemindersAsync();
    }

    public void Start()
    {
        if (_preferences.Current.Reminders.InstallmentRemindersEnabled
            || _preferences.Current.Reminders.RealEstateDebtRemindersEnabled)
            _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private async Task CheckRemindersAsync()
    {
        try
        {
            var today = DateTime.Today;
            if (_lastStateDate != today)
            {
                _notifiedToday.Clear();
                _lastStateDate = today;
                SaveState();
            }

            await using var scope = _scopeFactory.CreateAsyncScope();

            if (_systemProfile.ActiveSystem == ApplicationSystemType.RealEstateContracts
                && _preferences.Current.Reminders.RealEstateDebtRemindersEnabled)
            {
                await CheckRealEstateDebtsAsync(scope.ServiceProvider, today);
            }
            else if (_preferences.Current.Reminders.InstallmentRemindersEnabled)
            {
                await CheckInstallmentsAsync(scope.ServiceProvider, today);
            }

            SaveState();
        }
        catch
        {
            // silent — offline reminders should not crash the app
        }
    }

    private async Task CheckInstallmentsAsync(IServiceProvider sp, DateTime today)
    {
        var installmentService = sp.GetService<IInstallmentService>();
        if (installmentService is null)
            return;

        await installmentService.UpdateOverdueStatusesAsync();

        var overdue = await installmentService.GetOverdueInstallmentsAsync();
        foreach (var inst in overdue)
        {
            var key = $"overdue-{inst.Id}-{today:yyyyMMdd}";
            if (_notifiedToday.Contains(key)) continue;
            _notifiedToday.Add(key);
            ReminderRaised?.Invoke(new OfflineReminderEvent
            {
                Id = key,
                Title = "قسط متأخر",
                Message = $"قسط متأخر بمبلغ {inst.RemainingAmount:N0} د.ع — استحقاق {inst.DueDate:yyyy/MM/dd}",
                IsOverdue = true
            });
        }

        var dueToday = (await installmentService.GetPagedInstallmentsAsync(1, 500))
            .Items.Where(i => i.DueDate.Date == today && i.RemainingAmount > 0
                              && i.Status != InstallmentStatus.Paid);
        foreach (var inst in dueToday)
        {
            var key = $"today-{inst.Id}-{today:yyyyMMdd}";
            if (_notifiedToday.Contains(key)) continue;
            _notifiedToday.Add(key);
            ReminderRaised?.Invoke(new OfflineReminderEvent
            {
                Id = key,
                Title = "قسط مستحق اليوم",
                Message = $"قسط مستحق اليوم بمبلغ {inst.RemainingAmount:N0} د.ع",
                IsOverdue = false
            });
        }
    }

    private async Task CheckRealEstateDebtsAsync(IServiceProvider sp, DateTime today)
    {
        var contractService = sp.GetService<IRealEstateContractService>();
        if (contractService is null)
            return;

        var debts = await contractService.GetDebtsAsync(overdueOnly: false);
        foreach (var debt in debts)
        {
            if (debt.IsOverdue)
            {
                var key = $"re-overdue-{debt.ContractId}-{today:yyyyMMdd}";
                if (_notifiedToday.Contains(key)) continue;
                _notifiedToday.Add(key);
                ReminderRaised?.Invoke(new OfflineReminderEvent
                {
                    Id = key,
                    Title = "دين عقاري متأخر",
                    Message = $"{debt.DebtorName} — متبقي {debt.RemainingAmount:N0} — عقد {debt.ContractNumber}",
                    IsOverdue = true
                });
            }
            else if (debt.DueDate?.Date == today)
            {
                var key = $"re-today-{debt.ContractId}-{today:yyyyMMdd}";
                if (_notifiedToday.Contains(key)) continue;
                _notifiedToday.Add(key);
                ReminderRaised?.Invoke(new OfflineReminderEvent
                {
                    Id = key,
                    Title = "دين عقاري مستحق اليوم",
                    Message = $"{debt.DebtorName} — متبقي {debt.RemainingAmount:N0} — عقد {debt.ContractNumber}",
                    IsOverdue = false
                });
            }
        }
    }

    private DateTime _lastStateDate = DateTime.Today;

    private void LoadState()
    {
        if (!File.Exists(_statePath)) return;
        try
        {
            var json = File.ReadAllText(_statePath);
            var state = JsonSerializer.Deserialize<ReminderState>(json);
            if (state is null) return;
            _lastStateDate = state.Date;
            if (state.Date == DateTime.Today)
                _notifiedToday = state.Keys.ToHashSet(StringComparer.Ordinal);
        }
        catch { /* ignore */ }
    }

    private void SaveState()
    {
        try
        {
            var state = new ReminderState { Date = DateTime.Today, Keys = _notifiedToday.ToList() };
            var dir = Path.GetDirectoryName(_statePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ignore */ }
    }

    private sealed class ReminderState
    {
        public DateTime Date { get; set; }
        public List<string> Keys { get; set; } = [];
    }
}
