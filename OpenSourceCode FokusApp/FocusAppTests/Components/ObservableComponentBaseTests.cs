using System.ComponentModel;
using Bunit;
using FokusApp.Components.AbstractClasses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace FocusAppTests.Components;

public class ObservableComponentBaseTests : ComponentTestContext
{
    private readonly TestSource _source = new();

    [Fact]
    public void Render_ObservedObjectChanges_RendersAgain()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));
        Assert.Equal("0", cut.Find("span").TextContent);

        _source.Value = 5;

        cut.WaitForAssertion(() => Assert.Equal("5", cut.Find("span").TextContent));
    }

    [Fact]
    public void Render_ObservedObjectChanges_CallsTheChangeHookWithThePropertyName()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));

        _source.Value = 1;

        cut.WaitForAssertion(() => Assert.Contains(nameof(TestSource.Value), cut.Instance.ObservedProperties));
    }

    [Fact]
    public void Render_SeveralChangesInARow_AreAllReportedToTheHook()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));

        _source.Value = 1;
        _source.Value = 2;
        _source.Value = 3;

        cut.WaitForAssertion(() => Assert.Equal(3, cut.Instance.ObservedProperties.Count));
        cut.WaitForAssertion(() => Assert.Equal("3", cut.Find("span").TextContent));
    }

    [Fact]
    public async Task Dispose_AfterDispose_NoLongerReactsToChanges()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));
        var instance = cut.Instance;

        await DisposeComponentsAsync();
        _source.Value = 9;

        Assert.Empty(instance.ObservedProperties);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));

        var exception = Record.Exception(() =>
        {
            cut.Instance.Dispose();
            cut.Instance.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Observe_AfterDispose_Throws()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));
        cut.Instance.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cut.Instance.ObserveAgain(new TestSource()));
    }

    [Fact]
    public void Change_ComponentDisposedByEarlierSubscriberOfTheSameEvent_IsIgnored()
    {
        var cut = Render<ObservingComponent>(parameters => parameters.Add(component => component.Source, _source));
        var instance = cut.Instance;
        var lateSource = new TestSource();
        lateSource.PropertyChanged += (_, _) => instance.Dispose();
        instance.ObserveAgain(lateSource);

        var exception = Record.Exception(() => lateSource.Value = 1);

        Assert.Null(exception);
        Assert.Empty(instance.ObservedProperties);
    }

    private sealed class TestSource : INotifyPropertyChanged
    {
        private int _value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }

    private sealed class ObservingComponent : ObservableComponentBase
    {
        [Parameter]
        public TestSource Source { get; set; } = new();

        public List<string?> ObservedProperties { get; } = [];

        public void ObserveAgain(INotifyPropertyChanged observable)
        {
            Observe(observable);
        }

        protected override void OnInitialized()
        {
            Observe(Source);
        }

        protected override Task OnObservedPropertyChangedAsync(PropertyChangedEventArgs args)
        {
            ObservedProperties.Add(args.PropertyName);
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, Source.Value);
            builder.CloseElement();
        }
    }
}
