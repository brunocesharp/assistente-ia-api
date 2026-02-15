using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteIaApi.Infrastructure.persistence.migrations
{
    /// <inheritdoc />
    public partial class PatchTaskClassificationDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Tasks"
                SET "DomainType" = 'DocumentProcessing'
                WHERE "DomainType" IS NULL OR "DomainType" = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Tasks"
                SET "TaskExecutionType" = 'Async'
                WHERE "TaskExecutionType" IS NULL OR "TaskExecutionType" = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Tasks"
                SET "CapabilityType" = 'ExternalIntegration'
                WHERE "CapabilityType" IS NULL OR "CapabilityType" = '';
                """);

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

            migrationBuilder.Sql("""ALTER TABLE "Tasks" ALTER COLUMN "DomainType" SET DEFAULT 'DocumentProcessing';""");
            migrationBuilder.Sql("""ALTER TABLE "Tasks" ALTER COLUMN "TaskExecutionType" SET DEFAULT 'Async';""");
            migrationBuilder.Sql("""ALTER TABLE "Tasks" ALTER COLUMN "CapabilityType" SET DEFAULT 'ExternalIntegration';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "Tasks" ALTER COLUMN "DomainType" DROP DEFAULT;""");
            migrationBuilder.Sql("""ALTER TABLE "Tasks" ALTER COLUMN "TaskExecutionType" DROP DEFAULT;""");
            migrationBuilder.Sql("""ALTER TABLE "Tasks" ALTER COLUMN "CapabilityType" DROP DEFAULT;""");
        }
    }
}
