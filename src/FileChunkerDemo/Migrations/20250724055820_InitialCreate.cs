using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FileChunkerDemo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_file",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unique_identifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    checksum = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    checksum_algorithm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    number_of_chunks = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delete_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    file_process_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_file", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_stored_file",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delete_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_stored_file", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_file_chunk",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_id = table.Column<int>(type: "integer", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    chunk_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    chunk_size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_process_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    upload_error_reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_file_chunk", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_file_chunk_tbl_file_file_id",
                        column: x => x.file_id,
                        principalTable: "tbl_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_file_unique_identifier",
                table: "tbl_file",
                column: "unique_identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_file_chunk_file_id",
                table: "tbl_file_chunk",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_stored_file_file_name",
                table: "tbl_stored_file",
                column: "file_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_file_chunk");

            migrationBuilder.DropTable(
                name: "tbl_stored_file");

            migrationBuilder.DropTable(
                name: "tbl_file");
        }
    }
}
