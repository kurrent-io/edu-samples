using System.Text;
using System.Text.Json;
using EventStore.Client;

namespace PostgresProjection;
public class CartEventHandler
{
    private readonly PostgresDataAccess _postgres;

    public CartEventHandler(PostgresDataAccess postgres)
    {
        _postgres = postgres;
    }

    public void UpdatePostgresReadModel(ResolvedEvent resolvedEvent)
    {
        var eventType = resolvedEvent.Event.EventType;
        var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

        Console.WriteLine($"Processing event: {eventType}");

        switch (eventType)
        {
            case "visitor-started-shopping":
                var visitorEvent = JsonSerializer.Deserialize<VisitorStartedShopping>(eventData);
                if (visitorEvent != null)
                    _postgres.InsertCart(visitorEvent.cartId, null, "STARTED", visitorEvent.when);
                break;

            case "customer-started-shopping":
                var customerEvent = JsonSerializer.Deserialize<CustomerStartedShopping>(eventData);
                if (customerEvent != null)
                    _postgres.InsertCart(customerEvent.cartId, customerEvent.customerId, "STARTED", customerEvent.when);
                break;

            case "cart-shopper-got-identified":
                var identifiedEvent = JsonSerializer.Deserialize<CartShopperGotIdentified>(eventData);
                if (identifiedEvent != null)
                    _postgres.UpdateCartCustomer(identifiedEvent.cartId, identifiedEvent.customerId, identifiedEvent.when);
                break;

            case "cart-got-checked-out":
                var checkedOutEvent = JsonSerializer.Deserialize<CartGotCheckedOut>(eventData);
                if (checkedOutEvent != null)
                    _postgres.UpdateCartStatus(checkedOutEvent.cartId, "CHECKED_OUT", checkedOutEvent.when);
                break;

            case "cart-got-abandoned":
                var abandonedEvent = JsonSerializer.Deserialize<CartGotAbandoned>(eventData);
                if (abandonedEvent != null)
                    _postgres.UpdateCartStatus(abandonedEvent.cartId, "ABANDONED", abandonedEvent.when);
                break;
        }
    }
}