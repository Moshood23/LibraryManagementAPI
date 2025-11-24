using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class CommitCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Book",
                keyColumn: "Id",
                keyValue: new Guid("0095d286-088f-48a8-8bcc-f9d3ab1ec334"));

            migrationBuilder.UpdateData(
                table: "Author",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "CreatedOn", "UpdatedOn" },
                values: new object[] { new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1506), new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1501), new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1501) });

            migrationBuilder.InsertData(
                table: "Book",
                columns: new[] { "Id", "AuthorId", "CreatedAt", "CreatedOn", "DeletedAt", "Description", "GenreId", "ISBN", "IsDeleted", "PublicationYear", "Title", "UpdatedOn" },
                values: new object[] { new Guid("8ea4095c-1a55-49e0-9501-0426e96166f9"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1521), new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1518), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Description", new Guid("11111111-1111-1111-1111-111111111111"), "9780451524935", false, 2025, "1984", new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1518) });

            migrationBuilder.UpdateData(
                table: "Genre",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "CreatedOn", "UpdatedOn" },
                values: new object[] { new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1400), new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1395), new DateTime(2025, 11, 24, 14, 59, 9, 583, DateTimeKind.Utc).AddTicks(1397) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Book",
                keyColumn: "Id",
                keyValue: new Guid("8ea4095c-1a55-49e0-9501-0426e96166f9"));

            migrationBuilder.UpdateData(
                table: "Author",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "CreatedOn", "UpdatedOn" },
                values: new object[] { new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4532), new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4527), new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4527) });

            migrationBuilder.InsertData(
                table: "Book",
                columns: new[] { "Id", "AuthorId", "CreatedAt", "CreatedOn", "DeletedAt", "Description", "GenreId", "ISBN", "IsDeleted", "PublicationYear", "Title", "UpdatedOn" },
                values: new object[] { new Guid("0095d286-088f-48a8-8bcc-f9d3ab1ec334"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4548), new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4544), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Description", new Guid("11111111-1111-1111-1111-111111111111"), "9780451524935", false, 2025, "1984", new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4544) });

            migrationBuilder.UpdateData(
                table: "Genre",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "CreatedOn", "UpdatedOn" },
                values: new object[] { new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4425), new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4420), new DateTime(2025, 11, 24, 12, 14, 11, 341, DateTimeKind.Utc).AddTicks(4422) });
        }
    }
}
