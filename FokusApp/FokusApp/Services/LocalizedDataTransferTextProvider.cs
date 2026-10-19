using FokusApp.Core.Interfaces;
using FokusApp.Resources.Localization;
using Microsoft.Extensions.Localization;

namespace FokusApp.Services;

public sealed class LocalizedDataTransferTextProvider(IStringLocalizer<AppStrings> localizer) : IDataTransferTextProvider
{
    public string GetText(string key, params object[] arguments) => localizer[key, arguments];
}
