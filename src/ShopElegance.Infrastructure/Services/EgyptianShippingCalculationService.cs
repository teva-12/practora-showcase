using ShopElegance.Core.Constants;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Infrastructure.Services;

/// <summary>
/// Flat, zone-based shipping rates for Egypt with custom rules for Zagazig and high-value orders.
/// </summary>
public class EgyptianShippingCalculationService : IShippingCalculationService
{
    private const decimal FreeShippingThreshold = 1000m;

    private static readonly string[] ZagazigKeywords =
    {
        "zagazig", "el zagazig", "elzagazig", "الزقازيق", "زقازيق", "zag"
    };

    private static readonly HashSet<string> ZoneOne = new() { "Cairo", "Giza", "Qalyubia" };

    private static readonly HashSet<string> ZoneTwo = new()
    {
        "Alexandria", "Port Said", "Suez", "Ismailia", "Dakahlia", "Sharqia",
        "Gharbia", "Monufia", "Beheira", "Kafr El Sheikh", "Damietta", "Faiyum", "Beni Suef"
    };

    public IReadOnlyList<string> GetGovernorates() => EgyptianGovernorates.All;

    public ShippingCalculationResultDto Calculate(string governorate, decimal subTotal, string? city = null)
    {
        if (!EgyptianGovernorates.IsValid(governorate))
        {
            return new ShippingCalculationResultDto
            {
                Governorate = governorate,
                IsValid = false,
                ErrorMessage = "Please select a valid Egyptian governorate."
            };
        }

        // حساب عدد أيام التوصيل التقريبية بناءً على الزون
        int days = ZoneOne.Contains(governorate) ? 2
            : ZoneTwo.Contains(governorate) ? 3
            : 5;

        decimal cost;
        bool isFreeLocalDelivery = false;

        // 1. الشرط الأول: شحن مجاني لأي أوردر 1000 جنيه أو أكثر لأي مكان في مصر
        if (subTotal >= FreeShippingThreshold)
        {
            cost = 0m;
            isFreeLocalDelivery = true;
        }
        // 2. الشرط الثاني: لو الأوردر أقل من 1000 والمدينة الزقازيق = الشحن بـ 9 جنيه
        else if (IsZagazig(city))
        {
            cost = 9m;
        }
        // 3. الشرط الثالث: باقي المدن والمحافظات (فاقوس أو غيرها) = الشحن بـ 50 جنيه
        else
        {
            cost = 50m;
        }

        return new ShippingCalculationResultDto
        {
            Governorate = governorate,
            ShippingCost = cost,
            EstimatedDeliveryDays = days,
            IsValid = true,
            IsFreeLocalDelivery = isFreeLocalDelivery
        };
    }

    private static bool IsZagazig(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return false;
        var normalized = city.Trim();
        return ZagazigKeywords.Any(keyword =>
            normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}