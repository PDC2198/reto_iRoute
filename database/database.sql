IF DB_ID('CommerceDB') IS NULL
BEGIN
    CREATE DATABASE CommerceDB;
END;
GO

USE CommerceDB;
GO

IF OBJECT_ID('dbo.commerce_quarantine', 'U') IS NOT NULL
    DROP TABLE dbo.commerce_quarantine;

IF OBJECT_ID('dbo.commerce', 'U') IS NOT NULL
    DROP TABLE dbo.commerce;
GO

CREATE TABLE dbo.commerce
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    pc_processdate DATE NOT NULL,
    pc_nomcomred NVARCHAR(200) NULL,
    pc_numdoc NVARCHAR(50) NULL,
    pc_address NVARCHAR(250) NULL,
    pc_city NVARCHAR(100) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.commerce_quarantine
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    commerce_id INT NULL,
    pc_processdate DATE NULL,
    pc_nomcomred NVARCHAR(200) NULL,
    pc_numdoc NVARCHAR(50) NULL,
    pc_address NVARCHAR(250) NULL,
    pc_city NVARCHAR(100) NULL,
    motivo NVARCHAR(500) NOT NULL,
    quarantined_at DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE OR ALTER PROCEDURE dbo.sp_create_commerce
    @pc_processdate DATE,
    @pc_nomcomred NVARCHAR(200),
    @pc_numdoc NVARCHAR(50),
    @pc_address NVARCHAR(250) = NULL,
    @pc_city NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.commerce
    (
        pc_processdate,
        pc_nomcomred,
        pc_numdoc,
        pc_address,
        pc_city
    )
    VALUES
    (
        @pc_processdate,
        @pc_nomcomred,
        @pc_numdoc,
        @pc_address,
        @pc_city
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_process_commerce
    @pc_processdate DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cantidad INT = 0;

    BEGIN TRANSACTION;

    BEGIN TRY

        INSERT INTO dbo.commerce_quarantine
        (
            commerce_id,
            pc_processdate,
            pc_nomcomred,
            pc_numdoc,
            pc_address,
            pc_city,
            motivo
        )
        SELECT
            id,
            pc_processdate,
            pc_nomcomred,
            pc_numdoc,
            pc_address,
            pc_city,
            CASE
                WHEN NULLIF(LTRIM(RTRIM(pc_nomcomred)), '') IS NULL
                     AND NULLIF(LTRIM(RTRIM(pc_numdoc)), '') IS NULL
                    THEN 'El nombre del comercio se encuentra vacío. El número de documento se encuentra vacío.'

                WHEN NULLIF(LTRIM(RTRIM(pc_nomcomred)), '') IS NULL
                    THEN 'El nombre del comercio se encuentra vacío.'

                WHEN NULLIF(LTRIM(RTRIM(pc_numdoc)), '') IS NULL
                    THEN 'El número de documento se encuentra vacío.'

                WHEN pc_numdoc LIKE '%[^0-9]%'
                    THEN 'El número de documento contiene letras o caracteres especiales.'
            END
        FROM dbo.commerce
        WHERE pc_processdate = @pc_processdate
          AND
          (
              NULLIF(LTRIM(RTRIM(pc_nomcomred)), '') IS NULL
              OR NULLIF(LTRIM(RTRIM(pc_numdoc)), '') IS NULL
              OR pc_numdoc LIKE '%[^0-9]%'
          );

        SET @cantidad = @@ROWCOUNT;

        DELETE FROM dbo.commerce
        WHERE pc_processdate = @pc_processdate
          AND
          (
              NULLIF(LTRIM(RTRIM(pc_nomcomred)), '') IS NULL
              OR NULLIF(LTRIM(RTRIM(pc_numdoc)), '') IS NULL
              OR pc_numdoc LIKE '%[^0-9]%'
          );

        COMMIT TRANSACTION;

        SELECT @cantidad AS registros_cuarentena;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;

    END CATCH
END;
GO