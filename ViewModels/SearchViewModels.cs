using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>The filters of the advanced search.</summary>
public class AdvancedSearchViewModel
{
    [Display(Name = "Order or account id")]
    public string? UniqueId { get; set; }

    [Display(Name = "Customer name")]
    public string? Name { get; set; }

    [Display(Name = "Service type")]
    public string? ConnectionType { get; set; }

    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Display(Name = "From date")]
    public DateTime? DateFrom { get; set; }

    [Display(Name = "To date")]
    public DateTime? DateTo { get; set; }

    [Display(Name = "Sort by")]
    public string SortBy { get; set; } = "date-desc";

    [Display(Name = "Rows per page")]
    public int PageSize { get; set; } = 25;

    public int Page { get; set; } = 1;
}

/// <summary>One row of the advanced search result.</summary>
public class SearchResultRow
{
    public string OrderId { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? CityName { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public ConnectionStatus? ConnectionStatus { get; set; }
    public BillStatus? BillStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>The result of the advanced search with its paging information.</summary>
public class SearchResultViewModel
{
    public List<SearchResultRow> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalRows / (double)PageSize);
    public string Scope { get; set; } = string.Empty;
}

/// <summary>The search page of the customer.</summary>
public class CustomerSearchViewModel
{
    public AdvancedSearchViewModel Filters { get; set; } = new();
    public SearchResultViewModel Result { get; set; } = new();
}
