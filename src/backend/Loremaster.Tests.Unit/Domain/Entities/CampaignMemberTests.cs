using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for the CampaignMember domain entity.
/// Tests role management and membership (EPIC 1 & 2 - Ownership & Campaign Lifecycle).
/// </summary>
public class CampaignMemberTests
{
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithDefaults_ShouldCreatePlayerMember()
    {
        // Arrange & Act
        var member = CampaignMember.Create(
            campaignId: _campaignId,
            userId: _userId);

        // Assert
        member.CampaignId.Should().Be(_campaignId);
        member.UserId.Should().Be(_userId);
        member.Role.Should().Be(CampaignRole.Player); // Default role
        member.IsPlayer.Should().BeTrue();
        member.IsMaster.Should().BeFalse();
        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithMasterRole_ShouldCreateMasterMember()
    {
        // Arrange & Act
        var member = CampaignMember.Create(
            campaignId: _campaignId,
            userId: _userId,
            role: CampaignRole.Master);

        // Assert
        member.Role.Should().Be(CampaignRole.Master);
        member.IsMaster.Should().BeTrue();
        member.IsPlayer.Should().BeFalse();
    }

    [Fact]
    public void Create_WithPlayerRole_ShouldCreatePlayerMember()
    {
        // Arrange & Act
        var member = CampaignMember.Create(
            campaignId: _campaignId,
            userId: _userId,
            role: CampaignRole.Player);

        // Assert
        member.Role.Should().Be(CampaignRole.Player);
        member.IsPlayer.Should().BeTrue();
        member.IsMaster.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetJoinedAtToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var member = CampaignMember.Create(_campaignId, _userId);

        // Assert
        var afterCreate = DateTime.UtcNow;
        member.JoinedAt.Should().BeOnOrAfter(beforeCreate);
        member.JoinedAt.Should().BeOnOrBefore(afterCreate);
    }

    #endregion

    #region ChangeRole Tests

    [Fact]
    public void ChangeRole_FromPlayerToMaster_ShouldUpdateRole()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);

        // Act
        member.ChangeRole(CampaignRole.Master);

        // Assert
        member.Role.Should().Be(CampaignRole.Master);
        member.IsMaster.Should().BeTrue();
        member.IsPlayer.Should().BeFalse();
    }

    [Fact]
    public void ChangeRole_FromMasterToPlayer_ShouldUpdateRole()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Master);

        // Act
        member.ChangeRole(CampaignRole.Player);

        // Assert
        member.Role.Should().Be(CampaignRole.Player);
        member.IsPlayer.Should().BeTrue();
        member.IsMaster.Should().BeFalse();
    }

    [Fact]
    public void ChangeRole_ToSameRole_ShouldNotChangeAnything()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);

        // Act
        member.ChangeRole(CampaignRole.Player);

        // Assert
        member.Role.Should().Be(CampaignRole.Player);
    }

    [Fact]
    public void ChangeRole_ShouldNotAffectJoinedAt()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);
        var originalJoinedAt = member.JoinedAt;

        // Act
        member.ChangeRole(CampaignRole.Master);

        // Assert
        member.JoinedAt.Should().Be(originalJoinedAt);
    }

    #endregion

    #region Role Helper Properties Tests

    [Fact]
    public void IsMaster_ForMasterRole_ShouldReturnTrue()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Master);

        // Assert
        member.IsMaster.Should().BeTrue();
    }

    [Fact]
    public void IsMaster_ForPlayerRole_ShouldReturnFalse()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);

        // Assert
        member.IsMaster.Should().BeFalse();
    }

    [Fact]
    public void IsPlayer_ForPlayerRole_ShouldReturnTrue()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);

        // Assert
        member.IsPlayer.Should().BeTrue();
    }

    [Fact]
    public void IsPlayer_ForMasterRole_ShouldReturnFalse()
    {
        // Arrange
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Master);

        // Assert
        member.IsPlayer.Should().BeFalse();
    }

    #endregion

    #region Membership Scenarios

    [Fact]
    public void Membership_OwnerJoinsCampaign_ShouldBeMaster()
    {
        // When a campaign owner joins, they should be Master
        var ownerId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var ownerMembership = CampaignMember.Create(
            campaignId, 
            ownerId, 
            CampaignRole.Master);

        ownerMembership.IsMaster.Should().BeTrue();
    }

    [Fact]
    public void Membership_PlayerJoinsViaCode_ShouldBePlayer()
    {
        // When a player joins via invite code, they should be Player
        var playerId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var playerMembership = CampaignMember.Create(
            campaignId, 
            playerId, 
            CampaignRole.Player);

        playerMembership.IsPlayer.Should().BeTrue();
    }

    [Fact]
    public void Membership_PromotePlayerToCoMaster_ShouldBecomesMaster()
    {
        // Master can promote a player to co-master
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);
        
        member.IsPlayer.Should().BeTrue();
        
        // Promotion
        member.ChangeRole(CampaignRole.Master);
        
        member.IsMaster.Should().BeTrue();
    }

    [Fact]
    public void Membership_DemoteMasterToPlayer_ShouldBecomesPlayer()
    {
        // Master can demote a co-master to player
        var member = CampaignMember.Create(_campaignId, _userId, CampaignRole.Master);
        
        member.IsMaster.Should().BeTrue();
        
        // Demotion
        member.ChangeRole(CampaignRole.Player);
        
        member.IsPlayer.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithEmptyGuidCampaignId_ShouldStillCreate()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        var member = CampaignMember.Create(
            campaignId: Guid.Empty,
            userId: _userId);

        member.CampaignId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyGuidUserId_ShouldStillCreate()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        var member = CampaignMember.Create(
            campaignId: _campaignId,
            userId: Guid.Empty);

        member.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_MultipleMembersForSameCampaign_ShouldHaveDistinctIds()
    {
        // Multiple users can join the same campaign
        var member1 = CampaignMember.Create(_campaignId, Guid.NewGuid());
        var member2 = CampaignMember.Create(_campaignId, Guid.NewGuid());
        var member3 = CampaignMember.Create(_campaignId, Guid.NewGuid());

        member1.CampaignId.Should().Be(_campaignId);
        member2.CampaignId.Should().Be(_campaignId);
        member3.CampaignId.Should().Be(_campaignId);

        // Users should be different
        member1.UserId.Should().NotBe(member2.UserId);
        member2.UserId.Should().NotBe(member3.UserId);
    }

    [Fact]
    public void Create_SameUserInDifferentCampaigns_ShouldWork()
    {
        // A user can be a member of multiple campaigns
        var userId = Guid.NewGuid();
        var campaign1 = Guid.NewGuid();
        var campaign2 = Guid.NewGuid();

        var membership1 = CampaignMember.Create(campaign1, userId, CampaignRole.Master);
        var membership2 = CampaignMember.Create(campaign2, userId, CampaignRole.Player);

        membership1.UserId.Should().Be(userId);
        membership2.UserId.Should().Be(userId);
        membership1.CampaignId.Should().NotBe(membership2.CampaignId);
        
        // Can have different roles in different campaigns
        membership1.IsMaster.Should().BeTrue();
        membership2.IsPlayer.Should().BeTrue();
    }

    #endregion
}
