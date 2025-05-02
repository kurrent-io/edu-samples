#!/bin/bash

curl -i -X POST \
  -H "Content-Type: application/vnd.eventstore.events+json" \
  http://localhost:2113/streams/order-b3f2d72c-e008-44ec-a612-5f7fbe9c9240 \
  -d '
    [
        {
            "eventId": "fbf4a1a1-b4a3-4dfe-a01f-ec52c34e16e4",
            "eventType": "order-placed",
            "data": {
                "thisEvent": "is invalid"
            }
        }
    ]'