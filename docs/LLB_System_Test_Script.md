# LLB System Test Script

Use this script for functional, regression, and user acceptance testing. Each test case includes fields for `Comment` and `Overall Remark` so testers can record issues, observations, and the final judgement for that process.

## Test Run Details

| Field | Value |
| --- | --- |
| Test run date |  |
| Environment |  |
| Build/version |  |
| Tester |  |
| Browser/device |  |
| Overall status | Pass / Fail / Blocked |
| Overall comment |  |
| Overall remark |  |

## Standard Test Result Fields

Use these fields for every process below.

| Field | Purpose |
| --- | --- |
| Actual result | What happened during execution. |
| Status | Pass, Fail, Blocked, or Not Run. |
| Comment | Specific observation, defect number, missing data, or clarification. |
| Overall remark | Final process-level conclusion after all steps are completed. |

## Test Data Checklist

| Data item | Required for | Available | Comment |
| --- | --- | --- | --- |
| Client applicant account | Client workflows |  |  |
| Examiner account | Verification and examination |  |  |
| Recommender account | Recommendation stage |  |  |
| Approver/Secretary account | Approval stage and signatures |  |  |
| Accountant account | Payment verification |  |  |
| Admin account | Settings, users, tasks |  |  |
| Active licence type and fee | New licence applications |  |  |
| Hotel licence type | Hotel room count validation |  |  |
| Existing approved licence | Renewals and post-formation services |  |  |
| Sample PDF/image attachments | Upload tests |  |  |
| Paynow test credentials or manual POP | Payment tests |  |  |

## Process Test Cases

| ID | Process | Preconditions | Steps | Expected result | Actual result | Status | Comment | Overall remark |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AUTH-001 | User registration | Admin or public registration is available. | Open registration, enter valid personal details, email, password, role where applicable, submit. | User is created, validation succeeds, and user can log in with assigned role. |  | Not Run |  |  |
| AUTH-002 | Login and role redirect | User exists and is active. | Log in as Client, Examiner, Recommender, Approver, Accountant, and Admin. | Each role lands on the correct dashboard and sees only authorised menu items. |  | Not Run |  |  |
| AUTH-003 | Invalid login | User account exists. | Enter wrong password and submit. | Login is rejected with a clear error and no session is created. |  | Not Run |  |  |
| AUTH-004 | Change password | User is logged in. | Open change password, enter current password and a valid new password, submit, log out, then log in using new password. | Password changes successfully and old password no longer works. |  | Not Run |  |  |
| AUTH-005 | Forgot password | User email exists. | Request password reset or forgot-password action using registered email. | System accepts request and follows configured reset process without exposing account data. |  | Not Run |  |  |
| DASH-001 | Client dashboard | Client has no applications. | Log in as Client and open dashboard. | Dashboard loads with available application/process links and no errors. |  | Not Run |  |  |
| DASH-002 | Internal dashboards | Internal users exist. | Open Examiner, Recommender, Approver, Accountant, and Admin dashboards. | Dashboard counts and task lists load according to each role. |  | Not Run |  |  |
| LIC-001 | New licence application - applicant information | Client account exists. | Start new licence, enter applicant/company details, title, contact details, ID copy, fingerprints, and Form FF, then save. | Application is saved as draft and moves to outlet information step. |  | Not Run |  |  |
| LIC-002 | New licence application - validation | Client starts new licence. | Leave required fields blank and submit applicant form. | Required field validation prevents submission and displays clear errors. |  | Not Run |  |  |
| LIC-003 | Outlet information - non-hotel licence | Draft application exists with non-hotel licence type. | Enter licence, region, outlet, premise location, and outlet details. | Outlet information saves and hotel bedroom fields are not shown or saved. |  | Not Run |  |  |
| LIC-004 | Outlet information - hotel licence room counts | Draft application exists with hotel licence type. | Select hotel licence, enter outlet details, enter Double rooms and Single rooms, save, reopen outlet step. | Bedroom section appears only for hotel licence and saved counts are retained. |  | Not Run |  |  |
| LIC-005 | Hotel licence output bedroom statement | Approved hotel licence exists with Double and Single room counts. | Generate hotel licence document. | Document shows `Number of bedrooms to be maintained: Double: ... Single: ...` above approved managers statement. |  | Not Run |  |  |
| LIC-006 | Director details | Draft application exists. | Add director details with national ID, fingerprints, and Form 55 files. | Director is saved and appears in the director list. |  | Not Run |  |  |
| LIC-007 | Manager details | Draft application exists. | Add manager details with ID, fingerprints, and Form 55 files. | Manager is saved and appears in the manager list. |  | Not Run |  |  |
| LIC-008 | Required attachments | Draft application exists. | Upload required attachments one by one and remove one uploaded attachment. | Uploads save individually, removed attachment disappears, and missing required documents block final submission where applicable. |  | Not Run |  |  |
| LIC-009 | Licence payment - Paynow | Draft application has calculated fee. | Choose Paynow payment and complete test payment. | Payment record is created and status updates according to gateway response. |  | Not Run |  |  |
| LIC-010 | Licence payment - proof of payment | Draft application has calculated fee. | Upload proof of payment and remove it. | POP uploads, can be viewed, and can be removed before submission. |  | Not Run |  |  |
| LIC-011 | Submit licence application | Application has applicant, outlet, manager/director, attachments, and payment details. | Open finalising step and submit. | Application status changes from draft to submitted and a verification task is created. |  | Not Run |  |  |
| LIC-012 | Resolve application query | Application has query raised by internal reviewer. | Client opens query, updates requested section, and resubmits. | Query is marked resolved and application returns to the correct review queue. |  | Not Run |  |  |
| VERIFY-001 | Verification review - approve to next stage | Submitted licence application exists and task assigned to examiner. | Open verification dashboard, review application sections, approve verification. | Application moves to recommendation or next configured stage and task is completed. |  | Not Run |  |  |
| VERIFY-002 | Verification review - query | Submitted licence application exists. | Open application, enter query details, submit query. | Query is saved, applicant can see it, and task status reflects query state. |  | Not Run |  |  |
| VERIFY-003 | Verification inspection scheduling | Application requires inspection. | Set inspection date and assign inspector where applicable. | Inspection record/task is created with selected date and visible to assigned officer. |  | Not Run |  |  |
| INSPECT-001 | Inspection pass | Inspection task exists. | Open inspection, enter inspection findings, mark compliant, submit. | Inspection status becomes compliant and application proceeds to next stage. |  | Not Run |  |  |
| INSPECT-002 | Inspection fail | Inspection task exists. | Enter failed inspection findings and submit failed report. | Failed report is saved and application follows rejection/remediation path. |  | Not Run |  |  |
| REC-001 | Recommendation approval | Verified application exists. | Recommender opens dashboard, reviews application, approves recommendation. | Recommendation stage completes and approval task is created. |  | Not Run |  |  |
| REC-002 | Recommendation query | Verified application exists. | Recommender raises query. | Query is visible to applicant and application waits for resolution. |  | Not Run |  |  |
| APPROVE-001 | Approval approve | Recommended application exists. | Approver opens finalising view and approves application. | Licence is approved, licence number/status is set, and document can be generated. |  | Not Run |  |  |
| APPROVE-002 | Approval reject | Recommended application exists. | Approver rejects with reason. | Application is rejected and reason is visible in the relevant dashboard/detail view. |  | Not Run |  |  |
| DOC-001 | Standard licence PDF | Approved non-hotel licence exists. | Generate licence PDF. | PDF displays licensee, trading name, location, managers, QR code, dates, secretary signature, and excludes hotel room statement. |  | Not Run |  |  |
| DOC-002 | Licence QR verification | Approved licence with QR exists. | Scan QR code or open verification URL. | Verification page displays matching licence details and status. |  | Not Run |  |  |
| DOC-003 | Duplicate certificate request | Approved licence exists. | Request duplicate certificate and complete verification/approval. | Duplicate document becomes available according to approved process. |  | Not Run |  |  |
| RENEW-001 | Renewal application | Existing approved licence is eligible for renewal. | Start renewal from post-formation, upload previous certificate and health certificate, save. | Renewal draft is created with uploaded documents. |  | Not Run |  |  |
| RENEW-002 | Renewal payment and submission | Renewal draft exists. | Pay or upload POP, submit renewal. | Renewal is submitted and routed to verification. |  | Not Run |  |  |
| RENEW-003 | Renewal verification and approval | Submitted renewal exists. | Verify, inspect if required, recommend, and approve renewal. | Renewal status becomes approved and renewed licence/document is available. |  | Not Run |  |  |
| EXT-001 | Extended hours application | Approved licence exists. | Start extended hours, enter date and reason, save draft. | Extended hours draft is created and can be reopened. |  | Not Run |  |  |
| EXT-002 | Extended hours payment and submit | Extended hours draft exists. | Pay applicable fee and submit. | Application is submitted to review queue. |  | Not Run |  |  |
| EXT-003 | Extended hours approval/rejection | Submitted extended hours application exists. | Reviewer approves, then repeat with another record and reject. | Approved record generates certificate; rejected record records reason and no certificate is issued. |  | Not Run |  |  |
| TEMP-RET-001 | Temporary retail application | Approved licence exists. | Start temporary retail, enter date, reason, and location, save. | Temporary retail draft is saved and listed. |  | Not Run |  |  |
| TEMP-RET-002 | Temporary retail approval | Submitted temporary retail exists. | Reviewer opens application and approves. | Temporary retail certificate is available and verification page matches record. |  | Not Run |  |  |
| TEMP-REM-001 | Temporary removal application | Approved licence exists. | Start temporary removal, enter temporary outlet details, upload required attachments, save. | Temporary removal draft is saved with attachment list. |  | Not Run |  |  |
| TEMP-REM-002 | Temporary removal approval/rejection | Submitted temporary removal exists. | Reviewer approves one record and rejects another with reason. | Approved record moves to completed/approved status; rejected record stores rejection reason. |  | Not Run |  |  |
| TEMP-TRF-001 | Temporary transfer application | Approved licence exists. | Start temporary transfer, enter transfer details and manager details where required, submit. | Transfer application is created, paid, and routed for review. |  | Not Run |  |  |
| EXTRA-001 | Extra counter / permission to alter | Approved licence exists. | Start permission to alter, enter reason, upload plan/permission attachment, save. | Draft is saved and attachment can be viewed or replaced. |  | Not Run |  |  |
| EXTRA-002 | Extra counter approval/rejection | Submitted permission to alter exists. | Reviewer approves one record and rejects another. | Status and review reason are recorded correctly. |  | Not Run |  |  |
| MANAGER-001 | Manager change application | Approved licence exists. | Start manager change, add new manager details and files, submit. | Manager change application is created and routed for review. |  | Not Run |  |  |
| MANAGER-002 | Manager change approval/rejection | Submitted manager change exists. | Reviewer approves one record and rejects another. | Approved manager changes apply to licence; rejected change stores reason. |  | Not Run |  |  |
| AGENT-001 | Agent licence application | Client has valid applicant details. | Start agent licence, choose title, enter applicant details, upload each required document individually, save. | Draft is saved and each document can be uploaded/replaced independently. |  | Not Run |  |  |
| AGENT-002 | Agent licence payment auto-submit | Agent licence draft is ready for payment. | Complete payment. | Application automatically submits after successful payment and appears in reviewer queue. |  | Not Run |  |  |
| AGENT-003 | Agent licence approval and document | Submitted agent licence exists. | Reviewer approves and user downloads agent licence. | Agent licence PDF has colorful background, QR code with coat of arms, secretary signature, no URL text below QR, and verification works. |  | Not Run |  |  |
| AGENT-004 | Agent licence rejection | Submitted agent licence exists. | Reviewer rejects with reason. | Rejection reason is saved and visible to applicant/reviewer. |  | Not Run |  |  |
| GOV-001 | Government permit application | Client has approved licence if required by process. | Start government permit, select ministry, enter title of authority, location name, and upload authorisation letter. | Government permit draft is created; hidden fields LLB Number, Licence, Region, and Outlet are not shown on form. |  | Not Run |  |  |
| GOV-002 | Government permit ministry list | Government permit form is open. | Open ministry dropdown/list. | Ministries in Zimbabwe are listed and selectable. |  | Not Run |  |  |
| GOV-003 | Government permit payment | Government permit draft exists and fee is configured in post-formation fees. | Continue to payment and complete payment. | Payment uses government permit fee from post-formation fees and application routes to reviewer queue. |  | Not Run |  |  |
| GOV-004 | Government permit approval and document | Submitted government permit exists. | Reviewer approves and user downloads permit. | Permit PDF uses requested design, coat of arms at top, `LIQUOR LICENSING BOARD` below it, QR code with coat of arms, secretary signature, no URL text below QR, and verification works. |  | Not Run |  |  |
| GOV-005 | Government permit rejection | Submitted government permit exists. | Reviewer rejects with reason. | Rejection reason is saved and visible; no approved permit document is issued. |  | Not Run |  |  |
| PAY-001 | Accountant payment verification | Payment record pending verification exists. | Accountant opens payment verification, approve payment. | Payment status changes to approved and linked application can proceed. |  | Not Run |  |  |
| PAY-002 | Accountant payment rejection | Payment record pending verification exists. | Accountant rejects payment with reason/status. | Payment status changes to rejected and application remains unpaid or requires new payment. |  | Not Run |  |  |
| PAY-003 | Exchange rate maintenance | Accountant is logged in. | Open exchange rate page, update rate. | New exchange rate is saved and fee calculations use updated value where applicable. |  | Not Run |  |  |
| PAY-004 | Paynow transaction report | Paynow transactions exist. | Filter by date range and status. | Report displays matching transactions only. |  | Not Run |  |  |
| TASK-001 | Manual task assignment | Pending task exists. | Admin opens task assignment, selects user and stage, submits. | Task is assigned to selected user and appears on their dashboard. |  | Not Run |  |  |
| TASK-002 | Task reassignment | Assigned task exists. | Reassign task to another user. | Old assignee no longer sees task; new assignee sees task. |  | Not Run |  |  |
| TASK-003 | Bulk reassignment | Multiple tasks exist in same stage. | Select tasks, choose assignee/stage, submit bulk action. | Selected tasks are reassigned together and unselected tasks are unchanged. |  | Not Run |  |  |
| SETTINGS-001 | Licence type setup | Admin is logged in. | Create licence type, set fee, conditions, instructions, and region fee. | Licence type appears in application forms with configured fee/details. |  | Not Run |  |  |
| SETTINGS-002 | Renewal fee setup | Admin is logged in. | Create/update renewal fee, conditions, instructions, and region mapping. | Renewal process uses configured fee and information. |  | Not Run |  |  |
| SETTINGS-003 | Transfer fee setup | Admin is logged in. | Create/update transfer and transfer-with-manager fees. | Transfer processes use configured fees. |  | Not Run |  |  |
| SETTINGS-004 | Removal fee setup | Admin is logged in. | Create/update temporary removal fee. | Temporary removal process uses configured fee. |  | Not Run |  |  |
| SETTINGS-005 | Post-formation fee setup | Admin is logged in. | Create/update fees for inspection, renewal, extended hours, temporary retail, extra counter, manager change, agent licence, and government permit. | Each post-formation process shows and charges the configured fee. |  | Not Run |  |  |
| MASTER-001 | Province maintenance | Admin is logged in. | Add and update province. | Province list updates and province is available where used. |  | Not Run |  |  |
| MASTER-002 | District maintenance | Admin is logged in. | Add and update district linked to province. | District list updates and dependent dropdowns work. |  | Not Run |  |  |
| MASTER-003 | Council maintenance | Admin is logged in. | Add and update council linked to district/province. | Council list updates and applicant/outlet council data can be selected. |  | Not Run |  |  |
| ADMIN-001 | Internal user creation | Admin is logged in. | Create internal user with role and active status. | User can log in and access assigned role functions. |  | Not Run |  |  |
| ADMIN-002 | User role update | Internal user exists. | Change user role and save. | User permissions and dashboard change on next login. |  | Not Run |  |  |
| ADMIN-003 | User activation/block/unlock | Internal user exists. | Deactivate/block user, attempt login, then reactivate/unlock. | Blocked user cannot access system; reactivated user can log in again. |  | Not Run |  |  |
| REPORT-001 | Approval/reviewer reports | Applications exist in multiple statuses. | Open reports and filter by province, council, region, date, service, or status where available. | Report lists only matching records and totals are correct. |  | Not Run |  |  |
| REPORT-002 | Accountant reports export | Payment data exists. | Filter accountant report and export CSV. | CSV downloads and contains the same filtered records as the report page. |  | Not Run |  |  |
| SEC-001 | Role access control | Users with different roles exist. | Attempt to open restricted URLs for each role. | System blocks unauthorized access and allows authorized access only. |  | Not Run |  |  |
| SEC-002 | File upload restrictions | Upload forms are available. | Try valid PDF/image, invalid extension, and oversized file if size limit is configured. | Valid files upload; invalid files are rejected safely. |  | Not Run |  |  |
| REG-001 | Regression smoke test | Build is deployed. | Load home page, login, dashboard, create draft application, upload a file, open report, log out. | Core navigation and session flow work without unhandled errors. |  | Not Run |  |  |

## Process Sign-Off

| Process area | Status | Comment | Overall remark | Signed by | Date |
| --- | --- | --- | --- | --- | --- |
| Authentication and user accounts | Not Run |  |  |  |  |
| New licence application | Not Run |  |  |  |  |
| Verification, inspection, recommendation, approval | Not Run |  |  |  |  |
| Licence document and QR verification | Not Run |  |  |  |  |
| Renewals | Not Run |  |  |  |  |
| Extended hours | Not Run |  |  |  |  |
| Temporary retail | Not Run |  |  |  |  |
| Temporary removal | Not Run |  |  |  |  |
| Temporary transfer | Not Run |  |  |  |  |
| Extra counter / permission to alter | Not Run |  |  |  |  |
| Manager change | Not Run |  |  |  |  |
| Agent licence | Not Run |  |  |  |  |
| Government permit | Not Run |  |  |  |  |
| Payments and accountant functions | Not Run |  |  |  |  |
| Task assignment and reassignment | Not Run |  |  |  |  |
| Settings and master data | Not Run |  |  |  |  |
| Reports | Not Run |  |  |  |  |
| Security and access control | Not Run |  |  |  |  |

