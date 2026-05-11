namespace OfisYonetimSistemi.Security;

public static class RolePermissions
{
    public static bool IsLoggedIn(string? roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName);
    }

    public static bool IsManager(string? roleName)
    {
        return roleName == "Admin" || roleName == "Mudur";
    }

    public static bool CanCreateProject(string? roleName)
    {
        return IsManager(roleName);
    }

    public static bool CanEditProject(string? roleName)
    {
        return IsManager(roleName) || roleName == "Sekreter";
    }

    public static bool CanDeleteProject(string? roleName)
    {
        return IsManager(roleName);
    }

    public static bool CanManageApartmentInfo(string? roleName)
    {
        return IsManager(roleName) || roleName == "Sekreter";
    }

    public static bool CanManageApartmentSales(string? roleName)
    {
        return IsManager(roleName);
    }

    public static bool CanViewApartmentDetails(string? roleName)
    {
        return IsLoggedIn(roleName);
    }

    public static bool CanManageProjectExpenses(string? roleName)
    {
        return IsManager(roleName)
            || roleName == "Sekreter"
            || roleName == "Muhasebeci"
            || roleName == "Muhasebe";
    }

    public static bool CanManageProjectDocuments(string? roleName)
    {
        return CanManageProjectExpenses(roleName);
    }

    public static bool CanManageProjectMaterials(string? roleName)
    {
        return IsManager(roleName) || roleName == "Sekreter";
    }
}
