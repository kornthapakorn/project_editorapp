using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EditProfileApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "radcheck",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, defaultValue: ""),
                    attribute = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, defaultValue: "Cleartext-Password"),
                    op = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false, defaultValue: ":="),
                    value = table.Column<string>(type: "varchar(253)", unicode: false, maxLength: 253, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__radcheck__3213E83F6A0213D5", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    student_id = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    nickname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    emergency_mobile = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_pro__2A33069ABA77FC3B", x => x.student_id);
                });

            migrationBuilder.CreateIndex(
                name: "username_idx",
                table: "radcheck",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "radcheck");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
