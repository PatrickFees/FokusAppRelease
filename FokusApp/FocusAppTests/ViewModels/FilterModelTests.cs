using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;

namespace FocusAppTests.ViewModels;

public class FilterModelTests
{
    [Fact]
    public void Constructor_NoValues_HasNoDayFilter()
    {
        var model = new FilterModel();

        Assert.Null(model.SelectedDate);
    }

    [Fact]
    public void SelectedPriorities_UnorderedDuplicates_AreDistinctAndOrdered()
    {
        var model = new FilterModel();

        model.SelectedPriorities = [TaskPriority.Hoch, TaskPriority.Wenig, TaskPriority.Hoch];

        Assert.Equal([TaskPriority.Wenig, TaskPriority.Hoch], model.SelectedPriorities);
    }

    [Fact]
    public void SelectedPriorities_UndefinedValue_BecomesNone()
    {
        var model = new FilterModel();
        var undefined = (TaskPriority)42;

        model.SelectedPriorities = [undefined];

        Assert.Equal([TaskPriority.Keine], model.SelectedPriorities);
    }

    [Fact]
    public void Apply_OtherModel_CopiesAllValues()
    {
        var source = new FilterModel
        {
            SearchText = "Sport",
            SelectedPriorities = [TaskPriority.Mittel],
            IsSortDescending = false,
            SelectedDate = TestTasks.Day
        };
        var target = new FilterModel();

        target.Apply(source);

        Assert.Equal("Sport", target.SearchText);
        Assert.Equal([TaskPriority.Mittel], target.SelectedPriorities);
        Assert.False(target.IsSortDescending);
        Assert.Equal(TestTasks.Day, target.SelectedDate);
    }

    [Fact]
    public void Apply_Null_ClearsAllValuesIncludingDay()
    {
        var model = new FilterModel
        {
            SearchText = "Sport",
            SelectedPriorities = [TaskPriority.Mittel],
            IsSortDescending = false,
            SelectedDate = TestTasks.Day
        };

        model.Apply(null);

        Assert.Equal(string.Empty, model.SearchText);
        Assert.Empty(model.SelectedPriorities);
        Assert.True(model.IsSortDescending);
        Assert.Null(model.SelectedDate);
    }

    [Fact]
    public void Snapshot_ChangeAfterwards_DoesNotAffectSnapshot()
    {
        var model = new FilterModel { SearchText = "Sport" };

        var snapshot = model.Snapshot();
        model.SearchText = "Lesen";

        Assert.Equal("Sport", snapshot.SearchText);
    }

    [Fact]
    public void HasSameFilterState_SameValuesDifferentTimeOfDayAndPriorityOrder_IsTrue()
    {
        var first = new FilterModel
        {
            SearchText = "Sport",
            SelectedPriorities = [TaskPriority.Hoch, TaskPriority.Wenig],
            SelectedDate = new DateTime(2026, 3, 10, 8, 0, 0)
        };
        var second = new FilterModel
        {
            SearchText = "Sport",
            SelectedPriorities = [TaskPriority.Wenig, TaskPriority.Hoch],
            SelectedDate = new DateTime(2026, 3, 10, 20, 0, 0)
        };

        Assert.True(first.HasSameFilterState(second));
    }

    [Fact]
    public void HasSameFilterState_Null_IsFalse()
    {
        var model = new FilterModel();

        Assert.False(model.HasSameFilterState(null!));
    }

    [Fact]
    public void HasSameFilterState_SearchTextDiffersInCase_IsFalse()
    {
        var first = new FilterModel { SearchText = "Sport" };
        var second = new FilterModel { SearchText = "sport" };

        Assert.False(first.HasSameFilterState(second));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HasSameFilterState_DifferentSortDirection_IsFalse(bool otherIsDescending)
    {
        var first = new FilterModel { IsSortDescending = !otherIsDescending };
        var second = new FilterModel { IsSortDescending = otherIsDescending };

        Assert.False(first.HasSameFilterState(second));
    }

    [Fact]
    public void HasSameFilterState_DifferentDay_IsFalse()
    {
        var first = new FilterModel { SelectedDate = TestTasks.Day };
        var second = new FilterModel { SelectedDate = TestTasks.Day.AddDays(1) };

        Assert.False(first.HasSameFilterState(second));
    }

    [Fact]
    public void HasSameFilterState_DifferentPriorities_IsFalse()
    {
        var first = new FilterModel { SelectedPriorities = [TaskPriority.Hoch] };
        var second = new FilterModel { SelectedPriorities = [TaskPriority.Wenig] };

        Assert.False(first.HasSameFilterState(second));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToTaskFilterOptions_Model_MapsAllValues(bool keepRegularTemplatesVisible)
    {
        var model = new FilterModel
        {
            SearchText = "Sport",
            SelectedPriorities = [TaskPriority.Hoch],
            IsSortDescending = false,
            SelectedDate = TestTasks.Day
        };

        var options = model.ToTaskFilterOptions(keepRegularTemplatesVisible);

        Assert.Equal("Sport", options.SearchText);
        Assert.Equal([TaskPriority.Hoch], options.SelectedPriorities);
        Assert.False(options.IsSortDescending);
        Assert.Equal(TestTasks.Day, options.SelectedDate);
        Assert.Equal(keepRegularTemplatesVisible, options.KeepRegularTemplatesVisibleWhenDayFiltered);
    }
}
