Markdown# Bright Path Tutoring Centre API

A robust backend scheduling and management API built with **ASP.NET Core .NET 8**, **Entity Framework Core (SQLite)**, and **xUnit** to eliminate scheduling chaos and enforce strict operational rules for Bright Path Tutoring Centre.

---

## **Key Business Rules Enforced**

* **Conflict Prevention:** Instantly blocks overlapping bookings by validating conflicts across **Rooms (1–6)**, **Tutors**, and **Students**.
* **Tutor Daily Workload Limit:** Restricts tutors from exceeding a maximum of **6 bookings per day**.
* **Operational Day Restrictions:** Automatically blocks lesson creation on **Mondays**, as the centre is closed for facility cleaning.
* **Strict Payload Validation:** Enforces constraints such as room IDs (1–6), session durations (60–90 minutes), and valid status values (`booked`, `cancelled`, `no_show`).
* **Automated CSV Seed Ingestion:** Automatically imports historical records (`tutors.csv` and `lessons_export.csv`) into the local SQLite database upon startup.

---

## **Tech Stack**

* **Framework:** ASP.NET Core Web API (.NET 8)
* **Database & ORM:** SQLite via Entity Framework Core (`brightpath.db`)
* **Testing:** xUnit Framework
* **Documentation & Testing Tool:** Swagger/OpenAPI UI & VS Code REST Client (`test.http`)

---

## **Getting Started & Installation**

### **1. Prerequisites**
* Ensure you have [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your machine.

### **2. Clone & Run the Project**
Open your terminal and run the following commands sequentially:
<img width="1178" height="518" alt="image" src="https://github.com/user-attachments/assets/37ab1d93-69c2-42f2-acd6-f95fc342c158" />

Open your terminal and run the following commands sequentially:

```bash
# Navigate to the project directory
cd BrightPath_TestOnline_Interview/BrightPath_TestOnline_Interview

# Restore dependencies
dotnet restore

```bash

# Navigate to the project directory
cd BrightPath_TestOnline_Interview/BrightPath_TestOnline_Interview

# Restore dependencies
dotnet restore

# Run the application
dotnet run
3. Access the API on BrowserOnce the application launches successfully, the terminal will display the assigned local port (e.g., http://localhost:5252 or https://localhost:44332).Open your browser and navigate directly to:Interactive Swagger UI:Plaintexthttp://localhost:5252/swagger

Fetch All Lessons & Seed Data:Plaintexthttp://localhost:5252/api/v1/lessons

API Endpoints SummaryMethodEndpointDescriptionGET/api/v1/lessonsRetrieves system status, total counts, and the full list of seeded tutors and lessons.POST/api/v1/lessonsCreates a new lesson booking while rigorously validating all conflict and operational rules.Running TestsAutomated Unit TestsExecute the unit test suite to verify core logic:Bashdotnet test

HTTP Request Testing (test.http)If you use Visual Studio Code, install the REST Client extension, open the test.http file in the project, and click Send Request above any scenario to test successful bookings, conflict handling, and validation errors live.




