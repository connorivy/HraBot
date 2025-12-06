# HRA Hub; Add Employee(s) to Your Roster - Bulk

This article documents the steps on adding employees to your roster by CSV upload.

If you are looking to manually add an employee individually, see our help article [Add Employee(s) to Your Roster - Manual](https://intercom.help/take-command-health/en/articles/9952949-add-employee-s-to-your-roster-manual)

Streamline your employee onboarding process with our user-friendly CSV upload feature. Simply download our provided CSV template, populate it with your employee roster data, and upload it seamlessly into our system. This efficient tool eliminates the need for manual data entry, saving you valuable time and reducing the risk of errors. Our template ensures data consistency and accuracy, making it effortless to manage your employee information. With our CSV upload feature, you can focus on what truly matters – growing your business and empowering your workforce.

## Table of Contents

- [Starting the process](#starting-the-process)
- [Add from a CSV file](#add-from-a-csv-file)
- [Download CSV template](#download-csv-template)
- [Fill in CSV](#fill-in-csv)
- [Upload CSV](#upload-csv)
- [Correct errors or edit CSV](#correct-errors-or-edit-csv)
- [Verify and submit](#verify-and-submit)
- [You're done!](#youre-done)
- [Error troubleshooting](#error-troubleshooting)

**NOTE: This feature is only for bulk uploads. This does not support bulk termination.**

*Video Tutorial: HRA Hub - CSV Bulk Upload (3 min 19 views) - Visit the original article to view the video.*

## Detailed steps:

### Starting the process

- Select the "Add New" button from the People Management Table

### Add from a CSV file

- Select the "Continue" button under the "Add from a CSV file" section.

### Download CSV Template

- Download the CSV Template if you already do not have it.

[Download employee_roster_template.csv](https://downloads.intercomcdn.com/i/o/m61j6vi5/1202972860/64f8c23f71bfcaadb848c3d60697/employee_roster_template.csv)

### Fill in CSV

- Fill in the CSV with the required fields, and optional if you have them, for the individuals you are uploading.

#### CSV Field Requirements

| Field | Required? | Character Length | Allowable Values |
|-------|-----------|------------------|------------------|
| First Name | ✓ | 1-255 | Alpha, space, "-", "`" |
| Middle Name | | 1-255 | Alpha, space, "-", "`" |
| Last Name | ✓ | 1-255 | Alpha, space, "-", "`" |
| Preferred Name | | 1-255 | Alpha, space, "-", "`" |
| Email | ✓ | 5-255 | Alpha, space, "-", "`" |
| Phone # | | 10 | Numeric |
| Employee ID | | 1-255 | Alpha, space, "-", "`" |
| Role | ✓ | | Administrator, Broker, Employee, or System Administrator |
| Address | ✓ | 1-255 | Alpha, space, "-", "`", "." |
| Apt, Suite, etc. | | 1-255 | Alpha, space, "-", "`", "." |
| City | ✓ | 1-255 | Alpha, space, "-", "`" |
| Zip Code | ✓ | 5 | Numeric |
| County | ✓ | 1-255 | Alpha, space, "-", "`" |
| State | ✓ | 1-255 | Two letter state abbreviations |
| Employment Type | ✓ | 1-255 | Full-time, Part-Time, or Seasonal |
| Hire Date | ✓ | MM/DD/YYYY | MM/DD/YYYY |
| DOB | ✓ | MM/DD/YYYY | MM/DD/YYYY |
| Class | ✓ | 1-255 | Options are dependent on what classes have been created for the company. |

**NOTE: Benefit Eligible Date is calculated based on the waiting period of the Class assigned to the person, the Company's HRA Start Date, and the person's hire date.**

### Upload CSV

- Upload the completed CSV File and select "Next".

### Correct Errors or Edit CSV

- Correct any errors in the uploaded CSV. Examples include:
  - Missing required fields
  - Incorrect data in fields
- Or edit anything in the CSV:
  - Remove rows
  - Change data

### Verify and Submit

- Verify everything you have changed is correct and there are no more errors. If everything looks good then click "Submit".

### You're done!

- Once everything has uploaded then you're done!

## Error Troubleshooting:

If any of the fields you have entered are empty, not formatted properly, or any other sort of error, the system will let you know which fields have errors, and if you select the field it will tell you what is wrong with it.

For example, in the top image you can see that the cell cannot be empty so it needs to be filled. In the bottom image you can see the date format is incorrect.

*Note: This article contains multiple screenshots showing the CSV upload process and error messages. Please visit the original article to view these images.*

If there are any errors during the upload process itself, the system will let you know there is an error and how many rows had errors. If you select the "View errors and resubmit" button you will be brought back to the edit CSV page where you can see where, and what, the errors are.
