# RateYourSchool - Business Rules Documentation

**Version:** 1.0  
**Last Updated:** March 15, 2026  
**Status:** Current Implementation

---

## 1. School Management

### 1.1 School Creation Rules

**BR-001: School Required Information**
- A school MUST have the following mandatory information:
  - Name (non-null)
  - Physical Address (non-null)
  - City (non-null)
  - Province (non-null)
  - School Type
  - At least one Education Grade Type
  - Geographic Location (Latitude and Longitude)

**BR-002: School Unique Identification**
- Each school is assigned a unique GUID identifier upon creation
- The ID is immutable once assigned

**BR-003: School Initial State**
- When a new school is created, it is automatically set to "unpublished" status (`IsPublished = false`)
- Initial score is set to zero across all dimensions (Innovation: 0, Building: 0, Professorate: 0, ManagementTeam: 0)

**BR-004: School Types**
- The system supports four school types:
  - Public
  - Private
  - Charter
  - Magnet

**BR-005: Education Grade Types**
- A school can serve one or multiple education levels:
  - Elementary
  - Middle
  - High
  - K12 (Kindergarten through 12th grade)

### 1.2 School Publication Rules

**BR-006: School Publication Process**
- Schools can be published (activated) to make them visible to users
- Only unpublished schools can be published
- Publishing an already published school has no effect (idempotent operation)

**BR-007: School Unpublication Process**
- Published schools can be unpublished (deactivated)
- Only published schools can be unpublished
- Unpublishing an already unpublished school has no effect (idempotent operation)

### 1.3 Geographic Rules

**BR-008: Location Requirements**
- Every school MUST have a geographic location defined by:
  - Latitude (decimal degrees)
  - Longitude (decimal degrees)

---

## 2. School Rating System (Read Model)

### 2.1 SchoolRate Read Model Architecture

**BR-009: SchoolRate as Read Model**
- SchoolRate is a **read model** (CQRS pattern) that materializes data from write models
- It represents a denormalized view optimized for querying school information
- Composed of data from two write models:
  - **School** write model - school master data
  - **UserReview** write model - user-generated reviews
- Contains aggregated and denormalized data:
  - School information (name, address, image, etc.)
  - Current calculated score (aggregated from user reviews)
  - Last 25 user reviews

**BR-010: One-to-One Relationship**
- Each School has exactly ONE SchoolRate read model (1:1 relationship)
- The SchoolRate serves as the materialized view of the school's current state
- SchoolRate ID corresponds to the associated School ID

### 2.2 Read Model Update Rules

**BR-011: Write Model-Driven Updates**
- SchoolRate is recreated/updated when either write model changes:
  - When a **UserReview** is created for the specific school
  - When the **School** master data is updated
- The update process includes:
  - Recalculation of the school's aggregate score based on all reviews
  - Refreshing the list of the last 25 user reviews
  - Synchronizing school information from the School write model

**BR-012: Review Limit**
- SchoolRate maintains a maximum of the **last 25 user reviews**
- Older reviews beyond the 25th are not included in the read model
- Historical reviews are preserved elsewhere but not displayed in the read model

**BR-013: Score Recalculation**
- When a new UserReview is submitted, the school's score is automatically recalculated
- The score aggregation considers all user reviews for the school
- The multi-dimensional score (Innovation, Building, Professorate, ManagementTeam) is derived from user review data

### 2.3 Read Model Lifecycle Events

**BR-014: Read Model Creation Event**
- When a SchoolRate read model is created, it is logged in the event history
- The system records: "SchoolRate created for school {SchoolName}"

**BR-015: Read Model Deletion**
- SchoolRate read models can be soft-deleted (marked for deletion)
- When deleted, it is logged in the event history
- The system records: "SchoolRate deleted for school {SchoolName}"
- Deleted read models maintain their data but are flagged internally

---

## 3. Scoring System

### 3.1 Score Dimensions

**BR-016: Multi-Dimensional Scoring**
- Scores are evaluated across four dimensions:
  1. **Innovation** - Assessment of innovative practices
  2. **Building** - Evaluation of facilities and infrastructure
  3. **Professorate** - Rating of teaching staff quality
  4. **ManagementTeam** - Evaluation of administrative leadership

**BR-017: Score Calculation**
- The total score is calculated as the arithmetic mean of all four dimensions
- Formula: `Total = (Innovation + Building + Professorate + ManagementTeam) / 4`
- The calculation is automatic and always up-to-date

**BR-018: Score Data Type**
- All score values use decimal precision for accuracy

---

## 4. Event Logging & Audit Trail

### 4.1 Event Logging Rules

**BR-019: Automatic Event Logging**
- All significant entity state changes are automatically logged
- Each event log contains:
  - Timestamp (UTC)
  - Description of the event

**BR-020: School Lifecycle Events**
- The following school events are logged:
  - "School {Name} created" - when a school is first created
  - "School {Name} activated" - when a school is published
  - "School {Name} deactivated" - when a school is unpublished

**BR-021: Rating Lifecycle Events**
- The following rating events are logged:
  - "SchoolRate created for school {SchoolName}"
  - "SchoolRate deleted for school {SchoolName}"

**BR-022: Event Immutability**
- Event logs are append-only
- Once an event is logged, it cannot be modified or deleted
- Events are stored in chronological order

**BR-023: Timestamp Standardization**
- All event timestamps use UTC (Coordinated Universal Time)
- Ensures consistency across different time zones

---

## 5. API & Data Access Rules

### 5.1 School Retrieval Rules

**BR-024: Pagination Support**
- The schools list endpoint supports pagination with two parameters:
  - `page` - The page number to retrieve
  - `pageSize` - Number of items per page

**BR-025: Default Pagination Values**
- If `page` is not provided or invalid, default to page 1
- If `pageSize` is not provided, default to 20 items

**BR-026: Page Number Validation**
- Page numbers less than 1 are automatically corrected to 1
- Minimum valid page number: 1

**BR-027: Page Size Constraints**
- Minimum page size: 1 item
- Maximum page size: 1000 items
- Values outside this range are automatically clamped to the nearest boundary

**BR-028: School View Model**
- When retrieving schools, the following information is provided:
  - School Info (Name, Address, Image URL)
  - Aggregated Score (all four dimensions + total)
  - User Reviews (Username, Comment, Rating)

### 5.2 Map View Rules

**BR-029: Simplified Map View**
- The system provides a simplified read model optimized for map display
- Contains minimal school information:
  - Name
  - City
  - Province
  - School Type
  - Geographic Location (Latitude, Longitude)
- This lightweight projection enables efficient rendering of multiple schools on a map interface
- Excludes detailed information like reviews, scores, and full address to minimize data transfer

### 5.3 School Filtering and Ranking Rules

**BR-030: Filter Schools by Location**
- Users can filter schools based on geographic criteria:
  - **By City**: Retrieve schools within a specific city
  - **By Province**: Retrieve schools within a specific province
  - **By Geolocation**: Retrieve schools within a specified radius of geographic coordinates
- Multiple filters can be combined for refined results

**BR-031: Rank Schools by Score**
- Schools can be ranked by their aggregate score (highest to lowest)
- Ranking is based on the Total score calculated from the four dimensions
- Filters can be applied before ranking:
  - Top-scored schools in a specific city
  - Top-scored schools in a specific province
  - Top-scored schools within a geographic area

**BR-032: Geographic Proximity Search**
- Users can search for schools within a specified distance from a point
- Distance is calculated using coordinates (latitude/longitude)
- Results are ordered by proximity (nearest first) or by score
- Distance units should be configurable (kilometers/miles)

### 5.4 Review Retrieval Rules

**BR-033: View All School Reviews**
- Users can retrieve all reviews for a specific school
- Reviews are not limited to the last 25 (unlike the SchoolRate read model)
- Supports pagination for schools with many reviews

**BR-034: Review Filtering**
- Reviews can be filtered by:
  - Date range (reviews within a specific time period)
  - Rating range (e.g., only 4-5 star reviews)
  - Keyword search in comments

**BR-035: Review Ordering**
- Reviews can be ordered by:
  - Most recent first (default)
  - Oldest first
  - Highest rating first
  - Lowest rating first

---

## 6. User Review System

### 6.1 Review Structure

**BR-036: Review Components**
- User reviews contain:
  - Username (reviewer identification)
  - Comment (text feedback)
  - Rating (numerical evaluation)

**BR-037: Review Association**
- Reviews are associated with schools
- Multiple reviews can exist for a single school
- Each review is a write model event that triggers SchoolRate read model updates

### 6.2 Review Authorization

**BR-038: Anonymous Review Creation**
- Any anonymous user can create a school review
- No authentication or authorization is required to submit a review
- Users do not need to create an account or log in
- This promotes open feedback and wider participation

### 6.3 Review Processing

**BR-039: Review Submission Impact**
- When a UserReview is submitted for a school:
  1. The review is persisted as a write model entity
  2. The associated SchoolRate read model is recreated/updated
  3. The school's aggregate score is recalculated from all reviews
  4. The last 25 reviews list is refreshed
  5. School master data is synchronized from the School write model

**BR-040: SchoolRate Review Limit**
- Only the last 25 reviews are included in the SchoolRate read model
- This ensures the read model reflects current user sentiment
- For viewing all reviews, use the dedicated review retrieval endpoint (BR-033)

---

## 7. Data Validation Rules

### 7.1 Null Safety Rules

**BR-041: Required Field Validation**
- The system enforces null safety for all required fields
- Any attempt to create an entity with null required fields will fail
- Applies to:
  - School: name, address, city, province, location, type, grades, score
  - SchoolRate: id, school info, description, score

**BR-042: Collection Validation**
- Collections (grades, event logs) cannot be null
- Empty collections are allowed where appropriate

---

## 8. State Management Rules

### 8.1 Entity State Tracking

**BR-043: State Change Detection**
- The system tracks when an entity's state has been modified
- State changes are marked internally for persistence optimization

**BR-044: State Change Triggers**
- Any modification to an entity's state triggers the state change flag
- Event log additions automatically mark entities as changed

---

## 9. Business Constraints

### 9.1 Idempotency Rules

**BR-045: Idempotent Publication**
- Publishing an already published school is a safe, no-op operation
- The system does not generate duplicate events for redundant actions

**BR-046: Idempotent Unpublication**
- Unpublishing an already unpublished school is a safe, no-op operation
- The system does not generate duplicate events for redundant actions

### 9.2 CQRS Pattern

**BR-047: Command-Query Separation**
- The system implements CQRS (Command Query Responsibility Segregation)
- **Write Models**:
  - **School** - Core school entity with master data
  - **UserReview** - User-generated review events
- **Read Model**:
  - **SchoolRate** - Denormalized projection composed from School and UserReview data
- This separation allows for:
  - Optimized read performance through pre-materialized views
  - Scalable querying without impacting write operations
  - Independent scaling of read and write workloads

**BR-048: Eventual Consistency**
- SchoolRate read models are updated asynchronously after write model changes
- There may be a brief delay between:
  - UserReview submission and read model update
  - School data modification and read model synchronization
- The system embraces eventual consistency for performance optimization

---

## 10. Future Considerations

### 10.1 Not Yet Implemented

The following areas are identified but not yet implemented:

**Data Persistence**
- In-memory data store is defined but not populated
- Database integration pending

**Authentication & Authorization**
- User identity management (for administrative functions)
- Review moderation and management capabilities
- School ownership and management permissions

**Advanced Search & Filtering**
- Search schools by name with autocomplete
- Filter by grade levels
- Combined multi-criteria search
- Full-text search across school descriptions

**Rating Aggregation**
- Weight reviews based on recency or reviewer reputation
- Detect and handle outlier reviews
- Verified review badges

**Image Management**
- School image upload and storage
- Image URL validation and sanitization
- Multiple images per school (gallery)

**Review Moderation**
- Flag inappropriate reviews
- Review editing and deletion
- Spam detection and prevention

---

## Appendix

### Entity Relationships

```
Write Models:
  School (1) ----< (*) UserReview

Read Model Projection:
  School (1) ----projects-to----> (1) SchoolRate [Read Model]
  UserReview (*) --projects-to--> (1) SchoolRate [Read Model]

Update Triggers:
  School changes ---------> SchoolRate Update
  UserReview creation ----> SchoolRate Update
```

**Relationship Notes:**
- **Write Models**:
  - School (1) to UserReview (Many): Each school can have multiple user reviews
- **Read Model Projection**:
  - School (1) to SchoolRate (1): Each school projects to exactly one read model
  - UserReview (Many) to SchoolRate (1): Multiple reviews aggregate into one read model per school
- **Update Triggers**:
  - School data changes trigger SchoolRate read model synchronization
  - UserReview creation triggers SchoolRate read model recreation/update with recalculated scores

### Data Types

- **Identifiers**: String (GUID format)
- **Scores**: Decimal
- **Timestamps**: DateTime (UTC)
- **Text Fields**: String (Name, Address, City, Province, etc.)
- **Locations**: Double (Latitude/Longitude)

### Primary Use Cases

**UC-001: Display Schools on Map**
- Actor: Anonymous User
- Description: Retrieve a simplified subset of school data optimized for map display
- Data Retrieved: Name, City, Province, School Type, Geographic Location
- Purpose: Enable users to visualize school locations on an interactive map
- Related Business Rules: BR-029

**UC-002: Create School Review**
- Actor: Anonymous User (no authentication required)
- Description: Submit a review for a specific school
- Data Required: School identification, Username, Comment, Rating
- Impact: Triggers SchoolRate read model update and score recalculation
- Related Business Rules: BR-038, BR-039, BR-040

**UC-003: View School Details**
- Actor: Anonymous User
- Description: Retrieve comprehensive school information with ratings
- Data Retrieved: School Info, Multi-dimensional Score, Last 25 User Reviews
- Related Business Rules: BR-024 through BR-028

**UC-004: View All Reviews for a School**
- Actor: Anonymous User
- Description: Filter, list, and view all reviews for a specific school
- Capabilities:
  - Retrieve complete review history (not limited to last 25)
  - Filter by date range, rating, or keywords
  - Sort by date, rating, or relevance
  - Paginate through large review sets
- Data Retrieved: School identification, All user reviews with pagination
- Related Business Rules: BR-033, BR-034, BR-035

**UC-005: Find Top-Rated Schools**
- Actor: Anonymous User
- Description: Discover the highest-rated schools based on location and score filters
- Filter Options:
  - By City: Top schools in a specific city
  - By Province: Top schools in a specific province
  - By Geolocation: Top schools within a radius from user's location
  - Combined filters: e.g., top schools in a city AND within 10km
- Sort Options:
  - Highest overall score
  - Best score in specific dimension (Innovation, Building, etc.)
  - Nearest distance (when using geolocation filter)
- Data Retrieved: Filtered and ranked list of schools with names, locations, scores
- Related Business Rules: BR-030, BR-031, BR-032

---

**Document Control:**
- This document reflects the current implementation as of March 15, 2026
- Business rules are extracted from the Domain and API layers
- Changes to business logic should be reflected in this document
