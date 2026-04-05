IF COL_LENGTH('Products', 'Quantity') IS NULL
BEGIN
    ALTER TABLE Products
    ADD Quantity INT NOT NULL CONSTRAINT DF_Products_Quantity DEFAULT (0);
END
