using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Models;

public sealed class BarcodeLabelSizeOption
{
    public BarcodeLabelSizeOption(BarcodeLabelSize size, string displayName)
    {
        Size = size;
        DisplayName = displayName;
    }

    public BarcodeLabelSize Size { get; }
    public string DisplayName { get; }
}
