# Stripe Payment Testing Guide (Local CLI)

This guide documents the end-to-end process for simulating a successful Stripe payment without a frontend UI. This process is required to trigger the webhook that flips a `Pending` policy to `Active`.

## Prerequisites
You must have the official [Stripe CLI](https://stripe.com/docs/stripe-cli) installed and authenticated.
```bash
brew install stripe/stripe-cli/stripe
stripe login
```

## Step 1: Get the Pending Schedule
First, fetch the premium schedule for the existing `Pending` policy and copy the first payable schedule `id`.

**Request:** `GET {{baseUrl}}/api/v1/payments/schedule/{policyId}`
**Headers:** `Authorization: Bearer <JWT_TOKEN>`

## Step 2: Create the Payment Intent
Create a PaymentIntent for the copied schedule. The backend calculates the amount from the schedule.

**Request:** `POST {{baseUrl}}/api/v1/payments/pay/{scheduleId}`
**Headers:** `Authorization: Bearer <JWT_TOKEN>`
**Payload:**
```json
{
  "policyId": "INSERT_PENDING_POLICY_ID_HERE"
}
```

**Response Extraction:**
Extract the `paymentIntentId` from the JSON response (it usually begins with `pi_...`).

## Step 3: Start the Webhook Listener
Because Stripe needs to send a secure `POST` request to your backend, you must open a tunnel using the CLI.

Open a terminal and run:
```bash
stripe listen --forward-to http://localhost:5062/api/v1/payments/webhook
```
*Keep this terminal window running in the background!*

Copy the `whsec_...` value from this command into `Stripe:WebhookSecret` in `appsettings.Development.json`, then restart the API if the value changed.

## Step 4: Simulate the Payment
Instead of typing credit card numbers into a UI, use the Stripe CLI to force the payment to succeed using a test Visa card.

Open a **new terminal window** and run:
```bash
stripe payment_intents confirm <YOUR_PAYMENT_INTENT_ID> \
  --payment-method pm_card_visa \
  --return-url "http://localhost:5062/success"
```

> [!NOTE]
> The `--return-url` flag is mandatory when confirming via CLI to satisfy Stripe's Automatic Payment Methods configuration, even though we aren't actually redirecting a browser.

## Step 5: Verify Activation
1. Check your `stripe listen` terminal. You should see a `payment_intent.succeeded` event printed out.
2. The backend (`FinanceService.cs`) will reconcile the payment and update the database.
3. Check the policy status via **`GET {{baseUrl}}/api/v1/policies/my`** or the customer policy page. The status should now be `Active` instead of `Pending`.
4. The system will have also fired an automated "Policy Activated" email.
