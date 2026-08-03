-- Entry-point template rows for ONE resource type, as passed by ResourceTypeTag_SetForType.
-- RequirementLevel is NULL when the type does not override the tag definition's own level.
CREATE TYPE [HTResourceMapper].[ResourceTypeTagList] AS TABLE
(
    [TagDefinitionId]  INT NOT NULL,
    [IsDefaultPrimary] BIT NOT NULL,
    [RequirementLevel] NVARCHAR(20) NULL
);
