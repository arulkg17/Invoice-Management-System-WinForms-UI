using System.Text.Json.Serialization;

namespace Ims.WinForms.Models;

// These classes mirror the API DTOs (Invoice.DTOs). Property names = JSON names, every key is "Id".
// Master edit screens are generated from these classes, so adding a property adds a field.

public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool? IsActive { get; set; } = true;
}

public class Customer
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? MobileNo { get; set; }
    public string? Email { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public string? GstNo { get; set; }
    public bool? IsActive { get; set; } = true;
}

public class Vendor
{
    public int Id { get; set; }
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? MobileNo { get; set; }
    public string? Email { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public string? GstNo { get; set; }
    public bool? IsActive { get; set; } = true;
}

public class Itemmaster
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string ItemBarCode { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? Description { get; set; }
    public string Uom { get; set; } = "";
    public decimal? Rate { get; set; }
    public decimal? MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }
    public bool? IsActive { get; set; } = true;
}

// One class for the three user DTOs: UserDto (list: no password, has LastLoginDate),
// UserCreateDto (Password + CreatedBy) and UserUpdateDto (optional Password + UpdatedBy).
public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Password { get; set; }          // never returned by the API; blank on update = keep the old one
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? AlternatePhone { get; set; }
    public string AddressLine1 { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }   // output only
    public string CreatedBy { get; set; } = "";    // sent on create
    public string UpdatedBy { get; set; } = "";    // sent on update
}

public interface ITransactionRow { int Id { get; } }

// List rows = the DTOs returned by GetAll. Vendor / Customer / PurchaseOrder text columns are looked up by
// the screen after loading (they are not in the API response, hence JsonIgnore).
public class PurchaseOrderRow : ITransactionRow
{
    public int Id { get; set; }
    public string? PONumber { get; set; }
    public DateTime? PODate { get; set; }
    public int VendorId { get; set; }
    [JsonIgnore] public string? Vendor { get; set; }
    public string? Status { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ReceiptRow : ITransactionRow
{
    public int Id { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public int PurchaseOrderId { get; set; }
    [JsonIgnore] public string? PurchaseOrder { get; set; }
    public int VendorId { get; set; }
    [JsonIgnore] public string? Vendor { get; set; }
    public string? Status { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SalesInvoiceRow : ITransactionRow
{
    public int Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int CustomerId { get; set; }
    [JsonIgnore] public string? Customer { get; set; }
    public string? Status { get; set; }
    public decimal TotalAmount { get; set; }
}
