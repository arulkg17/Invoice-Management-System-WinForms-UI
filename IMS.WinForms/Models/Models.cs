using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Ims.WinForms.Models;

// NOTE: property names must match the JSON your Invoice.CoreAPI returns (case-insensitive).
// Master edit screens are generated from these classes, so adding a property adds a field.
// By convention the id property is "<ClassName>Id" (CategoryId, CustomerId ...).

public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Customer
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? Email { get; set; }
    public string? MobileNo { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Vendor
{
    public int Id { get; set; }
    public string VendorName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Itemmaster
{
    public int Id { get; set; }
    public string ItemName { get; set; } = "";
    public int CategoryId { get; set; }
    public string? Uom { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string? FullName { get; set; }
    public string? Email { get; set; }
    [Browsable(false)] public string? Password { get; set; }   // hidden in the grid, shown (masked) in the editor
    public bool IsActive { get; set; } = true;
}

// ---- transaction list rows ----
public interface ITransactionRow { int Id { get; } }

public class PurchaseOrderRow : ITransactionRow
{
    public int Id { get; set; }
    public string? PoNumber { get; set; }
    public string? VendorName { get; set; }
    public DateTime? PoDate { get; set; }
    public string? Status { get; set; }
    public decimal TotalAmount { get; set; }
    //[Browsable(false), JsonIgnore] public int Id => Id;
}

public class ReceiptRow : ITransactionRow
{
    public int Id { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? VendorName { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public string? Status { get; set; }
    //[Browsable(false), JsonIgnore] public int Id => Id;
}

public class SalesInvoiceRow : ITransactionRow
{
    public int Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Status { get; set; }
    public decimal TotalAmount { get; set; }
    //[Browsable(false), JsonIgnore] public int Id => Id;
}
