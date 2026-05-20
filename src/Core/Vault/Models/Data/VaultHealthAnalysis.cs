namespace Bit.Core.Vault.Models.Data;

public class VaultHealthAnalysis
{
    public VaultHealthAnalysis(
        int totalItems,
        int personalItems,
        int organizationItems,
        int favoriteItems,
        int deletedItems,
        int archivedItems,
        int folderCount,
        int sendCount,
        DateTime analyzedAt)
    {
        TotalItems = totalItems;
        PersonalItems = personalItems;
        OrganizationItems = organizationItems;
        FavoriteItems = favoriteItems;
        DeletedItems = deletedItems;
        ArchivedItems = archivedItems;
        FolderCount = folderCount;
        SendCount = sendCount;
        AnalyzedAt = analyzedAt;
    }

    public int TotalItems { get; }
    public int PersonalItems { get; }
    public int OrganizationItems { get; }
    public int FavoriteItems { get; }
    public int DeletedItems { get; }
    public int ArchivedItems { get; }
    public int FolderCount { get; }
    public int SendCount { get; }
    public DateTime AnalyzedAt { get; }
}
