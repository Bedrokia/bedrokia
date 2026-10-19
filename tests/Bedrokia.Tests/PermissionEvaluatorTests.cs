using Bedrokia.Server.Permissions;
using Xunit;

namespace Bedrokia.Tests;

public class PermissionEvaluatorTests
{
    [Fact]
    public void Exact_match_grants_permission()
    {
        var perms = new[] { "bedrokia.command.tp", "bedrokia.command.fly" };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", perms));
        Assert.False(PermissionEvaluator.Evaluate("bedrokia.command.stop", perms));
    }

    [Fact]
    public void Wildcard_star_grants_all_permissions()
    {
        var perms = new[] { "*" };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", perms));
        Assert.True(PermissionEvaluator.Evaluate("custom.plugin.perm", perms));
    }

    [Fact]
    public void Sub_wildcard_grants_sub_nodes()
    {
        var perms = new[] { "bedrokia.command.*" };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", perms));
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.give", perms));
        Assert.False(PermissionEvaluator.Evaluate("bedrokia.chat.color", perms));
        Assert.False(PermissionEvaluator.Evaluate("other.node", perms));
    }

    [Fact]
    public void Negation_overrides_wildcard()
    {
        var perms = new[] { "*", "-bedrokia.command.stop" };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", perms));
        Assert.False(PermissionEvaluator.Evaluate("bedrokia.command.stop", perms));
    }

    [Fact]
    public void Negation_wildcard_overrides_allow()
    {
        var perms = new[] { "bedrokia.*", "-bedrokia.admin.*" };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.user.profile", perms));
        Assert.False(PermissionEvaluator.Evaluate("bedrokia.admin.ban", perms));
    }

    [Fact]
    public void Operator_has_wildcard_unless_explicitly_negated()
    {
        var perms = new[] { "-bedrokia.command.stop" };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", perms, isOp: true));
        Assert.False(PermissionEvaluator.Evaluate("bedrokia.command.stop", perms, isOp: true));
    }

    [Fact]
    public void Case_insensitivity_and_whitespace_handling()
    {
        var perms = new[] { "  Bedrokia.Command.TP  " };
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", perms));
        Assert.True(PermissionEvaluator.Evaluate("BEDROKIA.COMMAND.TP", perms));
    }

    [Fact]
    public void Empty_or_null_requested_permission_behavior()
    {
        var perms = Array.Empty<string>();
        Assert.True(PermissionEvaluator.Evaluate("", perms));
        Assert.True(PermissionEvaluator.Evaluate(null!, perms));
    }

    [Fact]
    public void Null_permissions_collection_returns_isOp()
    {
        Assert.False(PermissionEvaluator.Evaluate("bedrokia.command.tp", null!));
        Assert.True(PermissionEvaluator.Evaluate("bedrokia.command.tp", null!, isOp: true));
    }
}
