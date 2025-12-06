# HRA Hub; Setting Up Your Account for AutoPay

This article documents the steps on how to setup your Take Command account for AutoPay, ensuring seamless and hassle-free automated payments

If you want more information on how our AutoPay service works, please take a look at our [AutoPay Explainer](https://intercom.help/take-command-health/en/articles/9953524-autopay-explainer-admin).

If you have already set up your account and need help navigating the AutoPay section in your admin portal, see this article [AutoPay in the Admin Portal](https://app.intercom.com/a/apps/m61j6vi5/knowledge-hub/folder/1211570?activeContentId=10819751&activeContentType=article).

Unit, our BaaS (Banking as a Service) provider, facilitates account setup and KYC (Know Your Customer) requirements to stay compliant with Banking regulations. Acceptance of Take Command's [Services Agreement](https://www.takecommandhealth.com/serviceagreement) is required to begin Unit setup.

## Navigating this article

- [How to apply for an account](#how-to-apply-for-an-account)
- [Reapplying for an account](#reapplying-for-an-account)
- [How to connect an external account](#how-to-connect-an-external-account-to-fund-autopay)
- [Troubleshooting](#troubleshooting-tips)

## How to Apply for an Account

Once enabled, the company admin has access to start the application process.

- In the Admin Portal, navigate to Settings > AutoPay and select "Start Your AutoPay Application"
  - To ensure successful completion and approval, collecting the required documentation before starting the application process is encouraged.

- Once the "Start Your AutoPay Application" button has been selected, then a new browser window will open to start the application.

- All required information must be complete to apply for review.

- Approval typically takes less than 2 hours, however, when the application is not immediately approved the application is then placed within 1 of 3 states.
  - **Under Review** - When the application is "Under Review" it has been flagged for manual review. This may result in the application being approved, requiring more supporting documents, or a denial.
  - **Awaiting Documents** - When the application is "Awaiting Documents" supporting documentation needs to be provided for approval.
  - **Denied** - When the application has been "Denied" a determination has been made to deny the application for the provided reason. The employer will then have an opportunity to re-apply for an account if they feel the denial was in error.

## Reapplying for an Account

An application can be denied for multiple reasons. If the admin believes they were denied in error, they can re-apply.

- In the Admin Portal, the admin will navigate to Settings > AutoPay.

- A new button to Re-apply is present in their AutoPay settings.

- See step 3 of "[How to apply for an account](#how-to-apply-for-an-account)."

## How to Connect an External Account to Fund AutoPay

- **Bank Account Approval**
  - Once your bank account application is approved, you'll receive an email notification. After receiving approval, navigate to the "Settings" tab in the Take Command portal. Select "AutoPay", then click on "Connect Your Bank Account". Pop-up window will open to start external account validation using Plaid

- **Bank Account Connection Options**
  - Follow the on-screen prompts to select you financial institution and complete the account setup via one of 2 methods:
    - **Option 1: "Instant" Log in with your bank's credentials**
      - Ensure your bank is listed among the institutions for instant verification and you have access to the account you would like funds to pull from.
    - **Option 2: "Manual" Use micro-deposits verification**
      - Once selected in the pop-up window, log into your online bank account via their app or website.
      - Locate the $0.01 deposit that was made into the account that you previously provided.
      - Click into this deposit/transaction to view the details. The deposit should come through with a description that reads:
        - `#XXX <app_name> ACCTVERIFY`
        - The "XXX" above is where a 3-letter code should be (e.g. #ABC Peanut App ACCTVERIFY).
      - Once you've located the 3-letter code in the description of the $0.01 deposit, copy the code or write it down for quick access later.
      - Go back to the Connect Your Bank Account screen in the Admin Portal. Paste or type in the 3-letter code you found earlier in your bank account to complete the connection process.
      - **Success!**
        - Once connected, a success message appears on the screen with the connected account detail.
          - At any time, the Admin may choose to "Update Bank Account" information should credentials or accounts change.
          - The connected account may become unlinked due to an issue or changes with the account. Should this occur, the Admin will be prompted to "Reconnect Bank Account" to avoid funding issues.

## Troubleshooting Tips:

**Error Messages:**

- If you encounter an error after entering your information, Avoid multiple attempts to prevent triggering our bank partner's fraud monitoring system, which may result in a temporary account freeze.

**Important Note:**

- To ensure your bank funds your AutoPay holding account without issue, please share the ACH Company ID for Unit and TransPecos Banks with your bank to whitelist: ACH Company ID: 114094397. If you have any questions or concerns about this process, please reach out to us promptly
