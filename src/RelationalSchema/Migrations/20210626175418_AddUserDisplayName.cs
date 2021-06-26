using Microsoft.EntityFrameworkCore.Migrations;

namespace RelationalSchema.Migrations
{
    public partial class AddUserDisplayName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "Empty user name");

            migrationBuilder.Sql(
@"
UPDATE Users
SET DisplayName = Name;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Users");
        }
    }
}
