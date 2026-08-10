using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260810070000_CleanLegacyVibeTags")]
    /// <inheritdoc />
    public partial class CleanLegacyVibeTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Map known legacy vibe tag variants to their canonical taxonomy values.
            migrationBuilder.Sql("""
                UPDATE dishes
                SET vibe_tags = ARRAY(
                    SELECT CASE tag
                        WHEN 'comfort food'    THEN 'ComfortFood'
                        WHEN 'quick'           THEN 'QuickWeeknight'
                        WHEN 'healthy'         THEN 'LightFresh'
                        WHEN 'family friendly' THEN 'FamilyFriendly'
                        ELSE tag
                    END
                    FROM unnest(vibe_tags) AS tag
                    WHERE tag = ANY(ARRAY['ComfortFood', 'QuickWeeknight', 'WeekendTreat', 'LightFresh', 'FamilyFriendly',
                                         'comfort food', 'quick', 'healthy', 'family friendly'])
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM unnest(vibe_tags) AS tag
                    WHERE tag != ALL(ARRAY['ComfortFood', 'QuickWeeknight', 'WeekendTreat', 'LightFresh', 'FamilyFriendly'])
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
