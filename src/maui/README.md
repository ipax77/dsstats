
# dsstats.maui

This is the dsstats Windows Desktop app used for decoding and uploading SC2 Direct Strike Replays to [dsstats.pax77.org](https://dsstats.pax77.org).

## Workflow

*   It detects SC2 Profiles in the user's documents folder.
*   Replays found in the profiles folders are decoded using [s2protocol.NET](https://github.com/ipax77/s2protocol.NET).
*   The decoded replay data is stored in a local SQLite database.
*   Optionally, the decoded data can be uploaded to [dsstats.pax77.org](httpss://dsstats.pax77.org).
*   No raw replay or personal data is transmitted, only the essence of the replays required to generate player and commander stats.
*   Optionally, new replays are automatically detected in the folders and decoded.
*   Recent replays decoded using the app produce and update a 'Session Progress' with winrate and stats from the website (if the replay data was uploaded).

## Getting Started

### Prerequisites

*   [.NET 10 SDK](httpss://dotnet.microsoft.com/download/dotnet/10.0)
*   .NET MAUI workload. Install it by running the following command:
    ```bash
    dotnet workload install maui
    ```

### Running the application

1.  Clone the repository:
    ```bash
    git clone https://github.com/ipax77/dsstats10.git
    ```
2.  Navigate to the project directory:
    ```bash
    cd dsstats.maui/src/maui
    ```
3.  Restore the dependencies:
    ```bash
    dotnet restore
    ```
4.  Run the application:
    ```bash
    dotnet build -t:Run -f net10.0-windows10.0.19041.0
    ```

## Project Structure

*   `dsstats.maui/`: The main MAUI project directory.
*   `Components/`: Contains the Blazor components for the UI.
*   `Platforms/`: Contains platform-specific code for Windows, Android, iOS, and MacCatalyst.
*   `Resources/`: Contains app resources like icons, fonts, and images.
*   `Services/`: Contains the core logic for replay decoding, database interaction, and API communication.
*   `wwwroot/`: Contains the root HTML and CSS files for the Blazor Hybrid app.

## Building

To build the application for a specific platform, use the `dotnet build` command with the appropriate target framework.

*   **Windows:**
    ```bash
    dotnet build -f net10.0-windows10.0.19041.0
    ```

## Development

For testing the upload functionality, a local development server with a MySQL database is required. Please refer to the server setup instructions (link to server repo or instructions would be great here).

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a pull request.

1.  Fork the Project.
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the Branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.
