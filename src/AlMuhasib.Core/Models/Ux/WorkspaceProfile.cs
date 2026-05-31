namespace AlMuhasib.Core.Models.Ux;

/// <summary>ملف عمل مبسّط لتخصيص الشريط السريع والتركيز اليومي.</summary>
public enum WorkspaceProfile
{
  /// <summary>كل الأدوات ظاهرة.</summary>
  Full,

  /// <summary>كاشير: بيع، POS، قبض، أقساط.</summary>
  Cashier,

  /// <summary>محاسب: تقارير، سندات، بدون POS.</summary>
  Accountant
}
