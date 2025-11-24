using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Book",
                keyColumn: "Id",
                keyValue: new Guid("8ea4095c-1a55-49e0-9501-0426e96166f9"));

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Author",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "CreatedOn", "UpdatedOn" },
                values: new object[] { new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5775), new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5770), new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5770) });

            migrationBuilder.InsertData(
                table: "Book",
                columns: new[] { "Id", "AuthorId", "CreatedAt", "CreatedOn", "DeletedAt", "Description", "GenreId", "ISBN", "IsDeleted", "PublicationYear", "Title", "UpdatedOn" },
                values: new object[] { new Guid("cba71909-9eaf-4e83-923b-5663f90ad43e"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5791), new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5788), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Description", new Guid("11111111-1111-1111-1111-111111111111"), "9780451524935", false, 2025, "1984", new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5788) });

            migrationBuilder.UpdateData(
                table: "Genre",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "CreatedOn", "UpdatedOn" },
                values: new object[] { new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5659), new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5654), new DateTime(2025, 11, 24, 15, 13, 18, 922, DateTimeKind.Utc).AddTicks(5656) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Book",
                keyColumn: "Id",
                keyValue: new Guid("cba71909-9eaf-4e83-923b-5663f90ad43e"));

            migrationBuilder.DropColumn(
                name: "Password",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

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
    }
}
