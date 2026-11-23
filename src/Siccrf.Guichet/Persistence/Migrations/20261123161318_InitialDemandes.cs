using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siccrf.Guichet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDemandes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Demandes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrouillonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChampsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Demandeur = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Bureau = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValideurPrevu = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateConfirmationUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateSoumissionUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModeConfirmation = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Statut = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DateValidationUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidePar = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demandes", x => x.Id);
                    table.CheckConstraint("CK_Demandes_Confirmation", "ModeConfirmation IN ('Bouton', 'Chat')");
                    table.CheckConstraint("CK_Demandes_Statut", "Statut IN ('Soumise', 'Validée')");
                    table.CheckConstraint("CK_Demandes_Validation", "(Statut = 'Soumise' AND DateValidationUtc IS NULL AND ValidePar IS NULL) OR (Statut = 'Validée' AND DateValidationUtc IS NOT NULL AND ValidePar IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Demandes_BrouillonId",
                table: "Demandes",
                column: "BrouillonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demandes_DateSoumissionUtc",
                table: "Demandes",
                column: "DateSoumissionUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Demandes");
        }
    }
}
