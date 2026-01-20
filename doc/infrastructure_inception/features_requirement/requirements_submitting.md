# Requirements Workup: Feature - "Reporting an Infrastructure Issue"

## Elicitation
Goal: Determine exactly how a user submits a report so we can code the form correctly.

1. Does the user need to be at the physical location to report the issue?
   - A: Ideally yes, but they might take a photo and upload it later when they have Wi-Fi.
   - Solution: We should prioritize extracting GPS data from the *image metadata* first. If that fails, we ask the user to drop a pin on a map.

2. What happens if the image isn't a pothole? (e.g., a cat photo)
   - A: We can't stop them from uploading it, but we don't want it on the public feed.
   - Solution: All new reports default to a "Pending" status. They are not visible on the main map until approved (either by AI or a moderator).

3. Do users need to be logged in?
   - A: Yes. We need to prevent spam and assign "points" to the user for the leaderboard.
   - Solution: Authentication is a hard prerequisite for this feature.

4. Where do the photos go?
   - A: Storing images directly in the SQL database will bloat it.
   - Solution: We need an external storage service (Azure Blob Storage) to hold the files; the database will just store the URL link.

## Analysis
Constraints & Bounds:
1.  **Authentication:** The "Report" button is disabled/hidden unless the user has a valid session.
2.  **File Type:** Images only (JPG, PNG). No video (too expensive/heavy for MVP). Max size 5MB.
3.  **Data Integrity:** Every Report MUST have:
    * A valid `UserID` (Who sent it)
    * A `PhotoURL` (The evidence)
    * `GPS Coordinates` (Where it is)
    * A `Timestamp` (When it happened)

**Conflicting Behaviors:**
* *Conflict:* We want high-res photos for the AI to analyze damage, but high-res photos take forever to upload on mobile data.
* *Resolution:* We will compress images client-side (in the browser/app) before uploading to balance speed vs. quality.

**Missing Items Discovered:**
* We originally forgot a way to "Categorize" the issue. We need to add a dropdown menu to the UI for "Pothole," "Graffiti," "Street Light," and so on.

## Design and Modeling

**Entities:**

1.  **User** (Existing entity)
    * `Id`: INT (PK)
    * `Username`: STRING

2.  **Report** (New entity)
    * `Id`: INT (Primary Key)
    * `UserId`: INT (Foreign Key -> User.Id)
    * `ImageUrl`: STRING (Link to Azure Blob)
    * `Latitude`: DECIMAL (10, 8)
    * `Longitude`: DECIMAL (10, 8)
    * `SeverityScore`: INT (Possibly calculated by AI later, nullable for now)
    * `Category`: ENUM 
    * `Status`: ENUM (Pending, Approved, Rejected)
    * `CreatedAt`: DATETIME

**Relationships:**
* **User to Report:** One-to-Many (One User can submit multiple Reports).
* **Report to User:** Many-to-One (A Report belongs to exactly one User).

## Analysis of the Design

1.  **Does it support the requirements?**
    * *Yes.* The `Status` field solves the moderation issue (Q2). The `ImageUrl` field allows us to use Azure Blob storage (Q4). The `UserId` link enables the Leaderboard feature later.

2.  **Can it be done correctly/easily?**
    * *Yes.* This is a standard CRUD operation. ASP.NET Core makes handling file uploads and SQL relations straightforward. We can implement the "Pending" status logic easily in the Controller.