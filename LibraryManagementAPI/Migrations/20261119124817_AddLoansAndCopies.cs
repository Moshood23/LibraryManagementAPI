using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLoansAndCopies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableCopies",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 1); // existing books start with one copy, on the shelf

            migrationBuilder.AddColumn<int>(
                name: "TotalCopies",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 1); // existing books start with one copy, on the shelf

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BorrowedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Books",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AvailableCopies", "TotalCopies" },
                values: new object[] { 3, 3 });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Books_Copies",
                table: "Books",
                sql: "[TotalCopies] >= 0 AND [AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BookId_ReturnedAt",
                table: "Loans",
                columns: new[] { "BookId", "ReturnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_DueAt",
                table: "Loans",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_UserId_ReturnedAt",
                table: "Loans",
                columns: new[] { "UserId", "ReturnedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Books_Copies",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "AvailableCopies",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "TotalCopies",
                table: "Books");
        }
    }
}
