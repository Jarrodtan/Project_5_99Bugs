using NUnit.Framework;
using UnityEngine;

// Pins the team decision for the Inspect tab's deselect behaviour, and the tab-bar outcome of each option.
public class InspectTabDeselectBehaviourTests
{
    private const int Events = 2;
    private const int Inspect = 3;

    [Test]
    public void Default_IsHideTab_PerTeamDecision()
    {
        var config = ScriptableObject.CreateInstance<InspectionUiConfig>();
        try
        {
            Assert.AreEqual(InspectTabDeselectBehaviour.HideTab, config.tabDeselectBehaviour);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void HideTab_DeselectRemovesInspectAndReturnsToPreviousTab()
    {
        var model = SelectInspectFromEvents();

        model.SetAvailable(Inspect, false);

        Assert.IsFalse(model.IsAvailable(Inspect));
        Assert.AreEqual(Events, model.SelectedIndex);
    }

    [Test]
    public void KeepLastSelection_DeselectLeavesInspectVisibleAndSelected()
    {
        var model = SelectInspectFromEvents();

        // Keep mode reports no availability change on deselect, so nothing moves.
        Assert.IsFalse(model.SetAvailable(Inspect, true));

        Assert.IsTrue(model.IsAvailable(Inspect));
        Assert.AreEqual(Inspect, model.SelectedIndex);
    }

    private static TabSelectionModel SelectInspectFromEvents()
    {
        var model = new TabSelectionModel(4, focusOnReveal: true);
        model.SetAvailable(Inspect, false, allowFocus: false);
        model.SelectDefault(Events);
        model.SetAvailable(Inspect, true);
        return model;
    }
}
