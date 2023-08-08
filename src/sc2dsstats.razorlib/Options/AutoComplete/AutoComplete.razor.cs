using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace sc2dsstats.razorlib.Options.AutoComplete;
/// <summary>
/// Adapted from <see href="https://github.com/vikramlearning/blazorbootstrap">Blazor Bootstrap Component Library</see>
/// </summary>
public partial class AutoComplete : ComponentBase, IDisposable
{
    /// <summary>
    /// This event fires immediately when the autocomplete selection changes by the user.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnChanged { get; set; }
    /// <summary>
    /// This is event fires on every user keystroke that changes the textbox value.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }
    [Parameter]
    public string Value { get; set; } = default!;
    [Parameter]
    public IReadOnlyList<string> PossibleValues { get; set; } = new List<string>();
    [Parameter]
    public string? Placeholder { get; set; }
    [Parameter]
    public bool Disabled { get; set; }

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    private ElementReference ElementRef;
    private string ElementId = Guid.NewGuid().ToString();

    private DotNetObjectReference<AutoComplete> objRef = default!;

    private string fieldCssClasses = "";

    private CancellationTokenSource cts = new();
    private bool inputHasValue;
    private bool isDropdownShown;
    private List<string>? items = null;
    private string? selectedItem;
    private int selectedIndex = -1;
    private ElementReference list; // ul element reference
    public Dictionary<string, object> Attributes { get; set; } = new();

    private void SetInputHasValue() => inputHasValue = Value is not null && Value.Length > 0;

    protected override async Task OnInitializedAsync()
    {
        objRef ??= DotNetObjectReference.Create(this);

        await base.OnInitializedAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.initialize", ElementRef, objRef);
        }
        base.OnAfterRender(firstRender);
    }

    private async Task OnInputChangedAsync(ChangeEventArgs args)
    {
        selectedIndex = -1;
        Value = args.Value?.ToString() ?? "";

        SetInputHasValue();

        if (inputHasValue)
        {
            await ShowAsync();
        }
        else
        {
            await HideAsync();
        }


        if (cts is not null
            && !cts.IsCancellationRequested)
        {
            cts.Cancel();
            cts.Dispose();
        }

        cts = new CancellationTokenSource();

        var token = cts.Token;
        await Task.Delay(300, token); // 300ms timeout for the debouncing
        FilterData();
    }

    private void FilterData(CancellationToken cancellationToken = default)
    {
        var searchKey = Value;
        if (string.IsNullOrWhiteSpace(searchKey))
        {
            return;
        }

        items = PossibleValues
            .Where(x => x.ToUpper().StartsWith(searchKey.ToUpper()))
            .ToList();
    }

    private async Task ShowAsync()
    {
        isDropdownShown = true;
        if (Attributes is not null && !Attributes.TryGetValue("data-bs-toggle", out _))
            Attributes.Add("data-bs-toggle", "dropdown");

        await JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.show", ElementRef);
    }

    private async Task HideAsync()
    {
        isDropdownShown = false;
        if (Attributes is not null && Attributes.TryGetValue("data-bs-toggle", out _))
            Attributes.Remove("data-bs-toggle");

        await JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.hide", ElementRef);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        var key = args.Code is not null ? args.Code : args.Key;

        if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
        {
            selectedIndex = await JSRuntime.InvokeAsync<int>("window.dsoptions.autocomplete.focusListItem", list, key, selectedIndex);
        }
        else if (key == "Enter")
        {
            if (selectedIndex >= 0 && selectedIndex <= items?.Count - 1)
            {
                await OnItemSelectedAsync(items?.ElementAt(selectedIndex) ?? "");
            }
            else
            {
                if (items?.Count > 0)
                {
                    await OnItemSelectedAsync(items?.ElementAt(0) ?? "");
                }
                else
                {
                    await OnItemSelectedAsync("");
                }
            }
        }
        else
        {
            // TODO: check anything needs to be handled here
        }
    }

    private async Task OnItemSelectedAsync(string item)
    {
        selectedItem = item;
        selectedIndex = -1;
        items = new();
        Value = item;
        await ValueChanged.InvokeAsync(Value);

        await HideAsync();

        SetInputHasValue();

        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync(item);
    }

    private async Task ClearInputTextAsync()
    {
        selectedItem = default;
        selectedIndex = -1;
        items = new();
        Value = string.Empty;
        await ValueChanged.InvokeAsync(Value);

        await HideAsync();

        SetInputHasValue();

        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync(default);

        await ElementRef.FocusAsync();
    }

    [JSInvokable] public void bsShowAutocomplete() { }
    [JSInvokable] public void bsShownAutocomplete() { }
    [JSInvokable] public void bsHideAutocomplete() { }

    [JSInvokable]
    public void bsHiddenAutocomplete()
    {
        if (isDropdownShown)
        {
            isDropdownShown = false;
            if (Attributes is not null && Attributes.TryGetValue("data-bs-toggle", out _))
                Attributes.Remove("data-bs-toggle");

            StateHasChanged();
        }
    }

    public void Dispose()
    {
        cts?.Dispose();
        JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.dispose", ElementRef);
        objRef?.Dispose();
    }
}
