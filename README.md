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



## Video Demonstration
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

