using CsvHelper.Configuration;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Globalization;
using System.Text;
using System.Reflection;

namespace dsstats.localization;

internal class Program
{
    static void Main(string[] args)
    {
        var languageCsvFile = Path.Combine(Environment.CurrentDirectory, "dsstats_languages.csv");

        if (!File.Exists(languageCsvFile))
        {
            throw new FileNotFoundException(languageCsvFile);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            NewLine = Environment.NewLine,
            Delimiter = ";"
        };
        using var reader = new StreamReader(languageCsvFile, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<DsstatsLocalization>();

        List<DsstatsLocalization> localizations = records.ToList();

        foreach (PropertyInfo property in typeof(DsstatsLocalization).GetProperties())
        {
            if (property.Name == "Name")
            {
                continue;
            }

            if (property.Name.StartsWith("Value"))
            {
                string cultureName = property.Name[^2..];
                CreateResxFile(localizations, property, cultureName);
            }
        }

        var defaultLang = Path.Combine(Environment.CurrentDirectory, $"DsstatsLoc.en.resx");
        var defaultFile = Path.Combine(Environment.CurrentDirectory, $"DsstatsLoc.resx");
        File.Copy(defaultLang, defaultFile, true);
    }

    private static void CreateResxFile(List<DsstatsLocalization> localizations, PropertyInfo propertyInfo, string cultureName)
    {
        StringBuilder sb = new();
        sb.Append(xmlFileStart);
        sb.AppendLine();

        foreach (var loc in localizations)
        {
            var propertyValue = propertyInfo.GetValue(loc, null);
            if (propertyValue is not null && propertyValue is string value)
            {
                sb.Append(GetLocValue(loc.Name.Trim(), value.Trim()));
            }
        }

        sb.AppendLine(xmlFileEnd);
        var file = Path.Combine(Environment.CurrentDirectory, $"DsstatsLoc.{cultureName}.resx");
        File.WriteAllText(file, sb.ToString());
    }

    private static string GetLocValue(string name, string value)
    {
        StringBuilder sb = new();
        sb.AppendLine($"  <data name=\"{name}\" xml:space=\"preserve\">");
        sb.AppendLine($"    <value>{value}</value>");
        sb.AppendLine("  </data>");
        return sb.ToString();
    }

    private static readonly string xmlFileStart =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
    <xsd:import namespace=""http://www.w3.org/XML/1998/namespace"" />
    <xsd:element name=""root"" msdata:IsDataSet=""true"">
      <xsd:complexType>
        <xsd:choice maxOccurs=""unbounded"">
          <xsd:element name=""metadata"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" use=""required"" type=""xsd:string"" />
              <xsd:attribute name=""type"" type=""xsd:string"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""assembly"">
            <xsd:complexType>
              <xsd:attribute name=""alias"" type=""xsd:string"" />
              <xsd:attribute name=""name"" type=""xsd:string"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""data"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
                <xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" msdata:Ordinal=""1"" />
              <xsd:attribute name=""type"" type=""xsd:string"" msdata:Ordinal=""3"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" msdata:Ordinal=""4"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""resheader"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>";

    private static readonly string xmlFileEnd = "</root>";

}

public record DsstatsLocalization
{
    [Index(0)]
    public string Name { get; set; } = string.Empty;
    [Index(1)]
    public string Valueen { get; set; } = string.Empty;
    [Index(2)]
    public string Valuede { get; set; } = string.Empty;
    [Index(3)]
    public string Valuees { get; set; } = string.Empty;
    [Index(4)]
    public string Valueru { get; set; } = string.Empty;
    [Index(5)]
    public string Valuefr { get; set; } = string.Empty;
    [Index(6)]
    public string Valueuk { get; set; } = string.Empty;

}