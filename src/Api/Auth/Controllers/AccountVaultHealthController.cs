using Bit.Api.Auth.Models.Response.Accounts;
using Bit.Core.Auth.Identity;
using Bit.Core.Services;
using Bit.Core.Vault.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Auth.Controllers;

[Route("account")]
[Authorize(Policies.Application)]
public class AccountVaultHealthController : Controller
{
    private readonly IUserService _userService;
    private readonly IGetVaultHealthAnalysisQuery _getVaultHealthAnalysisQuery;

    public AccountVaultHealthController(
        IUserService userService,
        IGetVaultHealthAnalysisQuery getVaultHealthAnalysisQuery)
    {
        _userService = userService;
        _getVaultHealthAnalysisQuery = getVaultHealthAnalysisQuery;
    }

    [HttpGet("vault-health-analysis")]
    public async Task<ActionResult<VaultHealthAnalysisResponseModel>> GetVaultHealthAnalysisAsync()
    {
        var user = await _userService.GetUserByPrincipalAsync(User);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }

        var analysis = await _getVaultHealthAnalysisQuery.GetByUserIdAsync(user.Id);
        return new VaultHealthAnalysisResponseModel(analysis);
    }
}
