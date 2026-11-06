using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>جدول مرفقات السندات المحاسبية.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261106120000_AddVoucherAttachments")]
public partial class AddVoucherAttachments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.VoucherAttachments', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[VoucherAttachments] (
                    [Id] int NOT NULL IDENTITY,
                    [VoucherId] int NOT NULL,
                    [FileName] nvarchar(500) NOT NULL,
                    [FilePath] nvarchar(1000) NOT NULL,
                    [Description] nvarchar(500) NULL,
                    [BranchId] int NOT NULL,
                    [SyncId] uniqueidentifier NOT NULL CONSTRAINT [DF_VoucherAttachments_SyncId] DEFAULT (NEWID()),
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(100) NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(100) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(100) NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_VoucherAttachments] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_VoucherAttachments_Vouchers_VoucherId]
                        FOREIGN KEY ([VoucherId]) REFERENCES [dbo].[Vouchers]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_VoucherAttachments_Branches_BranchId]
                        FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                );

                CREATE INDEX [IX_VoucherAttachments_VoucherId]
                    ON [dbo].[VoucherAttachments]([VoucherId]);
                CREATE INDEX [IX_VoucherAttachments_IsDeleted]
                    ON [dbo].[VoucherAttachments]([IsDeleted]);
                CREATE INDEX [IX_VoucherAttachments_BranchId]
                    ON [dbo].[VoucherAttachments]([BranchId]);
                CREATE INDEX [IX_VoucherAttachments_SyncId]
                    ON [dbo].[VoucherAttachments]([SyncId]);
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.VoucherAttachments', N'U') IS NOT NULL
                DROP TABLE [dbo].[VoucherAttachments];
            """);
    }
}
