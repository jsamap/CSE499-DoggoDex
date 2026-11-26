using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoggoDex.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the column nullable first so existing rows are accepted.
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Owners",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Dogs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            // 2. Backfill every existing profile in SQL. Slug is deliberately NOT copied from
            //    ShareableID: the resolver matches both columns, so no link breaks. The
            //    md5(Id) suffix keeps values distinct even for identically named profiles.
            //    Re-running is a no-op because only empty values are touched.
            migrationBuilder.Sql(BackfillSlugSql("Owners"));
            migrationBuilder.Sql(BackfillSlugSql("Dogs"));

            // 3. Enforce the invariant that the interceptor relies on.
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Owners",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Dogs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            // 4. Unique lookup index, per table (the resolver checks both tables).
            migrationBuilder.CreateIndex(
                name: "IX_Owners_Slug",
                table: "Owners",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_Slug",
                table: "Dogs",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Owners_Slug",
                table: "Owners");

            migrationBuilder.DropIndex(
                name: "IX_Dogs_Slug",
                table: "Dogs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Dogs");
        }

        private const string AccentedLetters = "ÀÁÂÃÄÅàáâãäåÇçÈÉÊËèéêëÌÍÎÏìíîïÑñÒÓÔÕÖØòóôõöøÙÚÛÜùúûüÝýÿ";
        private const string PlainLetters = "AAAAAAaaaaaaCcEEEEeeeeIIIIiiiiNnOOOOOOooooooUUUUuuuuYyy";

        /// <summary>
        /// Mirrors <c>SlugGenerator.Slugify</c> in SQL: lowercase, strip accents, collapse
        /// everything that is not a-z/0-9 into single hyphens, cap the base at 40 characters,
        /// fall back to "profile", then append five characters of md5(Id) for uniqueness.
        /// </summary>
        private static string BackfillSlugSql(string table) => $"""
            UPDATE "{table}"
            SET "Slug" = COALESCE(
                    NULLIF(
                        TRIM(BOTH '-' FROM LEFT(
                            TRIM(BOTH '-' FROM REGEXP_REPLACE(
                                LOWER(TRANSLATE("Name", '{AccentedLetters}', '{PlainLetters}')),
                                '[^a-z0-9]+', '-', 'g')),
                            40)),
                        ''),
                    'profile')
                || '-' || SUBSTRING(MD5("Id"::text), 1, 5)
            WHERE "Slug" IS NULL OR "Slug" = '';
            """;
    }
}
