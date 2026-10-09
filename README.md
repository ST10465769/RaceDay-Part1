# RaceDay Part-1

## Student Information

- **Student Name**: SIMAMKELE KINOLA KAULELA
- **Student Number**: ST10465769
- **Module**: PROG6212 - Programming 2B
- **Assessment**: POE Part 1 
- **Date**: 2/09/2026

## Project Description

RaceDay is a web-based event management system built for the South African road running, walking and
cycling community. Organisers can create and manage events, define age or distance categories, and
capture participant results. Participants can browse upcoming events, enter an event by selecting a
category, track their enrolments, and view their personal race history.

## User Roles

### Organiser
- Create, edit, and delete events
- Manage event categories
- View event enrolments
- Capture participant results
- View information relating to events they manage

### Participant
- Create an account and log in
- Browse available events
- Enter an event and select a category
- View their own enrolments
- Track their own race results and performance history

## Part 1 - System Planning and Database

### Entity Relationship Diagram (ERD)
- **File**: `docs/RaceDay_ERD.pdf`
- **Entities**: User, Event, Category, Enrolment, Result, Notification
- **Key Relationships**: One-to-Many between all entities
- **Primary Keys**: All entities have auto-incrementing ID fields
- **Foreign Keys**: Properly defined for all relationships

### API Endpoint Plan
- **File**: `docs/RaceDay_API_Endpoint_Plan.pdf`
- **Endpoints**: Authentication (2), User Profile (2), Events (5), Categories (4), Enrolments (5), Results (4)
- **Total Endpoints**: 22
- **Roles**: Public, Any (Logged In), Organiser, Participant

### SQL Database Script
- **File**: `docs/RaceDay_Database.sql`
- **Database**: MySQL
- **Tables**: 6 tables (User, Event, Category, Enrolment, Result, Notification)
- **Seed Data**: 2 Organisers, 2 Participants, 3 Events, Categories, Enrolments, Results, Notifications

## Repository Structure
<img width="472" height="275" alt="Screenshot 2026-09-02 123510" src="https://github.com/user-attachments/assets/e989da1c-a224-4486-a17b-9f1091270301" />



## Database Setup

1. Open MySQL Workbench
2. Connect to your MySQL instance
3. Open `docs/RaceDay_Database.sql`
4. Execute the entire script
5. The script will:
   - Drop existing RaceDayDB 
   - Create a new RaceDayDB database
   - Create all 6 tables
   
## CI/CD

## CI/CD - GitHub Actions

The GitHub Actions workflow validates the repository structure by checking:
- `/docs` folder exists
- `RaceDay_ERD.pdf` exists
- `RaceDay_API_Endpoint_Plan.pdf` exists
- `RaceDay_Database.sql` exists
- `README.md` exists

### Successful Build Screenshot
<img width="877" height="686" alt="Screenshot 2026-09-02 110739" src="https://github.com/user-attachments/assets/62749fe8-27f2-4f39-8ff4-c5bcc06acff1" />



## Video Presentation
YouTube link:
https://youtu.be/W6uhLkQWaiU


## Part 1 Video Content
- Introduction
- GitHub repository walkthrough
- ERD explanation (entities, relationships, cardinality)
- API Endpoint Plan explanation
- SQL script live demonstration in MySQL workbench
- Verification of seeded sample data
- GitHub Actions green build

## References

- PROG6212 PoE Document (2026)
- MySQL Workbench Documentation
- Draw.io Documentation

---

# Part 2 - RESTful API

## Technologies Used
- ASP.NET Core Web API (.NET 8), C#
- Entity Framework Core 8 (Code First) with SQL Server
- Swagger (Swashbuckle) for documentation and testing
- xUnit with WebApplicationFactory and the EF Core in-memory provider for tests
- GitHub Actions for CI

## Project Structure
- `RaceDay.Api` - controllers, models, DbContext, services, filters, migrations
- `RaceDay.Contracts` - shared DTOs for requests and responses
- `RaceDay.Api.Tests` - unit and integration tests
- `docs` - Part 1 ERD, endpoint plan and SQL script

## Database Setup
1. Install SQL Server Express and open SSMS.
2. In `RaceDay.Api/appsettings.json`, set `DefaultConnection` to your server (mine is `localhost\SQLEXPRESS`).
3. In Package Manager Console (default project `RaceDay.Api`) run `Update-Database`, or in a terminal run `dotnet ef database update --project RaceDay.Api`.
4. Open SSMS and confirm `RaceDayDb` has the tables `User`, `Event`, `Category`, `Enrolment`, `Result` and `Notification`.

## How to Run the API
1. Clone the repository and open the solution in Visual Studio 2022.
2. Set `RaceDay.Api` as the startup project and press F5.
3. Swagger opens at `http://localhost:5191/swagger`.

## Authentication
- Register hashes the password with PBKDF2 (random salt, 100,000 iterations). Only the hash is stored.
- Login checks the hash and stores `UserId` and `Role` in a server-side session.
- A `[SessionAuthorize]` filter protects endpoints: 401 if not logged in, 403 if the role is wrong.
- Organisers can only change their own events, categories, enrolments and results.

## API Endpoints
| Method | Route | Role |
|---|---|---|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Public |
| GET | /api/users/profile | Any logged in |
| PUT | /api/users/profile | Any logged in |
| GET | /api/events | Any logged in |
| GET | /api/events/mine | Organiser |
| GET | /api/events/{id} | Any logged in |
| POST | /api/events | Organiser |
| PUT | /api/events/{id} | Organiser (owner) |
| DELETE | /api/events/{id} | Organiser (owner) |
| GET | /api/events/{eventId}/categories | Any logged in |
| POST | /api/events/{eventId}/categories | Organiser (owner) |
| PUT | /api/categories/{id} | Organiser (owner) |
| DELETE | /api/categories/{id} | Organiser (owner) |
| POST | /api/enrolments | Participant |
| GET | /api/enrolments/mine | Participant |
| GET | /api/enrolments/event/{eventId} | Organiser (owner) |
| PUT | /api/enrolments/{id}/status | Organiser (owner) |
| POST | /api/results | Organiser (owner) |
| GET | /api/results/mine | Participant |
| GET | /api/results/event/{eventId} | Organiser (owner) |

## Differences from my Part 1 plan
- The ERD and SQL script from Part 1 were written for MySQL. In Part 2 EF Core generates the same tables and columns for SQL Server. ENUM columns (Role, EventType, Status) became text columns, and the values are checked in the API.
- I added `GET /api/events/mine`, `GET /api/enrolments/mine`, `GET /api/enrolments/event/{eventId}`, `PUT /api/enrolments/{id}/status` and the results routes so each role has a clear way to view its own data.
- The Notification table exists in the database as in my ERD, but no Part 2 endpoint uses it.

## Swagger
Swagger lists the endpoints in six groups (Auth, Profile, Events, Categories, Enrolments, Results). Each endpoint has a summary, a description and its response codes.

## Unit Testing
Run `dotnet test RaceDay.Api.Tests/RaceDay.Api.Tests.csproj`, or use Test Explorer in Visual Studio. The tests use an in-memory database and cover registration, login, logout, profile access, event management, role enforcement, enrolments and results, with both success and failure cases.

## CI/CD
GitHub Actions restores, builds and tests the solution on every push.

### GitHub Actions Screenshot
![Green CI build](docs/ci-green-build.png)

## Video Presentation
[Part 2 video (unlisted YouTube)]([PASTE YOUR LINK HERE])

## AI Disclosure
I used an AI assistant (Claude) to help plan the project structure, to troubleshoot setup errors and to review my code. I tested the API in Swagger, ran all the tests and can explain the code in my video.
