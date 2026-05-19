using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CEA_RPL.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rpl");

            migrationBuilder.CreateTable(
                name: "OtpRecords",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsMobileVerified = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Applicants",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentRelation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Citizenship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermanentAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrespondenceAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AlternateMobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    LastSavedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinanceRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GovIdType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherGovIdType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GovIdNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GovIdPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Categories = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnclosureDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalExperienceSectors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalSubjectExpertise = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasRecognizedLab = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaboratoryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InHouseLabDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applicants_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "rpl",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Awards",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedFrom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ProofPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Awards_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificationTrainings",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObtainedFrom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ProofPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationTrainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificationTrainings_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Declarations",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Place = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignaturePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRetiredGovtEmployee = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Declarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Declarations_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Educations",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Degree = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discipline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Institution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CertificatePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Educations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Educations_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObtainedFrom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProofPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memberships_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtherEnclosures",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherEnclosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherEnclosures_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaperPublishedEntries",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Place = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProofPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaperPublishedEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaperPublishedEntries_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDetails",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UtrNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiptPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDetails_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalExperiences",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NatureOfWork = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferencePhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProofPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalExperiences_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectExperiences",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectExperiences_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareSkills",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    SoftwareName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProficiencyLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareSkills_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadReports",
                schema: "rpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadReports_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "rpl",
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_UserId",
                schema: "rpl",
                table: "Applicants",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Awards_ApplicantId",
                schema: "rpl",
                table: "Awards",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTrainings_ApplicantId",
                schema: "rpl",
                table: "CertificationTrainings",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_ApplicantId",
                schema: "rpl",
                table: "Declarations",
                column: "ApplicantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Educations_ApplicantId",
                schema: "rpl",
                table: "Educations",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ApplicantId",
                schema: "rpl",
                table: "Memberships",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_OtherEnclosures_ApplicantId",
                schema: "rpl",
                table: "OtherEnclosures",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperPublishedEntries_ApplicantId",
                schema: "rpl",
                table: "PaperPublishedEntries",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_ApplicantId",
                schema: "rpl",
                table: "PaymentDetails",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalExperiences_ApplicantId",
                schema: "rpl",
                table: "ProfessionalExperiences",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExperiences_ApplicantId",
                schema: "rpl",
                table: "ProjectExperiences",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareSkills_ApplicantId",
                schema: "rpl",
                table: "SoftwareSkills",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadReports_ApplicantId",
                schema: "rpl",
                table: "UploadReports",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "rpl",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Mobile",
                schema: "rpl",
                table: "Users",
                column: "Mobile",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Awards",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "CertificationTrainings",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "Declarations",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "Educations",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "Memberships",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "OtherEnclosures",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "OtpRecords",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "PaperPublishedEntries",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "PaymentDetails",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "ProfessionalExperiences",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "ProjectExperiences",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "SoftwareSkills",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "UploadReports",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "Applicants",
                schema: "rpl");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "rpl");
        }
    }
}
