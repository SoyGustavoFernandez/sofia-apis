-- Refresh token store for short-lived JWT rotation
CREATE TABLE [dbo].[RefreshTokens] (
    [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [CuentaId]        UNIQUEIDENTIFIER NOT NULL,
    [TokenHash]       NVARCHAR(128)    NOT NULL,
    [ExpiresAt]       DATETIMEOFFSET   NOT NULL,
    [IsRevoked]       BIT              NOT NULL DEFAULT 0,
    [RevokedAt]       DATETIMEOFFSET   NULL,
    [TenantId]        UNIQUEIDENTIFIER NULL,
    [CreatedAt]       DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [CreatedBy]       NVARCHAR(256)    NULL,
    [LastModifiedAt]  DATETIMEOFFSET   NULL,
    [LastModifiedBy]  NVARCHAR(256)    NULL,
    [IsDeleted]       BIT              NOT NULL DEFAULT 0,
    [DeletedAt]       DATETIMEOFFSET   NULL,
    [DeletedBy]       NVARCHAR(256)    NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Cuentas] FOREIGN KEY ([CuentaId])
        REFERENCES [dbo].[Cuentas]([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens] ([TokenHash]);
CREATE INDEX [IX_RefreshTokens_CuentaId] ON [dbo].[RefreshTokens] ([CuentaId]);
