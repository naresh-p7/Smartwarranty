using SmartWarranty_2.Models;

namespace SmartWarranty_2.Services;

public static class WarrantyStatusService
{
    public const int ExpiringSoonThresholdDays = 30;

    public static string GetStatus(Warranty warranty)
    {
        var today = DateTime.UtcNow.Date;
        var startDate = warranty.StartDate.Date;
        var expiryDate = warranty.ExpiryDate.Date;

        if (expiryDate < today)
        {
            return "Expired";
        }

        if (startDate > today)
        {
            return "Not Started";
        }

        if (expiryDate <= today.AddDays(ExpiringSoonThresholdDays))
        {
            return "Expiring Soon";
        }

        return "Active";
    }

    public static bool IsEligible(Warranty warranty)
    {
        var today = DateTime.UtcNow.Date;
        return warranty.StartDate.Date <= today && warranty.ExpiryDate.Date >= today;
    }
}
