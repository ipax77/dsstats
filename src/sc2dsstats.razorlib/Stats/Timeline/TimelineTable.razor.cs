using Microsoft.AspNetCore.Components;
using pax.dsstats.shared;
using System.Text.RegularExpressions;

namespace sc2dsstats.razorlib.Stats.Timeline;

public partial class TimelineTable : ComponentBase
{
    [Parameter, EditorRequired]
    public TimelineResponse Response { get; set; } = default!;

    [Parameter, EditorRequired]
    public TimelineRequest Request { get; set; } = default!;

    [Parameter]
    public EventCallback<KeyValuePair<Commander, bool>> OnChartRequest { get; set; }

    List<DateTime> times = new();
    List<TimeTableData> tableDatas = new();

    TableOrder tableOrder = new()
    {
        Property = nameof(TimeTableData.Commander),
        Ascending = true
    };
    List<TableOrder> tableOrders = new();

    private bool shouldRender = true;
    protected override bool ShouldRender() => shouldRender;

    protected override void OnInitialized()
    {
        tableOrders.Add(tableOrder);
        PrepareData(Response, Request);
        base.OnInitialized();
    }

    public void PrepareData(TimelineResponse response, TimelineRequest request)
    {
        shouldRender = false;
        Response = response;
        Request = request;

        times = response.TimeLineEnts
            .Select(s => s.Time)
            .Distinct()
            .OrderByDescending(o => o)
            .ToList();

        var cmdrs = response.TimeLineEnts.Select(s => s.Commander)
            .Distinct()
            .ToList();

        tableDatas.Clear();

        foreach (var cmdr in cmdrs)
        {
            var tableData = new TimeTableData() { Commander = cmdr, Chart = true };
            for (int i = 0; i < times.Count; i++)
            {
                var data = response.TimeLineEnts.FirstOrDefault(f => f.Commander == cmdr && f.Time == times[i]);
                if (data == null)
                {
                    tableData.Strengths.Add(0);
                    tableData.Counts.Add(0);
                    tableData.Winrate.Add(0);
                    tableData.Gains.Add(0);
                    tableData.Ratings.Add(0);
                }
                else
                {
                    // tableData.Strengths.Add(data.Strength);
                    tableData.Time = data.Time;
                    tableData.Strengths.Add(data.AvgGain);
                    tableData.Counts.Add(data.Count);
                    tableData.Winrate.Add(data.Count == 0 ? 0 : data.Wins * 100.0 / data.Count);
                    tableData.Gains.Add(data.AvgGain);
                    tableData.Ratings.Add(data.AvgRating);
                }
            }
            tableDatas.Add(tableData);
        }

        shouldRender = true;
        InvokeAsync(() => StateHasChanged());
    }

    private void SetOrder(string property)
    {
        if (tableOrder.Property == property)
        {
            tableOrder.Ascending = !tableOrder.Ascending;
        }
        else
        {
            tableOrder.Property = property;
            tableOrder.Ascending = true;
        }
    }

    private List<TimeTableData> SortData()
    {
        if (tableOrder.Property == nameof(TimeTableData.Commander))
        {
            if (tableOrder.Ascending)
            {
                return tableDatas.OrderBy(o => o.Commander).ToList();
            }
            else
            {
                return tableDatas.OrderByDescending(o => o.Commander).ToList();
            }
        }
        else
        {
            var indexVal = Regex.Match(tableOrder.Property, @"\d+$").Value;

            if (int.TryParse(indexVal, out int index))
            {
                if (tableOrder.Ascending)
                {
                    return tableDatas.OrderBy(o => o.Strengths[index]).ToList();
                }
                else
                {
                    return tableDatas.OrderByDescending(o => o.Strengths[index]).ToList();
                }
            }
        }
        return tableDatas;
    }

    public void ClearChart()
    {
        tableDatas.ForEach(f => f.Chart = false);
    }

    public void SetChart()
    {
        tableDatas.ForEach(f => f.Chart = true);
    }

    private void ChartRequest(ChangeEventArgs e, Commander cmdr)
    {
        if (e.Value is bool value)
        {
            OnChartRequest.InvokeAsync(new(cmdr, value));
        }
    }

    private void LineChartRequest(TimeTableData data)
    {
        data.Chart = !data.Chart;
        OnChartRequest.InvokeAsync(new(data.Commander, data.Chart));
    }

    public record TimeTableData
    {
        public DateTime Time { get; set; }
        public bool Chart { get; set; }
        public Commander Commander { get; set; }
        public List<double> Strengths { get; set; } = new();
        public List<int> Counts { get; set; } = new();
        public List<double> Winrate { get; set; } = new();
        public List<double> Gains { get; set; } = new();
        public List<double> Ratings { get; set; } = new();
    }
}
