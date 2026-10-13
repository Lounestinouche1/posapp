# Vectron POS — User Manual

**Version:** Phase 2+3 (multi-language + RTL)  
**Languages:** English · Français · العربية  
**Audience:** operators, managers, and administrators

---

## Table of contents

1. [Getting started](#1-getting-started)
2. [Login and operators](#2-login-and-operators)
3. [Order screen](#3-order-screen)
4. [Rooms, tables and floor plan](#4-rooms-tables-and-floor-plan)
5. [Payments](#5-payments)
6. [Order management](#6-order-management)
7. [Discounts, tips and fees](#7-discounts-tips-and-fees)
8. [Customers, loyalty and gift cards](#8-customers-loyalty-and-gift-cards)
9. [Stock and purchasing](#9-stock-and-purchasing)
10. [Staff: clock, shifts and payroll](#10-staff-clock-shifts-and-payroll)
11. [Kitchen display and 86 board](#11-kitchen-display-and-86-board)
12. [Reports](#12-reports)
13. [End of day (X / Z)](#13-end-of-day-x--z)
14. [Settings](#14-settings)
15. [Multi-language and Arabic RTL](#15-multi-language-and-arabic-rtl)
16. [Network, cloud and API](#16-network-cloud-and-api)
17. [Guest ordering, QR and kiosk](#17-guest-ordering-qr-and-kiosk)
18. [Troubleshooting](#18-troubleshooting)

---

## 1. Getting started

### System requirements
- Windows 10/11 (x64)
- Self-contained build: no separate .NET install needed
- Receipt printer (ESC/POS, port 9100), cash drawer, customer display — all optional
- LAN recommended for multi-terminal (main PC + order PCs)

### Launch
1. Run **Vectron POS.exe**.
2. The **shared database folder** (`client database`) must be reachable. On the first PC choose *Main PC* mode; on other terminals choose *Order PC* and point it at the Main PC path (e.g. `\\MAIN-PC\client database`).
3. The **login screen** appears.

### First-run setup
1. Log in as **Manager** (default PIN `1234`).
2. Open **Settings** and configure:
   - **Manage operators** — create staff, set roles and PINs
   - **Manage products / families** — build the menu
   - **Room management** — rooms and tables
   - **Printer routing** — which printer receives which ticket
   - **Currency and TVA** — currency symbol and tax rates
3. Toggle features on/off under each Settings category to match how you trade.

---

## 2. Login and operators

- The login screen lists every operator. Select a name (or use the dropdown), enter the **PIN**, press **Enter**.
- Roles: **Manager** (full access) and **Operator** (floor duties). Permissions (products, refunds, voids, settings, reports…) are granted per role.
- **Clock-in on login** (when enabled): the app records a shift start automatically. Clock out from **Operator** in the sidebar or at end of day.
- **Training mode**: sales are not recorded and a banner reminds you. Toggle it from Settings → Staff.

---

## 3. Order screen

The main screen has three zones: **menu (center)**, **category tabs (top)**, and the **current order (right)**.

### Adding items
- Tap a **category tab** (e.g. Popular, Mains, Drinks), then tap a **product tile** to add one to the order.
- Tap the **+** on a tile to add without leaving the tab.
- **Modifiers**: products with options open a modifier dialog — choose size, extras, etc.
- **Item notes**: long-press or use the note action to attach a kitchen note to a line.
- **Barcode**: click the scan field and scan a product barcode to add it instantly.

### The current-order panel
- Shows **operator, room, table, order type**, and seat buttons.
- **Seat**: assign lines to a seat when splitting by seat.
- Line controls: quantity, price, remove.
- **TOTAL** updates live.

### Seats
Tap **SEAT 1/2/3…** to attribute items to a guest. Useful for split-by-seat payment later.

---

## 4. Rooms, tables and floor plan

- Sidebar **Rooms** shows all rooms; each room contains tables.
- Tap a **table** to open its order. Occupied tables show the running total.
- **Table transfer**: move an open order to another table (Manager).
- **Split bill**: divide one table's order across seats, items, or custom amounts.
- **Hold / Fire** (Course management): send items to the kitchen now or hold them for a later course.
- **Floor plan editor** (Manager): drag tables, rename, resize, set capacity. The layout is stored per room.

---

## 5. Payments

### Taking payment
1. Tap **Take payment**.
2. Choose a **payment method** (Cash, Card, …). Methods are configurable under Settings → Payment.
3. **Cash**: enter tendered amount — the screen shows **TENDERED** and **CHANGE**. Quick-tender buttons (exact, 20, 50…) speed this up.
4. **Card**: confirm on the terminal, then accept. Virtual terminal mode can email an invoice link.
5. The sale is recorded and the **receipt prints** to the routed printer.

### Split payments
A ticket can be paid with **multiple methods** (e.g. part cash, part card). Each method prints its own receipt line and appears in the tender breakdown.

### Cash rounding
When enabled, totals round to the nearest 0.05 (configurable). The receipt shows a **ROUNDING** line.

### Special payment types
- **Tabs / Charge tabs**: open a customer tab, charge items over time, settle later (credit limits apply).
- **Deposits & special orders**: take a deposit now, collect goods later.
- **Offline card queue**: if the network is down, card sales queue and settle when back online.
- **Refunds**: Manager → select a past receipt → choose items → refund to original method. Stock is restocked automatically; a refund ticket prints to the Refund destination.

---

## 6. Order management

- **Park / Recall**: park an order to clear the screen; recall it from the parked list.
- **Repeat order**: duplicate the last ticket (takeaway regulars).
- **Order type**: Dine-in, Takeaway, Delivery, Kiosk — shown on the ticket and in reports.
- **Split bill**: by seat, by item, or custom amount.
- **Table transfer**: move orders between tables.
- **Course / Hold-Fire**: fire starters now, hold mains for a timed course.
- **Combo deals**: bundled products at a set price.
- **86 an item**: mark a product out of stock — it greys out on the menu and shows on the 86 board.

---

## 7. Discounts, tips and fees

- **Discounts**: percentage or fixed amount, per line or per ticket. Employee discount applies automatically for staff (percentage set in Settings → Staff).
- **Voids / Comps**: void a line before payment; comps mark items as free (both audited).
- **Service charge**: automatic % on dine-in (toggle).
- **Cash discount / Card surcharge**: adjust price by payment method.
- **Tips**: add a tip at payment; tips appear on the shift report and payroll export.
- **Happy hour**: scheduled automatic discounts by time/day — configure under Settings → Customers.

---

## 8. Customers, loyalty and gift cards

- **Customer database**: name, phone, email, notes. Attach a customer to a ticket for history and loyalty.
- **Loyalty**: customers earn points per purchase; redeem as discounts.
- **Gift cards**: sell, activate, check balance, redeem. Balances live in the shared database.
- **e-Gift email**: send a gift card by email at sale time.

---

## 9. Stock and purchasing

- **Stock levels**: each product has a stock quantity; sales decrement it.
- **Reorder point + low-stock banner**: the home screen warns when items fall below reorder level.
- **Stocktake**: count physical stock; the system computes variance with an audit trail.
- **Waste & expiry**: log waste (spoilage, breakage); reports show waste cost.
- **Transfers**: move stock between locations (multi-branch).
- **Suppliers**: manage vendor list; **Purchase Orders** — create, receive, cancel, email as PDF. Receiving adds stock.
- **Food cost**: product cost fields drive margin reports.
- **Barcode labels (Code 39)**: print shelf labels from the product catalog.
- **Weight-priced items**: scale entry prompt for kg items.
- **Product variants**: size/color variants with CSV import.

---

## 10. Staff: clock, shifts and payroll

- **Clock in/out**: at login (optional) or from the Operator screen.
- **Shift report**: hours worked, sales per operator, tips, discounts given.
- **Payroll export (CSV)**: hours × gross-pay rate, ready for accounting.
- **Employee discount**: set a per-operator discount %.
- **Scheduling** (when enabled): rota view for the week.

---

## 11. Kitchen display and 86 board

- **Kitchen Display (KDS)**: live ticket queue on a second screen. Shows elapsed time, late alarm (configurable minutes), and tap-to-bump.
- **86 board**: dedicated screen of out-of-stock items, auto-populated when stock hits zero or staff 86 an item.

---

## 12. Reports

Open **Settings → Reports** (Manager). Key reports:

| Report | What it shows |
|---|---|
| Sales (X / Z) | Totals for the day / period |
| Payment breakdown | Cash, card, voucher, tip totals |
| By operator / product / family | Performance breakdowns |
| Discount & void report | Who discounted/voided what, and why |
| Occupancy heatmap | Covers by hour and daypart |
| Menu engineering | Stars, plowhorses, puzzles, dogs (qty × margin) |
| Accounting export | CSV journal (QuickBooks / Xero compatible) |
| Server performance | Terminals ranked by uptime/sales |
| AI insights | Plain-language summary of the day's patterns |
| Live dashboard | Real-time KPIs while trading |

All CSV exports use **invariant-culture decimals** (safe for any spreadsheet locale).

---

## 13. End of day (X / Z)

1. **Settings → End day (X / Z)**.
2. **X report**: mid-day snapshot (does not close the day).
3. **Z report**: closes the day. Shows:
   - SUMMARY (gross, discounts, tips, net)
   - PAYMENT BREAKDOWN (per method)
   - BY OPERATOR / PRODUCT / FAMILY
4. **Cash-up**: count the drawer, system shows expected vs counted, records variance.
5. **Cloud backup**: if configured, the Z report auto-uploads a full-database backup to the head office.
6. Print or export the report.

---

## 14. Settings

Settings is organised into categories (left column):

| Category | Contains |
|---|---|
| **Management** | Operators, products, families |
| **Reports** | All reports and exports |
| **Network** | Cloud sync, API, webhooks, store ID |
| **Ordering Table** | Room management, floor plan, table features |
| **Payment Pricing** | Methods, rounding, fees, tabs, deposits |
| **Staff** | Clock, shifts, payroll, discounts, training mode |
| **Inventory Stock** | Stock, purchasing, suppliers, variants, labels |
| **Customers** | Database, loyalty, gift cards, happy hour |
| **Operations** | Branches, refunds, QR, kiosk, customer display |
| **Receipts Display** | Email/SMS, receipt search, multi-language |

- Use the **toggles** to switch features on/off — disabled features disappear from the UI.
- **Language switcher** (when Multi-language is on): English / Français / العربية — the app restarts to apply.
- **Done** returns to the order screen.

---

## 15. Multi-language and Arabic RTL

- Three languages ship built-in: **English**, **Français**, **العربية**.
- Switch in **Settings → Language**; the app saves and restarts.
- Currency symbol and date format follow the language (en-GB / fr-FR / ar-MA).
- **Arabic mode flips the entire layout (RTL)**: sidebar on the right, order panel on the left, menus mirrored.
- **What is never translated**: product names, family names, room/table names, payment-method labels, printer names — these are your business data.
- Receipts print in the currently selected language.

---

## 16. Network, cloud and API

### Multi-terminal
- One **Main PC** holds the database; **Order PCs** connect via the UNC path.
- Status light at bottom-left: `MAIN PC` / `ORDER PC` + print-server status.

### Cloud sync
- **Settings → Network**: set head-office URL, shared key, sync interval.
- **Push-only by default**: each store pushes completed sales; the head office does not overwrite local data.
- **Pull sales history**: fetch your own sales from head office (scope = mine).
- **Full-database backup vault**: End Day Z report auto-uploads a zip; manual *Upload backup now* available; 14-zip rotation at head office.
- **Restore from home**: download the latest backup zip, extract, restart.

### REST API
- Enable in Settings → Network; set an **API key**.
- Routes: `GET/POST /sync/sales`, `POST/GET /sync/backup`, health endpoint.
- **Sale webhooks**: POST a JSON payload to your URL on every completed sale.

---

## 17. Guest ordering, QR and kiosk

- **Guest URL**: a public link (e.g. `https://your/order`) shows the menu on the customer's phone.
- **QR codes**: print QR table labels — each table's QR opens the menu pre-attributed to that table.
- **Call waiter / order status**: guests can call staff or check order status from the QR page.
- **Kiosk mode**: fullscreen self-order, pay at counter — launch from Settings → Operations.
- **Customer display**: second screen facing the guest shows the running total and thank-you message.
- Guest orders appear on the main screen with a source badge and can be managed like any order.

---

## 18. Troubleshooting

| Symptom | Fix |
|---|---|
| Login rejected | Confirm the correct operator is selected; PINs are per-operator. Reset via Manager → Manage operators. |
| Printer not printing | Settings → Printer routing: check the destination points to the right Windows printer; print a test page. Print server must be running on the Main PC (status shown bottom-left). |
| Order PC can't see data | Verify the UNC path (`\\MAIN-PC\client database`) is reachable; firewall must allow SMB. |
| Language doesn't change | The switcher restarts the app — allow a few seconds. If it reverts, check Multi-language toggle is on. |
| Cloud sync failing | Settings → Network: check URL, key, and that the head-office server is reachable. Sync errors appear in the status line. |
| Sale missing from report | Check training mode is off (training sales are not recorded). Check the date filter on the report. |
| Cash drawer not opening | Drawer is triggered by the payment printer — confirm the printer supports drawer kick and the cable is connected. |
| App feels slow | Settings → Staff → Server performance shows per-terminal health; check disk space on the Main PC. |

---

*Vectron POS — built for hospitality: fast floor service, honest reporting, three languages.*
