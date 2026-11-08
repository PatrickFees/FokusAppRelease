using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public static class TestSettings
{

    public static IRewardingSettingsService Create(AppRewardingSettings? rewarding = null)
    {
        var settings = Substitute.For<IRewardingSettingsService>();
        settings.Rewarding.Returns(rewarding ?? AppRewardingSettings.Default);
        return settings;
    }
}
