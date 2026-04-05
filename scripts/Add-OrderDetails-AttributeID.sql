IF COL_LENGTH('OrderDetails', 'AttributeID') IS NULL
BEGIN
    ALTER TABLE OrderDetails
    ADD AttributeID BIGINT NULL;

    ALTER TABLE OrderDetails
    ADD CONSTRAINT FK_OrderDetails_ProductAttributes
        FOREIGN KEY (AttributeID) REFERENCES ProductAttributes(AttributeID);
END
