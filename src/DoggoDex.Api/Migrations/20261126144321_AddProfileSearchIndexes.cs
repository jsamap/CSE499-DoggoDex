using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoggoDex.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Trigram indexes make the ILIKE '%term%' name search in SearchService indexable
            // instead of a sequential scan. Both blocks are guarded so the migration still
            // applies on a database where the app's role may not create extensions.
            migrationBuilder.Sql("""
                DO $dd$
                BEGIN
                    CREATE EXTENSION IF NOT EXISTS pg_trgm;
                EXCEPTION
                    WHEN insufficient_privilege THEN
                        RAISE NOTICE 'pg_trgm not enabled: profile search will fall back to sequential scans.';
                END
                $dd$;
                """);

            migrationBuilder.Sql("""
                DO $dd$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm') THEN
                        CREATE INDEX IF NOT EXISTS "IX_Owners_Name_Trgm"
                            ON "Owners" USING gin ("Name" gin_trgm_ops);
                        CREATE INDEX IF NOT EXISTS "IX_Dogs_Name_Trgm"
                            ON "Dogs" USING gin ("Name" gin_trgm_ops);
                    END IF;
                END
                $dd$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // pg_trgm is left installed: other indexes or schemas may use it.
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Owners_Name_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Dogs_Name_Trgm";""");
        }
    }
}
