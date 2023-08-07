using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace sc2dsstats.razorlib.Options.InputAutoText;

public partial class InputAutoText : IDisposable
{
    [Parameter]
    public IReadOnlyList<string> PossibleValues { get; set; } = new List<string>();
    [Parameter]
    public string? Placeholder { get; set; }
    [Parameter]
    public bool Disabled { get; set; }

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    private DotNetObjectReference<InputAutoText> objRef = default!;
    private CancellationTokenSource cts = new();

    private bool inputHasValue;
    private bool isDropdownShown;
    private List<string>? items = null;
    private string? selectedItem;
    private int selectedIndex = -1;
    private ElementReference list; // ul element reference

    private void SetInputHasValue() => inputHasValue = Value is not null && Value.Length > 0;

    protected override void OnInitialized()
    {
        objRef ??= DotNetObjectReference.Create(this);
        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.initialize", Element, objRef);
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

    private void FilterData()
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
        if (AdditionalAttributes is not null 
            && !AdditionalAttributes.TryGetValue("data-bs-toggle", out _)
            && ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
        {
            additionalAttributes.Add("data-bs-toggle", "dropdown");
            AdditionalAttributes = additionalAttributes;
        }

        await JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.show", Element);
    }

    private async Task HideAsync()
    {
        isDropdownShown = false;
        if (AdditionalAttributes is not null
            && AdditionalAttributes.TryGetValue("data-bs-toggle", out _)
            && ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
        {
            additionalAttributes.Remove("data-bs-toggle");
            AdditionalAttributes = additionalAttributes;
        }

        await JSRuntime.InvokeVoidAsync("window.dsoptions.autocomplete.hide", Element);
    }

    private async Task OnItemSelectedAsync(string item)
    {
        selectedItem = item;
        selectedIndex = -1;
        items = new();
        CurrentValue = item;

        await HideAsync();

        SetInputHasValue();
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
            string? item = null;
            if (selectedIndex >= 0 && selectedIndex <= items?.Count - 1)
            {
                item = items?.ElementAt(selectedIndex) ?? string.Empty;
            }
            else if (items?.Count > 0)
            {
                item = items?.ElementAt(0);
            }
            await Task.Delay(100, cts.Token);
            await OnItemSelectedAsync(item ?? string.Empty);
        }
        else
        {
            // TODO: check anything needs to be handled here
        }
    }

    private async Task ClearInputTextAsync()
    {
        selectedItem = default;
        selectedIndex = -1;
        items = new();
        CurrentValue = string.Empty;

        await HideAsync();

        SetInputHasValue();
        Element?.FocusAsync();
    }

    private static bool ConvertToDictionary(IReadOnlyDictionary<string, object>? source, out Dictionary<string, object> result)
    {
        var newDictionaryCreated = true;
        if (source == null)
        {
            result = new Dictionary<string, object>();
        }
        else if (source is Dictionary<string, object> currentDictionary)
        {
            result = currentDictionary;
            newDictionaryCreated = false;
        }
        else
        {
            result = new Dictionary<string, object>();
            foreach (var item in source)
            {
                result.Add(item.Key, item.Value);
            }
        }

        return newDictionaryCreated;
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
            if (AdditionalAttributes is not null
                && AdditionalAttributes.TryGetValue("data-bs-toggle", out _)
                && ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
            {
                additionalAttributes.Remove("data-bs-toggle");
                AdditionalAttributes = additionalAttributes;
            }

            StateHasChanged();
        }
    }

    public void Dispose()
    {
        objRef?.Dispose();
        cts?.Dispose();
        base.Dispose(true);
    }
}
