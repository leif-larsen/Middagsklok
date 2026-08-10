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
            // Map known legacy vibe tag variants to their canonical taxonomy values and drop
            // anything that cannot be mapped. Matching uses lower(tag) throughout so every casing
            // variant is handled in one pass. DISTINCT prevents duplicates when a row already
            // holds e.g. both 'ComfortFood' and 'comfort food'. The mapping reproduces the manual
            // cleanup recorded in docs/pre-cleanup-snapshot-2026-08-09.json (captured 2026-08-09).
            migrationBuilder.Sql("""
                UPDATE dishes
                SET vibe_tags = ARRAY(
                    SELECT DISTINCT CASE lower(tag)
                        WHEN 'comfortfood'     THEN 'ComfortFood'
                        WHEN 'comfort food'    THEN 'ComfortFood'
                        WHEN 'quickweeknight'  THEN 'QuickWeeknight'
                        WHEN 'quick'           THEN 'QuickWeeknight'
                        WHEN 'easy'            THEN 'QuickWeeknight'
                        WHEN 'weekendtreat'    THEN 'WeekendTreat'
                        WHEN 'lightfresh'      THEN 'LightFresh'
                        WHEN 'familyfriendly'  THEN 'FamilyFriendly'
                        WHEN 'family friendly' THEN 'FamilyFriendly'
                        ELSE tag
                    END
                    FROM unnest(vibe_tags) AS tag
                    WHERE lower(tag) = ANY(ARRAY['comfortfood', 'comfort food',
                                                 'quickweeknight', 'quick', 'easy',
                                                 'weekendtreat',
                                                 'lightfresh',
                                                 'familyfriendly', 'family friendly'])
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM unnest(vibe_tags) AS tag
                    WHERE lower(tag) != ALL(ARRAY['comfortfood', 'quickweeknight', 'weekendtreat', 'lightfresh', 'familyfriendly'])
                );
                """);
        }

        /// <inheritdoc />
        // Down is intentionally empty: the migration drops free-text tags that cannot be
        // recovered without the original source data, so the operation is irreversible by design.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
