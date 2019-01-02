using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace XamarinForms.Reactive.Sample.Mars.Migrations.Migrations
{
    public partial class EpochZero : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoManifests",
                columns: table => new
                {
                    Name = table.Column<string>(maxLength: 256, nullable: false),
                    LandingDate = table.Column<DateTime>(nullable: false),
                    LaunchDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    MaxSol = table.Column<int>(nullable: false),
                    MaxDate = table.Column<DateTime>(nullable: false),
                    TotalPhotos = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoManifests", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "PhotoSets",
                columns: table => new
                {
                    RoverName = table.Column<string>(maxLength: 16, nullable: false),
                    Sol = table.Column<int>(nullable: false),
                    HeadlineImage = table.Column<string>(maxLength: 256, nullable: true),
                    EarthDate = table.Column<DateTime>(nullable: false),
                    TotalPhotos = table.Column<int>(nullable: false),
                    Cameras = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoSets", x => new { x.RoverName, x.Sol });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoManifests");

            migrationBuilder.DropTable(
                name: "PhotoSets");
        }
    }
}
