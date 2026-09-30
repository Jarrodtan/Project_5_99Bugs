using NUnit.Framework;

// EditMode tests for tab-switching rules shared by all tabs, including the contextual Inspect tab.
public class TabSelectionModelTests
{
    private const int Population = 0;
    private const int Resources = 1;
    private const int Events = 2;
    private const int Inspect = 3;

    private TabSelectionModel model;

    [SetUp]
    public void SetUp()
    {
        model = new TabSelectionModel(4, focusOnReveal: true);
        model.SetAvailable(Inspect, false, allowFocus: false);
        model.SelectDefault(Population);
    }

    [Test]
    public void Startup_SelectsDefault_AndInspectStartsHidden()
    {
        Assert.AreEqual(Population, model.SelectedIndex);
        Assert.IsFalse(model.IsAvailable(Inspect));
    }

    [Test]
    public void Select_SwitchesToClickedTab()
    {
        Assert.IsTrue(model.Select(Events));
        Assert.AreEqual(Events, model.SelectedIndex);
    }

    [Test]
    public void Select_SameTabOrOutOfRange_IsIgnored()
    {
        Assert.IsFalse(model.Select(Population));
        Assert.IsFalse(model.Select(-1));
        Assert.IsFalse(model.Select(9));
        Assert.AreEqual(Population, model.SelectedIndex);
    }

    [Test]
    public void HiddenTab_CannotBeSelected()
    {
        Assert.IsFalse(model.Select(Inspect));
        Assert.AreEqual(Population, model.SelectedIndex);
    }

    [Test]
    public void RevealedTab_TakesFocus_ThenBehavesLikeAnyOtherTab()
    {
        model.SetAvailable(Inspect, true);
        Assert.AreEqual(Inspect, model.SelectedIndex);

        Assert.IsTrue(model.Select(Resources));
        Assert.AreEqual(Resources, model.SelectedIndex);
        Assert.IsTrue(model.Select(Inspect));
        Assert.AreEqual(Inspect, model.SelectedIndex);
    }

    [Test]
    public void RevealWithoutFocus_KeepsCurrentTab()
    {
        var quiet = new TabSelectionModel(4, focusOnReveal: false);
        quiet.SetAvailable(Inspect, false, allowFocus: false);
        quiet.SelectDefault(Events);

        quiet.SetAvailable(Inspect, true);

        Assert.AreEqual(Events, quiet.SelectedIndex);
        Assert.IsTrue(quiet.IsAvailable(Inspect));
    }

    [Test]
    public void HidingSelectedTab_FallsBackToPreviousTab()
    {
        model.Select(Events);
        model.SetAvailable(Inspect, true);

        model.SetAvailable(Inspect, false);

        Assert.AreEqual(Events, model.SelectedIndex);
    }

    [Test]
    public void HidingUnselectedTab_KeepsSelection()
    {
        model.SetAvailable(Inspect, true);
        model.Select(Resources);

        model.SetAvailable(Inspect, false);

        Assert.AreEqual(Resources, model.SelectedIndex);
    }

    [Test]
    public void HidingSelectedTab_WhenPreviousIsAlsoHidden_FallsBackToFirstAvailable()
    {
        model.SetAvailable(Population, false);
        Assert.AreEqual(Resources, model.SelectedIndex, "hiding the selected default falls back to the first visible tab");

        model.SetAvailable(Inspect, true);
        model.SetAvailable(Resources, false);
        model.SetAvailable(Inspect, false);

        Assert.AreEqual(Events, model.SelectedIndex);
    }

    [Test]
    public void RepeatedAvailabilityUpdates_AreNoOps()
    {
        Assert.IsFalse(model.SetAvailable(Inspect, false));
        model.SetAvailable(Inspect, true);
        model.Select(Population);

        Assert.IsFalse(model.SetAvailable(Inspect, true), "availability alone doesn't refocus an already-visible tab");
        Assert.AreEqual(Population, model.SelectedIndex);
    }

    [Test]
    public void FocusRequest_OnVisibleTab_SwitchesToIt()
    {
        model.SetAvailable(Inspect, true);
        model.Select(Events);

        Assert.IsTrue(model.RequestFocus(Inspect), "selecting a new object pulls the panel back to Inspect");
        Assert.AreEqual(Inspect, model.SelectedIndex);
    }

    [Test]
    public void FocusRequest_OnHiddenTab_IsIgnored()
    {
        Assert.IsFalse(model.RequestFocus(Inspect));
        Assert.AreEqual(Population, model.SelectedIndex);
    }

    [Test]
    public void FocusRequest_WithFocusDisabled_IsIgnored()
    {
        var quiet = new TabSelectionModel(4, focusOnReveal: false);
        quiet.SelectDefault(Population);

        Assert.IsFalse(quiet.RequestFocus(Inspect));
        Assert.AreEqual(Population, quiet.SelectedIndex);
    }

    [Test]
    public void FocusRequest_ThenDeselect_ReturnsToTabBeforeInspect()
    {
        model.SetAvailable(Inspect, true);
        model.Select(Events);
        model.RequestFocus(Inspect);

        model.SetAvailable(Inspect, false);

        Assert.AreEqual(Events, model.SelectedIndex);
    }

    [Test]
    public void DefaultOnHiddenTab_FallsBackToFirstAvailable()
    {
        var m = new TabSelectionModel(4, focusOnReveal: true);
        m.SetAvailable(Inspect, false, allowFocus: false);

        m.SelectDefault(Inspect);

        Assert.AreEqual(Population, m.SelectedIndex);
    }
}
