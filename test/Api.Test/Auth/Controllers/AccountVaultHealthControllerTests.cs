using System.Reflection;
using System.Security.Claims;
using Bit.Api.Auth.Controllers;
using Bit.Api.Auth.Models.Response.Accounts;
using Bit.Core.Auth.Identity;
using Bit.Core.Entities;
using Bit.Core.Services;
using Bit.Core.Vault.Models.Data;
using Bit.Core.Vault.Queries;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Bit.Api.Test.Auth.Controllers;

public class AccountVaultHealthControllerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IGetVaultHealthAnalysisQuery _getVaultHealthAnalysisQuery =
        Substitute.For<IGetVaultHealthAnalysisQuery>();
    private readonly AccountVaultHealthController _sut;

    public AccountVaultHealthControllerTests()
    {
        _sut = new AccountVaultHealthController(_userService, _getVaultHealthAnalysisQuery);
    }

    [Fact]
    public async Task GetVaultHealthAnalysisAsync_ReturnsCurrentUsersAnalysis()
    {
        var user = new User { Id = Guid.NewGuid() };
        var analysis = new VaultHealthAnalysis(
            totalItems: 8,
            personalItems: 3,
            organizationItems: 5,
            favoriteItems: 2,
            deletedItems: 1,
            archivedItems: 4,
            folderCount: 6,
            sendCount: 7,
            analyzedAt: DateTime.UtcNow);

        _userService.GetUserByPrincipalAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(user);
        _getVaultHealthAnalysisQuery.GetByUserIdAsync(user.Id)
            .Returns(analysis);

        var result = await _sut.GetVaultHealthAnalysisAsync();

        var response = Assert.IsType<VaultHealthAnalysisResponseModel>(result.Value);
        Assert.Equal(analysis.TotalItems, response.TotalItems);
        Assert.Equal(analysis.PersonalItems, response.PersonalItems);
        Assert.Equal(analysis.OrganizationItems, response.OrganizationItems);
        Assert.Equal(analysis.FavoriteItems, response.FavoriteItems);
        Assert.Equal(analysis.DeletedItems, response.DeletedItems);
        Assert.Equal(analysis.ArchivedItems, response.ArchivedItems);
        Assert.Equal(analysis.FolderCount, response.FolderCount);
        Assert.Equal(analysis.SendCount, response.SendCount);
        Assert.Equal(analysis.AnalyzedAt, response.AnalyzedAt);

        await _getVaultHealthAnalysisQuery.Received(1).GetByUserIdAsync(user.Id);
    }

    [Fact]
    public async Task GetVaultHealthAnalysisAsync_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userService.GetUserByPrincipalAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Task.FromResult<User>(null!));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetVaultHealthAnalysisAsync());

        await _getVaultHealthAnalysisQuery.DidNotReceiveWithAnyArgs().GetByUserIdAsync(default);
    }

    [Fact]
    public void Controller_RequiresApplicationAuthentication()
    {
        var authorizeAttribute = typeof(AccountVaultHealthController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(Policies.Application, authorizeAttribute.Policy);
    }
}
