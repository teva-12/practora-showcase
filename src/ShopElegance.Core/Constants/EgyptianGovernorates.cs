namespace ShopElegance.Core.Constants;

/// <summary>The 27 Egyptian governorates, used for shipping address selection and fee calculation.</summary>
public static class EgyptianGovernorates
{
    public static readonly IReadOnlyList<string> All = new List<string>
    {
        "Cairo", "Giza", "Alexandria", "Qalyubia", "Port Said", "Suez",
        "Dakahlia", "Sharqia", "Gharbia", "Monufia", "Beheira", "Kafr El Sheikh",
        "Damietta", "Ismailia", "North Sinai", "South Sinai",
        "Faiyum", "Beni Suef", "Minya", "Asyut", "Sohag", "Qena", "Luxor", "Aswan",
        "Red Sea", "New Valley", "Matrouh"
    };

    public static bool IsValid(string? governorate) =>
        !string.IsNullOrWhiteSpace(governorate) && All.Contains(governorate);
}
