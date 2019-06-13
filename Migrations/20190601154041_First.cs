using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GeoMapDownloader.Migrations
{
    public partial class First : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CacheUrl",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(nullable: true),
                    Data = table.Column<byte[]>(nullable: true),
                    _Headers = table.Column<string>(nullable: true),
                    Action = table.Column<string>(nullable: true),
                    Mime = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CacheUrl", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TilesData",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tile = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilesData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tiles",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<long>(nullable: false),
                    Y = table.Column<long>(nullable: false),
                    Zoom = table.Column<long>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    CacheTime = table.Column<DateTime>(nullable: false),
                    Hash = table.Column<string>(nullable: true),
                    DataId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tiles_TilesData_DataId",
                        column: x => x.DataId,
                        principalTable: "TilesData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_DataId",
                table: "Tiles",
                column: "DataId");

            migrationBuilder.CreateIndex(
                name: "IndexOfHash",
                table: "Tiles",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IndexOfTiles",
                table: "Tiles",
                columns: new[] { "X", "Y", "Zoom", "Type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CacheUrl");

            migrationBuilder.DropTable(
                name: "Tiles");

            migrationBuilder.DropTable(
                name: "TilesData");
        }
    }
}
