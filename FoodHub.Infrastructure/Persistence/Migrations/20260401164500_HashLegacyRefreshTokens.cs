using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HashLegacyRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                UPDATE refresh_tokens
                SET token = UPPER(ENCODE(DIGEST(token, 'sha256'), 'hex'))
                WHERE token IS NOT NULL
                  AND token <> ''
                  AND token !~ '^[0-9A-F]{64}$';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: plaintext refresh tokens cannot be reconstructed from hashes.
        }
    }
}
