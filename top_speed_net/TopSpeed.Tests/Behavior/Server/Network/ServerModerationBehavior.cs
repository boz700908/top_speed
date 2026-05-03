using System.Collections.Generic;
using TopSpeed.Server.Config;
using TopSpeed.Server.Moderation;
using Xunit;

namespace TopSpeed.Tests;

public sealed class ServerModerationBehaviorTests
{
    [Fact]
    public void NameModeration_ShouldRejectNameThatExceedsConfiguredLimit()
    {
        var settings = new ServerModerationSettings
        {
            MaxNameLength = 5,
            BlockRepeatedLettersInName = false,
            AllowDuplicateNames = true
        };

        var result = NameModeration.Validate(settings, "123456", 1, new List<ModerationNameEntry>());

        result.Accepted.Should().BeFalse();
        result.RejectReasonCode.Should().Be("name_too_long");
    }

    [Fact]
    public void NameModeration_ShouldRejectRepeatedLettersWhenEnabled()
    {
        var settings = new ServerModerationSettings
        {
            MaxNameLength = 40,
            BlockRepeatedLettersInName = true,
            AllowDuplicateNames = true
        };

        var result = NameModeration.Validate(settings, "loool", 1, new List<ModerationNameEntry>());

        result.Accepted.Should().BeFalse();
        result.RejectReasonCode.Should().Be("name_repeated_letters");
    }

    [Fact]
    public void NameModeration_ShouldAllowRepeatedLettersWhenDisabled()
    {
        var settings = new ServerModerationSettings
        {
            MaxNameLength = 40,
            BlockRepeatedLettersInName = false,
            AllowDuplicateNames = true
        };

        var result = NameModeration.Validate(settings, "loool", 1, new List<ModerationNameEntry>());

        result.Accepted.Should().BeTrue();
        result.NormalizedName.Should().Be("loool");
    }

    [Fact]
    public void NameModeration_ShouldRejectDuplicateNameWhenDisabled()
    {
        var settings = new ServerModerationSettings
        {
            MaxNameLength = 40,
            BlockRepeatedLettersInName = false,
            AllowDuplicateNames = false
        };

        var existing = new List<ModerationNameEntry>
        {
            new ModerationNameEntry(2, "Diamond")
        };
        var result = NameModeration.Validate(settings, "diamond", 1, existing);

        result.Accepted.Should().BeFalse();
        result.RejectReasonCode.Should().Be("name_duplicate");
    }

    [Fact]
    public void NameModeration_ShouldAllowDuplicateNameWhenEnabled()
    {
        var settings = new ServerModerationSettings
        {
            MaxNameLength = 40,
            BlockRepeatedLettersInName = false,
            AllowDuplicateNames = true
        };

        var existing = new List<ModerationNameEntry>
        {
            new ModerationNameEntry(2, "Diamond")
        };
        var result = NameModeration.Validate(settings, "diamond", 1, existing);

        result.Accepted.Should().BeTrue();
    }

    [Fact]
    public void TextChatModeration_ShouldRejectWhenDisabled()
    {
        var settings = new ServerFeaturesSettings
        {
            TextChat = false
        };

        var allowed = TextChatModeration.TryAllowTextChat(settings, out var message);

        allowed.Should().BeFalse();
        message.Should().NotBeNullOrWhiteSpace();
    }
}
