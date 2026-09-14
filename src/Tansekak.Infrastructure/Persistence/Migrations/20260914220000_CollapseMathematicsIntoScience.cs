using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tansekak.Infrastructure.Persistence;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260914220000_CollapseMathematicsIntoScience")]
public class CollapseMathematicsIntoScience : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "AdmissionCutoffs" AS m
            WHERE m."Track" = 2
              AND EXISTS (
                  SELECT 1
                  FROM "AdmissionCutoffs" AS s
                  WHERE s."AdmissionYearId" = m."AdmissionYearId"
                    AND s."UniversityFacultyId" = m."UniversityFacultyId"
                    AND s."Track" = 1
              );

            UPDATE "AdmissionCutoffs"
            SET "Track" = 1
            WHERE "Track" = 2;

            UPDATE "StudentResults"
            SET "Track" = 1
            WHERE "Track" = 2;

            UPDATE "Faculties" AS f
            SET "AllowedTracks" = COALESCE((
                SELECT jsonb_agg(DISTINCT mapped ORDER BY mapped)::text
                FROM (
                    SELECT CASE
                        WHEN elem IN ('Mathematics', '2') THEN 'Science'
                        WHEN elem IN ('Science', '1') THEN 'Science'
                        WHEN elem IN ('Literature', '3') THEN 'Literature'
                        ELSE elem
                    END AS mapped
                    FROM jsonb_array_elements_text(
                        CASE
                            WHEN f."AllowedTracks" IS NULL OR btrim(f."AllowedTracks") = '' THEN '[]'::jsonb
                            ELSE f."AllowedTracks"::jsonb
                        END
                    ) AS elem
                ) mapped_tracks
            ), '[]');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
