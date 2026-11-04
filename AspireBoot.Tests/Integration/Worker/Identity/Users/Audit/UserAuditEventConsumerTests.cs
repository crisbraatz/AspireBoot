using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Infrastructure.Identity.Users.Audit;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Tracking;

namespace AspireBoot.Tests.Integration.Worker.Identity.Users.Audit;

[Trait("Category", "Integration")]
public class UserAuditEventConsumerTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldPersistAnAuditOfTheChangedFields()
    {
        User user = await CreateAccountAsync();
        UserAuditEvent userAuditEvent = new(user.Id, UserAuditEvent.Update, [UserAuditFields.Name]);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(userAuditEvent);

        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserAudit userAudit = await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().UserAudits.SingleAsync(itemUserAudit => itemUserAudit.MessageId == userAuditEvent.MessageId, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, userAudit.UserId);
        Assert.Equal(UserAuditEvent.Update, userAudit.Action);
    }

    [Fact]
    public async Task ShouldNotDuplicateAnAlreadyPersistedAudit()
    {
        UserAuditEvent userAuditEvent = new((await CreateAccountAsync()).Id, UserAuditEvent.Update, [UserAuditFields.Name]);
        await using (AsyncServiceScope setupAsyncServiceScope = TestFixture.Host.Services.CreateAsyncScope())
            await setupAsyncServiceScope.ServiceProvider.GetRequiredService<IUserAuditRepository>().AddAsync(userAuditEvent, TestContext.Current.CancellationToken);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(userAuditEvent);

        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.Equal(1, await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().UserAudits.CountAsync(itemUserAudit => itemUserAudit.MessageId == userAuditEvent.MessageId, TestContext.Current.CancellationToken));
    }

    private async Task<User> CreateAccountAsync(string identityUserId = "account", string email = "user@example.com", bool isEmailConfirmed = true, string role = "User")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? identityUser = await userManager.FindByIdAsync(identityUserId);
        if (identityUser is null)
        {
            identityUser = new IdentityUser { Id = identityUserId, UserName = identityUserId, Email = email, EmailConfirmed = isEmailConfirmed };
            Assert.True((await userManager.CreateAsync(identityUser, "Test-Password123!")).Succeeded);
        }
        RoleManager<IdentityRole> roleManager = asyncServiceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(role))
            Assert.True((await roleManager.CreateAsync(new IdentityRole(role))).Succeeded);
        if (!await userManager.IsInRoleAsync(identityUser, role))
            Assert.True((await userManager.AddToRoleAsync(identityUser, role)).Succeeded);
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        User? profileUser = await aspireBootDbContext.Users.SingleOrDefaultAsync(user => user.IdentityUserId == identityUserId);
        if (profileUser is null)
        {
            profileUser = new User(identityUserId, email, "User Name");
            aspireBootDbContext.Users.Add(profileUser);
            await aspireBootDbContext.SaveChangesAsync();
        }
        return profileUser;
    }
}
