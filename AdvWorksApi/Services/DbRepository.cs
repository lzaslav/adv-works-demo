
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using AdvWorksApi.Models;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AdvWorksApi.Services
{
    /// <summary>
    /// An interface representing database repository and supported operations
    /// </summary>
    public interface IDbRepository
    {
        Task<List<SalesOrder>> GetOrdersAsync(List<int> orderIds);
        Task<List<int>> AddOrdersAsync(List<SalesOrder> orders);
        Task DeleteOrdersAsync(List<int> orderIds);
        Task<List<string>> UpdateOrdersAsync(List<SalesOrder> orders);
    }

    /// <summary>
    /// Class representing database operations
    /// </summary>
    public class DbRepository : IDbRepository
    {
        private readonly IDbContext _context;
        public DbRepository(IDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves several sales orders based on provided list of order ids.
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public async Task<List<SalesOrder>> GetOrdersAsync(List<int> orderIds)
        {
            // Obtain a connection to the database.
            using (var connection = _context.GetConnection())
            {
                connection.Open();
                // Query both sales orders and order items
                var queryResult = await _context.QueryAsync<SalesOrder, SalesOrderItem, SalesOrder>(connection,
                    @"
                    SELECT H.*, D.SalesOrderID as ParentOrderID, D.*
                    FROM Sales.SalesOrderHeader H
                    LEFT JOIN Sales.SalesOrderDetail D ON H.SalesOrderID=D.SalesOrderID
                    WHERE H.SalesOrderID IN @ids
                    ",
                    // map function that adds order it to its parent sales order
                    (order, orderItem) =>
                    {
                        order.OrderItems.Add(orderItem);
                        return order;
                    },
                    new { ids = orderIds },
                    // Sales order and sales order item are split using ParentOrderID field
                    splitOn: "ParentOrderID"
                    );

                // Group results based on sales order id and combine order items from same sales order
                var orders = queryResult.GroupBy(x => x.SalesOrderId).Select(g =>
                {
                    var order = g.First();
                    order.OrderItems = g.SelectMany(x => x.OrderItems).ToList();
                    return order;
                });

                // Finally, return collection of sales orders to the caller.
                return orders.ToList();
            }
        }

        /// <summary>
        /// Saves new sales orders and returns a list of newly inserted order ids.
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public async Task<List<int>> AddOrdersAsync(List<SalesOrder> orders)
        {
            // A collection of newly inserted order ids that will be return to the caller.
            var orderIds = new List<int>();

            // Obtain a connection to the database.
            using (var connection = _context.GetConnection())
            {
                connection.Open();
                // Start a transaction. Addition of all orders is treated as a single transaction.
                // If one fails, everything is aborted/rolled back.
                using (var transaction = _context.BeginTransaction(connection))
                {
                    try
                    {
                        // Go through each order
                        foreach (var order in orders)
                        {
                            // and save it to the database.
                            // Getting back a new sales order id
                            var orderId = await _context.ExecuteScalarAsync<int>(connection,
                                @"
                                    INSERT INTO Sales.SalesOrderHeader(OrderDate, DueDate, CustomerID, BillToAddressID, ShipToAddressID, ShipMethodId, 
                                        Status, OnlineOrderFlag, SubTotal, TaxAmt)
                                    OUTPUT inserted.SalesOrderID
                                    VALUES (@OrderDate, @OrderDate, @CustomerId, @BillToAddressId, @ShipToAddressId, @ShipMethodId, 
                                        @Status, @OnlineOrderFlag, @SubTotal, @TaxAmt);
                                ",
                                order, transaction);

                            // Add it to the collection
                            orderIds.Add(orderId);

                            // Add order items if any present
                            if (order.OrderItems?.Count > 0)
                            {
                                // First make sure that each order item is referencing parent sales order id
                                order.OrderItems.ForEach(o => o.SalesOrderId = orderId);

                                // and then save it to the database
                                await _context.ExecuteAsync(connection, @"
                                        INSERT INTO Sales.SalesOrderDetail(SalesOrderID, OrderQty, ProductID, SpecialOfferID, UnitPrice, UnitPriceDiscount)
                                        VALUES (@SalesOrderID, @OrderQty, @ProductID, 1, @UnitPrice, @UnitPriceDiscount);
                                    ",
                                    order.OrderItems, transaction);
                            }
                        }

                        // If we get here, db save succeedeed and we can commit the transaction.
                        transaction.Commit();
                    }
                    catch
                    {
                        // In case of any errors, roll back transaction and re-throw the exception
                        transaction.Rollback();
                        throw;
                    }
                }

                // Return list of saved order ids to the caller.
                return orderIds;
            }
        }

        /// <summary>
        /// Deletes sales orders based on provided list of order ids.
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public async Task DeleteOrdersAsync(List<int> orderIds)
        {
            // Obtain a connection to the database.
            using (var connection = _context.GetConnection())
            {
                connection.Open();
                // Start a transaction. Deletion of all orders is treated as a single transaction.
                // If one fails, everything is aborted/rolled back.
                using (var transaction = _context.BeginTransaction(connection))
                {
                    try
                    {
                        // First delete order items and then delete order
                        await _context.ExecuteAsync(connection, @"
                            DELETE FROM Sales.SalesOrderDetail WHERE SalesOrderID IN @ids;

                            DELETE FROM Sales.SalesOrderHeader WHERE SalesOrderID IN @ids;
                        ", new { ids = orderIds }, transaction);
                        
                        // If we get here, db deletion succeedeed and we can commit the transaction.
                        transaction.Commit();
                    }
                    catch
                    {
                        // In case of any errors, roll back transaction and re-throw the exception
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<List<string>> UpdateOrdersAsync(List<SalesOrder> orders)
        {
            var orderUpdateStatus = new List<string>();
            using (var connection = _context.GetConnection())
            {
                connection.Open();
                using (var transaction = _context.BeginTransaction(connection))
                {
                    try
                    {
                        foreach (var order in orders)
                        {
                            var result = await _context.ExecuteScalarAsync<long>(connection,
                                @"
                                UPDATE Sales.SalesOrderHeader
                                SET ShipDate=@ShipDate, Status=@Status, CustomerID=@CustomerId,
                                    BillToAddressID=@BillToAddressId, ShipToAddressID=@ShipToAddressId, ShipMethodID=@ShipMethodId,
                                    SubTotal=@SubTotal, TaxAmt=@TaxAmt, ModifiedDate=getdate(),
                                    RevisionNumber=RevisionNumber+1
                                WHERE SalesOrderID=@SalesOrderId AND RevisionNumber=@RevisionNumber;
                                SELECT @@ROWCOUNT;
                            ", order, transaction);

                            orderUpdateStatus.Add($"{order.SalesOrderId}: Order was{(result > 0 ? "" : " not")} updated");

                            // TODO: Update OrderItems
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return orderUpdateStatus;
        }
    }
}