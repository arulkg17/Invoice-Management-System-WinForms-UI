# Invoice-Management-System-WinForms-UI

Windows Forms (.NET 10) client for **Invoice.CoreAPI**: HTTP + JWT only, no reference to any Invoice.* project.
Same screens, same API calls and the same look and feel as the WPF client. Every control is drawn in code,
so students can read each screen top to bottom.

## Run
1. Start Invoice.CoreAPI.
2. Set `ApiBaseUrl` in `Program.cs` (default `http://localhost:5063/`).
3. Open `Ims.WinForms.csproj` in Visual Studio, restore NuGet packages, press F5.

## Screens
Login, Dashboard, Categories, Customers, Vendors, Item Master, Users, Purchase Orders / Receipts / Sales Invoices
(new, edit, delete, post / approve / cancel, PDF), AI Assistant.

## Project layout
- `GlobalUsings.cs`: common namespaces for every file (System.Net.Http, System.Linq, System.Windows.Forms ...)
- `Models/Models.cs`: copies of the API DTOs, every key is `Id`
- `Services/`: `ApiClient` (JWT, ApiResponse unwrapping), `MasterService<T>` (GetAll / Create / Update/{id} / Delete/{id})
- `UI/Theme.cs`: colours, fonts, grid styling, accent colour
- `UI/Controls.cs`: ModernButton, CardPanel, GradientPanel, InputBox, KpiCard, BarChart, Animator ...
- `UI/MasterView.cs` + `UI/FieldPanel.cs`: one generic screen for all masters (the form is built by reflection)
- `UI/TransactionView.cs`, `UI/DocumentEntryForm.cs`, `UI/ReceiptEntryForm.cs`: transactions
- `UI/MainForm.cs`: sidebar + the one-line-per-screen menu

## WPF vs WinForms: the same look, different effort (good class discussion)
| Look and feel | WPF | WinForms |
|---|---|---|
| Rounded buttons, hover colour | a Style in XAML | `ModernButton` (owner-drawn, ~60 lines) |
| Cards with shadow | `Border` + `DropShadowEffect` | `CardPanel` (custom painting) |
| Gradient sidebar | `LinearGradientBrush` | `GradientPanel` (override `OnPaintBackground`) |
| Accent colour switch | `DynamicResource` brushes, 3 lines | `Theme.AccentChanged` event + `Invalidate()` in every control |
| Page transition | `EventTrigger` + `DoubleAnimation` | `Animator` with a `Timer` |
| Animated numbers and bars | storyboards | `Timer` + custom `OnPaint` |
| Screen picked from the ViewModel type | `DataTemplate` | a `Func<Control>` per menu entry |

## Where to change things
- Model property names: `Models/Models.cs` (copy them from the DTOs)
- Routes: `Services/MasterService.cs`, `UI/TransactionView.cs` (actions and PDF), `UI/DocumentEntryForm.cs`, `UI/ReceiptEntryForm.cs`
- Colours and fonts: `UI/Theme.cs`
- Add a master: a model class + one `AddNav(...)` line in `MainForm.cs`
