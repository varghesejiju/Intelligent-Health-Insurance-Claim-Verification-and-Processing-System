# Intelligent Health Insurance Claim Verification and Processing System

**Project Type:** MCA Final-Year Academic Project  
**Technology Stack:** ASP.NET Core 8.0 MVC (C#), Entity Framework Core 8, Microsoft SQL Server (LocalDB), Bootstrap 5  
**Development Environment:** Visual Studio 2022 / .NET 8.0 SDK  

---

## 1. Project Overview

The **Intelligent Health Insurance Claim Verification and Processing System** is a complete, enterprise-grade web application engineered to streamline health insurance claim submission, automated rule-based policy verification, and insurance officer adjudication.

### Core Capabilities:
- **Multi-Role Authorization:** Dedicated interfaces for **Hospital Billing Desk**, **Customers (Patients)**, **Insurance Officers**, and **Administrators** built on ASP.NET Core Identity.
- **Hospital TPA & Billing Desk Portal:** Network hospital desks initiate claims directly against the patient's verified insurance policy ID, itemize the medical quotation, and upload official hospital bills, discharge summaries, prescriptions, and diagnostic reports.
- **Patient Enquiry & Tracking Portal:** Patients view assigned active policies, coverage limits, and expiry dates, enquire live claim status, and view settlement amounts and deduction reasons.
- **Rule-Based Verification Engine:** Pure, deterministic C# `decimal` financial calculation engine comparing claimed line items against policy coverage caps and exclusions.
  - Computes **Total Claimed Amount**, **Indicative Eligible Amount**, and **Non-Eligible Deductions**.
  - Flags **Verification Exceptions** (e.g. unconfigured categories, date mismatches) requiring manual officer review.
  - **Advisory Model:** Verification outputs serve as recommendations; the final decision is strictly rendered by an authorized Insurance Officer.
- **Strict Claim Workflow State Machine:** Enforces valid lifecycle transitions:  
  `Draft` &rarr; `Submitted` &rarr; `UnderReview` &rarr; `InformationRequested` &rarr; `Resubmitted` &rarr; `Approved` / `PartiallyApproved` / `Rejected` &rarr; `Closed`.
- **Secure Medical Document Storage:** Files (PDF, JPG, PNG &le; 10MB) are stored outside `wwwroot` (`App_Data/SecureUploads`) using cryptographically random GUID filenames, served strictly via authenticated streaming endpoints with access validation.
- **Immutable Audit Trail:** Chronological record of user logins, policy modifications, claim submissions, document uploads, and officer adjudication decisions.

---

## 2. Default Demonstration Credentials (Viva Showcase)

The database automatically seeds realistic synthetic accounts on initial launch:

| Role | Email Address | Password | Account Holder / Description |
|---|---|---|---|
| **Administrator** | `admin@insurance.com` | `Admin@123` | System Administrator (Full System Governance) |
| **Hospital Desk** | `apollo.desk@apollohospital.com` | `Hospital@123` | Apollo Hospital TPA Desk (Initiates Claims, Uploads Bills & Reports) |
| **Insurance Officer** | `officer1@insurance.com` | `Officer@123` | Rajesh Kumar (Senior Claims Adjudication Officer) |
| **Insurance Officer** | `officer2@insurance.com` | `Officer@123` | Priya Sharma (Claims Verification Officer) |
| **Customer (Patient)** | `customer1@gmail.com` | `Customer@123` | Amit Verma (Policy: `POL-IND-2026-001`, Limit: ₹5,00,000) |
| **Customer (Patient)** | `customer2@gmail.com` | `Customer@123` | Sneha Patel (Policy: `POL-FAM-2026-002`, Limit: ₹10,00,000) |

> **Tip for Viva:** The Login page includes one-click demo credential filler buttons for rapid evaluation.

---

## 3. Solution Architecture

```
HealthInsuranceClaims.sln
│
├── src/
│   ├── HealthInsuranceClaims.Domain/          # Pure entities, domain enums, base classes
│   ├── HealthInsuranceClaims.Application/     # Service interfaces, verification engine, workflow service, DTOs
│   ├── HealthInsuranceClaims.Infrastructure/  # EF Core DbContext, migrations, Identity, file storage, seeder
│   └── HealthInsuranceClaims.Web/             # MVC Controllers, Razor Views, Bootstrap 5 UI, view models
│
├── tests/
│   └── HealthInsuranceClaims.Tests/           # xUnit test suite (Verification, Workflow, Security)
│
└── docs/
    ├── DatabaseDesign.md                      # Schema dictionary, ER diagram, indexing strategy
    ├── SystemArchitectureAndDiagrams.md       # Use Case, Activity, DFD, Sequence diagrams
    └── TestingReport.md                       # Actual execution report of all 25 automated tests
```

---

## 4. Setup & Running Instructions

### 4.1 Prerequisites
- **.NET 8.0 SDK** (or .NET 9 SDK with .NET 8 runtime)
- **Microsoft SQL Server LocalDB** (included with Visual Studio or SQL Server Express)

### 4.2 Build Solution
```powershell
dotnet build
```

### 4.3 Database Migrations & Seeding
The application uses Entity Framework Core Code First. The database will automatically migrate and seed itself upon first launch. Alternatively, you can run migrations manually:
```powershell
dotnet ef database update --project src/HealthInsuranceClaims.Infrastructure --startup-project src/HealthInsuranceClaims.Web
```

### 4.4 Run Web Application
```powershell
dotnet run --project src/HealthInsuranceClaims.Web
```
Open your browser and navigate to:  
`https://localhost:5001` or `http://localhost:5000`

### 4.5 Run Automated Tests
```powershell
dotnet test
```
All **25 tests** will execute and report 100% passing results.

---

## 5. End-to-End Workflow Demonstration Guide

To demonstrate the full application lifecycle for viva:

1. **Sign In as Hospital TPA Desk (`apollo.desk@apollohospital.com` / `Hospital@123`):**
   - Click the quick **Hospital Desk** button on the sign-in page.
   - Navigate to **Submit Patient Claim**.
   - Select the patient's verified insurance policy (e.g., `Amit Verma — POL-IND-2026-001 (Comprehensive Health Shield)`).
   - Enter admission date, discharge date, and treatment/diagnosis details (Claim Type: Cashless).
   - Click **Proceed to Quotation & Bills**.
   - In the Claim Details workbench:
     - Add itemized medical expenses:
       - Hospital Room Charges: ₹12,000 (Exceeds policy limit of ₹8,000)
       - Surgery Charges: ₹80,000 (Within policy limit of ₹1,50,000)
       - Other Medical Expenses: ₹3,000 (Cosmetic/comfort item excluded under policy)
     - Upload official patient medical documents:
       - Hospital Final Bill / Invoice
       - Discharge Summary
       - Doctor Prescription / Operative Notes
     - Click **Submit Claim to Insurance**.
     - Status updates to **Submitted**, and automatic rule verification runs immediately.

2. **Sign Out and Log In as Insurance Officer (`officer1@insurance.com` / `Officer@123`):**
   - On the Officer Dashboard, observe the new hospital claim in **Claims Awaiting Review**.
   - Click **Review Claim** (status automatically transitions to **Under Review**).
   - Inspect the **Automatic Policy Verification Results**:
     - Room Charges: Capped at ₹8,000; non-eligible deduction of ₹4,000 with explanation.
     - Surgery Charges: Full ₹80,000 marked eligible.
     - Other Medical Expenses: ₹0 eligible; full ₹3,000 non-eligible under exclusion clause.
   - Download and inspect the hospital bills and discharge summary uploaded by the hospital.
   - Click **Final Decision**:
     - Select **Partially Approved**.
     - Enter Approved Amount: ₹88,000.
     - Enter mandatory justification: *"Room rent capped per policy terms; non-medical comfort expenses deducted under exclusion clause."*
     - Click **Confirm & Record Decision**.

3. **Sign Out and Log In as Customer / Patient (`customer1@gmail.com` / `Customer@123`):**
   - View **My Insurance Policies** to see active coverage, assigned dates, and expiry dates.
   - Open **Claim Status & Enquiry** &rarr; **Track Progress**.
   - View the live visual timeline updated to **Partially Approved**.
   - View the hospital details (Apollo Hospital), claimed quotation amount (₹95,000), approved amount (₹88,000), deduction explanations, and officer notes.

4. **Log In as Administrator (`admin@insurance.com` / `Admin@123`):**
   - View **Administration Dashboard** metrics and financial distributions.
   - Inspect **Policies** and add/edit coverage category limits and exclusions.
   - Open **Audit Logs** to inspect the chronological, tamper-proof record of every event.

---

## 6. Academic Documentation Links

- [Database Design & Data Dictionary](file:///D:/Projects/FinalYearProject/docs/DatabaseDesign.md)
- [System Architecture & UML Diagrams](file:///D:/Projects/FinalYearProject/docs/SystemArchitectureAndDiagrams.md)
- [Automated Testing Report (25/25 Passed)](file:///D:/Projects/FinalYearProject/docs/TestingReport.md)
