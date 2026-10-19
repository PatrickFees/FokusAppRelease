using FokusApp.Resources.Localization;
using Microsoft.Extensions.Localization;

namespace FokusApp.Services;

public sealed class LocalizedUiTextProvider(IStringLocalizer<AppStrings> localizer) : IUiTextProvider
{
    public string GetText(string key, params object[] arguments) => localizer[key, arguments];
}
