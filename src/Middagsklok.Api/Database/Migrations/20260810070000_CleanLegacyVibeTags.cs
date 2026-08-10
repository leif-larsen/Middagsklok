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
            migrationBuilder.Sql("""
                UPDATE dishes
                SET vibe_tags = ARRAY(
                    SELECT tag
                    FROM unnest(vibe_tags) AS tag
                    WHERE tag = ANY(ARRAY['ComfortFood', 'QuickWeeknight', 'WeekendTreat', 'LightFresh', 'FamilyFriendly'])
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
