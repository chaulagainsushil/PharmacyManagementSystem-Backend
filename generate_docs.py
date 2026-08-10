#!/usr/bin/env python3
"""Generate PDF documentation for the PMS (Pharmacy Management System) project."""

from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    HRFlowable, PageBreak, KeepTogether
)
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_JUSTIFY
from datetime import datetime

# ── Output path ────────────────────────────────────────────────────────────────
OUTPUT_PATH = "/home/sushilchaulagain/RiderProjects/PMS/PMS_Documentation.pdf"

# ── Colour palette ─────────────────────────────────────────────────────────────
C_PRIMARY   = colors.HexColor("#1a5276")   # dark navy
C_ACCENT    = colors.HexColor("#2e86c1")   # medium blue
C_LIGHT     = colors.HexColor("#d6eaf8")   # pale blue
C_HEADER_BG = colors.HexColor("#1a5276")
C_ROW_ALT   = colors.HexColor("#eaf4fb")
C_WHITE     = colors.white
C_BLACK     = colors.black
C_GRAY      = colors.HexColor("#566573")
C_GREEN     = colors.HexColor("#1e8449")
C_ORANGE    = colors.HexColor("#ca6f1e")

# ── Styles ─────────────────────────────────────────────────────────────────────
base_styles = getSampleStyleSheet()

def make_styles():
    s = {}

    s["title"] = ParagraphStyle(
        "DocTitle",
        fontName="Helvetica-Bold",
        fontSize=28,
        textColor=C_PRIMARY,
        alignment=TA_CENTER,
        spaceAfter=6,
    )
    s["subtitle"] = ParagraphStyle(
        "DocSubtitle",
        fontName="Helvetica",
        fontSize=14,
        textColor=C_ACCENT,
        alignment=TA_CENTER,
        spaceAfter=4,
    )
    s["meta"] = ParagraphStyle(
        "DocMeta",
        fontName="Helvetica",
        fontSize=10,
        textColor=C_GRAY,
        alignment=TA_CENTER,
        spaceAfter=2,
    )
    s["h1"] = ParagraphStyle(
        "H1",
        fontName="Helvetica-Bold",
        fontSize=18,
        textColor=C_WHITE,
        spaceBefore=14,
        spaceAfter=6,
        leftIndent=0,
        leading=22,
    )
    s["h2"] = ParagraphStyle(
        "H2",
        fontName="Helvetica-Bold",
        fontSize=14,
        textColor=C_PRIMARY,
        spaceBefore=12,
        spaceAfter=4,
        leading=18,
    )
    s["h3"] = ParagraphStyle(
        "H3",
        fontName="Helvetica-Bold",
        fontSize=11,
        textColor=C_ACCENT,
        spaceBefore=8,
        spaceAfter=3,
        leading=14,
    )
    s["body"] = ParagraphStyle(
        "Body",
        fontName="Helvetica",
        fontSize=10,
        textColor=C_BLACK,
        spaceBefore=2,
        spaceAfter=4,
        leading=14,
        alignment=TA_JUSTIFY,
    )
    s["bullet"] = ParagraphStyle(
        "Bullet",
        fontName="Helvetica",
        fontSize=10,
        textColor=C_BLACK,
        leftIndent=16,
        bulletIndent=6,
        spaceAfter=2,
        leading=14,
    )
    s["code"] = ParagraphStyle(
        "Code",
        fontName="Courier",
        fontSize=9,
        textColor=colors.HexColor("#1b2631"),
        backColor=colors.HexColor("#f2f3f4"),
        leftIndent=12,
        rightIndent=12,
        spaceAfter=6,
        leading=13,
    )
    s["table_header"] = ParagraphStyle(
        "TableHeader",
        fontName="Helvetica-Bold",
        fontSize=9,
        textColor=C_WHITE,
        alignment=TA_CENTER,
        leading=12,
    )
    s["table_cell"] = ParagraphStyle(
        "TableCell",
        fontName="Helvetica",
        fontSize=9,
        textColor=C_BLACK,
        leading=12,
    )
    s["table_cell_code"] = ParagraphStyle(
        "TableCellCode",
        fontName="Courier",
        fontSize=8,
        textColor=colors.HexColor("#1a5276"),
        leading=11,
    )
    s["note"] = ParagraphStyle(
        "Note",
        fontName="Helvetica-Oblique",
        fontSize=9,
        textColor=C_GRAY,
        leftIndent=12,
        spaceAfter=4,
        leading=13,
    )
    return s

ST = make_styles()

# ── Helper builders ────────────────────────────────────────────────────────────

def h1_block(text):
    """Dark-background H1 banner."""
    tbl = Table([[Paragraph(text, ST["h1"])]], colWidths=[17.5 * cm])
    tbl.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), C_PRIMARY),
        ("TOPPADDING",    (0, 0), (-1, -1), 8),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 8),
        ("LEFTPADDING",   (0, 0), (-1, -1), 12),
        ("RIGHTPADDING",  (0, 0), (-1, -1), 12),
        ("ROUNDEDCORNERS", [4]),
    ]))
    return tbl


def hr():
    return HRFlowable(width="100%", thickness=1, color=C_ACCENT, spaceAfter=6, spaceBefore=4)


def bullet(text):
    return Paragraph(f"• {text}", ST["bullet"])


def code_block(text):
    return Paragraph(text, ST["code"])


def make_table(headers, rows, col_widths=None):
    """Build a styled two-tone table."""
    header_row = [Paragraph(h, ST["table_header"]) for h in headers]
    data = [header_row]
    for i, row in enumerate(rows):
        data.append([Paragraph(str(cell), ST["table_cell"]) for cell in row])

    if col_widths is None:
        col_widths = [17.5 * cm / len(headers)] * len(headers)

    tbl = Table(data, colWidths=col_widths, repeatRows=1)
    style = [
        ("BACKGROUND",    (0, 0), (-1, 0),  C_HEADER_BG),
        ("TEXTCOLOR",     (0, 0), (-1, 0),  C_WHITE),
        ("FONTNAME",      (0, 0), (-1, 0),  "Helvetica-Bold"),
        ("FONTSIZE",      (0, 0), (-1, 0),  9),
        ("ALIGN",         (0, 0), (-1, 0),  "CENTER"),
        ("TOPPADDING",    (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("LEFTPADDING",   (0, 0), (-1, -1), 6),
        ("RIGHTPADDING",  (0, 0), (-1, -1), 6),
        ("GRID",          (0, 0), (-1, -1), 0.4, colors.HexColor("#aab7b8")),
        ("VALIGN",        (0, 0), (-1, -1), "TOP"),
    ]
    for i in range(1, len(data)):
        bg = C_ROW_ALT if i % 2 == 0 else C_WHITE
        style.append(("BACKGROUND", (0, i), (-1, i), bg))
    tbl.setStyle(TableStyle(style))
    return tbl


def endpoint_table(rows):
    """Table for API endpoints: Method | Path | Auth | Description."""
    headers = ["Method", "Path", "Auth / Roles", "Description"]
    col_widths = [1.8*cm, 6.2*cm, 4.2*cm, 5.3*cm]
    header_row = [Paragraph(h, ST["table_header"]) for h in headers]
    data = [header_row]
    method_colors = {
        "GET":    colors.HexColor("#1e8449"),
        "POST":   colors.HexColor("#1a5276"),
        "PUT":    colors.HexColor("#7d6608"),
        "PATCH":  colors.HexColor("#6e2f7e"),
        "DELETE": colors.HexColor("#922b21"),
    }
    for i, (method, path, auth, desc) in enumerate(rows):
        mc = method_colors.get(method, C_BLACK)
        method_p = Paragraph(f'<font color="{mc.hexval()}" name="Helvetica-Bold"><b>{method}</b></font>', ST["table_cell"])
        path_p   = Paragraph(path, ST["table_cell_code"])
        auth_p   = Paragraph(auth, ST["table_cell"])
        desc_p   = Paragraph(desc, ST["table_cell"])
        data.append([method_p, path_p, auth_p, desc_p])

    tbl = Table(data, colWidths=col_widths, repeatRows=1)
    style = [
        ("BACKGROUND",    (0, 0), (-1, 0),  C_HEADER_BG),
        ("TOPPADDING",    (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("LEFTPADDING",   (0, 0), (-1, -1), 6),
        ("RIGHTPADDING",  (0, 0), (-1, -1), 6),
        ("GRID",          (0, 0), (-1, -1), 0.4, colors.HexColor("#aab7b8")),
        ("VALIGN",        (0, 0), (-1, -1), "TOP"),
    ]
    for i in range(1, len(data)):
        bg = C_ROW_ALT if i % 2 == 0 else C_WHITE
        style.append(("BACKGROUND", (0, i), (-1, i), bg))
    tbl.setStyle(TableStyle(style))
    return tbl


# ── Section builders ───────────────────────────────────────────────────────────

def build_cover(story):
    story.append(Spacer(1, 3*cm))
    story.append(Paragraph("PMS", ST["title"]))
    story.append(Paragraph("Pharmacy Management System", ST["subtitle"]))
    story.append(Spacer(1, 0.3*cm))
    story.append(hr())
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph("Technical Documentation", ST["subtitle"]))
    story.append(Spacer(1, 0.5*cm))
    story.append(Paragraph(f"Generated: {datetime.now().strftime('%B %d, %Y')}", ST["meta"]))
    story.append(Paragraph("Platform: ASP.NET Core 10 · PostgreSQL · Docker", ST["meta"]))
    story.append(Paragraph("Architecture: Clean Architecture · Repository Pattern · JWT Auth", ST["meta"]))
    story.append(Spacer(1, 2*cm))

    # summary box
    summary_data = [[
        Paragraph("5\nProjects", ST["subtitle"]),
        Paragraph("13\nControllers", ST["subtitle"]),
        Paragraph("40+\nAPI Endpoints", ST["subtitle"]),
        Paragraph("4\nUser Roles", ST["subtitle"]),
    ]]
    summary_tbl = Table(summary_data, colWidths=[4.375*cm]*4)
    summary_tbl.setStyle(TableStyle([
        ("BACKGROUND",    (0, 0), (-1, -1), C_LIGHT),
        ("ALIGN",         (0, 0), (-1, -1), "CENTER"),
        ("TOPPADDING",    (0, 0), (-1, -1), 14),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 14),
        ("GRID",          (0, 0), (-1, -1), 1, C_ACCENT),
        ("ROUNDEDCORNERS", [4]),
    ]))
    story.append(summary_tbl)
    story.append(PageBreak())


def build_toc(story):
    story.append(h1_block("Table of Contents"))
    story.append(Spacer(1, 0.4*cm))
    toc_items = [
        ("1", "Project Overview"),
        ("2", "Solution Architecture"),
        ("3", "Technology Stack"),
        ("4", "Project Structure"),
        ("5", "Data Models"),
        ("6", "User Roles & Permissions"),
        ("7", "API Reference"),
        ("  7.1", "Authentication"),
        ("  7.2", "Categories"),
        ("  7.3", "Manufacturers"),
        ("  7.4", "Suppliers"),
        ("  7.5", "Customers"),
        ("  7.6", "Units of Measure (UoM)"),
        ("  7.7", "Medicines"),
        ("  7.8", "Medicine Units"),
        ("  7.9", "Medicine Batches"),
        ("  7.10", "Sales"),
        ("  7.11", "Disposals"),
        ("  7.12", "Reports"),
        ("  7.13", "Pharmacy Profile"),
        ("8", "Database Schema"),
        ("9", "Authentication & Security"),
        ("10", "Configuration & Deployment"),
        ("11", "Docker & Container Setup"),
    ]
    toc_data = [[Paragraph(f"<b>{n}</b>", ST["table_cell"]), Paragraph(t, ST["table_cell"])]
                for n, t in toc_items]
    toc_tbl = Table(toc_data, colWidths=[2*cm, 15.5*cm])
    toc_tbl.setStyle(TableStyle([
        ("TOPPADDING",    (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("LEFTPADDING",   (0, 0), (-1, -1), 6),
        ("LINEBELOW",     (0, 0), (-1, -1), 0.3, colors.HexColor("#d5d8dc")),
    ]))
    story.append(toc_tbl)
    story.append(PageBreak())


def build_overview(story):
    story.append(h1_block("1. Project Overview"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "PMS (Pharmacy Management System) is a full-featured RESTful backend API built with "
        "ASP.NET Core 10. It is designed to manage the complete day-to-day operations of a "
        "pharmacy: medicine catalogue, stock batches, sales at the point-of-sale (POS), "
        "expired-medicine disposal, reporting and the pharmacy's own identity profile.",
        ST["body"]))
    story.append(Spacer(1, 0.2*cm))

    story.append(Paragraph("<b>Key capabilities:</b>", ST["body"]))
    for item in [
        "Medicine catalogue with categories, manufacturers, generic names and prescription flags.",
        "Multi-unit support per medicine (Tablet, Strip, Box, Bottle …) with configurable conversion factors and per-unit pricing.",
        "Batch-level inventory tracking with expiry dates; FEFO (First-Expiry First-Out) stock deduction on every sale.",
        "Near-expiry alerts — query batches expiring within a configurable number of days.",
        "Sales module with invoice generation, discount support (% or fixed), and multiple payment modes (Cash, Card, Online, Credit).",
        "Expired-medicine disposal workflow with numbered disposal records and transactional stock deduction.",
        "Rich reporting: dashboard summary, today/month sales, top medicines/manufacturers/suppliers, monthly trends, low-stock alerts, and stock-consumption reports.",
        "Pharmacy profile management including logo and authorised-signature image uploads.",
        "JWT-based authentication with four role tiers; self-registration always receives the Pharmacist role.",
        "Docker-ready with a two-stage Dockerfile and docker-compose with health checks.",
        "PostgreSQL on Neon (cloud-hosted) with Entity Framework Core migrations.",
    ]:
        story.append(bullet(item))
    story.append(Spacer(1, 0.3*cm))


def build_architecture(story):
    story.append(h1_block("2. Solution Architecture"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "PMS follows a <b>Clean Architecture</b> / layered approach, keeping domain models "
        "and business rules decoupled from infrastructure concerns.",
        ST["body"]))
    story.append(Spacer(1, 0.2*cm))

    layers = [
        ["Layer / Project", "Responsibility"],
        ["PMS  (Web API)", "HTTP controllers, middleware pipeline, DI wiring, static-file serving."],
        ["PMS.Application", "Business logic: Services + Repository interfaces (contracts). No EF or HTTP dependencies."],
        ["PMS.Repository", "EF Core DbContext, concrete Repository implementations, Migrations."],
        ["PMS.Models", "Domain entity classes and Enums. Zero external dependencies."],
        ["PMS.Common", "Shared DTOs, AppRoles constants. Referenced by all layers."],
    ]
    tbl = make_table(layers[0], layers[1:], col_widths=[4.5*cm, 13*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "The dependency rule flows inward: Web API → Application → Repository → Models. "
        "Controllers never access the DbContext directly; they only call Service interfaces "
        "injected via DI.",
        ST["note"]))
    story.append(Spacer(1, 0.3*cm))

    story.append(Paragraph("<b>Design patterns used:</b>", ST["body"]))
    for p in [
        "Repository Pattern — every data-access concern lives in a typed repository class.",
        "Service Layer — business rules and orchestration are in Service classes; controllers stay thin.",
        "Dependency Injection — all services and repositories registered in DependencyHandler/.",
        "DTO mapping — input/output contracts are defined in PMS.Common/DTOs; entities never leave the service layer.",
        "FEFO (First-Expiry First-Out) — SaleService sorts batches by ExpiryDate ascending to deduct oldest stock first.",
    ]:
        story.append(bullet(p))
    story.append(Spacer(1, 0.3*cm))


def build_tech_stack(story):
    story.append(h1_block("3. Technology Stack"))
    story.append(Spacer(1, 0.3*cm))
    rows = [
        ["ASP.NET Core", "10.0", "Web framework & HTTP pipeline"],
        ["Entity Framework Core", "Latest (EF10)", "ORM and migrations"],
        ["ASP.NET Core Identity", "10.0", "User/role management"],
        ["PostgreSQL (Neon)", "—", "Cloud-hosted relational database"],
        ["Npgsql", "Latest", "PostgreSQL EF Core provider"],
        ["JWT Bearer Auth", "—", "Stateless token authentication"],
        ["ReportLab / PDF gen", "In-app", "Prescription PDF output (wwwroot/prescriptions)"],
        ["Docker", "—", "Containerisation (two-stage build)"],
        ["docker-compose", "—", "Local orchestration with health check"],
        [".NET SDK / Runtime", "10.0", "Build (SDK) and production runtime (ASP.NET)"],
    ]
    tbl = make_table(["Technology", "Version", "Purpose"], rows,
                     col_widths=[5*cm, 3*cm, 9.5*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.3*cm))


def build_project_structure(story):
    story.append(h1_block("4. Project Structure"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph("The solution contains five .NET projects:", ST["body"]))
    story.append(Spacer(1, 0.2*cm))

    structure = """PMS/  (solution root)
├── PMS/                        ← Web API entry point
│   ├── Controllers/            ← 13 API controllers
│   ├── DependencyHandler/      ← DI registration extensions
│   ├── Properties/launchSettings.json
│   ├── appsettings.json / .Development / .Production
│   ├── Program.cs              ← App bootstrap & middleware pipeline
│   └── wwwroot/
│       ├── prescriptions/      ← Uploaded prescription files
│       └── pharmacy/
│           ├── logos/          ← Pharmacy logo images
│           └── signatures/     ← Authorised signature images
├── PMS.Application/
│   └── Application/
│       ├── Interface/
│       │   ├── Service/        ← IXxxService interfaces
│       │   └── Repository/     ← IXxxRepository interfaces
│       ├── Service/            ← Business logic implementations
│       └── Repository/        ← Data-access implementations (EF)
├── PMS.Repository/
│   ├── Data/ApplicationDbContext.cs
│   └── Migrations/
├── PMS.Models/
│   ├── Models/                 ← Domain entity classes
│   └── Enums/                  ← PaymentMode, SaleUnitType
├── PMS.Common/
│   ├── AppRoles.cs
│   └── DTOs/                   ← Input/output DTOs for every module
├── docker-compose.yml
├── Dockerfile
├── .env.example
└── PMS.sln"""
    story.append(code_block(structure))
    story.append(Spacer(1, 0.2*cm))


def build_data_models(story):
    story.append(h1_block("5. Data Models"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "All entities live in <b>PMS.Models</b>. Below is a summary of each entity and its key fields.",
        ST["body"]))
    story.append(Spacer(1, 0.2*cm))

    models = [
        ("Medicine", [
            ("MedicineId", "int PK"),
            ("Name", "string (300) — required"),
            ("GenericName", "string? (300)"),
            ("CategoryId", "int? FK → Category"),
            ("ManufacturerId", "int? FK → Manufacturer"),
            ("TabletsPerStrip", "int — base packaging"),
            ("StrapsPerBox", "int — strips per box"),
            ("StripPrice / TabletPrice", "decimal — legacy pricing"),
            ("ReorderLevel", "int — low-stock threshold"),
            ("RequiresPrescription", "bool"),
            ("IsActive", "bool"),
        ]),
        ("MedicineBatch", [
            ("BatchId", "int PK"),
            ("MedicineId", "int FK → Medicine"),
            ("BatchNumber", "string (100)"),
            ("SupplierId", "int? FK → Supplier"),
            ("ManufactureDate", "DateTime?"),
            ("ExpiryDate", "DateTime"),
            ("QuantityInTablets", "int — stock in base units"),
            ("PurchasePricePerTablet", "decimal"),
            ("ReceivedDate", "DateTime"),
        ]),
        ("UnitOfMeasure", [
            ("UnitOfMeasureId", "int PK"),
            ("Name", "string (100) — unique"),
            ("Symbol", "string (20)"),
            ("IsActive", "bool"),
        ]),
        ("MedicineUnit", [
            ("MedicineUnitId", "int PK"),
            ("MedicineId", "int FK → Medicine"),
            ("UnitOfMeasureId", "int FK → UnitOfMeasure"),
            ("ConversionFactorToBase", "decimal — how many base units per this unit"),
            ("IsBaseUnit", "bool — partial-unique per medicine"),
            ("IsDefault", "bool — default POS unit, partial-unique per medicine"),
            ("IsActive", "bool"),
            ("UnitPrice", "decimal"),
        ]),
        ("Sale", [
            ("SaleId", "int PK"),
            ("InvoiceNumber", "string (50) — unique"),
            ("CustomerId", "int FK → Customer"),
            ("PharmacistId", "int FK → User"),
            ("SaleDate / CreatedAt", "DateTime"),
            ("Subtotal / DiscountPercent / DiscountAmount / TotalAmount", "decimal"),
            ("PaymentMode", "enum: Cash | Card | Online | Credit"),
        ]),
        ("SaleItem", [
            ("SaleItemId", "int PK"),
            ("SaleId", "int FK → Sale"),
            ("MedicineId", "int FK → Medicine"),
            ("BatchId", "int FK → MedicineBatch"),
            ("MedicineUnitId", "int FK → MedicineUnit"),
            ("Quantity", "int — in selected unit"),
            ("QuantityInTablets", "int — in base units"),
            ("UnitPrice / DiscountPercent / DiscountAmount / LineTotal", "decimal"),
        ]),
        ("Customer", [
            ("CustomerId", "int PK"),
            ("FullName", "string (200)"),
            ("PhoneNumber", "string (20) — unique"),
            ("Address", "string?"),
        ]),
        ("ExpiredMedicineDisposal", [
            ("DisposalId", "int PK"),
            ("DisposalNumber", "string — e.g. DSP-20260801-0001"),
            ("PharmacistId", "int FK → User"),
            ("DisposalDate / CreatedAt", "DateTime"),
        ]),
        ("ExpiredMedicineDisposalItem", [
            ("DisposalItemId", "int PK"),
            ("DisposalId", "int FK → Disposal"),
            ("MedicineId", "int FK → Medicine"),
            ("BatchId", "int FK → MedicineBatch"),
            ("QuantityInTablets", "int"),
            ("Reason", "string (500)"),
        ]),
        ("PharmacyProfile", [
            ("PharmacyProfileId", "int PK"),
            ("PharmacyName", "string (300)"),
            ("LogoUrl / SignatureUrl", "string? — relative file URLs"),
            ("PanNumber / VatNumber", "string? (20)"),
            ("Location", "string? (500)"),
            ("CreatedAt / UpdatedAt", "DateTime"),
        ]),
        ("Manufacturer / Category / Supplier", [
            ("ManufacturerId / CategoryId / SupplierId", "int PK"),
            ("Name", "string — required"),
            ("Phone / Address", "string? (Supplier only)"),
        ]),
    ]

    for model_name, fields in models:
        story.append(Paragraph(model_name, ST["h3"]))
        rows = [[f, t] for f, t in fields]
        tbl = make_table(["Field", "Type / Notes"], rows, col_widths=[6*cm, 11.5*cm])
        story.append(tbl)
        story.append(Spacer(1, 0.3*cm))


def build_roles(story):
    story.append(h1_block("6. User Roles & Permissions"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "PMS uses ASP.NET Core Identity with four roles. Roles are seeded automatically "
        "on startup via <b>SeedRolesAsync()</b>. Self-registered users always receive the "
        "<b>Pharmacist</b> role; only a SystemAdmin can assign other roles.",
        ST["body"]))
    story.append(Spacer(1, 0.3*cm))

    rows = [
        ["SystemAdmin",    "Full access to everything including user management and role assignment."],
        ["PharmacyAdmin",  "Manages pharmacy settings, medicines, categories, manufacturers, suppliers. Cannot assign roles."],
        ["Pharmacist",     "Processes sales, manages batches, performs expired-medicine disposal. Cannot manage master data."],
        ["Receptionist",   "Can create sales and manage customers only. Read-only for everything else."],
    ]
    tbl = make_table(["Role", "Capabilities"], rows, col_widths=[4*cm, 13.5*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.3*cm))

    story.append(Paragraph("<b>Permission matrix by operation type:</b>", ST["body"]))
    story.append(Spacer(1, 0.15*cm))
    matrix = [
        ["Operation",              "SystemAdmin", "PharmacyAdmin", "Pharmacist", "Receptionist"],
        ["Read (GET) most data",   "✓", "✓", "✓", "✓"],
        ["Create/Update Medicine", "✓", "✓", "—", "—"],
        ["Create/Update Batch",    "✓", "✓", "✓", "—"],
        ["Create Sale",            "✓", "✓", "✓", "✓"],
        ["Disposal",               "✓", "✓", "✓", "—"],
        ["Pharmacy Profile",       "✓", "✓", "—", "—"],
        ["Delete Pharmacy Profile","✓", "—", "—", "—"],
        ["Create/Assign Users",    "✓", "—", "—", "—"],
        ["Manage UoM",             "✓", "✓", "—", "—"],
        ["Delete UoM",             "✓", "—", "—", "—"],
    ]
    tbl2 = make_table(matrix[0], matrix[1:], col_widths=[5.5*cm, 3*cm, 3*cm, 3*cm, 3*cm])
    story.append(tbl2)
    story.append(Spacer(1, 0.3*cm))


def build_api_reference(story):
    story.append(h1_block("7. API Reference"))
    story.append(Spacer(1, 0.2*cm))
    story.append(Paragraph(
        "All endpoints are prefixed with <b>/api/[controller]</b>. "
        "Every protected endpoint requires a valid JWT Bearer token in the "
        "<b>Authorization</b> header. Roles listed under 'Auth' are the minimum "
        "required for write operations; read (GET) endpoints accept any authenticated user "
        "unless otherwise noted.",
        ST["body"]))
    story.append(Spacer(1, 0.3*cm))

    # 7.1 Auth
    story.append(Paragraph("7.1  Authentication  —  /api/auth", ST["h2"]))
    story.append(endpoint_table([
        ("POST", "/api/auth/signup",      "Public",            "Register a new account. Role forced to Pharmacist."),
        ("POST", "/api/auth/login",       "Public",            "Authenticate and receive a JWT token."),
        ("POST", "/api/auth/create-user", "SystemAdmin",       "Create a user with any role."),
        ("POST", "/api/auth/assign-role", "SystemAdmin",       "Assign or reassign a role to an existing user."),
    ]))
    story.append(Spacer(1, 0.2*cm))
    story.append(Paragraph("<b>Login response fields:</b> token (JWT), expiry, userId, email, role.", ST["note"]))
    story.append(Spacer(1, 0.3*cm))

    # 7.2 Categories
    story.append(Paragraph("7.2  Categories  —  /api/category", ST["h2"]))
    story.append(endpoint_table([
        ("GET",    "/api/category",     "Any auth",                  "List all categories."),
        ("GET",    "/api/category/{id}","Any auth",                  "Get a category by ID."),
        ("POST",   "/api/category",     "SystemAdmin, PharmacyAdmin","Create a new category."),
        ("PUT",    "/api/category/{id}","SystemAdmin, PharmacyAdmin","Update a category."),
        ("DELETE", "/api/category/{id}","SystemAdmin, PharmacyAdmin","Delete a category."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.3 Manufacturers
    story.append(Paragraph("7.3  Manufacturers  —  /api/manufacturer", ST["h2"]))
    story.append(endpoint_table([
        ("GET",    "/api/manufacturer",     "Any auth",                  "List all manufacturers."),
        ("GET",    "/api/manufacturer/{id}","Any auth",                  "Get a manufacturer by ID."),
        ("POST",   "/api/manufacturer",     "SystemAdmin, PharmacyAdmin","Create a manufacturer."),
        ("PUT",    "/api/manufacturer/{id}","SystemAdmin, PharmacyAdmin","Update a manufacturer."),
        ("DELETE", "/api/manufacturer/{id}","SystemAdmin, PharmacyAdmin","Delete a manufacturer."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.4 Suppliers
    story.append(Paragraph("7.4  Suppliers  —  /api/supplier", ST["h2"]))
    story.append(endpoint_table([
        ("GET",    "/api/supplier",     "Any auth",                  "List all suppliers."),
        ("GET",    "/api/supplier/{id}","Any auth",                  "Get a supplier by ID."),
        ("POST",   "/api/supplier",     "SystemAdmin, PharmacyAdmin","Create a supplier."),
        ("PUT",    "/api/supplier/{id}","SystemAdmin, PharmacyAdmin","Update a supplier."),
        ("DELETE", "/api/supplier/{id}","SystemAdmin, PharmacyAdmin","Delete a supplier."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.5 Customers
    story.append(Paragraph("7.5  Customers  —  /api/customer", ST["h2"]))
    story.append(endpoint_table([
        ("GET",    "/api/customer",     "Any auth",                  "List all customers."),
        ("GET",    "/api/customer/{id}","Any auth",                  "Get a customer by ID."),
        ("POST",   "/api/customer",     "Any auth",                  "Create a customer."),
        ("PUT",    "/api/customer/{id}","Any auth",                  "Update a customer."),
        ("DELETE", "/api/customer/{id}","SystemAdmin, PharmacyAdmin","Delete a customer."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.6 UoM
    story.append(Paragraph("7.6  Units of Measure  —  /api/uom", ST["h2"]))
    story.append(Paragraph(
        "Manages the master list of measurement units (Tablet, Strip, Box, Bottle, etc.). "
        "The <b>includeInactive</b> query param on GET all controls whether soft-deleted units appear.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",    "/api/uom",                    "Any auth",                  "List UoMs. ?includeInactive=true to see all."),
        ("GET",    "/api/uom/{id}",               "Any auth",                  "Get a UoM by ID."),
        ("POST",   "/api/uom",                    "SystemAdmin, PharmacyAdmin","Create a UoM."),
        ("PUT",    "/api/uom/{id}",               "SystemAdmin, PharmacyAdmin","Update a UoM."),
        ("DELETE", "/api/uom/{id}",               "SystemAdmin",               "Delete a UoM (soft delete)."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.7 Medicines
    story.append(Paragraph("7.7  Medicines  —  /api/medicine", ST["h2"]))
    story.append(Paragraph(
        "Core catalogue. The <b>search</b> endpoint performs a case-insensitive, partial-match "
        "server-side filter on Name and GenericName. Bulk creation processes each row independently "
        "and returns per-item results. Prescription file uploads accept JPG/PNG/WebP/PDF up to 10 MB.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",    "/api/medicine",                    "Any auth",                  "List all medicines."),
        ("GET",    "/api/medicine/search?q=",          "Any auth",                  "Search by name or generic name."),
        ("GET",    "/api/medicine/{id}",               "Any auth",                  "Get medicine by ID."),
        ("POST",   "/api/medicine",                    "SystemAdmin, PharmacyAdmin","Create a medicine."),
        ("POST",   "/api/medicine/bulk",               "SystemAdmin, PharmacyAdmin","Bulk-create medicines."),
        ("PUT",    "/api/medicine/{id}",               "SystemAdmin, PharmacyAdmin","Update a medicine."),
        ("DELETE", "/api/medicine/{id}",               "SystemAdmin, PharmacyAdmin","Delete a medicine."),
        ("POST",   "/api/medicine/prescription-upload","Any auth",                  "Upload a prescription image/PDF. Returns URL."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.8 Medicine Units
    story.append(Paragraph("7.8  Medicine Units  —  /api/medicineunit", ST["h2"]))
    story.append(Paragraph(
        "Links units of measure to a specific medicine, configuring conversion factors, "
        "pricing and the default POS unit. The <b>pos/{medicineId}</b> endpoint returns "
        "only active units sorted with the default first — optimised for the sale screen.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",    "/api/medicineunit/by-medicine/{medicineId}","Any auth",                          "List all units for a medicine."),
        ("GET",    "/api/medicineunit/pos/{medicineId}",        "Any auth",                          "Active units for POS dropdown, default first."),
        ("POST",   "/api/medicineunit",                         "SystemAdmin, PharmacyAdmin, Pharmacist","Attach a UoM to a medicine."),
        ("POST",   "/api/medicineunit/set-default",             "SystemAdmin, PharmacyAdmin, Pharmacist","Set the default POS unit."),
        ("PATCH",  "/api/medicineunit/{id}/price",              "SystemAdmin, PharmacyAdmin, Pharmacist","Update unit price."),
        ("DELETE", "/api/medicineunit/{id}",                    "SystemAdmin, PharmacyAdmin",         "Deactivate a unit (base units cannot be deactivated)."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.9 Medicine Batches
    story.append(Paragraph("7.9  Medicine Batches  —  /api/medicinebatch", ST["h2"]))
    story.append(Paragraph(
        "Stock is tracked at the batch level. The <b>near-expiry</b> endpoint defaults to "
        "45 days and is used for proactive stock management alerts.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",    "/api/medicinebatch",                        "Any auth",                               "List all batches."),
        ("GET",    "/api/medicinebatch/by-medicine/{medicineId}","Any auth",                              "Batches for a specific medicine."),
        ("GET",    "/api/medicinebatch/near-expiry?days=45",    "Any auth",                               "Batches expiring within N days."),
        ("GET",    "/api/medicinebatch/{id}",                   "Any auth",                               "Get batch by ID."),
        ("POST",   "/api/medicinebatch",                        "SystemAdmin, PharmacyAdmin, Pharmacist", "Create a batch."),
        ("POST",   "/api/medicinebatch/bulk",                   "SystemAdmin, PharmacyAdmin, Pharmacist", "Bulk-create batches."),
        ("PUT",    "/api/medicinebatch/{id}",                   "SystemAdmin, PharmacyAdmin, Pharmacist", "Update a batch."),
        ("DELETE", "/api/medicinebatch/{id}",                   "SystemAdmin, PharmacyAdmin",             "Delete a batch."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.10 Sales
    story.append(Paragraph("7.10  Sales  —  /api/sales", ST["h2"]))
    story.append(Paragraph(
        "The create-sale endpoint is the most complex: it validates stock availability, "
        "applies FEFO batch deduction across all sale items, calculates line-level and "
        "header-level discounts, and commits everything atomically in a single database "
        "transaction. An invoice number is auto-generated.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",  "/api/sales",     "Any auth",                                      "List all sales, newest first."),
        ("GET",  "/api/sales/{id}","Any auth",                                      "Get sale by ID."),
        ("POST", "/api/sales",     "SystemAdmin, PharmacyAdmin, Pharmacist, Receptionist","Create a sale (FEFO stock deduction)."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.11 Disposals
    story.append(Paragraph("7.11  Disposals  —  /api/disposal", ST["h2"]))
    story.append(Paragraph(
        "Records destruction of expired or near-expired medicine. Stock is deducted "
        "transactionally. A numbered disposal record (DSP-YYYYMMDD-NNNN) is returned.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",  "/api/disposal",     "Any auth",                               "List all disposals, newest first."),
        ("GET",  "/api/disposal/{id}","Any auth",                               "Get disposal by ID."),
        ("POST", "/api/disposal",     "SystemAdmin, PharmacyAdmin, Pharmacist", "Record a new disposal."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.12 Reports
    story.append(Paragraph("7.12  Reports  —  /api/report", ST["h2"]))
    story.append(Paragraph(
        "All report endpoints are read-only and accessible to any authenticated user. "
        "The stock-consumption endpoint accepts optional date range and filter parameters.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET", "/api/report/dashboard",          "Any auth", "Full dashboard: today, month, top-10 lists."),
        ("GET", "/api/report/today",              "Any auth", "Today's sales summary."),
        ("GET", "/api/report/this-month",         "Any auth", "Current month's sales summary."),
        ("GET", "/api/report/top-medicines",      "Any auth", "Top-selling medicines (?top=10)."),
        ("GET", "/api/report/top-manufacturers",  "Any auth", "Top manufacturers by revenue (?top=10)."),
        ("GET", "/api/report/top-suppliers",      "Any auth", "Top suppliers by units supplied (?top=10)."),
        ("GET", "/api/report/monthly-sales",      "Any auth", "Monthly breakdown for last N months (?months=12)."),
        ("GET", "/api/report/low-stock",          "Any auth", "Medicines at or below reorder level."),
        ("GET", "/api/report/stock-consumption",  "Any auth", "Stock consumed in date range with optional filters."),
    ]))
    story.append(Spacer(1, 0.3*cm))

    # 7.13 Pharmacy Profile
    story.append(Paragraph("7.13  Pharmacy Profile  —  /api/pharmacyprofile", ST["h2"]))
    story.append(Paragraph(
        "Stores the pharmacy's identity (name, PAN, VAT, location, phone numbers, logo and signature). "
        "Only one profile should exist. Logo and signature images are uploaded via dedicated endpoints "
        "and stored under <b>wwwroot/pharmacy/logos/</b> and <b>wwwroot/pharmacy/signatures/</b> "
        "as GUID-named files. Accepted formats: JPG, PNG, WebP. Max size: 5 MB.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(endpoint_table([
        ("GET",    "/api/pharmacyprofile",                    "Any auth",                  "List all profiles (normally one)."),
        ("GET",    "/api/pharmacyprofile/{id}",               "Any auth",                  "Get profile by ID."),
        ("POST",   "/api/pharmacyprofile",                    "SystemAdmin, PharmacyAdmin","Create the pharmacy profile."),
        ("PUT",    "/api/pharmacyprofile/{id}",               "SystemAdmin, PharmacyAdmin","Update profile details and phone numbers."),
        ("DELETE", "/api/pharmacyprofile/{id}",               "SystemAdmin",               "Delete the profile."),
        ("POST",   "/api/pharmacyprofile/{id}/upload-logo",   "SystemAdmin, PharmacyAdmin","Upload/replace the logo image."),
        ("POST",   "/api/pharmacyprofile/{id}/upload-signature","SystemAdmin, PharmacyAdmin","Upload/replace the signature image."),
    ]))
    story.append(Spacer(1, 0.3*cm))


def build_database(story):
    story.append(h1_block("8. Database Schema"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "The database is PostgreSQL managed by Entity Framework Core. Migrations live in "
        "<b>PMS.Repository/Migrations/</b>. The following tables are present after applying "
        "all migrations:",
        ST["body"]))
    story.append(Spacer(1, 0.2*cm))
    rows = [
        ["AspNetUsers",                   "Identity users (email, hashed password, etc.)"],
        ["AspNetRoles / AspNetUserRoles",  "Identity role and user-role join tables"],
        ["AppUsers",                       "Application-level user info (FullName, Email, UserId)"],
        ["Categories",                     "Medicine categories"],
        ["Manufacturers",                  "Medicine manufacturers"],
        ["Suppliers",                      "Batch suppliers with phone and address"],
        ["Medicines",                      "Medicine catalogue"],
        ["UnitsOfMeasure",                 "Master UoM list (Tablet, Strip, Box …)"],
        ["MedicineUnits",                  "Per-medicine unit configs with price and conversion factor"],
        ["MedicineBatches",                "Stock batches with expiry, quantity and purchase price"],
        ["Customers",                      "Customer records with unique phone number"],
        ["Sales",                          "Sale header with invoice number, totals, payment mode"],
        ["SaleItems",                      "Line items: medicine, batch, unit, qty, price"],
        ["ExpiredMedicineDisposals",       "Disposal header with numbered reference"],
        ["ExpiredMedicineDisposalItems",   "Disposal line items: batch, qty, reason"],
        ["PharmacyProfiles",               "Pharmacy identity (name, PAN, VAT, URLs)"],
        ["PharmacyPhoneNumbers",           "Phone numbers belonging to a pharmacy profile"],
    ]
    tbl = make_table(["Table", "Purpose"], rows, col_widths=[6.5*cm, 11*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.2*cm))
    story.append(Paragraph(
        "Notable constraints: InvoiceNumber and DisposalNumber have UNIQUE indexes. "
        "Customer.PhoneNumber is unique. MedicineUnit has partial unique indexes ensuring "
        "exactly one IsBaseUnit=true and one IsDefault=true per medicine.",
        ST["note"]))
    story.append(Spacer(1, 0.3*cm))


def build_auth_security(story):
    story.append(h1_block("9. Authentication & Security"))
    story.append(Spacer(1, 0.3*cm))

    story.append(Paragraph("JWT Authentication", ST["h2"]))
    story.append(Paragraph(
        "PMS uses JWT Bearer tokens. After a successful login the API returns a signed token "
        "containing the user ID, email and role claim. The token must be included in subsequent "
        "requests as:", ST["body"]))
    story.append(code_block("Authorization: Bearer <token>"))
    story.append(Spacer(1, 0.1*cm))

    rows = [
        ["SecretKey",        "Min 32-character HMAC signing key — set via JWT_SECRET env var."],
        ["Issuer",           "PMS.API"],
        ["Audience",         "PMS.Client"],
        ["ExpiryInMinutes",  "Default 60 minutes — configurable via JWT_EXPIRY_MINUTES."],
    ]
    tbl = make_table(["Setting", "Detail"], rows, col_widths=[4.5*cm, 13*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.3*cm))

    story.append(Paragraph("Security Practices", ST["h2"]))
    for item in [
        "Passwords are hashed by ASP.NET Core Identity (PBKDF2 with salting).",
        "Role escalation is prevented: self-registration always receives Pharmacist regardless of the request body.",
        "Only SystemAdmin can create users with arbitrary roles or reassign roles.",
        "The Docker image runs as a non-root user (appuser, UID 1001).",
        "CORS is configurable via AllowedOrigins; credentials are allowed only for listed origins.",
        "File uploads validate extension and size before saving; filenames are GUID-based (no user-controlled paths).",
        "Secrets (DB connection string, JWT key) are injected via environment variables — never committed.",
    ]:
        story.append(bullet(item))
    story.append(Spacer(1, 0.3*cm))


def build_config(story):
    story.append(h1_block("10. Configuration"))
    story.append(Spacer(1, 0.3*cm))
    story.append(Paragraph(
        "Configuration is layered: <b>appsettings.json</b> → environment-specific overrides "
        "→ environment variables. Docker injects all secrets via env vars at runtime.",
        ST["body"]))
    story.append(Spacer(1, 0.2*cm))

    rows = [
        ["ConnectionStrings__DefaultConnection", "PostgreSQL connection string (Npgsql format)."],
        ["JwtSettings__SecretKey",               "HMAC-SHA signing key (≥ 32 chars)."],
        ["JwtSettings__Issuer",                  "Token issuer claim (default: PMS.API)."],
        ["JwtSettings__Audience",                "Token audience claim (default: PMS.Client)."],
        ["JwtSettings__ExpiryInMinutes",         "Token lifetime in minutes (default: 60)."],
        ["AllowedOrigins",                        "Comma-separated list of allowed CORS origins."],
        ["ASPNETCORE_ENVIRONMENT",               "Development | Production"],
        ["ASPNETCORE_URLS",                      "Listen address inside container (http://+:8080)."],
    ]
    tbl = make_table(["Environment Variable / Key", "Description"], rows,
                     col_widths=[7.5*cm, 10*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.2*cm))
    story.append(Paragraph(
        "Copy <b>.env.example</b> to <b>.env</b>, fill in real values, and never commit the .env file "
        "(it is already in .gitignore).",
        ST["note"]))
    story.append(Spacer(1, 0.3*cm))


def build_docker(story):
    story.append(h1_block("11. Docker & Container Setup"))
    story.append(Spacer(1, 0.3*cm))

    story.append(Paragraph("Dockerfile", ST["h2"]))
    story.append(Paragraph(
        "A two-stage build is used to minimise the final image size:",
        ST["body"]))
    for item in [
        "Stage 1 (build) — uses <b>mcr.microsoft.com/dotnet/sdk:10.0</b>. Restores NuGet packages, then publishes the Release build.",
        "Stage 2 (runtime) — uses <b>mcr.microsoft.com/dotnet/aspnet:10.0</b>. Copies only the publish output. Runs as a non-root user.",
        "Exposed port: 8080 (HTTP). HTTPS redirect is handled by a reverse proxy in production.",
    ]:
        story.append(bullet(item))
    story.append(Spacer(1, 0.2*cm))

    story.append(Paragraph("docker-compose.yml", ST["h2"]))
    story.append(Paragraph(
        "The compose file defines a single service <b>pms-api</b> mapped to host port <b>5259</b>. "
        "All secrets are loaded from the <b>.env</b> file. A health check polls "
        "<b>GET /api/category</b> every 30 s and accepts 200 or 401 as healthy.",
        ST["body"]))
    story.append(Spacer(1, 0.1*cm))
    story.append(code_block(
        "# Build and start\n"
        "docker compose up --build -d\n\n"
        "# View logs\n"
        "docker compose logs -f pms-api\n\n"
        "# Stop\n"
        "docker compose down"
    ))
    story.append(Spacer(1, 0.2*cm))

    story.append(Paragraph("Running locally without Docker", ST["h2"]))
    story.append(code_block(
        "# Restore & run in development mode\n"
        "cd PMS\n"
        "dotnet restore PMS.sln\n"
        "dotnet run --project PMS/PMS.csproj\n\n"
        "# Apply EF migrations manually\n"
        "dotnet ef database update --project PMS.Repository --startup-project PMS"
    ))
    story.append(Spacer(1, 0.3*cm))

    story.append(Paragraph("Static file paths (served by UseStaticFiles):", ST["h3"]))
    rows = [
        ["/prescriptions/{filename}",         "Uploaded prescription images and PDFs"],
        ["/pharmacy/logos/{filename}",         "Pharmacy logo images"],
        ["/pharmacy/signatures/{filename}",    "Authorised signature images"],
    ]
    tbl = make_table(["URL Pattern", "Content"], rows, col_widths=[7.5*cm, 10*cm])
    story.append(tbl)
    story.append(Spacer(1, 0.4*cm))


# ── Page numbering ─────────────────────────────────────────────────────────────

def on_page(canvas, doc):
    canvas.saveState()
    # Footer bar
    canvas.setFillColor(C_PRIMARY)
    canvas.rect(doc.leftMargin, 1.0*cm, doc.width, 0.5*cm, fill=1, stroke=0)
    canvas.setFillColor(C_WHITE)
    canvas.setFont("Helvetica", 8)
    canvas.drawString(doc.leftMargin + 6, 1.15*cm, "PMS — Pharmacy Management System Documentation")
    canvas.drawRightString(doc.leftMargin + doc.width - 6, 1.15*cm, f"Page {doc.page}")
    canvas.restoreState()


# ── Main ───────────────────────────────────────────────────────────────────────

def main():
    doc = SimpleDocTemplate(
        OUTPUT_PATH,
        pagesize=A4,
        leftMargin=2*cm,
        rightMargin=2*cm,
        topMargin=2*cm,
        bottomMargin=2.5*cm,
        title="PMS — Pharmacy Management System Documentation",
        author="PMS Development Team",
        subject="Technical API and Architecture Documentation",
    )

    story = []
    build_cover(story)
    build_toc(story)
    build_overview(story)
    story.append(PageBreak())
    build_architecture(story)
    story.append(PageBreak())
    build_tech_stack(story)
    build_project_structure(story)
    story.append(PageBreak())
    build_data_models(story)
    story.append(PageBreak())
    build_roles(story)
    story.append(PageBreak())
    build_api_reference(story)
    story.append(PageBreak())
    build_database(story)
    build_auth_security(story)
    story.append(PageBreak())
    build_config(story)
    build_docker(story)

    doc.build(story, onFirstPage=on_page, onLaterPages=on_page)
    print(f"✅  PDF generated: {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
