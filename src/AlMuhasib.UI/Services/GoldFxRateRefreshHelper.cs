using System.Windows;
using AlMuhasib.UI.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace AlMuhasib.UI.Services;

public static class GoldFxRateRefreshHelper
{
    public static void Publish(decimal usdToIqd, DateTime rateDate) =>
        WeakReferenceMessenger.Default.Send(new GoldFxRateChangedMessage
        {
            UsdToIqd = usdToIqd,
            RateDate = rateDate
        });

    public static void Register(object recipient, Func<decimal, Task> applyRateAsync) =>
        WeakReferenceMessenger.Default.Register<GoldFxRateChangedMessage>(recipient, (_, msg) =>
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await applyRateAsync(msg.UsdToIqd);
            });
        });
}
