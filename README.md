# RaceDay API

## Project Description

RaceDay API is an ASP.NET Core Web API for managing race events,
categories, participant enrolments, user profiles and race results.

## Technologies Used

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT authentication
- Swagger / OpenAPI
- xUnit automated testing
- GitHub Actions

## Prerequisites

- Visual Studio 2022
- The .NET SDK version required by the project
- SQL Server
- Git

## Setup Instructions

1. Clone this repository.
2. Open the solution in Visual Studio 2022.
3. Check the target framework in RaceDay.API.csproj.
4. Configure the DefaultConnection connection string in appsettings.json.
5. Ensure SQL Server is running and the RaceDayDb database exists.
6. Apply the required Entity Framework migrations, if configured.
7. Set RaceDay.API as the startup project.
8. Run the project.
9. Open the Swagger URL displayed by Visual Studio.

## Authentication

Register a user and log in using the authentication endpoints.
Use the returned JWT bearer token in Swagger's Authorize option
to access protected endpoints.

Do not commit real passwords, database credentials or JWT secrets.

## Automated Tests

The RaceDay.API.Tests project contains automated API tests.

Run tests in Visual Studio using Test Explorer, or from the
repository root using:

dotnet test

## CI/CD

GitHub Actions is configured in:

.github/workflows/dotnet-tests.yml

The workflow restores dependencies, builds the project and runs
the automated tests when changes are pushed or a pull request
is opened.

## Demonstration Video

YouTube: https://youtu.be/KaArDrUocjU?feature=shared

## Author

PABALLO RAMBAU
