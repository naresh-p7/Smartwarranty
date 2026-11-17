using SmartWarranty_2.Models;

namespace SmartWarranty_2.Services;

public static class WarrantyStatusService
{
    public const int ExpiringSoonThresholdDays = 30;

    public static string GetStatus(Warranty warranty)
    {
        var today = DateTime.UtcNow;

        if (warranty.ExpiryDate < today)
        {
            return "Expired";
        }

        if (warranty.ExpiryDate <= today.AddDays(ExpiringSoonThresholdDays))
        {
            return "Expiring Soon";
        }

        return "Active";
    }

    public static bool IsEligible(Warranty warranty)
    {
        return warranty.StartDate <= DateTime.UtcNow && warranty.ExpiryDate >= DateTime.UtcNow;
    }
}
