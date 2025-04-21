using System;
using Common;
using EventStore.Client;
using StackExchange.Redis;

namespace RedisProjection;

public class CartProjection
{
    public static bool TryProject(ITransaction transaction, ResolvedEvent resolvedEvent)
    {
        var decodedEvent = CartEventEncoder.Decode(                             // Deserialize the KurrentDB resolved event into cart events
            resolvedEvent.Event.Data, resolvedEvent.Event.EventType);

        if (decodedEvent is not ItemGotAdded &&                                 // If the event is not of type ItemGotAdded or ItemGotRemoved
            decodedEvent is not ItemGotRemoved) 
            return false;                                                       // then return false

        switch (decodedEvent)                                                   // Check the type of the event
        {
            case ItemGotAdded added:                                            // If it is ItemGotAdded, call the Project method for it
                Project(transaction, added);
                break;
            case ItemGotRemoved removed:                                        // If it is ItemGotRemoved, call the Project method for it
                Project(transaction, removed);
                break;
            default:
                return false;                                                   // If it is neither, return false
        }

        return true;                                                            // Return true if the event was successfully projected
    }

    public static void Project(ITransaction txn, ItemGotAdded addedEvent)
    {
        var hourKey = $"top-10-products:{addedEvent.at:yyyyMMddHH}";            // Create a key for the current hour
        var productKey = addedEvent.productId;                                  // Use the product ID as the member in the sorted set
        var productName = addedEvent.productName;                               // Assuming `productName` is part of the event

        txn.SortedSetIncrementAsync(hourKey, productKey, addedEvent.quantity);  // Increment the quantity of the product in the sorted set
        txn.HashSetAsync("product-names", productKey, productName);             // Store product name in a hash;

        Console.WriteLine($"Incremented product {addedEvent.productId} in " +
                          $"{hourKey} by {addedEvent.quantity}");
    }

    public static void Project(ITransaction txn, ItemGotRemoved removedEvent)
    {
        var hourKey = $"top-10-products:{removedEvent.at:yyyyMMddHH}";          // Create a key for the current hour
        var productKey = removedEvent.productId;                                // Use the product ID as the member in the sorted set

        txn.SortedSetDecrementAsync(hourKey, productKey,                        // Decrement the quantity of the product in the sorted set
            removedEvent.quantity); 

        Console.WriteLine($"Decremented product {removedEvent.productId} in " +
                          $"{hourKey} by {removedEvent.quantity}");
    }
}