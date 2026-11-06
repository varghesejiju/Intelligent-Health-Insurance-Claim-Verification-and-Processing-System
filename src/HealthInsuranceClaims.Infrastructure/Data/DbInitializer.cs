using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure Database is applied
        await context.Database.MigrateAsync();

        // 1. Seed Roles
        string[] roleNames = { "Administrator", "InsuranceOfficer", "Customer", "HospitalDesk" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Admin User
        var adminEmail = "admin@insurance.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                PhoneNumber = "+91 9876543210",
                Address = "Insurance HQ, Connaught Place, New Delhi",
                AccountStatus = "Active",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }

        // 3. Seed Officers
        var officer1Email = "officer1@insurance.com";
        var officer1 = await userManager.FindByEmailAsync(officer1Email);
        if (officer1 == null)
        {
            officer1 = new ApplicationUser
            {
                UserName = officer1Email,
                Email = officer1Email,
                FullName = "Rajesh Kumar (Senior Claims Officer)",
                PhoneNumber = "+91 9812345678",
                Address = "Regional Claims Center, Sector 62, Noida",
                AccountStatus = "Active",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(officer1, "Officer@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(officer1, "InsuranceOfficer");
            }
        }

        var officer2Email = "officer2@insurance.com";
        var officer2 = await userManager.FindByEmailAsync(officer2Email);
        if (officer2 == null)
        {
            officer2 = new ApplicationUser
            {
                UserName = officer2Email,
                Email = officer2Email,
                FullName = "Priya Sharma (Claims Verification Officer)",
                PhoneNumber = "+91 9823456789",
                Address = "Regional Claims Center, Bandra Kurla Complex, Mumbai",
                AccountStatus = "Active",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(officer2, "Officer@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(officer2, "InsuranceOfficer");
            }
        }

        // 4. Seed Customers
        var customer1Email = "customer1@gmail.com";
        var customer1 = await userManager.FindByEmailAsync(customer1Email);
        if (customer1 == null)
        {
            customer1 = new ApplicationUser
            {
                UserName = customer1Email,
                Email = customer1Email,
                FullName = "Amit Verma",
                PhoneNumber = "+91 9988776655",
                Address = "Flat 402, Green Valley Apartments, Indirapuram, Ghaziabad",
                DateOfBirth = new DateTime(1992, 5, 14),
                Gender = "Male",
                AccountStatus = "Active",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(customer1, "Customer@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer1, "Customer");
            }
        }

        var customer2Email = "customer2@gmail.com";
        var customer2 = await userManager.FindByEmailAsync(customer2Email);
        if (customer2 == null)
        {
            customer2 = new ApplicationUser
            {
                UserName = customer2Email,
                Email = customer2Email,
                FullName = "Sneha Patel",
                PhoneNumber = "+91 9876512345",
                Address = "B-12, Orchid Enclave, Koramangala, Bengaluru",
                DateOfBirth = new DateTime(1995, 8, 22),
                Gender = "Female",
                AccountStatus = "Active",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(customer2, "Customer@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer2, "Customer");
            }
        }

        // 5. Seed Hospitals
        if (!await context.Hospitals.AnyAsync())
        {
            var hospitals = new List<Hospital>
            {
                new()
                {
                    HospitalName = "Apollo Multispecialty Hospital",
                    Address = "Sarita Vihar, Delhi Mathura Road, New Delhi",
                    ContactNumber = "+91 11 2692 5858",
                    Email = "info@apollohospital.com",
                    IsActive = true
                },
                new()
                {
                    HospitalName = "Fortis Memorial Research Institute",
                    Address = "Sector 44, Opposite HUDA City Centre, Gurugram, Haryana",
                    ContactNumber = "+91 124 496 2200",
                    Email = "claims@fmri.in",
                    IsActive = true
                },
                new()
                {
                    HospitalName = "Max Super Speciality Hospital",
                    Address = "1, 2, Press Enclave Road, Saket, New Delhi",
                    ContactNumber = "+91 11 2651 5050",
                    Email = "desk@maxhealthcare.com",
                    IsActive = true
                },
                new()
                {
                    HospitalName = "Manipal Hospital",
                    Address = "98, HAL Old Airport Rd, Kodihalli, Bengaluru",
                    ContactNumber = "+91 80 2502 4444",
                    Email = "info@manipalhospitals.com",
                    IsActive = true
                }
            };

            context.Hospitals.AddRange(hospitals);
            await context.SaveChangesAsync();
        }

        // 5b. Seed Hospital Desk Staff User
        var apolloHospital = await context.Hospitals.FirstOrDefaultAsync(h => h.HospitalName.Contains("Apollo"));
        var hospitalDeskEmail = "apollo.desk@apollohospital.com";
        var hospitalDeskUser = await userManager.FindByEmailAsync(hospitalDeskEmail);
        if (hospitalDeskUser == null)
        {
            hospitalDeskUser = new ApplicationUser
            {
                UserName = hospitalDeskEmail,
                Email = hospitalDeskEmail,
                FullName = "Apollo Hospital TPA Desk",
                PhoneNumber = "+91 11 2692 5859",
                Address = "Apollo Hospital TPA & Insurance Desk, Sarita Vihar, New Delhi",
                AccountStatus = "Active",
                EmailConfirmed = true,
                HospitalId = apolloHospital?.HospitalId,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(hospitalDeskUser, "Hospital@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hospitalDeskUser, "HospitalDesk");
            }
        }

        // 6. Seed Policies with Coverages and Exclusions
        if (!await context.InsurancePolicies.AnyAsync())
        {
            var policy1 = new InsurancePolicy
            {
                PolicyNumber = "POL-IND-2026-001",
                PolicyName = "Comprehensive Health Shield (Individual)",
                PolicyDescription = "Standard individual medical policy with comprehensive in-patient hospitalization coverage.",
                PolicyType = PolicyType.Individual,
                StartDate = DateTime.UtcNow.AddMonths(-6),
                EndDate = DateTime.UtcNow.AddMonths(18),
                OverallCoverageLimit = 500000m,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            policy1.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.HospitalRoomCharges,
                CoverageDescription = "Standard AC room rent capped per policy terms",
                MaxCoverageAmount = 8000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy1.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.SurgeryCharges,
                CoverageDescription = "Operation theatre, surgeon and anesthesia fee",
                MaxCoverageAmount = 150000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy1.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.DoctorConsultation,
                CoverageDescription = "Specialist and physician visits during hospitalization",
                MaxCoverageAmount = 5000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy1.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.MedicineExpenses,
                CoverageDescription = "Pharmacy and medicine supplies during in-patient care",
                MaxCoverageAmount = 25000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy1.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.LaboratoryTests,
                CoverageDescription = "Pathology, blood and urine investigations",
                MaxCoverageAmount = 15000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy1.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.DiagnosticScans,
                CoverageDescription = "CT Scan, MRI, Ultrasound and X-Ray charges",
                MaxCoverageAmount = 20000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy1.Exclusions.Add(new PolicyExclusion
            {
                ExcludedCategory = ExpenseCategory.OtherMedicalExpenses,
                ExclusionDescription = "Cosmetics, toiletries, non-medical comfort expenses and luxury charges are excluded under policy terms.",
                IsActive = true
            });

            var policy2 = new InsurancePolicy
            {
                PolicyNumber = "POL-FAM-2026-002",
                PolicyName = "Family Floater Plus",
                PolicyDescription = "High-coverage family medical plan providing cashless and reimbursement benefits.",
                PolicyType = PolicyType.FamilyFloater,
                StartDate = DateTime.UtcNow.AddMonths(-4),
                EndDate = DateTime.UtcNow.AddMonths(20),
                OverallCoverageLimit = 1000000m,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            policy2.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.HospitalRoomCharges,
                CoverageDescription = "Deluxe room rent coverage",
                MaxCoverageAmount = 12000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy2.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.SurgeryCharges,
                CoverageDescription = "Major and minor surgery procedures",
                MaxCoverageAmount = 300000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy2.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.DoctorConsultation,
                CoverageDescription = "Doctor and attending physician consultations",
                MaxCoverageAmount = 10000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy2.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.MedicineExpenses,
                CoverageDescription = "In-patient pharmacy and medicines",
                MaxCoverageAmount = 50000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy2.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.LaboratoryTests,
                CoverageDescription = "Clinical laboratory diagnostics",
                MaxCoverageAmount = 30000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy2.Coverages.Add(new PolicyCoverage
            {
                ExpenseCategory = ExpenseCategory.DiagnosticScans,
                CoverageDescription = "Radiology and diagnostic scanning investigations",
                MaxCoverageAmount = 40000m,
                CoverageLimitType = CoverageLimitType.CategoryCap,
                IsActive = true
            });

            policy2.Exclusions.Add(new PolicyExclusion
            {
                ExcludedCategory = ExpenseCategory.OtherMedicalExpenses,
                ExclusionDescription = "Dietary supplements, comfort kits and cosmetic procedures are excluded.",
                IsActive = true
            });

            context.InsurancePolicies.AddRange(policy1, policy2);
            await context.SaveChangesAsync();

            // 7. Assign Policies to Seed Customers
            if (customer1 != null)
            {
                context.CustomerPolicies.Add(new CustomerPolicy
                {
                    CustomerId = customer1.Id,
                    PolicyId = policy1.PolicyId,
                    AssignedDate = DateTime.UtcNow.AddMonths(-3),
                    ExpiryDate = policy1.EndDate,
                    IsActive = true
                });
            }

            if (customer2 != null)
            {
                context.CustomerPolicies.Add(new CustomerPolicy
                {
                    CustomerId = customer2.Id,
                    PolicyId = policy2.PolicyId,
                    AssignedDate = DateTime.UtcNow.AddMonths(-2),
                    ExpiryDate = policy2.EndDate,
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
        }

        // 8. Seed Comprehensive Realistic Demonstration Claims (Approved, Partially Approved, Rejected, Under Review, Information Requested, Draft)
        if (!await context.Claims.AnyAsync(c => c.ClaimReferenceNumber == "CLM-2026-0001"))
        {
            var apollo = await context.Hospitals.FirstOrDefaultAsync(h => h.HospitalName.Contains("Apollo"));
            var fortis = await context.Hospitals.FirstOrDefaultAsync(h => h.HospitalName.Contains("Fortis"));
            var max = await context.Hospitals.FirstOrDefaultAsync(h => h.HospitalName.Contains("Max"));
            var pol1 = await context.InsurancePolicies.FirstOrDefaultAsync(p => p.PolicyNumber == "POL-IND-2026-001");
            var pol2 = await context.InsurancePolicies.FirstOrDefaultAsync(p => p.PolicyNumber == "POL-FAM-2026-002");
            var cust1 = await userManager.FindByEmailAsync("customer1@gmail.com");
            var cust2 = await userManager.FindByEmailAsync("customer2@gmail.com");
            var off1 = await userManager.FindByEmailAsync("officer1@insurance.com");
            var off2 = await userManager.FindByEmailAsync("officer2@insurance.com");
            var deskUser = await userManager.FindByEmailAsync("apollo.desk@apollohospital.com");

            if (pol1 != null && pol2 != null && cust1 != null && cust2 != null && off1 != null && off2 != null && apollo != null)
            {
                // Ensure sample upload file exists on disk
                var secureUploadPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HealthInsuranceClaims.Web", "App_Data", "SecureUploads");
                if (!Directory.Exists(secureUploadPath))
                {
                    secureUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "SecureUploads");
                    Directory.CreateDirectory(secureUploadPath);
                }
                var samplePdfGuid = "sample-medical-bill-" + Guid.NewGuid().ToString("N") + ".pdf";
                var samplePdfFullPath = Path.Combine(secureUploadPath, samplePdfGuid);
                if (!File.Exists(samplePdfFullPath))
                {
                    var minimalPdfBytes = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj 2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj 3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R/Resources<<>>>>endobj\nxref\n0 4\n0000000000 65535 f\n0000000009 00000 n\n0000000052 00000 n\n0000000101 00000 n\ntrailer<</Size 4/Root 1 0 R>>\nstartxref\n178\n%%EOF");
                    await File.WriteAllBytesAsync(samplePdfFullPath, minimalPdfBytes);
                }

                // -------------------------------------------------------------
                // CLAIM 1: Fully Approved Claim (Apollo Hospital -> Customer 1)
                // -------------------------------------------------------------
                var claim1 = new Claim
                {
                    ClaimReferenceNumber = "CLM-2026-0001",
                    CustomerId = cust1.Id,
                    PolicyId = pol1.PolicyId,
                    HospitalId = apollo.HospitalId,
                    PatientName = "Amit Verma",
                    AdmissionDate = DateTime.UtcNow.AddDays(-20),
                    DischargeDate = DateTime.UtcNow.AddDays(-16),
                    ClaimType = "Cashless",
                    ClaimDescription = "Acute appendicitis with emergency laparoscopic appendectomy and in-patient recovery.",
                    TotalClaimedAmount = 101500m,
                    ClaimStatus = ClaimStatus.Approved,
                    SubmittedDate = DateTime.UtcNow.AddDays(-15),
                    CreatedDate = DateTime.UtcNow.AddDays(-16)
                };

                var c1e1 = new ClaimExpense { ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ExpenseDescription = "Twin Sharing AC Room (4 days @ ₹1,750/day)", ExpenseDate = DateTime.UtcNow.AddDays(-19), ClaimedAmount = 7000m };
                var c1e2 = new ClaimExpense { ExpenseCategory = ExpenseCategory.SurgeryCharges, ExpenseDescription = "Laparoscopic Appendectomy & OT charges", ExpenseDate = DateTime.UtcNow.AddDays(-19), ClaimedAmount = 72000m };
                var c1e3 = new ClaimExpense { ExpenseCategory = ExpenseCategory.DoctorConsultation, ExpenseDescription = "General Surgeon & Anesthesiologist Consultations", ExpenseDate = DateTime.UtcNow.AddDays(-18), ClaimedAmount = 4500m };
                var c1e4 = new ClaimExpense { ExpenseCategory = ExpenseCategory.MedicineExpenses, ExpenseDescription = "In-patient IV antibiotics, analgesics and fluids", ExpenseDate = DateTime.UtcNow.AddDays(-17), ClaimedAmount = 12000m };
                var c1e5 = new ClaimExpense { ExpenseCategory = ExpenseCategory.LaboratoryTests, ExpenseDescription = "CBC, Blood sugar, Urine analysis & Histopathology", ExpenseDate = DateTime.UtcNow.AddDays(-19), ClaimedAmount = 6000m };

                claim1.Expenses.Add(c1e1);
                claim1.Expenses.Add(c1e2);
                claim1.Expenses.Add(c1e3);
                claim1.Expenses.Add(c1e4);
                claim1.Expenses.Add(c1e5);

                var run1 = new VerificationRun
                {
                    RunDate = DateTime.UtcNow.AddDays(-15),
                    TotalClaimed = 101500m,
                    TotalEligible = 101500m,
                    TotalNonEligible = 0m,
                    HasExceptions = false,
                    SummaryNotes = "All 5 claimed items comply with policy coverage limits. No exclusions detected."
                };
                run1.Results.Add(new VerificationResult { Expense = c1e1, ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ClaimedAmount = 7000m, EligibleAmount = 7000m, NonEligibleAmount = 0m, AppliedLimit = 8000m, Reason = "Within configured limit of ₹8,000.00" });
                run1.Results.Add(new VerificationResult { Expense = c1e2, ExpenseCategory = ExpenseCategory.SurgeryCharges, ClaimedAmount = 72000m, EligibleAmount = 72000m, NonEligibleAmount = 0m, AppliedLimit = 150000m, Reason = "Within configured limit of ₹1,50,000.00" });
                run1.Results.Add(new VerificationResult { Expense = c1e3, ExpenseCategory = ExpenseCategory.DoctorConsultation, ClaimedAmount = 4500m, EligibleAmount = 4500m, NonEligibleAmount = 0m, AppliedLimit = 5000m, Reason = "Within configured limit of ₹5,000.00" });
                run1.Results.Add(new VerificationResult { Expense = c1e4, ExpenseCategory = ExpenseCategory.MedicineExpenses, ClaimedAmount = 12000m, EligibleAmount = 12000m, NonEligibleAmount = 0m, AppliedLimit = 25000m, Reason = "Within configured limit of ₹25,000.00" });
                run1.Results.Add(new VerificationResult { Expense = c1e5, ExpenseCategory = ExpenseCategory.LaboratoryTests, ClaimedAmount = 6000m, EligibleAmount = 6000m, NonEligibleAmount = 0m, AppliedLimit = 15000m, Reason = "Within configured limit of ₹15,000.00" });
                claim1.VerificationRuns.Add(run1);

                claim1.Decision = new ClaimDecision
                {
                    OfficerId = off1.Id,
                    Decision = DecisionType.Approved,
                    ApprovedAmount = 101500m,
                    DecisionReason = "All medical bills, discharge summary, and operative notes verified. Incurred expenses are within policy limits. Full claim approved.",
                    DecisionDate = DateTime.UtcNow.AddDays(-13)
                };

                claim1.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Draft, ChangedBy = deskUser?.Id ?? cust1.Id, ChangedByName = "Apollo TPA Desk", ChangedDate = DateTime.UtcNow.AddDays(-16), Comment = "Patient claim initialized by hospital desk." });
                claim1.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Submitted, ChangedBy = deskUser?.Id ?? cust1.Id, ChangedByName = "Apollo TPA Desk", ChangedDate = DateTime.UtcNow.AddDays(-15), Comment = "Quotation items and hospital bills submitted." });
                claim1.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Submitted, NewStatus = ClaimStatus.UnderReview, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddDays(-14), Comment = "Review initiated by Senior Claims Officer." });
                claim1.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.UnderReview, NewStatus = ClaimStatus.Approved, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddDays(-13), Comment = "Claim fully approved for ₹1,01,500.00." });

                claim1.Documents.Add(new ClaimDocument { DocumentType = DocumentType.HospitalBill, OriginalFileName = "Apollo_Final_Tax_Invoice_CLM001.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 245000, UploadedBy = deskUser?.Id ?? cust1.Id, UploadedDate = DateTime.UtcNow.AddDays(-15) });
                claim1.Documents.Add(new ClaimDocument { DocumentType = DocumentType.DischargeSummary, OriginalFileName = "Apollo_Discharge_Summary_CLM001.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 185000, UploadedBy = deskUser?.Id ?? cust1.Id, UploadedDate = DateTime.UtcNow.AddDays(-15) });


                // -------------------------------------------------------------
                // CLAIM 2: Partially Approved Claim (Room Rent Cap & Exclusion Deduction)
                // -------------------------------------------------------------
                var claim2 = new Claim
                {
                    ClaimReferenceNumber = "CLM-2026-0002",
                    CustomerId = cust2.Id,
                    PolicyId = pol2.PolicyId,
                    HospitalId = fortis?.HospitalId ?? apollo.HospitalId,
                    PatientName = "Sneha Patel",
                    AdmissionDate = DateTime.UtcNow.AddDays(-12),
                    DischargeDate = DateTime.UtcNow.AddDays(-8),
                    ClaimType = "Cashless",
                    ClaimDescription = "Elective laparoscopic cholecystectomy for symptomatic gallstones.",
                    TotalClaimedAmount = 187000m,
                    ClaimStatus = ClaimStatus.PartiallyApproved,
                    SubmittedDate = DateTime.UtcNow.AddDays(-7),
                    CreatedDate = DateTime.UtcNow.AddDays(-8)
                };

                var c2e1 = new ClaimExpense { ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ExpenseDescription = "Super Deluxe Suite (4 days @ ₹4,500/day)", ExpenseDate = DateTime.UtcNow.AddDays(-11), ClaimedAmount = 18000m };
                var c2e2 = new ClaimExpense { ExpenseCategory = ExpenseCategory.SurgeryCharges, ExpenseDescription = "Laparoscopic Cholecystectomy Surgical Procedure & OT", ExpenseDate = DateTime.UtcNow.AddDays(-11), ClaimedAmount = 115000m };
                var c2e3 = new ClaimExpense { ExpenseCategory = ExpenseCategory.DoctorConsultation, ExpenseDescription = "Chief Laparoscopic Surgeon Consultations", ExpenseDate = DateTime.UtcNow.AddDays(-10), ClaimedAmount = 9000m };
                var c2e4 = new ClaimExpense { ExpenseCategory = ExpenseCategory.MedicineExpenses, ExpenseDescription = "Hospital pharmacy & surgical consumables", ExpenseDate = DateTime.UtcNow.AddDays(-9), ClaimedAmount = 28000m };
                var c2e5 = new ClaimExpense { ExpenseCategory = ExpenseCategory.DiagnosticScans, ExpenseDescription = "Pre-op Abdominal Ultrasound & MRCP", ExpenseDate = DateTime.UtcNow.AddDays(-12), ClaimedAmount = 12000m };
                var c2e6 = new ClaimExpense { ExpenseCategory = ExpenseCategory.OtherMedicalExpenses, ExpenseDescription = "Patient luxury comfort kit, toiletries & specialized diet", ExpenseDate = DateTime.UtcNow.AddDays(-9), ClaimedAmount = 5000m };

                claim2.Expenses.Add(c2e1);
                claim2.Expenses.Add(c2e2);
                claim2.Expenses.Add(c2e3);
                claim2.Expenses.Add(c2e4);
                claim2.Expenses.Add(c2e5);
                claim2.Expenses.Add(c2e6);

                var run2 = new VerificationRun
                {
                    RunDate = DateTime.UtcNow.AddDays(-7),
                    TotalClaimed = 187000m,
                    TotalEligible = 176000m,
                    TotalNonEligible = 11000m,
                    HasExceptions = true,
                    SummaryNotes = "Room rent exceeded configured policy cap of ₹12,000. Comfort kits & toiletries excluded under policy terms."
                };
                run2.Results.Add(new VerificationResult { Expense = c2e1, ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ClaimedAmount = 18000m, EligibleAmount = 12000m, NonEligibleAmount = 6000m, AppliedLimit = 12000m, Reason = "Exceeded policy room limit of ₹12,000.00. Deduction: ₹6,000.00" });
                run2.Results.Add(new VerificationResult { Expense = c2e2, ExpenseCategory = ExpenseCategory.SurgeryCharges, ClaimedAmount = 115000m, EligibleAmount = 115000m, NonEligibleAmount = 0m, AppliedLimit = 300000m, Reason = "Within configured limit of ₹3,00,000.00" });
                run2.Results.Add(new VerificationResult { Expense = c2e3, ExpenseCategory = ExpenseCategory.DoctorConsultation, ClaimedAmount = 9000m, EligibleAmount = 9000m, NonEligibleAmount = 0m, AppliedLimit = 10000m, Reason = "Within configured limit of ₹10,000.00" });
                run2.Results.Add(new VerificationResult { Expense = c2e4, ExpenseCategory = ExpenseCategory.MedicineExpenses, ClaimedAmount = 28000m, EligibleAmount = 28000m, NonEligibleAmount = 0m, AppliedLimit = 50000m, Reason = "Within configured limit of ₹50,000.00" });
                run2.Results.Add(new VerificationResult { Expense = c2e5, ExpenseCategory = ExpenseCategory.DiagnosticScans, ClaimedAmount = 12000m, EligibleAmount = 12000m, NonEligibleAmount = 0m, AppliedLimit = 40000m, Reason = "Within configured limit of ₹40,000.00" });
                run2.Results.Add(new VerificationResult { Expense = c2e6, ExpenseCategory = ExpenseCategory.OtherMedicalExpenses, ClaimedAmount = 5000m, EligibleAmount = 0m, NonEligibleAmount = 5000m, IsExcluded = true, Reason = "Explicitly excluded under policy terms (Comfort kits and luxury toiletries)." });
                run2.Exceptions.Add(new VerificationException { Expense = c2e1, ExceptionType = "RoomRentCapExceeded", Message = "Hospital room charges of ₹18,000 exceeded policy limit of ₹12,000.", RequiresManualReview = true });
                claim2.VerificationRuns.Add(run2);

                claim2.Decision = new ClaimDecision
                {
                    OfficerId = off2.Id,
                    Decision = DecisionType.PartiallyApproved,
                    ApprovedAmount = 176000m,
                    DecisionReason = "Room charges capped at policy limit of ₹12,000 (deduction ₹6,000). Non-medical comfort kits & luxury charges deducted per exclusion terms (deduction ₹5,000). Net admissible settlement amount ₹1,76,000 approved.",
                    DecisionDate = DateTime.UtcNow.AddDays(-5)
                };

                claim2.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Submitted, ChangedBy = cust2.Id, ChangedByName = cust2.FullName, ChangedDate = DateTime.UtcNow.AddDays(-7), Comment = "Claim submitted for adjudication." });
                claim2.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Submitted, NewStatus = ClaimStatus.UnderReview, ChangedBy = off2.Id, ChangedByName = off2.FullName, ChangedDate = DateTime.UtcNow.AddDays(-6), Comment = "Review initiated by Verification Officer." });
                claim2.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.UnderReview, NewStatus = ClaimStatus.PartiallyApproved, ChangedBy = off2.Id, ChangedByName = off2.FullName, ChangedDate = DateTime.UtcNow.AddDays(-5), Comment = "Partially approved for ₹1,76,000.00 with policy deductions." });

                claim2.Documents.Add(new ClaimDocument { DocumentType = DocumentType.HospitalBill, OriginalFileName = "Fortis_Invoice_CLM002.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 312000, UploadedBy = cust2.Id, UploadedDate = DateTime.UtcNow.AddDays(-7) });


                // -------------------------------------------------------------
                // CLAIM 3: Rejected Claim (Cosmetic Procedure Excluded)
                // -------------------------------------------------------------
                var claim3 = new Claim
                {
                    ClaimReferenceNumber = "CLM-2026-0003",
                    CustomerId = cust1.Id,
                    PolicyId = pol1.PolicyId,
                    HospitalId = max?.HospitalId ?? apollo.HospitalId,
                    PatientName = "Amit Verma",
                    AdmissionDate = DateTime.UtcNow.AddDays(-9),
                    DischargeDate = DateTime.UtcNow.AddDays(-8),
                    ClaimType = "Reimbursement",
                    ClaimDescription = "Elective cosmetic rhinoplasty and aesthetic dermatological contouring.",
                    TotalClaimedAmount = 55000m,
                    ClaimStatus = ClaimStatus.Rejected,
                    SubmittedDate = DateTime.UtcNow.AddDays(-7),
                    CreatedDate = DateTime.UtcNow.AddDays(-8)
                };

                var c3e1 = new ClaimExpense { ExpenseCategory = ExpenseCategory.OtherMedicalExpenses, ExpenseDescription = "Aesthetic Rhinoplasty Cosmetic Surgeon Fee", ExpenseDate = DateTime.UtcNow.AddDays(-8), ClaimedAmount = 45000m };
                var c3e2 = new ClaimExpense { ExpenseCategory = ExpenseCategory.DoctorConsultation, ExpenseDescription = "Aesthetic Dermatology Pre-op Consultation", ExpenseDate = DateTime.UtcNow.AddDays(-9), ClaimedAmount = 10000m };

                claim3.Expenses.Add(c3e1);
                claim3.Expenses.Add(c3e2);

                var run3 = new VerificationRun
                {
                    RunDate = DateTime.UtcNow.AddDays(-7),
                    TotalClaimed = 55000m,
                    TotalEligible = 0m,
                    TotalNonEligible = 55000m,
                    HasExceptions = true,
                    SummaryNotes = "Elective cosmetic rhinoplasty is strictly excluded under policy terms."
                };
                run3.Results.Add(new VerificationResult { Expense = c3e1, ExpenseCategory = ExpenseCategory.OtherMedicalExpenses, ClaimedAmount = 45000m, EligibleAmount = 0m, NonEligibleAmount = 45000m, IsExcluded = true, Reason = "Cosmetic, aesthetic and luxury surgeries are excluded from policy coverage." });
                run3.Results.Add(new VerificationResult { Expense = c3e2, ExpenseCategory = ExpenseCategory.DoctorConsultation, ClaimedAmount = 10000m, EligibleAmount = 0m, NonEligibleAmount = 10000m, IsExcluded = true, Reason = "Consultation for excluded cosmetic treatment is not admissible." });
                run3.Exceptions.Add(new VerificationException { Expense = c3e1, ExceptionType = "CosmeticExclusionDetected", Message = "Claimed treatment involves excluded cosmetic procedures.", RequiresManualReview = true });
                claim3.VerificationRuns.Add(run3);

                claim3.Decision = new ClaimDecision
                {
                    OfficerId = off1.Id,
                    Decision = DecisionType.Rejected,
                    ApprovedAmount = 0m,
                    DecisionReason = "Claim rejected under Section 4.2 of Policy Terms: Elective aesthetic, plastic or cosmetic surgery procedures are explicitly excluded from health insurance coverage.",
                    DecisionDate = DateTime.UtcNow.AddDays(-4)
                };

                claim3.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Submitted, ChangedBy = cust1.Id, ChangedByName = cust1.FullName, ChangedDate = DateTime.UtcNow.AddDays(-7), Comment = "Submitted by patient." });
                claim3.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Submitted, NewStatus = ClaimStatus.UnderReview, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddDays(-5), Comment = "Under officer review." });
                claim3.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.UnderReview, NewStatus = ClaimStatus.Rejected, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddDays(-4), Comment = "Claim rejected: Cosmetic surgery exclusion." });

                claim3.Documents.Add(new ClaimDocument { DocumentType = DocumentType.HospitalBill, OriginalFileName = "Max_Cosmetic_Invoice_CLM003.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 195000, UploadedBy = cust1.Id, UploadedDate = DateTime.UtcNow.AddDays(-7) });


                // -------------------------------------------------------------
                // CLAIM 4: Under Review Claim (Queued for Officer Review)
                // -------------------------------------------------------------
                var claim4 = new Claim
                {
                    ClaimReferenceNumber = "CLM-2026-0004",
                    CustomerId = cust2.Id,
                    PolicyId = pol2.PolicyId,
                    HospitalId = apollo.HospitalId,
                    PatientName = "Sneha Patel",
                    AdmissionDate = DateTime.UtcNow.AddDays(-4),
                    DischargeDate = DateTime.UtcNow.AddDays(-1),
                    ClaimType = "Cashless",
                    ClaimDescription = "Severe Dengue fever with thrombocytopenia requiring platelet transfusion and ICU monitoring.",
                    TotalClaimedAmount = 48000m,
                    ClaimStatus = ClaimStatus.UnderReview,
                    SubmittedDate = DateTime.UtcNow.AddDays(-1),
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                };

                var c4e1 = new ClaimExpense { ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ExpenseDescription = "High Dependency Unit (3 days @ ₹3,500/day)", ExpenseDate = DateTime.UtcNow.AddDays(-3), ClaimedAmount = 10500m };
                var c4e2 = new ClaimExpense { ExpenseCategory = ExpenseCategory.DoctorConsultation, ExpenseDescription = "Internal Medicine & Critical Care Consultations", ExpenseDate = DateTime.UtcNow.AddDays(-2), ClaimedAmount = 7500m };
                var c4e3 = new ClaimExpense { ExpenseCategory = ExpenseCategory.MedicineExpenses, ExpenseDescription = "IV fluids, supportive medications and platelets", ExpenseDate = DateTime.UtcNow.AddDays(-2), ClaimedAmount = 18000m };
                var c4e4 = new ClaimExpense { ExpenseCategory = ExpenseCategory.LaboratoryTests, ExpenseDescription = "Serial Platelet Counts, NS1 Antigen, Dengue Serology", ExpenseDate = DateTime.UtcNow.AddDays(-3), ClaimedAmount = 12000m };

                claim4.Expenses.Add(c4e1);
                claim4.Expenses.Add(c4e2);
                claim4.Expenses.Add(c4e3);
                claim4.Expenses.Add(c4e4);

                var run4 = new VerificationRun
                {
                    RunDate = DateTime.UtcNow.AddDays(-1),
                    TotalClaimed = 48000m,
                    TotalEligible = 48000m,
                    TotalNonEligible = 0m,
                    HasExceptions = false,
                    SummaryNotes = "All 4 medical expenses are within policy limits. No exclusions detected."
                };
                run4.Results.Add(new VerificationResult { Expense = c4e1, ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ClaimedAmount = 10500m, EligibleAmount = 10500m, NonEligibleAmount = 0m, AppliedLimit = 12000m, Reason = "Within configured limit of ₹12,000.00" });
                run4.Results.Add(new VerificationResult { Expense = c4e2, ExpenseCategory = ExpenseCategory.DoctorConsultation, ClaimedAmount = 7500m, EligibleAmount = 7500m, NonEligibleAmount = 0m, AppliedLimit = 10000m, Reason = "Within configured limit of ₹10,000.00" });
                run4.Results.Add(new VerificationResult { Expense = c4e3, ExpenseCategory = ExpenseCategory.MedicineExpenses, ClaimedAmount = 18000m, EligibleAmount = 18000m, NonEligibleAmount = 0m, AppliedLimit = 50000m, Reason = "Within configured limit of ₹50,000.00" });
                run4.Results.Add(new VerificationResult { Expense = c4e4, ExpenseCategory = ExpenseCategory.LaboratoryTests, ClaimedAmount = 12000m, EligibleAmount = 12000m, NonEligibleAmount = 0m, AppliedLimit = 30000m, Reason = "Within configured limit of ₹30,000.00" });
                claim4.VerificationRuns.Add(run4);

                claim4.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Submitted, ChangedBy = deskUser?.Id ?? cust2.Id, ChangedByName = "Apollo TPA Desk", ChangedDate = DateTime.UtcNow.AddDays(-1), Comment = "Submitted by Apollo hospital billing desk." });
                claim4.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Submitted, NewStatus = ClaimStatus.UnderReview, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddHours(-12), Comment = "Claim queued in review queue. Officer examining documents." });

                claim4.Documents.Add(new ClaimDocument { DocumentType = DocumentType.HospitalBill, OriginalFileName = "Apollo_Dengue_Bill_CLM004.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 210000, UploadedBy = deskUser?.Id ?? cust2.Id, UploadedDate = DateTime.UtcNow.AddDays(-1) });
                claim4.Documents.Add(new ClaimDocument { DocumentType = DocumentType.LaboratoryReport, OriginalFileName = "Apollo_Dengue_Platelet_Reports.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 340000, UploadedBy = deskUser?.Id ?? cust2.Id, UploadedDate = DateTime.UtcNow.AddDays(-1) });


                // -------------------------------------------------------------
                // CLAIM 5: Information Requested (Action Required / Query Raised)
                // -------------------------------------------------------------
                var claim5 = new Claim
                {
                    ClaimReferenceNumber = "CLM-2026-0005",
                    CustomerId = cust1.Id,
                    PolicyId = pol1.PolicyId,
                    HospitalId = apollo.HospitalId,
                    PatientName = "Amit Verma",
                    AdmissionDate = DateTime.UtcNow.AddDays(-5),
                    DischargeDate = DateTime.UtcNow.AddDays(-2),
                    ClaimType = "Cashless",
                    ClaimDescription = "Right knee arthroscopic repair for medial meniscus tear.",
                    TotalClaimedAmount = 98000m,
                    ClaimStatus = ClaimStatus.InformationRequested,
                    SubmittedDate = DateTime.UtcNow.AddDays(-2),
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                };

                var c5e1 = new ClaimExpense { ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ExpenseDescription = "Standard AC Room (3 days @ ₹2,000/day)", ExpenseDate = DateTime.UtcNow.AddDays(-4), ClaimedAmount = 6000m };
                var c5e2 = new ClaimExpense { ExpenseCategory = ExpenseCategory.SurgeryCharges, ExpenseDescription = "Knee Arthroscopy & Meniscal Repair Procedure", ExpenseDate = DateTime.UtcNow.AddDays(-4), ClaimedAmount = 75000m };
                var c5e3 = new ClaimExpense { ExpenseCategory = ExpenseCategory.MedicineExpenses, ExpenseDescription = "Antibiotics, post-op pain management and knee brace", ExpenseDate = DateTime.UtcNow.AddDays(-3), ClaimedAmount = 17000m };

                claim5.Expenses.Add(c5e1);
                claim5.Expenses.Add(c5e2);
                claim5.Expenses.Add(c5e3);

                var run5 = new VerificationRun
                {
                    RunDate = DateTime.UtcNow.AddDays(-2),
                    TotalClaimed = 98000m,
                    TotalEligible = 98000m,
                    TotalNonEligible = 0m,
                    HasExceptions = false,
                    SummaryNotes = "Preliminary verification indicates expenses within limits. Awaiting requested surgical implant invoice."
                };
                run5.Results.Add(new VerificationResult { Expense = c5e1, ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ClaimedAmount = 6000m, EligibleAmount = 6000m, NonEligibleAmount = 0m, AppliedLimit = 8000m, Reason = "Within configured limit of ₹8,000.00" });
                run5.Results.Add(new VerificationResult { Expense = c5e2, ExpenseCategory = ExpenseCategory.SurgeryCharges, ClaimedAmount = 75000m, EligibleAmount = 75000m, NonEligibleAmount = 0m, AppliedLimit = 150000m, Reason = "Within configured limit of ₹1,50,000.00" });
                run5.Results.Add(new VerificationResult { Expense = c5e3, ExpenseCategory = ExpenseCategory.MedicineExpenses, ClaimedAmount = 17000m, EligibleAmount = 17000m, NonEligibleAmount = 0m, AppliedLimit = 25000m, Reason = "Within configured limit of ₹25,000.00" });
                claim5.VerificationRuns.Add(run5);

                claim5.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Submitted, ChangedBy = deskUser?.Id ?? cust1.Id, ChangedByName = "Apollo TPA Desk", ChangedDate = DateTime.UtcNow.AddDays(-2), Comment = "Quotation and bills submitted by hospital." });
                claim5.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Submitted, NewStatus = ClaimStatus.UnderReview, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddDays(-1), Comment = "Review started." });
                claim5.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.UnderReview, NewStatus = ClaimStatus.InformationRequested, ChangedBy = off1.Id, ChangedByName = off1.FullName, ChangedDate = DateTime.UtcNow.AddHours(-18), Comment = "Please upload detailed itemized surgical implant invoice and operative surgeon notes before final settlement." });

                claim5.Documents.Add(new ClaimDocument { DocumentType = DocumentType.HospitalBill, OriginalFileName = "Apollo_Surgery_Bill_CLM005.pdf", StoredFileName = samplePdfGuid, FilePath = samplePdfFullPath, ContentType = "application/pdf", FileSize = 275000, UploadedBy = deskUser?.Id ?? cust1.Id, UploadedDate = DateTime.UtcNow.AddDays(-2) });


                // -------------------------------------------------------------
                // CLAIM 6: Draft Claim (Hospital Desk currently adding items)
                // -------------------------------------------------------------
                var claim6 = new Claim
                {
                    ClaimReferenceNumber = "CLM-2026-0006",
                    CustomerId = cust2.Id,
                    PolicyId = pol2.PolicyId,
                    HospitalId = apollo.HospitalId,
                    PatientName = "Sneha Patel",
                    AdmissionDate = DateTime.UtcNow.AddDays(-1),
                    DischargeDate = DateTime.UtcNow,
                    ClaimType = "Cashless",
                    ClaimDescription = "Acute gastroenteritis with severe dehydration and electrolyte replacement.",
                    TotalClaimedAmount = 12500m,
                    ClaimStatus = ClaimStatus.Draft,
                    CreatedDate = DateTime.UtcNow.AddHours(-10)
                };

                claim6.Expenses.Add(new ClaimExpense { ExpenseCategory = ExpenseCategory.HospitalRoomCharges, ExpenseDescription = "Day Care AC Bed (1 day)", ExpenseDate = DateTime.UtcNow.AddDays(-1), ClaimedAmount = 4500m });
                claim6.Expenses.Add(new ClaimExpense { ExpenseCategory = ExpenseCategory.DoctorConsultation, ExpenseDescription = "Emergency Physician & Gastroenterologist Consultation", ExpenseDate = DateTime.UtcNow.AddDays(-1), ClaimedAmount = 3000m });
                claim6.Expenses.Add(new ClaimExpense { ExpenseCategory = ExpenseCategory.MedicineExpenses, ExpenseDescription = "IV fluids, Ondansetron & oral rehydration solutions", ExpenseDate = DateTime.UtcNow, ClaimedAmount = 5000m });

                claim6.StatusHistories.Add(new ClaimStatusHistory { PreviousStatus = ClaimStatus.Draft, NewStatus = ClaimStatus.Draft, ChangedBy = deskUser?.Id ?? cust2.Id, ChangedByName = "Apollo TPA Desk", ChangedDate = DateTime.UtcNow.AddHours(-10), Comment = "Draft claim initialized. Awaiting discharge summary from ward." });

                context.Claims.AddRange(claim1, claim2, claim3, claim4, claim5, claim6);
                await context.SaveChangesAsync();
            }
        }
    }
}
