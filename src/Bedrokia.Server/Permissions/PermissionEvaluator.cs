// ┌───────────────────────────────────────────────────────────────────┐
// │  ____  _____ ____  ____   ___  _  __ ___    _                     │
// │ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    │
// │ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   │
// │ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  │
// │ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 │
// │                                                                   │
// │  Bedrokia - Minecraft: Bedrock Edition server software            │
// │  Licensed under the Apache License 2.0 - see LICENSE              │
// └───────────────────────────────────────────────────────────────────┘

namespace Bedrokia.Server.Permissions;

/// <summary>
/// Evaluates granular permission nodes with wildcards (*, group.*) and negations (-node).
/// Inspired by Dragonfly and LuckPerms permission evaluation semantics.
/// </summary>
public static class PermissionEvaluator
{
    /// <summary>
    /// Checks whether <paramref name="requested"/> is granted by the given permission set.
    /// Negations (e.g. "-bedrokia.command.stop") take precedence over wildcard allowances.
    /// Operators implicitly hold "*" unless explicitly negated.
    /// </summary>
    public static bool Evaluate(string? requested, IEnumerable<string> permissions, bool isOp = false)
    {
        if (string.IsNullOrWhiteSpace(requested) ||
            requested.Equals("everyone", StringComparison.OrdinalIgnoreCase) ||
            requested.Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;

        if (permissions is null)
            return isOp;

        var target = requested.Trim().ToLowerInvariant();

        var negations = new List<string>();
        var allows = new List<string>();

        foreach (var p in permissions)
        {
            if (string.IsNullOrWhiteSpace(p))
                continue;
            var clean = p.Trim().ToLowerInvariant();
            if (clean.StartsWith('-'))
                negations.Add(clean[1..]);
            else
                allows.Add(clean);
        }

        // 1. Any matching negation immediately denies the permission.
        foreach (var neg in negations)
        {
            if (Matches(neg, target))
                return false;
        }

        // 2. Exact or wildcard match in allowed permissions.
        foreach (var allow in allows)
        {
            if (Matches(allow, target))
                return true;
        }

        // 3. Operators hold wildcard '*' by default.
        if (isOp)
            return true;

        return false;
    }

    /// <summary>
    /// Checks whether a pattern (exact, prefix.* or *) matches the target node.
    /// </summary>
    public static bool Matches(string pattern, string target)
    {
        if (pattern == "*" || pattern == target)
            return true;

        if (pattern.EndsWith(".*"))
        {
            var prefix = pattern[..^2];
            return target == prefix || target.StartsWith(prefix + ".", StringComparison.Ordinal);
        }

        return false;
    }
}
