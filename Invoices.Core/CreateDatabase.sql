CREATE TABLE Customer
(
    CustomerID INT NOT NULL IDENTITY(1, 1),

    CONSTRAINT PK_Customer PRIMARY KEY (CustomerID)
)

CREATE TABLE CustomerLineTypes
(
    LineTypeID  INT          NOT NULL,
    Description NVARCHAR(30) NOT NULL,

    CONSTRAINT PK_CustomerLineTypes PRIMARY KEY (LineTypeID)
)

INSERT INTO CustomerLineTypes (LineTypeID, Description)
VALUES
(1, 'Name'),
(2, 'Address'),
(3, 'E-mail'),
(4, 'Phone'),
(5, 'Note')

CREATE TABLE CustomerLines
(
    RowID      INT           NOT NULL IDENTITY(1, 1),
    CustomerID INT           NOT NULL,
    LineTypeID INT           NOT NULL,
    Sequence   INT           NOT NULL,
    LineText   NVARCHAR(200) NOT NULL,

    CONSTRAINT PK_CustomerLines PRIMARY KEY (RowID),
    CONSTRAINT FK_CustomerLines_CustomerID FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    CONSTRAINT FK_CustomerLines_LineTypeID FOREIGN KEY (LineTypeID) REFERENCES CustomerLineTypes(LineTypeID)
)

CREATE TABLE InvoiceStates
(
    InvoiceStateID INT           NOT NULL,
    Name           NVARCHAR(15)  NOT NULL,
    Description    NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_InvoiceStates PRIMARY KEY (InvoiceStateID)
)

INSERT INTO InvoiceStates (InvoiceStateID, Name, Description)
VALUES
(0, 'Unknown', 'Unknown'),
(1, 'Ready', 'Ready for me to do work on'),
(2, 'Waiting', 'Waiting on something external'),
(3, 'Finished', 'Completed')

CREATE TABLE Invoices
(
    InvoiceID               INT           NOT NULL IDENTITY(1, 1),
    InvoiceNumber           NVARCHAR(10)  NOT NULL,
    InvoiceDate             DATETIME2     NOT NULL,
    InvoiceStateID          INT           NOT NULL,
    InvoiceStateDescription NVARCHAR(250) NOT NULL,
    InvoiceeCustomerID      INT               NULL,
    PayableTo               NVARCHAR(250) NOT NULL DEFAULT N'Wizards of the Plains',
    ProjectName             NVARCHAR(250),
    DueDate                 DATETIME2,

    CONSTRAINT PK_Invoices PRIMARY KEY (InvoiceID),
    CONSTRAINT FK_Invoices_InvoiceStateID FOREIGN KEY (InvoiceStateID) REFERENCES InvoiceStates (InvoiceStateID),
    CONSTRAINT FK_Invoices_InvoiceeCustomerID FOREIGN KEY (InvoiceeCustomerID) REFERENCES Customers (CustomerID)
)

CREATE TABLE InvoiceRelationTypes
(
    RelationTypeID INT          NOT NULL,
    Name           NVARCHAR(20) NOT NULL,

    CONSTRAINT PK_InvoiceRelationTypes PRIMARY KEY (RelationTypeID)
)

INSERT INTO InvoiceRelationTypes (RelationTypeID, Name)
VALUES
(1, N'Predecessor'),
(2, N'Successor'),
(3, N'Related')

CREATE TABLE InvoiceRelations
(
    RowID               INT NOT NULL IDENTITY(1, 1),
    InvoiceID           INT NOT NULL,
    RelationTypeID      INT NOT NULL,
    ReferencesInvoiceID INT NOT NULL,

    CONSTRAINT PK_InvoiceRelations PRIMARY KEY (RowID),
    CONSTRAINT FK_InvoiceRelations_RelationTypeID FOREIGN KEY (RelationTypeID) REFERENCES InvoiceRelationTypes(RelationTypeID),
    CONSTRAINT FK_InvoiceRelations_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID),
    CONSTRAINT FK_InvoiceRelations_ReferencesInvoiceID FOREIGN KEY (ReferencesInvoiceID) REFERENCES Invoices(InvoiceID)
)

CREATE TABLE InvoiceItems
(
    RowID       INT            NOT NULL IDENTITY(1, 1),
    InvoiceID   INT            NOT NULL,
    Sequence    INT            NOT NULL,
    Description NVARCHAR(250)  NOT NULL,
    Quantity    INT            NOT NULL,
    UnitPrice   DECIMAL(18, 2) NOT NULL,

    CONSTRAINT PK_InvoiceItems PRIMARY KEY (RowID),
    CONSTRAINT FK_InvoiceItems_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices (InvoiceID),
    CONSTRAINT UQ_InvoiceItems_InvoiceIDItemNumber UNIQUE (InvoiceID, Sequence)
)

CREATE TABLE Taxes
(
    TaxID   INT            NOT NULL IDENTITY(1, 1),
    TaxName NVARCHAR(100)  NOT NULL,
    TaxRate DECIMAL(10, 4) NOT NULL,

    CONSTRAINT PK_Taxes PRIMARY KEY (TaxID)
)

CREATE TABLE InvoiceTaxes
(
    RowID     INT NOT NULL IDENTITY(1, 1),
    InvoiceID INT NOT NULL,
    Sequence  INT NOT NULL,
    TaxID     INT NOT NULL,

    CONSTRAINT PK_InvoiceTaxes PRIMARY KEY (RowID),
    CONSTRAINT FK_InvoiceTaxes_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID),
    CONSTRAINT FK_InvoiceTaxes_TaxID FOREIGN KEY (TaxID) REFERENCES Taxes(TaxID)
)

CREATE TABLE PaymentTypes
(
    PaymentTypeID INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_PaymentTypes PRIMARY KEY (PaymentTypeID)
)

INSERT INTO PaymentTypes (PaymentTypeID, Name)
VALUES
(0, N'Unknown'),
(1, N'Custom'),
(2, N'Cash'),
(3, N'e-Transfer'),
(4, N'Wire Transfer'),
(5, N'PayPal'),
(6, N'Debit Card'),
(7, N'Credit Card'),
(8, N'MasterCard'),
(9, N'Visa'),
(10, N'American Express'),
(11, N'Discover'),
(12, N'JCB'),
(13 N'UnionPay')

CREATE TABLE InvoicePayments
(
    RowID             INT            NOT NULL IDENTITY(1, 1),
    InvoiceID         INT            NOT NULL,
    Sequence          INT            NOT NULL,
    PaymentTypeID     INT            NOT NULL,
    PaymentTypeCustom NVARCHAR(100)      NULL,
    ReceivedDateTime  DATETIME2          NULL,
    Amount            DECIMAL(18, 2) NOT NULL,
    ReferenceNumber   NVARCHAR(100)      NULL,

    CONSTRAINT PK_InvoicePayments PRIMARY KEY (RowID),
    CONSTRAINT FK_InvoicePayments_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID),
    CONSTRAINT FK_InvoicePayments_PaymentTypeID FOREIGN KEY (PaymentTypeID) REFERENCES PaymentTypes(PaymentTypeID)
)

CREATE TABLE InvoiceNotes
(
    RowID      INT           NOT NULL IDENTITY(1, 1),
    InvoiceID  INT           NOT NULL,
    Sequence   INT           NOT NULL,
    TextLine   NVARCHAR(200) NOT NULL,
    IsInternal BIT           NOT NULL,

    CONSTRAINT PK_InvoiceNotes PRIMARY KEY (RowID),
    CONSTRAINT FK_InvoiceNotes_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID)
)

CREATE TYPE StringList AS TABLE (Sequence INT NOT NULL, Value NVARCHAR(MAX) NOT NULL)