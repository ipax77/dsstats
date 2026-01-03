namespace dsstats.maui.Services.Models;

public record DecodeStatus(
    int TotalInDb,
    int NewInFolders,
    List<string> ToDoReplayPaths);
