using System.Globalization;
using Bunit;
using FokusApp.Components.Onboarding;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Resources.Localization;
using FokusApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Components;

public abstract class ComponentTestContext : BunitContext
{
    protected ComponentTestContext()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("de");

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<ITaskPriorityFormatter, FixedPriorityFormatter>();
        Services.AddSingleton(Substitute.For<IUiBackPressService>());
        Services.AddSingleton<IUiTextProvider, LocalizedUiTextProvider>();
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<IOnboardingDemoViewModel, OnboardingDemoViewModel>();
    }

    protected void AddFilterComponentServices()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 9, 0, 0, TimeSpan.Zero));
        var dayChangeService = Substitute.For<IDayChangeService>();

        Services.AddSingleton<TimeProvider>(timeProvider);
        Services.AddSingleton(dayChangeService);
        Services.AddTransient<IFilterComponentViewModel, FilterComponentViewModel>();
        Services.AddSingleton(Substitute.For<ICalendarPopupViewModel>());
    }

    protected string Text(string key)
    {
        var localizer = Services.GetRequiredService<IStringLocalizer<AppStrings>>();
        var text = localizer[key];

        Assert.False(text.ResourceNotFound, $"Schlüssel '{key}' fehlt in AppStrings.resx.");
        return text.Value;
    }

    protected string Text(string key, params object[] arguments)
    {
        var localizer = Services.GetRequiredService<IStringLocalizer<AppStrings>>();
        var text = localizer[key, arguments];

        Assert.False(text.ResourceNotFound, $"Schlüssel '{key}' fehlt in AppStrings.resx.");
        return text.Value;
    }

    private sealed class FixedPriorityFormatter : ITaskPriorityFormatter
    {
        public string Format(TaskPriority priority)
        {
            return priority.ToString();
        }

        public string GetCssClass(TaskPriority? priority)
        {
            return string.Empty;
        }
    }
}
