IF COL_LENGTH('Orders', 'CustomerNote') IS NULL
BEGIN
    ALTER TABLE Orders
    ADD CustomerNote NVARCHAR(1000) NULL;
END
