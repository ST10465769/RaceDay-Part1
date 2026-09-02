-- ============================================================
-- RaceDay Event Management System - Database Schema
-- PROG6212 Part 1
-- ============================================================

-- Drop database if it exists (for clean testing)
DROP DATABASE IF EXISTS RaceDayDB;
CREATE DATABASE RaceDayDB;
USE RaceDayDB;

-- ============================================================
-- TABLE: USER
-- Stores all user accounts for both Organisers and Participants
-- ============================================================
CREATE TABLE `User` (
    UserID INT PRIMARY KEY AUTO_INCREMENT,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role ENUM('Organiser', 'Participant') NOT NULL,
    PhoneNumber VARCHAR(20),
    DateRegistered DATETIME DEFAULT CURRENT_TIMESTAMP,
    ProfilePictureUrl VARCHAR(500)
);

-- ============================================================
-- TABLE: EVENT
-- Stores event information created by Organisers
-- ============================================================
CREATE TABLE Event (
    EventID INT PRIMARY KEY AUTO_INCREMENT,
    EventName VARCHAR(100) NOT NULL,
    Description TEXT,
    EventDate DATE NOT NULL,
    Location VARCHAR(200) NOT NULL,
    Distance DECIMAL(5,2) NOT NULL,
    EventType ENUM('Run', 'Walk', 'Cycle') NOT NULL,
    BannerImageUrl VARCHAR(500),
    DateCreated DATETIME DEFAULT CURRENT_TIMESTAMP,
    OrganiserID INT NOT NULL,
    FOREIGN KEY (OrganiserID) REFERENCES `User`(UserID) ON DELETE CASCADE
);

-- ============================================================
-- TABLE: CATEGORY
-- Age or distance categories for each event
-- ============================================================
CREATE TABLE Category (
    CategoryID INT PRIMARY KEY AUTO_INCREMENT,
    CategoryName VARCHAR(50) NOT NULL,
    Description VARCHAR(200),
    EventID INT NOT NULL,
    FOREIGN KEY (EventID) REFERENCES Event(EventID) ON DELETE CASCADE
);

-- ============================================================
-- TABLE: ENROLMENT
-- Links Participants to Events and Categories
-- ============================================================
CREATE TABLE Enrolment (
    EnrolmentID INT PRIMARY KEY AUTO_INCREMENT,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Status ENUM('Pending', 'Confirmed', 'Cancelled') DEFAULT 'Pending',
    FOREIGN KEY (ParticipantID) REFERENCES `User`(UserID) ON DELETE CASCADE,
    FOREIGN KEY (EventID) REFERENCES Event(EventID) ON DELETE CASCADE,
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID) ON DELETE CASCADE,
    UNIQUE KEY unique_enrolment (ParticipantID, EventID)
);

-- ============================================================
-- TABLE: RESULT
-- Stores finish times and positions for participants
-- ============================================================
CREATE TABLE Result (
    ResultID INT PRIMARY KEY AUTO_INCREMENT,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NOT NULL,
    FinishingPosition INT NOT NULL,
    FOREIGN KEY (EnrolmentID) REFERENCES Enrolment(EnrolmentID) ON DELETE CASCADE
);

-- ============================================================
-- TABLE: NOTIFICATION
-- Stores notifications for users
-- ============================================================
CREATE TABLE Notification (
    NotificationID INT PRIMARY KEY AUTO_INCREMENT,
    UserID INT NOT NULL,
    Message TEXT NOT NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    DateSent DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserID) REFERENCES `User`(UserID) ON DELETE CASCADE
);

-- ============================================================
-- SEED DATA
-- ============================================================

-- ============================================================
-- Insert Organisers (2)
-- ============================================================
INSERT INTO `User` (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Thabo', 'Nkosi', 'thabo.nkosi@raceday.co.za', 'hashed_password_1', 'Organiser', '0821234567'),
('Priya', 'Naidoo', 'priya.naidoo@raceday.co.za', 'hashed_password_2', 'Organiser', '0832345678');

-- ============================================================
-- Insert Participants (2)
-- ============================================================
INSERT INTO `User` (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Sipho', 'Mthembu', 'sipho.mthembu@gmail.com', 'hashed_password_3', 'Participant', '0713456789'),
('Leah', 'Williams', 'leah.williams@gmail.com', 'hashed_password_4', 'Participant', '0724567890');

-- ============================================================
-- Insert Events (3)
-- ============================================================
INSERT INTO Event (EventName, Description, EventDate, Location, Distance, EventType, OrganiserID) VALUES
('Soweto Marathon 2026', 'The iconic Soweto Marathon through the streets of Soweto, one of South Africa\'s most prestigious road races.', '2026-09-15', 'Soweto, Johannesburg', 42.20, 'Run', 1),
('Cape Town Cycle Tour 2026', 'The world\'s largest timed cycle race, taking you along the stunning Cape Peninsula.', '2026-03-08', 'Cape Town, Western Cape', 109.00, 'Cycle', 2),
('Two Oceans Ultra Marathon 2026', 'The beautiful 56km ultra marathon around the Cape Peninsula, known as "the world\'s most beautiful marathon".', '2026-04-11', 'Cape Town, Western Cape', 56.00, 'Run', 1);

-- ============================================================
-- Insert Categories for Events
-- ============================================================
-- Categories for Soweto Marathon
INSERT INTO Category (CategoryName, Description, EventID) VALUES
('Under 20', 'Participants aged 19 and under', 1),
('Senior', 'Participants aged 20-39', 1),
('Master', 'Participants aged 40 and over', 1);

-- Categories for Cape Town Cycle Tour
INSERT INTO Category (CategoryName, Description, EventID) VALUES
('Elite', 'Competitive cyclists with racing experience', 2),
('Open', 'Recreational cyclists of all levels', 2),
('Veteran', 'Cyclists aged 50 and over', 2);

-- Categories for Two Oceans Ultra Marathon
INSERT INTO Category (CategoryName, Description, EventID) VALUES
('Under 30', 'Participants aged 29 and under', 3),
('30-39', 'Participants aged 30-39', 3),
('40-49', 'Participants aged 40-49', 3),
('50+', 'Participants aged 50 and over', 3);

-- ============================================================
-- Insert Enrolments
-- ============================================================
-- Participant 1 enrols in Soweto Marathon (Senior category)
INSERT INTO Enrolment (ParticipantID, EventID, CategoryID, Status) VALUES
(3, 1, 2, 'Confirmed');

-- Participant 1 enrols in Two Oceans Ultra Marathon (30-39 category)
INSERT INTO Enrolment (ParticipantID, EventID, CategoryID, Status) VALUES
(3, 3, 6, 'Pending');

-- Participant 2 enrols in Soweto Marathon (Senior category)
INSERT INTO Enrolment (ParticipantID, EventID, CategoryID, Status) VALUES
(4, 1, 2, 'Confirmed');

-- Participant 2 enrols in Cape Town Cycle Tour (Open category)
INSERT INTO Enrolment (ParticipantID, EventID, CategoryID, Status) VALUES
(4, 2, 5, 'Confirmed');

-- ============================================================
-- Insert Results
-- ============================================================
-- Results for Soweto Marathon
INSERT INTO Result (EnrolmentID, FinishTime, FinishingPosition) VALUES
(1, '03:45:22', 127);  -- Sipho's result

INSERT INTO Result (EnrolmentID, FinishTime, FinishingPosition) VALUES
(3, '04:12:08', 389);  -- Leah's result

-- ============================================================
-- Insert Notifications
-- ============================================================
INSERT INTO Notification (UserID, Message, IsRead) VALUES
(3, 'Your enrolment for the Soweto Marathon has been confirmed.', TRUE),
(4, 'Your enrolment for the Soweto Marathon has been confirmed.', TRUE),
(4, 'Your enrolment for the Cape Town Cycle Tour has been confirmed.', FALSE),
(3, 'Your enrolment for the Two Oceans Ultra Marathon is pending confirmation.', FALSE);

-- ============================================================
-- END OF SCRIPT
-- ============================================================

<<<<<<< HEAD
-- Verification queries 
=======
-- Verification queries
>>>>>>> 3be2c7bd2a5a4f77227a59a3ca2d0c51cc3fc188
SELECT * FROM `User`;
SELECT * FROM Event;
SELECT * FROM Category;
SELECT * FROM Enrolment;
SELECT * FROM Result;
<<<<<<< HEAD
SELECT * FROM Notification;
=======
SELECT * FROM Notification;
>>>>>>> 3be2c7bd2a5a4f77227a59a3ca2d0c51cc3fc188
