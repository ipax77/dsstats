
# dsstats Server

This repository contains the server-side components for the dsstats project, including the API, web interface, and database migrations.

## Project Structure

The solution is divided into several projects:

*   **dsstats.api:** The main ASP.NET Core web API that serves data to the clients.
*   **dsstats.web:** A web application that provides a user interface for the dsstats project.
*   **dsstats.db:** Contains the Entity Framework Core database context and models.
*   **dsstats.dbServices:** Contains services for accessing and manipulating the database.
*   **dsstats.apiServices:** Contains services used by the API.
*   **dsstats.migrations.mysql:** Contains the database migrations for MySQL.
*   **dsstats.migrations.postgresql:** Contains the database migrations for PostgreSQL.
*   **dsstats.migrations.sqlite:** Contains the database migrations for SQLite.
*   **dsstats.ratings:** Contains the logic for calculating player ratings.
*   **sc2arcade.crawler:** A service for crawling SC2 arcade data.

## Getting Started

### Prerequisites

*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   MySQL 8 (a Docker setup is available in the `docker` folder for use with WSL)
*   [Optional] Other database providers like PostgreSQL or SQLite.

### Running the application

1.  Clone the repository:
    ```bash
    git clone https://github.com/ipax77/dsstats10.git
    ```
2.  Navigate to the server directory:
    ```bash
    cd dsstats10/src/server
    ```
3.  Set up the database:
    *   Start the MySQL Docker container:
        ```bash
        cd docker
        docker-compose up -d
        ```

4.  Run the API:
    ```bash
    cd ../dsstats.api
    dotnet run
    ```

5.  Run the web project:
    ```bash
    cd ../dsstats.web
    dotnet run
    ```

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a pull request.

1.  Fork the Project.
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the Branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.