using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedLifeUtilityMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // L2: 水電瓦斯 under LIFE_ROOT (Id=20)
            migrationBuilder.Sql(@"
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (20, 2, 'LIFE_UTILITY', N'水電瓦斯', NULL, 2, 1, GETUTCDATE());

                DECLARE @utilityId BIGINT = SCOPE_IDENTITY();

                -- L3: 每期電費紀錄
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (@utilityId, 3, 'LIFE_UTILITY_ELEC_PERIOD', N'每期電費紀錄', '/life/utility/electricity/period-records', 1, 1, GETUTCDATE());

                -- L3: 電費儀表板
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (@utilityId, 3, 'LIFE_UTILITY_ELEC_DASHBOARD', N'電費儀表板', '/life/utility/electricity/dashboard', 2, 1, GETUTCDATE());

                -- L3: 每期水費紀錄 (預留)
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (@utilityId, 3, 'LIFE_UTILITY_WATER_PERIOD', N'每期水費紀錄', '/life/utility/water/period-records', 3, 1, GETUTCDATE());

                -- L3: 水費儀表板 (預留)
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (@utilityId, 3, 'LIFE_UTILITY_WATER_DASH', N'水費儀表板', '/life/utility/water/dashboard', 4, 1, GETUTCDATE());

                -- L3: 每期瓦斯費紀錄 (預留)
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (@utilityId, 3, 'LIFE_UTILITY_GAS_PERIOD', N'每期瓦斯費紀錄', '/life/utility/gas/period-records', 5, 1, GETUTCDATE());

                -- L3: 瓦斯費儀表板 (預留)
                INSERT INTO MenuFunction (ParentId, [Level], FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, CreateTime)
                VALUES (@utilityId, 3, 'LIFE_UTILITY_GAS_DASH', N'瓦斯費儀表板', '/life/utility/gas/dashboard', 6, 1, GETUTCDATE());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM MenuFunction WHERE FuncCode IN (
                    'LIFE_UTILITY_GAS_DASH', 'LIFE_UTILITY_GAS_PERIOD',
                    'LIFE_UTILITY_WATER_DASH', 'LIFE_UTILITY_WATER_PERIOD',
                    'LIFE_UTILITY_ELEC_DASHBOARD', 'LIFE_UTILITY_ELEC_PERIOD',
                    'LIFE_UTILITY'
                );
            ");
        }
    }
}
