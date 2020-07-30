# AdvWorks Demo

Pre-requisites:
1. Visual Studio 2017 with ASP.NET and web development workload
2. .NET Core 2.1
3. AdventureWorks DB restored in SQL Server. A copy of DB can be obtained here - https://docs.microsoft.com/en-us/sql/samples/adventureworks-install-configure?view=sql-server-ver15&tabs=ssms

Steps:
1. Open up the solution in Visual Studio and make changes to SalesDbConnectionString value in appsettings.json file. Replace "Server_instance" with your SQL Server instance name, "DB_name" with AdventureWorks DB name.
2. Compile the solution.
3. Run it
4. Use Postman (or similar tool) to test

Order retrieval:
 1. Select "GET" as method, "http://localhost:49493/api/order?ids=43659,43660" as url
 2. Click "Send"
 
New order creation:
 1. Select "POST" as method, "http://localhost:49493/api/order" as url
 2. Click "Body" and select "JSON" as type
 3. Paste following snippet
  ```json
  [{
    "orderDate": "2020-04-04",
    "status": "InProgress",
    "onlineOrderFlag": false,
    "shipDate": "2020-04-06",
    "customerId": 1,
    "billToAddressId": 1,
    "shipToAddressId": 1,
    "shipMethodId": 1,
    "subTotal": 100,
    "taxAmt": 10,
    "orderItems": [{
      "orderQty": 10,
      "productId": 680,
      "unitPrice": 5,
      "unitPriceDiscount": 0.1
    }]
  }]
  ```
  4. Click "Send" button
  5. The response body will contain an array of created sales orders ids
  
 Order update:
 1. Select "PUT" as method, "http://localhost:49493/api/order" as url
 2. Click "Body" and select "JSON" as type
 3. Paste following snippet
  ```json
  [{
    "salesOrderId": "sales order id from creation step",
    "revisionNumber": 1,
    "orderDate": "2020-04-04",
    "shipDate": "2020-04-20",
    "status": "InProgress",
    "onlineOrderFlag": true,
    "customerId": 1,
    "billToAddressId": 1,
    "shipToAddressId": 1,
    "shipMethodId": 1,
    "subTotal": 100,
    "taxAmt": 11
  }]
  ```
  4. Click "Send" button
  5. The response body will contain an array messages indicating if order was updated or not
  
Order deletion:
 1. Select "DELETE" as method, "http://localhost:49493/api/order?ids=sales-order-id-from-creation-step" as url
 2. Click "Send" button
  
  
The AdvWorksApi.Tests project contains unit tests for Controllers and Services. Use Test Explorer window to run them.
