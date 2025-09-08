# Summary
This task is to create a combination of SQL and SqlRepository code to retrieve a list of items from the database.

This task must return a paged list of items, with the ability to filter the items by a search term, sort the items by a specified column and direction, and paginate the results using skip and take parameters.

This task must match the signature of the method ResourceSqlRepository.GetResourceGridItemsAsync().  The agent is free to change the existing method implementation as needed to meet the requirements of this task.

````csharp
Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(string? requestSearchFor, string? requestOrderBy, string? requestOrderDirection,
            int skipRecords, int takeRecords, CancellationToken cancellationToken)
````

## Steps For Task
**Focus ONLY on the requirements in this task.  Do NOT make any other changes to the code base. Only change code that is required to meet the requirements of this task.**
1. Create an internal TODO list
1. Review this task and ask any clarifying questions
1. Review target method and SQL database tables
1. Create one or more solutions to meet the requirements using SQL and C#
1. If more than one solution is created, evaluate the pros and cons of each solution and let me chose which one I want.
1. Implement the chosen solution

### Possible options to consider
- Use a multiple recordset query and map the results to the output object in C#
- Use a single recordset with redundant columns for tag values and map the results to the output object in C#
- Perhaps use other strategies

## SQL Server Requirements
### Store Procedure Name
- Name the stored procedure `ResourceMapper.Resource_GetItems`
- Place the stored procedure in the `ResourceMapper/StoredProcedures` folder of the dataase project.
### Store Procedure Parameters
- `@SearchFor NVARCHAR(255) = NULL` -- used to filter the items
- `@OrderBy NVARCHAR(50) = NULL` -- the database column name to sort by
- `@OrderDirection NVARCHAR(4) = NULL` -- "ASC" or "DESC"
- `@Skip INT = 0` -- 0 based index of the first record to return
- `@Take INT = 50` -- the number of records to return

### Input Parameters
- *SearchFor*: A string parameter used to filter the items based on a search term. Use a contains wild card on the following fields:
    - Resource:ResourceKey
    - Resource:ResourceName
    - Resource:Description
    - ResourceTag:TagValue
    - TagDefintion:TakDefinitionKey

- *OrderBy*: The database column name parameter used to sort the results. This should be a direct mapping to the database column name.  If null then use natuarl sort of the query.
- *OrderDirection*: The direction of the sort, either "ASC" for ascending or "DESC" for descending.  If NULL then default to "ASC".
- *Skip*: 0 based index of the first record to return. You might have to adjust for 1 based indexing in SQL.
- *Take*: The number of records to return.  This is used for paging.

### Output Parameters
- *TotalRecords*: The total number of records that match the search criteria, before applying pagination.

### SQL Result Set
- determined by agent solution

### Important Notes:
1) TotalRecords must be set to the total number of records that match the search criteria before applying pagination of Skip and Take.
2) Sorting must be applied based on the OrderBy and OrderDirection parameters.
3) Sorting must be done before applying pagination.



