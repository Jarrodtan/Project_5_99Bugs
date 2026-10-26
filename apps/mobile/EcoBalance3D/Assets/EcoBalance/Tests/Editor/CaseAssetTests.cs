// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// The shipped investigation cases (Resources/Cases) must load and point at real species/resource
// assets; a broken reference would make a case silently impossible to unlock.
public class CaseAssetTests
{
    [Test]
    public void Cases_LoadWithUniqueIdsAndValidConditions()
    {
        var cases = Resources.LoadAll<InvestigationCase>("Cases");
        Assert.GreaterOrEqual(cases.Length, 3, "expected the example cases in Resources/Cases");
        var ids = new HashSet<string>();
        foreach (var c in cases)
        {
            Assert.IsTrue(ids.Add(c.Key), $"duplicate case id '{c.Key}'");
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.title), $"{c.name} has no title");
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.hint), $"{c.name} has no hint");
            Assert.IsNotEmpty(c.conditions, $"{c.name} has no conditions");
            foreach (var cond in c.conditions)
            {
                Assert.IsTrue(cond.subject is SpeciesDefinition || cond.subject is ResourceDefinition,
                    $"{c.name}: condition subject must be a species or resource asset");
                if (cond.kind == CaseConditionKind.RatioAtLeast)
                    Assert.IsNotNull(cond.other, $"{c.name}: ratio condition needs 'other'");
            }
        }
    }
}
// [AI-GENERATED END]
