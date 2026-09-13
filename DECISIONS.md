# Engineering Assessment: Bright Path Learning Centre

## PHASE 1: Read the situation

### Questions for the Owner
1. **The "Exam Season" Override:** Mai mentioned she purposefully places two students with one tutor in the same room and slot during exam season for half price. Should the new system support this as a valid "Shared Session" workflow, or strictly enforce the "one-to-one" rule and block these completely? 
2. **Tutor Load Limit:** The rule states a strict max of 6 bookings per tutor per day, but Mai frequently breaks this when desperate. Does the system need an "Admin Override" flag to allow this under exceptional circumstances, or should it be a hard system block?
3. **Paid Late Cancellations:** If a family cancels within the 4-hour window, they are charged, and the tutor is paid. Does that room/slot become available again for another student (potentially double-booking the slot for extra profit), or must it remain locked to prevent confusion?

### Contradictions & Our Reading
* **The Brief argues with itself regarding rules vs. reality:** The official rules mandate "one-to-one lessons" and "max 6 bookings/day". However, Mai's quotes explicitly show the centre operates by violating both rules to accommodate demand and maximize exam season revenue. 
* **Our Reading:** We will read the Owner's word as the ultimate source of truth for system integrity ("if the system allows it, the system is broken"). The system will treat "one-to-one" and "no double booking a tutor/room" as non-negotiable database constraints. Any business exceptions (like exam pairing) must be treated as future feature requests, not silent data corruption.

### Assumptions Invented
* **Cut-off Time Handling:** The 16:00 cut-off applies to the *communication* of the schedule. To ensure tutors see changes rather than overwrites, we assume the system needs a versioning or state-tracking mechanism. Once a schedule is "Published" at 16:00, any subsequent edits create a new log/state (e.g., `Amended`) rather than replacing the original row.
* **Timezone:** All operations, especially the 16:00 cut-off and 4-hour cancellation window, are strictly evaluated against Da Nang local time (ICT).

---

## PHASE 2: Choose what to build

### Potential Features Needed (The Shortlist)
1. Conflict detection engine (Preventing double-booked rooms/tutors).
2. Schedule freezing & revision tracking API (Handling the 16:00 cut-off).
3. Late cancellation workflow API (Handling the 4-hour charge window).
4. Tutor daily schedule view API (Immutable view of their assigned day).
5. Receptionist daily dashboard (The "open the laptop and see today" view).
6. automated WhatsApp notification trigger system.

### The Chosen Feature: Scheduling & Conflict Detection API
I have chosen to build the **Conflict Detection Engine & Booking API**. 

**Why it's worth more to Bright Path:** 
The owner explicitly stated: *"Twice last term we had a student booked into two places at once... if the system allows it, the system is broken."* Building UI dashboards or notification systems on top of corrupt data solves nothing. By enforcing structural integrity at the database and API level—ensuring a tutor is never double-booked and a room never over capacity—we eliminate the centre's most catastrophic operational failure. It forms the solid foundation required before any other feature can be reliably built.

**What is left broken:**
By choosing the backend integrity, we leave the frontend experience broken. Mai will not get her visual "daily dashboard" today, and we are not solving the automated WhatsApp messaging for tutors. They will still have to communicate manually, but the information they communicate will finally be 100% accurate.



## PHASE 3: Design and build that one

### Data Model
To solve the conflict detection and handle the 16:00 cut-off, the database requires two primary tables:

1. **`Lessons`**: The source of truth for bookings.
   * `id` (UUID, Primary Key)
   * `lesson_code` (String, nullable - e.g., L001, L002)
   * `student_name` (String, nullable)
   * `tutor_id` (String, Foreign Key to Tutors)
   * `room_id` (Integer, 1-6)
   * `start_time` (DateTimeOffset)
   * `duration_min` (Integer, typically 60 or 90)
   * `status` (Enum/String: `booked`, `cancelled`, `no_show`)
   * `cancelled_at` (DateTimeOffset, nullable)
   * `note` (String, nullable - captures operational notes like exam pairs or family requests)

2. **Handling Post-Cut-off Changes (`LessonRevisions`)**:
   To ensure changes after 16:00 do not "quietly overwrite" what the tutor was told, we introduce an append-only `LessonRevisions` (or Audit Log) table. 
   * `id` (UUID)
   * `lesson_id` (UUID, Foreign Key)
   * `action` (String: `Late_Cancel`, `Rescheduled`)
   * `created_at` (DateTimeOffset)
   * `is_acknowledged` (Boolean, default false)
   
   **How it works:** When a cancellation or move occurs, the application logic checks if the current time is past 16:00 on the day before the lesson. If yes, it updates the `Lessons` table BUT ALSO inserts a record into `LessonRevisions`. The tutor's UI/Notification system will fetch unacknowledged revisions to highlight exactly what changed.

### Rules Enforcement: Database vs. Code
* **Enforced in Database:**
  * **Referential Integrity & Constraints:** `tutor_id` must exist in the Tutors table. `room_id` is constrained to values 1-6. `status` is constrained to the 3 valid states. 
  * *Why:* The database is the last line of defense. Invalid states or orphan records should be physically impossible to insert.
* **Enforced in Code (Application Level):**
  * **Conflict Detection (Overlapping Times):** Checking if a new booking overlaps with an existing one for the same `tutor_id` or `room_id`. 
  * **Tutor Load Limit (Max 6/day):** Grouping today's lessons for a tutor and ensuring the count is < 6.
  * *Why:* Temporal logic (overlapping intervals) and timezone-aware daily aggregations are complex and database-specific. Handling this in code (e.g., using a single parameterized query before inserting) makes the application scalable, easier to unit test, and avoids complex DB triggers.

### API Shape
The core endpoint for the chosen feature:
**`POST /api/v1/lessons`**
* **Payload:** `{ "tutor_id": "...", "room_id": 3, "start_time": "2026-03-12T14:00:00+07:00", "duration_min": 60 }`
* **Response (Success):** `201 Created` with the lesson object.
* **Response (Failure):** `409 Conflict` (e.g., `{"error": "Room 3 is already booked during this time"}`) or `400 Bad Request` (`{"error": "Tutor exceeded 6 bookings"}`).

### The Rejected Endpoint
* **`PUT /api/v1/lessons/{id}` (Generic Update)**
* **Why it was rejected:** Allowing a generic "update all fields" endpoint completely undermines the 16:00 cut-off rule. A client could quietly change the time or room of a lesson. Instead, mutations must happen via explicit intent-based endpoints like `POST /api/v1/lessons/{id}/cancel` or `POST /api/v1/lessons/{id}/reschedule`, which explicitly contain the business logic to check the cut-off time and generate Revision logs.


## PHASE 4: Reflect

* **What I would build next with another week:**
  I would build the frontend "Daily Dashboard" for Mai. Since the backend now guarantees 100% data integrity and conflict prevention, I can confidently build a read-only React view that strictly visualizes today's layout across the 6 rooms without scrolling, directly solving her primary complaint.

* **What I know is weak:**
  The concurrency handling in my conflict detection logic is a potential weakness. If two receptionists (or a future online booking system) attempt to book the exact same room at the exact same millisecond, the application-level read-then-write check might cause a race condition. Implementing strict database-level locking (or Serializable transaction isolation) would be required for a truly robust production environment.

* **Where my AI assistant helped:**
  I used Gemini to act as a sounding board while reading the brief. It helped me spot the specific contradiction between the Owner's strict "one-to-one" rule and Mai's admission of purposefully double-booking students during exam season. It also helped structure this markdown document to ensure I didn't miss any of the specific prompt requirements.

* **One suggestion I threw away and why I was right to:**
  I initially considered implementing a "Soft Delete" (e.g., `is_deleted = true`) for cancellations. I threw this away because a cancellation in this business is not a deletion; it is a critical state change. Because late cancellations (within 4 hours) trigger payments and charges, treating them as deleted records would destroy financial auditability. Representing them as a `cancelled` status with a `cancelled_at` timestamp was the right call.
