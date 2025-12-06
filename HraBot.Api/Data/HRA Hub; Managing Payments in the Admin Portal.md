# HRA Hub; Managing Payments in the Admin Portal

This article guides admins on updating payment details and understanding invoice statuses, after account set-up.

## Overview

This documentation outlines how you can view invoices and manage payment details regarding your Take Command subscription using the Stripe customer portal, which has been integrated into the Take Command admin platform.

### Navigating this Article

- [Where to manage payment details](#where-to-manage-payment-details)
- [Invoice statuses](#invoice-statuses)
- [Update billing information](#editing-billing-information)
- [Setting up ACH payment method](#ach-payments)
- [Billing address requirement](#billing-address-requirement)
- [Pay a past due invoice](#pay-a-past-due-invoice)

## Where to Manage Payment Details:

This is where you can manage your company payment details, including updating payment information and paying past due invoices.

1. **Login to Your Account**

2. **Navigate to Settings > Billing**

3. **Click on "Update Payment Details"**
   - By clicking this option, you will be redirected to Stripe's customer portal.

From here, you can view invoices, edit payment details, and pay past due invoices.

## Invoice Statuses:

Invoice statuses provide insights into the payment status of invoices generated for subscriptions. Within the customer portal, you can perform the following actions:

- **Paid**: Indicates that the customer has successfully completed payment during checkout.

- **Unpaid/Subscription/Open**: Denotes invoices that have not been paid yet or are associated with ongoing subscriptions.

- **Retry/Failed**: Represents invoices where payment attempts have failed initially, and Stripe retries the payment automatically.

- **Draft**: Invoices are placed in draft mode when an account is in collection status. Stripe does not automatically update payment information for these invoices.

## Editing Billing Information:

Billing information can be edited and updated from the Stripe portal, including updating default payment methods or adding new ones (such as credit cards or ACH).

1. From Stripe's customer portal, select "+Add payment method"

2. Update your payment method
   - You may set a new default payment method or add additional payment methods as needed.

## ACH Payments:

ACH payments are eligible for customers with US bank, ensuring wider accessibility to our services.

## Billing Address Requirement:

Valid billing addresses must be provided, as it is necessary for assessing sales tax accurately.

> Tax automatically calculates the taxes on all purchases and subscriptions accumulated during a Checkout session

## Pay a Past Due Invoice:

To pay a past due invoice, Stripe provides a "Pay Now" button to facilitate immediate payment.
