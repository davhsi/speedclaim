"""Generate the three SpeedClaim fictional demo product brochures.

The values in this file intentionally mirror the repeatable EF seed in
``SpeedClaimDbContext``. These PDFs are demo material only - they are not a
quote, product filing, or an offer of insurance.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import (
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output" / "pdf"

INK = colors.HexColor("#17233A")
BLUE = colors.HexColor("#176B87")
GOLD = colors.HexColor("#F4A61D")
MIST = colors.HexColor("#F3F7FA")
LINE = colors.HexColor("#D9E2EA")
MUTED = colors.HexColor("#5D6D7E")


@dataclass(frozen=True)
class Brochure:
    filename: str
    domain: str
    product_name: str
    uin: str
    description: str
    facts: list[tuple[str, str]]
    rate_heading: str
    rate_data: list[list[str]]
    benefits: list[str]
    exclusions: list[str]
    documents: list[tuple[str, str]]


HEALTH = Brochure(
    filename="demo-health-product-brochure.pdf",
    domain="HEALTH INSURANCE",
    product_name="CareNest Family Shield",
    uin="UIN-HC-DEMO-2026-01",
    description="A fictional family-floater health plan for catalog, quotation, and policy-Q&amp;A testing.",
    facts=[
        ("Entry age", "18-60 years"),
        ("Policy term", "1 year"),
        ("Available cover amounts", "INR 3 lakh, 5 lakh, 10 lakh, 15 lakh"),
        ("Waiting period", "30 days"),
        ("Family floater", "Available for up to 6 members"),
        ("Catalog status", "Active and available for sale"),
    ],
    rate_heading="Illustrative annual premium bands (INR)",
    rate_data=[
        ["Entry age", "3 lakh", "5 lakh", "10 lakh", "15 lakh"],
        ["18-30", "4,800", "6,800", "9,900", "13,400"],
        ["31-40", "5,900", "8,300", "12,100", "16,700"],
        ["41-50", "8,300", "11,900", "17,600", "24,300"],
        ["51-60", "12,500", "17,800", "26,400", "36,900"],
    ],
    benefits=[
        "In-patient hospitalisation after a 24-hour admission.",
        "Listed day-care procedures and eligible pre/post-hospitalisation expenses.",
        "Cashless treatment at eligible network hospitals.",
        "Annual preventive health check after the first renewal.",
    ],
    exclusions=[
        "Pre-existing conditions during the first 36 months.",
        "Cosmetic, experimental, or non-medically necessary procedures.",
        "Self-inflicted injury, substance misuse, and war-related events.",
        "Maternity benefits unless an optional rider is selected.",
    ],
    documents=[
        ("Government photo ID", "Required"),
        ("Age proof", "Required"),
        ("Medical declaration", "Required"),
        ("Previous policy copy", "When porting"),
    ],
)

LIFE = Brochure(
    filename="demo-life-product-brochure.pdf",
    domain="LIFE INSURANCE",
    product_name="Horizon Term Protect",
    uin="UIN-LI-DEMO-2026-01",
    description="A fictional pure-protection term plan for catalog, quotation, and policy-Q&amp;A testing.",
    facts=[
        ("Entry age", "18-55 years"),
        ("Policy term", "10-30 years"),
        ("Sum assured range", "INR 25 lakh to INR 1 crore"),
        ("Cover increment", "INR 25 lakh"),
        ("Waiting period", "None"),
        ("Catalog status", "Active and available for sale"),
    ],
    rate_heading="Illustrative annual premium bands (INR)",
    rate_data=[
        ["Entry age", "25 lakh", "50 lakh", "75 lakh", "1 crore"],
        ["18-30", "3,200", "5,800", "8,400", "10,900"],
        ["31-40", "4,700", "8,700", "12,500", "16,200"],
        ["41-50", "8,900", "16,600", "23,900", "31,200"],
        ["51-55", "14,800", "27,700", "39,800", "52,100"],
    ],
    benefits=[
        "Lump-sum death benefit during the active policy term.",
        "Optional accidental-death benefit rider for test scenarios.",
        "Optional critical-illness acceleration rider for test scenarios.",
        "Nominee update support and annual policy statement.",
    ],
    exclusions=[
        "Suicide exclusion during the first 12 months, subject to fictional terms.",
        "Claims arising from material non-disclosure in the proposal.",
        "Rider benefits not selected at policy inception.",
        "No maturity benefit for this pure-protection plan.",
    ],
    documents=[
        ("Government photo ID", "Required"),
        ("PAN or tax ID", "Required"),
        ("Income proof", "Above INR 50 lakh cover"),
        ("Medical tests", "When requested"),
    ],
)

MOTOR = Brochure(
    filename="demo-motor-product-brochure.pdf",
    domain="MOTOR INSURANCE",
    product_name="DriveSure Comprehensive",
    uin="UIN-MO-DEMO-2026-01",
    description="A fictional private-car comprehensive plan for catalog, quotation, and claims-flow testing.",
    facts=[
        ("Vehicle type", "Private car"),
        ("Driver entry age", "18-75 years"),
        ("Policy term", "1 year"),
        ("IDV guardrails", "INR 1 lakh to INR 20 lakh"),
        ("Waiting period", "None"),
        ("Catalog status", "Active and available for sale"),
    ],
    rate_heading="Illustrative annual premium bands (INR)",
    rate_data=[
        ["Vehicle IDV band", "Annual premium"],
        ["INR 1 lakh - 5 lakh", "7,200"],
        ["INR 5,00,001 - 10 lakh", "10,900"],
        ["INR 10,00,001 - 15 lakh", "15,700"],
        ["INR 15,00,001 - 20 lakh", "22,600"],
    ],
    benefits=[
        "Accidental damage to the insured private car.",
        "Theft cover up to the fictional IDV.",
        "Third-party liability as required by the selected cover.",
        "Towing assistance up to INR 1,500 per event.",
    ],
    exclusions=[
        "Normal wear and tear, mechanical or electrical breakdown.",
        "Driving without a valid licence or under the influence of alcohol.",
        "Use outside the declared purpose or geographical area.",
        "Consequential loss and consumables unless an add-on is selected.",
    ],
    documents=[
        ("Registration certificate", "Required"),
        ("Valid driving licence", "Required"),
        ("Existing policy copy", "When applicable"),
        ("Vehicle photographs", "When inspection is requested"),
    ],
)


def styles() -> dict[str, ParagraphStyle]:
    base = getSampleStyleSheet()
    return {
        "kicker": ParagraphStyle("kicker", parent=base["Normal"], fontName="Helvetica-Bold", fontSize=9, leading=12, textColor=GOLD, spaceAfter=5),
        "title": ParagraphStyle("title", parent=base["Title"], fontName="Helvetica-Bold", fontSize=25, leading=29, textColor=INK, spaceAfter=7),
        "body": ParagraphStyle("body", parent=base["BodyText"], fontName="Helvetica", fontSize=10, leading=14, textColor=MUTED),
        "section": ParagraphStyle("section", parent=base["Heading2"], fontName="Helvetica-Bold", fontSize=13, leading=16, textColor=INK, spaceBefore=12, spaceAfter=7),
        "small": ParagraphStyle("small", parent=base["BodyText"], fontName="Helvetica", fontSize=8.5, leading=11, textColor=MUTED),
        "cell": ParagraphStyle("cell", parent=base["BodyText"], fontName="Helvetica", fontSize=8.5, leading=11, textColor=INK),
        "cell_head": ParagraphStyle("cell_head", parent=base["BodyText"], fontName="Helvetica-Bold", fontSize=8.5, leading=11, textColor=colors.white),
    }


def bullet(items: list[str], style: ParagraphStyle) -> list[Paragraph]:
    return [Paragraph(f"- {item}", style) for item in items]


def fact_table(facts: list[tuple[str, str]], s: dict[str, ParagraphStyle]) -> Table:
    rows = [[Paragraph(f"<b>{key}</b>", s["cell"]), Paragraph(value, s["cell"])] for key, value in facts]
    table = Table(rows, colWidths=[48 * mm, 112 * mm], hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), MIST),
        ("BOX", (0, 0), (-1, -1), 0.6, LINE),
        ("INNERGRID", (0, 0), (-1, -1), 0.4, LINE),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
        ("TOPPADDING", (0, 0), (-1, -1), 7),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
    ]))
    return table


def rate_table(rows: list[list[str]], s: dict[str, ParagraphStyle]) -> Table:
    rendered = [[Paragraph(value, s["cell_head"] if row_index == 0 else s["cell"]) for value in row] for row_index, row in enumerate(rows)]
    width = 160 * mm
    columns = len(rows[0])
    table = Table(rendered, colWidths=[width / columns] * columns, repeatRows=1, hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), BLUE),
        ("BACKGROUND", (0, 1), (-1, -1), colors.white),
        ("BOX", (0, 0), (-1, -1), 0.6, LINE),
        ("INNERGRID", (0, 0), (-1, -1), 0.4, LINE),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("ALIGN", (1, 1), (-1, -1), "RIGHT"),
        ("LEFTPADDING", (0, 0), (-1, -1), 7),
        ("RIGHTPADDING", (0, 0), (-1, -1), 7),
        ("TOPPADDING", (0, 0), (-1, -1), 7),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
    ]))
    return table


def document_table(documents: list[tuple[str, str]], s: dict[str, ParagraphStyle]) -> Table:
    rows = [[Paragraph("Document", s["cell_head"]), Paragraph("When to provide it", s["cell_head"])]]
    rows += [[Paragraph(label, s["cell"]), Paragraph(condition, s["cell"])] for label, condition in documents]
    table = Table(rows, colWidths=[78 * mm, 82 * mm], repeatRows=1, hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), BLUE),
        ("BOX", (0, 0), (-1, -1), 0.6, LINE),
        ("INNERGRID", (0, 0), (-1, -1), 0.4, LINE),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
        ("TOPPADDING", (0, 0), (-1, -1), 7),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
    ]))
    return table


def footer(canvas, doc) -> None:
    canvas.saveState()
    canvas.setStrokeColor(LINE)
    canvas.line(18 * mm, 14 * mm, 192 * mm, 14 * mm)
    canvas.setFillColor(MUTED)
    canvas.setFont("Helvetica", 7.5)
    canvas.drawString(18 * mm, 9 * mm, "SpeedClaim demo brochure - fictional training data only")
    canvas.drawRightString(192 * mm, 9 * mm, f"Page {doc.page}")
    canvas.restoreState()


def build(brochure: Brochure) -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    doc = SimpleDocTemplate(
        str(OUTPUT / brochure.filename), pagesize=A4,
        leftMargin=18 * mm, rightMargin=18 * mm, topMargin=18 * mm, bottomMargin=20 * mm,
        title=f"SpeedClaim - {brochure.product_name}", author="SpeedClaim",
    )
    s = styles()
    story = [
        Paragraph("SPEEDCLAIM PRODUCT CATALOG", s["kicker"]),
        Paragraph(brochure.domain, s["kicker"]),
        Paragraph(brochure.product_name, s["title"]),
        Paragraph(brochure.description, s["body"]),
        Spacer(1, 10),
        Paragraph(f"<b>Catalog UIN:</b> {brochure.uin}", s["small"]),
        Paragraph("Product configuration", s["section"]),
        fact_table(brochure.facts, s),
        Paragraph(brochure.rate_heading, s["section"]),
        rate_table(brochure.rate_data, s),
        Spacer(1, 7),
        Paragraph("These figures are fictional test data. They are not quotes, underwriting decisions, or an offer of insurance.", s["small"]),
        PageBreak(),
        Paragraph("Plan features", s["section"]),
        *bullet(brochure.benefits, s["body"]),
        Paragraph("Key exclusions and limitations", s["section"]),
        *bullet(brochure.exclusions, s["body"]),
        Paragraph("Product proposal documents", s["section"]),
        document_table(brochure.documents, s),
        Spacer(1, 10),
        Paragraph("This document exists solely for SpeedClaim demo, catalog, brochure-ingestion, and policy-Q&amp;A testing.", s["small"]),
    ]
    doc.build(story, onFirstPage=footer, onLaterPages=footer)


if __name__ == "__main__":
    for brochure in (HEALTH, LIFE, MOTOR):
        build(brochure)
