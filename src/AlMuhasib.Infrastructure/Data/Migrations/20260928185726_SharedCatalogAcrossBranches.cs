using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SharedCatalogAcrossBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Branches_BranchId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_PackagingTypes_Branches_BranchId",
                table: "PackagingTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_PricingTypes_Branches_BranchId",
                table: "PricingTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductColors_Branches_BranchId",
                table: "ProductColors");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOffers_Branches_BranchId",
                table: "ProductOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPrices_Branches_BranchId",
                table: "ProductPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Branches_BranchId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizes_Branches_BranchId",
                table: "ProductSizes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductUnits_Branches_BranchId",
                table: "ProductUnits");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Name",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnits_BranchId",
                table: "ProductUnits");

            migrationBuilder.DropIndex(
                name: "IX_ProductSizes_BranchId",
                table: "ProductSizes");

            migrationBuilder.DropIndex(
                name: "IX_Products_BranchId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_BranchId",
                table: "ProductPrices");

            migrationBuilder.DropIndex(
                name: "IX_ProductOffers_BranchId",
                table: "ProductOffers");

            migrationBuilder.DropIndex(
                name: "IX_ProductColors_BranchId",
                table: "ProductColors");

            migrationBuilder.DropIndex(
                name: "IX_PricingTypes_BranchId",
                table: "PricingTypes");

            migrationBuilder.DropIndex(
                name: "IX_PackagingTypes_BranchId",
                table: "PackagingTypes");

            migrationBuilder.DropIndex(
                name: "IX_Categories_BranchId",
                table: "Categories");

            // دمج المكررات قبل إسقاط BranchId (الأولوية للفرع الرئيسي)
            migrationBuilder.Sql("""
                ;WITH ranked AS (
                    SELECT c.[Id], c.[Name],
                           ROW_NUMBER() OVER (
                               PARTITION BY LOWER(LTRIM(RTRIM(c.[Name])))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, c.[Id]
                           ) AS rn
                    FROM [dbo].[Categories] c
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = c.[BranchId]
                    WHERE c.[IsDeleted] = 0
                ),
                map AS (
                    SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                    FROM ranked keep
                    INNER JOIN ranked dup ON LOWER(LTRIM(RTRIM(keep.[Name]))) = LOWER(LTRIM(RTRIM(dup.[Name])))
                        AND keep.rn = 1 AND dup.rn > 1
                )
                UPDATE p SET p.[CategoryId] = m.KeepId
                FROM [dbo].[Products] p
                INNER JOIN map m ON p.[CategoryId] = m.DupId;

                ;WITH ranked AS (
                    SELECT c.[Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY LOWER(LTRIM(RTRIM(c.[Name])))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, c.[Id]
                           ) AS rn
                    FROM [dbo].[Categories] c
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = c.[BranchId]
                    WHERE c.[IsDeleted] = 0
                )
                UPDATE c SET c.[IsDeleted] = 1, c.[DeletedAt] = SYSUTCDATETIME(), c.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[Categories] c
                INNER JOIN ranked r ON c.[Id] = r.[Id]
                WHERE r.rn > 1;

                ;WITH ranked AS (
                    SELECT t.[Id], t.[Name],
                           ROW_NUMBER() OVER (
                               PARTITION BY LOWER(LTRIM(RTRIM(t.[Name])))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, t.[Id]
                           ) AS rn
                    FROM [dbo].[PricingTypes] t
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = t.[BranchId]
                    WHERE t.[IsDeleted] = 0
                ),
                map AS (
                    SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                    FROM ranked keep
                    INNER JOIN ranked dup ON LOWER(LTRIM(RTRIM(keep.[Name]))) = LOWER(LTRIM(RTRIM(dup.[Name])))
                        AND keep.rn = 1 AND dup.rn > 1
                )
                UPDATE pp SET pp.[PricingTypeId] = m.KeepId
                FROM [dbo].[ProductPrices] pp
                INNER JOIN map m ON pp.[PricingTypeId] = m.DupId;

                UPDATE ii SET ii.[PricingTypeId] = m.KeepId
                FROM [dbo].[InvoiceItems] ii
                INNER JOIN (
                    SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                    FROM (
                        SELECT t.[Id], t.[Name],
                               ROW_NUMBER() OVER (
                                   PARTITION BY LOWER(LTRIM(RTRIM(t.[Name])))
                                   ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, t.[Id]
                               ) AS rn
                        FROM [dbo].[PricingTypes] t
                        LEFT JOIN [dbo].[Branches] b ON b.[Id] = t.[BranchId]
                        WHERE t.[IsDeleted] = 0
                    ) keep
                    INNER JOIN (
                        SELECT t.[Id], t.[Name],
                               ROW_NUMBER() OVER (
                                   PARTITION BY LOWER(LTRIM(RTRIM(t.[Name])))
                                   ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, t.[Id]
                               ) AS rn
                        FROM [dbo].[PricingTypes] t
                        LEFT JOIN [dbo].[Branches] b ON b.[Id] = t.[BranchId]
                        WHERE t.[IsDeleted] = 0
                    ) dup ON LOWER(LTRIM(RTRIM(keep.[Name]))) = LOWER(LTRIM(RTRIM(dup.[Name])))
                        AND keep.rn = 1 AND dup.rn > 1
                ) m ON ii.[PricingTypeId] = m.DupId;

                ;WITH ranked AS (
                    SELECT t.[Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY LOWER(LTRIM(RTRIM(t.[Name])))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, t.[Id]
                           ) AS rn
                    FROM [dbo].[PricingTypes] t
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = t.[BranchId]
                    WHERE t.[IsDeleted] = 0
                )
                UPDATE t SET t.[IsDeleted] = 1, t.[DeletedAt] = SYSUTCDATETIME(), t.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[PricingTypes] t
                INNER JOIN ranked r ON t.[Id] = r.[Id]
                WHERE r.rn > 1;

                ;WITH ranked AS (
                    SELECT p.[Id],
                           COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name])))) AS KeyName,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name]))))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, p.[Id]
                           ) AS rn
                    FROM [dbo].[Products] p
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = p.[BranchId]
                    WHERE p.[IsDeleted] = 0
                ),
                map AS (
                    SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                    FROM ranked keep
                    INNER JOIN ranked dup ON keep.KeyName = dup.KeyName AND keep.rn = 1 AND dup.rn > 1
                )
                UPDATE ws SET ws.[ProductId] = m.KeepId
                FROM [dbo].[WarehouseStocks] ws
                INNER JOIN map m ON ws.[ProductId] = m.DupId
                WHERE NOT EXISTS (
                    SELECT 1 FROM [dbo].[WarehouseStocks] x
                    WHERE x.[WarehouseId] = ws.[WarehouseId] AND x.[ProductId] = m.KeepId AND x.[IsDeleted] = 0
                );

                ;WITH ranked AS (
                    SELECT p.[Id],
                           COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name])))) AS KeyName,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name]))))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, p.[Id]
                           ) AS rn
                    FROM [dbo].[Products] p
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = p.[BranchId]
                    WHERE p.[IsDeleted] = 0
                ),
                map AS (
                    SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                    FROM ranked keep
                    INNER JOIN ranked dup ON keep.KeyName = dup.KeyName AND keep.rn = 1 AND dup.rn > 1
                )
                UPDATE keepStock SET keepStock.[Quantity] = keepStock.[Quantity] + dupStock.[Quantity],
                                     keepStock.[OpeningQuantity] = keepStock.[OpeningQuantity] + dupStock.[OpeningQuantity]
                FROM [dbo].[WarehouseStocks] keepStock
                INNER JOIN map m ON keepStock.[ProductId] = m.KeepId
                INNER JOIN [dbo].[WarehouseStocks] dupStock ON dupStock.[ProductId] = m.DupId
                    AND dupStock.[WarehouseId] = keepStock.[WarehouseId] AND dupStock.[IsDeleted] = 0
                WHERE keepStock.[IsDeleted] = 0;

                ;WITH ranked AS (
                    SELECT p.[Id],
                           COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name])))) AS KeyName,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name]))))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, p.[Id]
                           ) AS rn
                    FROM [dbo].[Products] p
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = p.[BranchId]
                    WHERE p.[IsDeleted] = 0
                ),
                map AS (
                    SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                    FROM ranked keep
                    INNER JOIN ranked dup ON keep.KeyName = dup.KeyName AND keep.rn = 1 AND dup.rn > 1
                )
                UPDATE ws SET ws.[IsDeleted] = 1, ws.[DeletedAt] = SYSUTCDATETIME(), ws.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[WarehouseStocks] ws
                INNER JOIN map m ON ws.[ProductId] = m.DupId;

                ;WITH ranked AS (
                    SELECT p.[Id],
                           COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name])))) AS KeyName,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name]))))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, p.[Id]
                           ) AS rn
                    FROM [dbo].[Products] p
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = p.[BranchId]
                    WHERE p.[IsDeleted] = 0
                )
                SELECT keep.[Id] AS KeepId, dup.[Id] AS DupId
                INTO #ProductMergeMap
                FROM ranked keep
                INNER JOIN ranked dup ON keep.KeyName = dup.KeyName AND keep.rn = 1 AND dup.rn > 1;

                UPDATE ii SET ii.[ProductId] = m.KeepId
                FROM [dbo].[InvoiceItems] ii
                INNER JOIN #ProductMergeMap m ON ii.[ProductId] = m.DupId;

                UPDATE wti SET wti.[ProductId] = m.KeepId
                FROM [dbo].[WarehouseTransferItems] wti
                INNER JOIN #ProductMergeMap m ON wti.[ProductId] = m.DupId;

                UPDATE pp SET pp.[ProductId] = m.KeepId
                FROM [dbo].[ProductPrices] pp
                INNER JOIN #ProductMergeMap m ON pp.[ProductId] = m.DupId
                WHERE NOT EXISTS (
                    SELECT 1 FROM [dbo].[ProductPrices] x
                    WHERE x.[ProductId] = m.KeepId AND x.[PricingTypeId] = pp.[PricingTypeId] AND x.[IsDeleted] = 0
                );

                UPDATE pp SET pp.[IsDeleted] = 1, pp.[DeletedAt] = SYSUTCDATETIME(), pp.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[ProductPrices] pp
                INNER JOIN #ProductMergeMap m ON pp.[ProductId] = m.DupId;

                UPDATE pu SET pu.[ProductId] = m.KeepId
                FROM [dbo].[ProductUnits] pu
                INNER JOIN #ProductMergeMap m ON pu.[ProductId] = m.DupId;

                UPDATE pc SET pc.[ProductId] = m.KeepId
                FROM [dbo].[ProductColors] pc
                INNER JOIN #ProductMergeMap m ON pc.[ProductId] = m.DupId;

                UPDATE ps SET ps.[ProductId] = m.KeepId
                FROM [dbo].[ProductSizes] ps
                INNER JOIN #ProductMergeMap m ON ps.[ProductId] = m.DupId;

                UPDATE pb SET pb.[ProductId] = m.KeepId
                FROM [dbo].[ProductBatches] pb
                INNER JOIN #ProductMergeMap m ON pb.[ProductId] = m.DupId;

                UPDATE pss SET pss.[ProductId] = m.KeepId
                FROM [dbo].[ProductSizeStocks] pss
                INNER JOIN #ProductMergeMap m ON pss.[ProductId] = m.DupId;

                UPDATE po SET po.[TriggerProductId] = m.KeepId
                FROM [dbo].[ProductOffers] po
                INNER JOIN #ProductMergeMap m ON po.[TriggerProductId] = m.DupId;

                UPDATE po SET po.[GiftProductId] = m.KeepId
                FROM [dbo].[ProductOffers] po
                INNER JOIN #ProductMergeMap m ON po.[GiftProductId] = m.DupId;

                DROP TABLE #ProductMergeMap;

                ;WITH ranked AS (
                    SELECT p.[Id],
                           COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name])))) AS KeyName,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(NULLIF(LOWER(LTRIM(RTRIM(p.[Barcode]))), N''), N'#' + LOWER(LTRIM(RTRIM(p.[Name]))))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, p.[Id]
                           ) AS rn
                    FROM [dbo].[Products] p
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = p.[BranchId]
                    WHERE p.[IsDeleted] = 0
                )
                UPDATE p SET p.[IsDeleted] = 1, p.[DeletedAt] = SYSUTCDATETIME(), p.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[Products] p
                INNER JOIN ranked r ON p.[Id] = r.[Id]
                WHERE r.rn > 1;

                ;WITH ranked AS (
                    SELECT pp.[Id],
                           ROW_NUMBER() OVER (PARTITION BY pp.[ProductId], pp.[PricingTypeId] ORDER BY pp.[Id]) AS rn
                    FROM [dbo].[ProductPrices] pp
                    WHERE pp.[IsDeleted] = 0
                )
                UPDATE pp SET pp.[IsDeleted] = 1, pp.[DeletedAt] = SYSUTCDATETIME(), pp.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[ProductPrices] pp
                INNER JOIN ranked r ON pp.[Id] = r.[Id]
                WHERE r.rn > 1;

                ;WITH ranked AS (
                    SELECT t.[Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY LOWER(LTRIM(RTRIM(t.[Name])))
                               ORDER BY CASE WHEN b.[IsMain] = 1 THEN 0 ELSE 1 END, t.[Id]
                           ) AS rn
                    FROM [dbo].[PackagingTypes] t
                    LEFT JOIN [dbo].[Branches] b ON b.[Id] = t.[BranchId]
                    WHERE t.[IsDeleted] = 0
                )
                UPDATE t SET t.[IsDeleted] = 1, t.[DeletedAt] = SYSUTCDATETIME(), t.[DeletedBy] = N'SharedCatalogMerge'
                FROM [dbo].[PackagingTypes] t
                INNER JOIN ranked r ON t.[Id] = r.[Id]
                WHERE r.rn > 1;
                """);

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductSizes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductColors");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PricingTypes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PackagingTypes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId_Name",
                table: "Warehouses",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BranchId_Name",
                table: "Warehouses");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductSizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductPrices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductColors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PricingTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PackagingTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Name",
                table: "Warehouses",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_BranchId",
                table: "ProductUnits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizes_BranchId",
                table: "ProductSizes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BranchId",
                table: "Products",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_BranchId",
                table: "ProductPrices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_BranchId",
                table: "ProductOffers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductColors_BranchId",
                table: "ProductColors",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingTypes_BranchId",
                table: "PricingTypes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagingTypes_BranchId",
                table: "PackagingTypes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_BranchId",
                table: "Categories",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Branches_BranchId",
                table: "Categories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackagingTypes_Branches_BranchId",
                table: "PackagingTypes",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PricingTypes_Branches_BranchId",
                table: "PricingTypes",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductColors_Branches_BranchId",
                table: "ProductColors",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOffers_Branches_BranchId",
                table: "ProductOffers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPrices_Branches_BranchId",
                table: "ProductPrices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Branches_BranchId",
                table: "Products",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizes_Branches_BranchId",
                table: "ProductSizes",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductUnits_Branches_BranchId",
                table: "ProductUnits",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
