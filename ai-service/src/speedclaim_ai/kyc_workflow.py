"""Deterministic, privacy-safe KYC workflow guidance shared by every Speedy surface."""

from speedclaim_ai.contracts.speedy import SpeedyAccountSnapshot


def kyc_is_under_review_or_approved(account: SpeedyAccountSnapshot) -> bool:
    kyc = account.kyc
    return bool(
        kyc
        and kyc.aadhaar_uploaded
        and kyc.pan_uploaded
        and kyc.status in {"Pending", "UnderReview", "Approved"}
    )


def kyc_status_answer(account: SpeedyAccountSnapshot) -> str | None:
    """Return a factual KYC status response, never identity values or document paths."""
    if not account.is_authenticated or account.kyc is None:
        return None

    kyc = account.kyc
    if kyc.aadhaar_uploaded and kyc.pan_uploaded and kyc.status in {"Pending", "UnderReview"}:
        return (
            "⏳ **KYC status: under review**\n\n"
            "Your Aadhaar and PAN have already been submitted and are awaiting underwriter review. "
            "You do not need to submit them again. 📬 We will notify you in SpeedClaim and by email once the review is complete."
        )
    if kyc.aadhaar_uploaded and kyc.pan_uploaded and kyc.status == "Approved":
        return "✅ **KYC status: verified**\n\nYour KYC is verified. You do not need to submit any documents again."
    if kyc.status == "Rejected":
        return (
            "⚠️ **KYC status: action needed**\n\n"
            "Your KYC needs updated documents before it can be reviewed again. Please re-upload Aadhaar and PAN in their labelled slots."
        )

    missing = []
    if not kyc.aadhaar_uploaded:
        missing.append("Aadhaar")
    if not kyc.pan_uploaded:
        missing.append("PAN")
    label = " and ".join(missing)
    plural = "s" if len(missing) > 1 else ""
    return f"📋 **KYC status: incomplete**\n\nPlease attach the missing {label} document{plural} in the labelled slot{plural}."
