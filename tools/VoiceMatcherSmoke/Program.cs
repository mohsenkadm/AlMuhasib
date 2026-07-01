using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Services;

var matcher = new VoiceCommandMatcher();
var commands = new List<VoiceCommandDefinition>
{
    new()
    {
        Id = "sale",
        DisplayLabel = "فاتورة مبيعات",
        Phrases = ["فاتورة مبيعات", "بيع", "مبيعات"],
        ActionType = VoiceCommandActionType.OpenScreen
    },
    new()
    {
        Id = "products",
        DisplayLabel = "المنتجات",
        Phrases = ["المنتجات", "منتجات"],
        ActionType = VoiceCommandActionType.OpenScreen
    },
    new()
    {
        Id = "search",
        DisplayLabel = "بحث",
        Phrases = ["بحث", "ابحث"],
        ActionType = VoiceCommandActionType.OpenGlobalSearch
    }
};

var samples = new[] { "فاتورة مبيعات", "المنتجات", "ابحث", "اغلاق", "نص عشوائي" };
var failed = 0;

foreach (var sample in samples)
{
    var match = matcher.Match(sample, commands);
    var ok = sample switch
    {
        "فاتورة مبيعات" => match?.Command.Id == "sale",
        "المنتجات" => match?.Command.Id == "products",
        "ابحث" => match?.Command.Id == "search",
        "اغلاق" => match is null,
        "نص عشوائي" => match is null,
        _ => false
    };

    Console.WriteLine($"{sample}: {(match?.Command.DisplayLabel ?? "—")} => {(ok ? "OK" : "FAIL")}");
    if (!ok) failed++;
}

return failed == 0 ? 0 : 1;
