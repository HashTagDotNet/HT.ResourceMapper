# Hashtag Database Conventions

*It is important to note that these are not strict rules but rather common practices. The goal is to be consistent and aim for descriptive and meaningful names that make the purpose of the object clear.*

---

## Common Terms

- **MUST** - rule that is mandatory and is enforced by code review.  Likely to be rejected if not followed.
- **SHOULD** - rule that is recommended to follow generally accepted conventions and practices.
- **MAY** - rule that is optional and can be followed at the discretion of the developer.
- **AVOID** - condition in code that, if included, is likely to be rejected by code review.

## Table Conventions

- Table names MUST be singular
- Use PascalCase (example: `OrderNotification`).
- **AVOID** using "_" underscores in table names.

## Index Conventions

- Index names **MUST** follow this pattern `[IndexType]_[TableName]_[ColumnName]`
- **SHOULD** define index immediately after the field in the table definition
- **SHOULD** include all columns in the index definition `[IndexType]_[TableName]_[Column1]_[Column2]`
- **AVOID** using default system generated index names
- **MUST** name PrimaryKey with 'Id'
suffix (Pascal case)
- **AVOID** creating constraints should never be created with NOCHECK or NOT FOR REPLICATION. This causes the query optimizer to ignore them.
- **SHOULD** define index using `WITH (ONLINE = ON)`
- If you have covered columns, you **MUST** use a double underscore to signify the covered column list is starting, then single underscores to separate the column list. (example: `IX_CentralOffice_AreaCode__CountryId`)

### Index Types

| Index Type | Description |
| --- | --- |
| IX | Non-unique index |
| PK | Primary key index |
| FK | Foreign key index |
| UQ | Unique index |
| AK | Specialized unique intended lookup by application code |

### Primary Key Notes

- Name **MUST** start with 'PK'
- **AVOID** using default (not specified) index
- Primary key id fields **MUST** be named `<tablename>Id` with Pascal case 'Id' (example: `CompanyId`,`UserId`)

## Table Conventions

- Tables **MUST** be named singular PascalCase (e.g. MyTable)
- **SHOULD** have an `UpdatedOn DATETIME2(7)` with default `DF_<tablename>_UpdatedOn` value of `SYSUTCDATETIME()`

### Primary Key Field

- **MUST** have a primary key field
- Primary key **MUST** be named `<tablename>Id` with Pascal case 'Id' (example: `CompanyId`,`UserId`)
- **SHOULD** have a primary auto incrementing key (IDENTITY(1,1)) as the first field
- Primary key example:
````sql
[MyTableId] [int] IDENTITY(1,1) NOT NULL
        CONSTRAINT [PK_MyTable_MyTableId] PRIMARY KEY WITH (ONLINE = ON)
````
#### Foreign Key Fields

- Foreign key fields **SHOULD** use the primary key name of the target table and use the same casing as the target table (example: `AlertId`, `CompanyID`)
- Multiple foreign keys to the same table **SHOULD** use an optional selector prefix. (example: `BossUserId`, `AssistantUserId`)
- You **SHOULD** consider if an index is needed on foreign key fields to improve query performance or to optimize when the target table is being purged. Example:
````sql
CREATE TABLE [MyTable]
(
   PingTypeLookupId TINYINT
    CONSTRAINT [FK_MyTable_PingTypeLookup_PingTypeLookupId] FOREIGN KEY REFERENCES PingTypeLookup(PingTypeLookupId)
    INDEX [IX_MyTable_PingTypeLookupId]
)
````

### Fields

- Use PascalCase for field names (example: `RoadMilesToGo`)
- **AVOID** using '_' as word separators.
- **SHOULD** choose meaningful names that convey the purpose of the field. Consider using suffix to avoid confusion (bad: `Distance`, better: `DistanceInMiles`)
- Care should be taken to avoid using SQL reserved words unless they are needed for clarity
- Default values **MUST** have a named constraint with this pattern `DF_<tablename>_<columnName>` (example: `DF_MyTable_MyColumn`)
- DateTime fields are usually DATETIME2(7) unless otherwise indicated
- **AVOID** Adding `UTC` to date or time related fields are UTC is the internal default.

## Common CONSTRAINT conventions

- **MUST** use the following naming conventions for constraints
`<constrainttype-upper>_<tablename>_<columnname(s)>`:
- If there is more than on column name, separate with underscores.
- **AVOID** creating CONSTRAINTS with NOCHECK or NOT FOR REPLICATION. This causes the query optimizer to ignore them.
- Constraint Types (UpperCase):
  - Primary key constraints: `PK_<tablename>_<columnname>`
  - Foreign key constraints: `FK_<tablename>_<referencedtable>_<columnname>`
  - Unique constraints: `UQ_<tablename>_<columnname>`
  - Check constraints: `CK_<tablename>_<columnname>`
  - Default constraints: `DF_<tablename>_<columnname>`
- Examples:
  -  `PK_Charge_ChargeID`
  - `FK_OrderRegistration_Order_OrderID`
  - `AK_Company_OrderSearch_Parameter_Override_CompanyID`
  - `UQ_CompanyUrlAuthentication_CompanyID_CompanyUrlTypeID`
  - `CK_CompanyImage_CompanyID_CompanyImageTypeID`
  - `DF_ColdChainReading_CreatedDateTime`

## Examples

### Table
````sql
CREATE TABLE [DemoSchema].[MyTable](
    [MyTableId] [int] IDENTITY(1,1) NOT NULL
        CONSTRAINT [PK_MyTable_MyTableId] PRIMARY KEY WITH (ONLINE = ON)        
    ,[MyForeignKeyId] [int] NOT NULL
        CONSTRAINT [FK_MyTable_OtherTable_MyForeignKeyId] FOREIGN KEY REFERENCES OtherTable(OtherTableId)
    ,[MyColumn] [varchar](100) NOT NULL
        CONSTRAINT [DF_MyTable_MyColumn] DEFAULT ('DefaultValue')
    ,[UpdatedOn] [datetime2](7) NOT NULL
        CONSTRAINT [DF_MyTable_UpdatedOn] DEFAULT (SYSUTCDATETIME())
)

````

## Views

 - Pascal case names.
 - **MUST** start with `VW`.  
 - *We currently have 4 views and none of them follow this standard.*

## Stored Procedures

 - Pascal case names.  
 - `<TableName>_<Verb> or <TableName>_<Verb><Noun>` (example: `Company_Get`, `Company_CheckMPID`,`Company_GetByMpid`)

## Functions

- Pascal case names.
- User-defined functions have a verb followed by a noun (example: `GetTimeZone`)

## Triggers

- Pascal case names
- **MUST** start with `TR_`

## Lookup Tables

Lookup tables are tables that have static data such as enums or country codes.

### Lookup Reference Table

* Name table `<lookuptype>Lookup` with `Lookup` suffix (example: `PingTypeLookup`)

* Create primary key as per convention (example: `PingTypeLookupId`) For an enum backed lookup, *do not use Identity(1,1) on primary key*.
* Primary key type should be the smallest feasible integer type; usually `tinyint`
* Create a script in the /scripts folder to initialize and update the lookup table.  Script should be idempotent; in that it can be run multiple times without causing adverse affects.
* Usually there is an human readable field associated with the lookup. Often this the enum text. This field **SHOULD** be called 'CodeText'.

###### Lookup Table Example

````sql
CREATE TABLE [PingTypeLookup]
(
    [PingTypeLookupId] TINYINT NOT NULL
       CONSTRAINT [PK_PingTypeLookup_PingTypeLookupId] PRIMARY KEY,
    [CodeText] VARCHAR(32) NULL,
)

````

### Data Table With Lookup Reference

* Use the standard foreign key practices outlined in this document
* CONSIDER if you need an index on your table's Lookup reference field (example: PingTypeLookupId)

###### Referencing Table Example

````sql
CREATE TABLE [MyTable]
(
   PingTypeLookupId TINYINT
    CONSTRAINT [FK_MyTable_PingTypeLookup_PingTypeLookupId] FOREIGN KEY REFERENCES PingTypeLookup(PingTypeLookupId)
    --INDEX [IX_MyTable_PingTypeLookupId]
)
````
