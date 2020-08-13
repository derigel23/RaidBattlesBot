using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace RaidBattlesBot.Model
{
    public class SqlServerMigrationsSqlGeneratorWithExec : SqlServerMigrationsSqlGenerator
    {
      public SqlServerMigrationsSqlGeneratorWithExec([NotNull] MigrationsSqlGeneratorDependencies dependencies, [NotNull] IMigrationsAnnotationProvider migrationsAnnotations)
        : base(dependencies, migrationsAnnotations) { }

        protected override void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var subBuilder = new MigrationCommandListBuilder(Dependencies);
            base.Generate(operation, model, subBuilder);
            subBuilder.EndCommand();

            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
            foreach (var command in subBuilder.GetCommandList())
            {
                builder
                    .Append("EXEC(")
                    .Append(stringTypeMapping.GenerateSqlLiteral(command.CommandText.TrimEnd('\n', '\r', ';')))
                    .Append(")")
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                    .EndCommand(command.TransactionSuppressed);
            }
        }
    }
}