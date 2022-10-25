using CsvHelper;
using pax.dsstats.shared;
using System.Globalization;
using System.Text.Json;

namespace pax.dsstats.parser;
public partial class Parse
{
    public static List<DsUnitData> DsUnitDatas = new List<DsUnitData>();
    public static void Import()
    {
        string csvFile = "/data/ds/dsdata.csv";
        string jsonFile = "/data/ds/dsdata.json";

        if (!File.Exists(jsonFile))
        {
            using (var reader = new StreamReader(csvFile))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<DsUnitDataCsvDummy>();
                records.ToList().ForEach(f => DsUnitDatas.Add(new DsUnitData(f)));
            }
            var json = JsonSerializer.Serialize(DsUnitDatas, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(jsonFile, json);
        }
        else
        {
            var data = JsonSerializer.Deserialize<List<DsUnitData>>(File.ReadAllText(jsonFile));
            if (data != null)
            {
                DsUnitDatas = data;
            }
        }
    }
}



