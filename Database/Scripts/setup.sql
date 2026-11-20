-- ================================================================
-- ShopElegance Database Setup Script
-- Run this ONLY if you prefer manual SQL over EF Core migrations.
-- Otherwise use: dotnet ef database update
-- ================================================================

-- Create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ShopEleganceDb')
BEGIN
    CREATE DATABASE ShopEleganceDb;
END
GO

USE ShopEleganceDb;
GO

-- The schema is auto-created by EF Core migrations.
-- Run the following commands from the solution root to set up:
--
--   cd src/ShopElegance.Web
--   dotnet ef migrations add InitialCreate --project ../ShopElegance.Infrastructure
--   dotnet ef database update
--
-- Seed data is automatically applied on first run via DatabaseSeeder.cs
-- Default admin credentials: admin@shopelegance.com / Admin@123456!

PRINT 'Database ready. Run EF Core migrations to create schema.';
GO
