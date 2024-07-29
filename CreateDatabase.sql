CREATE TABLE InvoiceStates
(
    InvoiceStateID INT           NOT NULL PRIMARY KEY,
    Name           NVARCHAR(15)  NOT NULL,
    Description    NVARCHAR(100) NOT NULL
)

INSERT INTO InvoiceStates (InvoiceStateID, Name, Description)
VALUES
(0, 'Unknown', 'Unknown'),
(1, 'Ready', 'Ready for me to do work on'),
(2, 'Waiting', 'Waiting on something external'),
(3, 'Finished', 'Completed')

CREATE TABLE Invoices
(
    InvoiceID               INT           NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceNumber           NVARCHAR(10)  NOT NULL,
    InvoiceDate             DATETIME2     NOT NULL,
    InvoiceStateID          INT           NOT NULL,
    InvoiceStateDescription NVARCHAR(250) NOT NULL,
    PayableTo               NVARCHAR(250) NOT NULL DEFAULT N'Wizards of the Plains',
    ProjectName             NVARCHAR(250),
    DueDate                 DATETIME2

    CONSTRAINT FK_Invoices_InvoiceStateID FOREIGN KEY (InvoiceStateID) REFERENCES InvoiceStates (InvoiceStateID)
)

CREATE TABLE InvoiceRelationTypes
(
    RelationTypeID INT          NOT NULL PRIMARY KEY,
    Name           NVARCHAR(20) NOT NULL
)

INSERT INTO InvoiceRelationTypes (RelationTypeID, Name)
VALUES
(1, N'Predecessor'),
(2, N'Successor'),
(3, N'Related')

CREATE TABLE InvoiceRelations
(
    RowID               INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceID           INT NOT NULL,
    RelationTypeID      INT NOT NULL,
    ReferencesInvoiceID INT NOT NULL,

    CONSTRAINT FK_InvoiceRelations_RelationTypeID FOREIGN KEY (RelationTypeID) REFERENCES InvoiceRelationTypes(RelationTypeID),
    CONSTRAINT FK_InvoiceRelations_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID),
    CONSTRAINT FK_InvoiceRelations_ReferencesInvoiceID FOREIGN KEY (ReferencesInvoiceID) REFERENCES Invoices(InvoiceID)
)

CREATE TABLE InvoiceInvoicees
(
    RowID        INT           NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceID    INT           NOT NULL,
    LineNumber   INT           NOT NULL,
    InvoiceeLine NVARCHAR(250) NOT NULL,

    CONSTRAINT FK_InvoiceInvoicees_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices (InvoiceID)
)

CREATE TABLE InvoiceItems
(
    RowID       INT            NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceID   INT            NOT NULL,
    Sequence    INT            NOT NULL,
    Description NVARCHAR(250)  NOT NULL,
    Quantity    INT            NOT NULL,
    UnitPrice   DECIMAL(18, 2) NOT NULL,

    CONSTRAINT FK_InvoiceItems_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices (InvoiceID),
    CONSTRAINT UQ_InvoiceItems_InvoiceIDItemNumber UNIQUE (InvoiceID, Sequence)
)

CREATE TABLE Taxes
(
    TaxID   INT            NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    TaxName NVARCHAR(100)  NOT NULL,
    TaxRate DECIMAL(10, 4) NOT NULL
)

CREATE TABLE InvoiceTaxes
(
    RowID     INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceID INT NOT NULL,
    Sequence  INT NOT NULL,
    TaxID     INT NOT NULL,

    CONSTRAINT FK_InvoiceTaxes_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID),
    CONSTRAINT FK_InvoiceTaxes_TaxID FOREIGN KEY (TaxID) REFERENCES Taxes(TaxID)
)

CREATE TABLE PaymentTypes
(
    PaymentTypeID INT NOT NULL PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
)

INSERT INTO PaymentTypes (PaymentTypeID, Name)
VALUES
(0, N'Unknown'),
(1, N'Custom'),
(2, N'Cash'),
(3, N'e-Transfer'),
(4, N'Wire Transfer'),
(5, N'Debit Card'),
(6, N'Credit Card'),
(7, N'MasterCard'),
(8, N'Visa'),
(9, N'American Express'),
(10, N'Discover'),
(11, N'JCB'),
(12, N'UnionPay')

CREATE TABLE InvoicePayments
(
    RowID             INT            NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceID         INT            NOT NULL,
    Sequence          INT            NOT NULL,
    PaymentTypeID     INT            NOT NULL,
    PaymentTypeCustom NVARCHAR(100)      NULL,
    ReceivedDateTime  DATETIME2          NULL,
    Amount            DECIMAL(18, 2) NOT NULL,
    ReferenceNumber   NVARCHAR(50)       NULL,

    CONSTRAINT FK_InvoicePayments_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID),
    CONSTRAINT FK_InvoicePayments_PaymentTypeID FOREIGN KEY (PaymentTypeID) REFERENCES PaymentTypes(PaymentTypeID)
)

CREATE TABLE InvoiceNotes
(
    RowID      INT           NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    InvoiceID  INT           NOT NULL,
    Sequence   INT           NOT NULL,
    TextLine   NVARCHAR(200) NOT NULL,
    IsInternal BIT           NOT NULL,

    CONSTRAINT FK_InvoiceNotes_InvoiceID FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID)
)
