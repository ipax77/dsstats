
using System.Reflection;

namespace dsstats.import.tests;

internal class Startup
{
    // private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    public static string GetTestFilePath(string fileName)
    {
        // return Path.Combine(assemblyPath, "testdata", fileName);
        return Path.Combine("/data/ds/testdata", fileName);
    }
}
