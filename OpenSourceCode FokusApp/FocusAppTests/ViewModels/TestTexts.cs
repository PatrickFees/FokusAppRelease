using FokusApp.Services;
using NSubstitute;
using NSubstitute.Core;

namespace FocusAppTests.ViewModels;

public static class TestTexts
{
    public static IUiTextProvider Create()
    {
        var provider = Substitute.For<IUiTextProvider>();
        provider.GetText(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => Format(call));
        return provider;
    }

    private static string Format(CallInfo call)
    {
        var key = call.ArgAt<string>(0);
        var arguments = call.ArgAt<object[]>(1);

        if (arguments.Length == 0)
        {
            return key;
        }

        var joinedArguments = string.Join("|", arguments);
        return $"{key}:{joinedArguments}";
    }
}
