using System;
using System.Data.SqlClient;
using Turfirm.Infrastructure;

namespace Turfirm.Services
{
    public static class DatabaseInitializer
    {
        public static void EnsureCreatedAndSeeded()
        {
            using (var master = Db.Open(Db.MasterConnection))
            using (var createDbCmd = new SqlCommand("IF DB_ID('DBTurfirma') IS NULL CREATE DATABASE DBTurfirma;", master))
            {
                createDbCmd.ExecuteNonQuery();
            }

            using (var connection = Db.Open(Db.AppConnection))
            {
                ExecuteBatch(connection, @"
IF OBJECT_ID('Users') IS NULL
CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(120) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone NVARCHAR(25) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    PassportSeries NVARCHAR(10) NOT NULL,
    PassportNumber NVARCHAR(20) NOT NULL,
    PassportIssueDate DATE NOT NULL,
    Role INT NOT NULL DEFAULT 1
);

IF OBJECT_ID('Guides') IS NULL
CREATE TABLE Guides (
    Id INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(120) NOT NULL,
    Phone NVARCHAR(25) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

IF OBJECT_ID('Transports') IS NULL
CREATE TABLE Transports (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Capacity INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

IF OBJECT_ID('Tours') IS NULL
CREATE TABLE Tours (
    Id INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Direction NVARCHAR(100) NOT NULL,
    TourType NVARCHAR(50) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    MaxGroupSize INT NOT NULL,
    BookedSeats INT NOT NULL DEFAULT 0,
    BasePrice DECIMAL(12,2) NOT NULL,
    OldPrice DECIMAL(12,2) NULL,
    DiscountPercent INT NULL,
    ImagePath NVARCHAR(260) NULL,
    Description NVARCHAR(1000) NULL
);

IF OBJECT_ID('AdditionalServices') IS NULL
CREATE TABLE AdditionalServices (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    FeeType NVARCHAR(20) NOT NULL,
    Value DECIMAL(10,2) NOT NULL
);

IF OBJECT_ID('Orders') IS NULL
CREATE TABLE Orders (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ReserveUntil DATETIME2 NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    TotalAmount DECIMAL(12,2) NOT NULL,
    GuideId INT NULL FOREIGN KEY REFERENCES Guides(Id),
    TransportId INT NULL FOREIGN KEY REFERENCES Transports(Id)
);

IF OBJECT_ID('OrderItems') IS NULL
CREATE TABLE OrderItems (
    Id INT IDENTITY PRIMARY KEY,
    OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(Id),
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL,
    Insurance BIT NOT NULL,
    Transfer BIT NOT NULL,
    TransferFee DECIMAL(10,2) NOT NULL,
    ItemTotal DECIMAL(12,2) NOT NULL
);
");

                Seed(connection);
            }
        }

        private static void ExecuteBatch(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void Seed(SqlConnection connection)
        {
            ExecuteBatch(connection, @"
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email='admin@turfirma.local')
BEGIN
 INSERT INTO Users(FullName,Email,Phone,PasswordHash,PassportSeries,PassportNumber,PassportIssueDate,Role)
 VALUES ('Администратор Системы','admin@turfirma.local','79990000001',CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',N'admin123'),2),'4510','100001','2015-04-12',3);
END
ELSE
BEGIN
 UPDATE Users
    SET PasswordHash = CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',N'admin123'),2),
        Role = 3
  WHERE Email='admin@turfirma.local';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email='manager@turfirma.local')
BEGIN
 INSERT INTO Users(FullName,Email,Phone,PasswordHash,PassportSeries,PassportNumber,PassportIssueDate,Role)
 VALUES ('Менеджер Ольга','manager@turfirma.local','79990000002',CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',N'manager123'),2),'4510','100002','2016-06-18',2);
END
ELSE
BEGIN
 UPDATE Users
    SET PasswordHash = CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',N'manager123'),2),
        Role = 2
  WHERE Email='manager@turfirma.local';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email='user@turfirma.local')
BEGIN
 INSERT INTO Users(FullName,Email,Phone,PasswordHash,PassportSeries,PassportNumber,PassportIssueDate,Role)
 VALUES ('Иван Петров','user@turfirma.local','79990000003',CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',N'user12345'),2),'4510','100003','2017-03-20',1);
END
ELSE
BEGIN
 UPDATE Users
    SET PasswordHash = CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',N'user12345'),2),
        Role = 1
  WHERE Email='user@turfirma.local';
END

IF NOT EXISTS (SELECT 1 FROM Guides)
BEGIN
 INSERT INTO Guides(FullName,Phone,IsActive)
 VALUES ('Сергей Иванов','79991110001',1), ('Мария Лебедева','79991110002',1), ('Алексей Волков','79991110003',1);
END

IF NOT EXISTS (SELECT 1 FROM Transports)
BEGIN
 INSERT INTO Transports(Name,Capacity,IsActive)
 VALUES ('Mercedes Sprinter',18,1), ('King Long Bus',45,1), ('Ford Transit',14,1);
END

IF NOT EXISTS (SELECT 1 FROM AdditionalServices)
BEGIN
 INSERT INTO AdditionalServices(Name,FeeType,Value)
 VALUES ('Страховка','percent',8), ('Трансфер (до 30 км)','fixed',50), ('Трансфер (30+ км)','fixed',120);
END

IF NOT EXISTS (SELECT 1 FROM Tours)
BEGIN
 INSERT INTO Tours(Title,Direction,TourType,StartDate,EndDate,MaxGroupSize,BookedSeats,BasePrice,OldPrice,DiscountPercent,ImagePath,Description)
 VALUES
 ('Сочи: Морской релакс','Сочи','Пляжный','2026-05-10','2026-05-17',20,5,980,1200,18,'https://images.unsplash.com/photo-1507525428034-b723cf961d3e','Пляжный отдых на Черном море'),
 ('Алтай: Активные горы','Алтай','Активный','2026-06-01','2026-06-08',15,7,1350,1550,13,'https://images.unsplash.com/photo-1464822759023-fed622ff2c3b','Трекинг и активные маршруты'),
 ('Санкт-Петербург: История','Санкт-Петербург','Экскурсионный','2026-05-20','2026-05-24',25,10,890,NULL,NULL,'https://images.unsplash.com/photo-1513326738677-b964603b136d','Классические экскурсии по музеям'),
 ('Байкал: Эко-экспедиция','Байкал','Активный','2026-07-12','2026-07-20',18,3,1490,1700,12,'https://images.unsplash.com/photo-1472396961693-142e6e269027','Природа Байкала и прогулки');
END
");
        }
    }
}
