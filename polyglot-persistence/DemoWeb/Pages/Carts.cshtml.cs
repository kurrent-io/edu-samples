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

    public List<Cart> Carts { get; private set; } = new();
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
        Carts = await _postgresService.GetCartsAsync(FilterOptions);
        TotalCount = await _postgresService.GetCartsTotalCountAsync(FilterOptions);
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