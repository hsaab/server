using Bit.Core.Models.Api;
using Bit.Core.Vault.Models.Data;

namespace Bit.Api.Auth.Models.Response.Accounts;

public class VaultHealthAnalysisResponseModel : ResponseModel
{
    public VaultHealthAnalysisResponseModel(VaultHealthAnalysis analysis)
        : base("vaultHealthAnalysis")
    {
        TotalItems = analysis.TotalItems;
        PersonalItems = analysis.PersonalItems;
        OrganizationItems = analysis.OrganizationItems;
        FavoriteItems = analysis.FavoriteItems;
        DeletedItems = analysis.DeletedItems;
        ArchivedItems = analysis.ArchivedItems;
        FolderCount = analysis.FolderCount;
        SendCount = analysis.SendCount;
        AnalyzedAt = analysis.AnalyzedAt;
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
