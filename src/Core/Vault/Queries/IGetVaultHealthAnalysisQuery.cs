using Bit.Core.Vault.Models.Data;

namespace Bit.Core.Vault.Queries;

public interface IGetVaultHealthAnalysisQuery
{
    Task<VaultHealthAnalysis> GetByUserIdAsync(Guid userId);
}
