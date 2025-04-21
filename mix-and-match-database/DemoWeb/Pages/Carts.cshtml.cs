using DemoWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoWeb.Pages;

public class CartsModel : PageModel
{
    private readonly PostgresService _postgresService;

    // Direct property binding
    [BindProperty(SupportsGet = true)]
    public string CartId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string CustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortColumn { get; set; } = "created_at";

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = "DESC";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    public List<CartItem> Carts { get; private set; } = new();
    public List<GroupedCart> GroupedCarts { get; private set; } = new();
    public int TotalCount { get; private set; }
    public int TotalPages => (int)System.Math.Ceiling(TotalCount / (double)PageSize);

    // Create a computed FilterOptions property
    private CartFilterOptions FilterOptions => new()
    {
        CartId = CartId,
        CustomerId = CustomerId,
        Status = Status,
        SortColumn = SortColumn,
        SortDirection = SortDirection,
        Page = PageNumber,
        PageSize = PageSize
    };

    public CartsModel(PostgresService postgresService)
    {
        _postgresService = postgresService;
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostRefreshAsync()
    {
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        // Reset all properties
        CartId = null;
        CustomerId = null;
        Status = null;
        SortColumn = "created_at";
        SortDirection = "DESC";
        PageNumber = 1;
        PageSize = 10;

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        // Set up filter options based on query parameters
        var filterOptions = new CartFilterOptions
        {
            CartId = CartId,
            CustomerId = CustomerId,
            Status = Status,
            SortColumn = SortColumn,
            SortDirection = SortDirection,
            Page = PageNumber,
            PageSize = PageSize
        };

        // Get cart items with pagination
        Carts = await _postgresService.GetCartItemsAsync(filterOptions);
        TotalCount = await _postgresService.GetCartItemsTotalCountAsync(filterOptions);

        // Group cart items by cart ID
        GroupedCarts = Carts
            .GroupBy(c => new { c.CartId, c.CustomerId, c.Status })
            .Select(g => new GroupedCart
            {
                CartId = g.Key.CartId,
                CustomerId = g.Key.CustomerId,
                Status = g.Key.Status,
                Items = g.ToList(),
                TotalItems = g.Count(),
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalPrice = g.Sum(i => i.Quantity * i.PricePerUnit),
                TotalTax = g.Sum(i => i.Tax),
                LastUpdated = g.Max(i => i.UpdatedAt) // Get the most recent update for the cart
            })
            .ToList();
    }

    public string GetPageUrl(int pageNumber)
    {
        var routeValues = new RouteValueDictionary
        {
            { "CartId", CartId },
            { "CustomerId", CustomerId },
            { "Status", Status },
            { "SortColumn", SortColumn },
            { "SortDirection", SortDirection },
            { "PageSize", PageSize },
            { "PageNumber", pageNumber } // Changed from "Page" to "PageNumber"
        };

        return Url.Page("./Carts", routeValues);
    }

    public string GetSortUrl(string column)
    {
        string direction = "ASC";

        if (SortColumn == column)
        {
            direction = SortDirection == "ASC" ? "DESC" : "ASC";
        }

        return Url.Page("./Carts", new
        {
            CartId,
            CustomerId,
            Status,
            sortColumn = column,
            sortDirection = direction,
            PageSize,
            page = 1
        });
    }
}

// Class to represent a grouped cart with its items
public class GroupedCart
{
    public string CartId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalTax { get; set; } // Total tax for all cart items
    public DateTime LastUpdated { get; set; } // Last updated timestamp for the cart
}