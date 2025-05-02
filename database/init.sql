-- Create the WeatherForecastDb database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'WeatherForecastDb')
BEGIN
    CREATE DATABASE WeatherForecastDb;
END
GO

-- Use the WeatherForecastDb database
USE WeatherForecastDb;
GO

-- Create the WeatherForecasts table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WeatherForecasts')
BEGIN
    CREATE TABLE WeatherForecasts (
        Id NVARCHAR(36) PRIMARY KEY,
        Date DATETIME2 NOT NULL,
        TemperatureC INT NOT NULL,
        Summary NVARCHAR(100)
    );
END
GO

-- Seed sample data
IF NOT EXISTS (SELECT * FROM WeatherForecasts)
BEGIN
    INSERT INTO WeatherForecasts (Id, Date, TemperatureC, Summary)
    VALUES 
        (NEWID(), DATEADD(day, 1, GETDATE()), 20, 'Mild'),
        (NEWID(), DATEADD(day, 2, GETDATE()), 25, 'Warm'),
        (NEWID(), DATEADD(day, 3, GETDATE()), 15, 'Chilly'),
        (NEWID(), DATEADD(day, 4, GETDATE()), 30, 'Hot'),
        (NEWID(), DATEADD(day, 5, GETDATE()), 10, 'Cool');
END
GO