namespace AlMuhasib.Core.Enums;

/// <summary>أوضاع تقريب أسعار البيع عند التعديل الجماعي.</summary>
public enum PriceRoundingMode
{
    /// <summary>بدون تقريب — الإبقاء على ناتج النسبة كما هو (ضمن دقة decimal).</summary>
    None = 0,

    /// <summary>رقمين عشريين.</summary>
    TwoDecimals = 1,

    /// <summary>ثلاثة أرقام عشرية.</summary>
    ThreeDecimals = 2,

    /// <summary>أقرب 0.25.</summary>
    NearestQuarter = 3,

    /// <summary>أقرب 0.5.</summary>
    NearestHalf = 4,

    /// <summary>أقرب واحد صحيح.</summary>
    NearestOne = 5,

    /// <summary>للأعلى إلى مضاعف صحيح (Ceiling).</summary>
    CeilingToOne = 6
}
