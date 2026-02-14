using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteIaApi.Infrastructure.persistence.migrations
{
    /// <inheritdoc />
    public partial class AddTaskClassificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Tasks",
                newName: "CapabilityType");

            migrationBuilder.AlterColumn<string>(
                name: "CapabilityType",
                table: "Tasks",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<string>(
                name: "DomainType",
                table: "Tasks",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "DocumentProcessing");

            migrationBuilder.AddColumn<string>(
                name: "TaskExecutionType",
                table: "Tasks",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Async");

            migrationBuilder.Sql(
                """
                UPDATE "Tasks"
                SET "CapabilityType" = 'ExternalIntegration'
                WHERE "CapabilityType" NOT IN (
                    'LLM_Generation',
                    'LLM_Classification',
                    'LLM_Reasoning',
                    'Vision_OCR',
                    'Vision_ObjectDetection',
                    'Embedding_Search',
                    'RuleEngine',
                    'ExternalIntegration'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DomainType",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TaskExecutionType",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "CapabilityType",
                table: "Tasks",
                newName: "Type");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Tasks",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);
        }
    }
}
