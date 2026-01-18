
# dsstats.service

This is the dsstats Windows Service used for decoding and uploading SC2 Direct Strike Replays to [dsstats.pax77.org](https://dsstats.pax77.org).

Deplyed to https://github.com/ipax77/dsstats.service

## Workflow

*   It detects SC2 Profiles in the user's documents folder.
*   Replays found in the profiles folders are decoded using [s2protocol.NET](https://github.com/ipax77/s2protocol.NET).
*   The decoded replay data is stored in a local SQLite database.
*   Optionally, the decoded data can be uploaded to [dsstats.pax77.org](httpss://dsstats.pax77.org).
*   No raw replay or personal data is transmitted, only the essence of the replays required to generate player and commander stats.

## Getting Started

### Prerequisites

*   [.NET 10 SDK](httpss://dotnet.microsoft.com/download/dotnet/10.0)
*   [WiX Toolset](https://www.firegiant.com/wixtoolset/)
    ```bash
        dotnet tool install --global wix
    ```

## Project Structure

*   `dsstats.service/`: The dotnet worker project.
*   `dsstats.installer/`: The WiX Toolset installer project.

## Development

For testing the upload functionality, a local development server with a MySQL database is required. Please refer to the server setup instructions (link to server repo or instructions would be great here).

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a pull request.

1.  Fork the Project.
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the Branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.
