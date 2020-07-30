using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AdvWorksApi.Models;
using AdvWorksApi.Services;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Services
{
    public class DbRepositoryTests
    {
        private Mock<IDbContext> _mockContext;
        private Mock<IDbConnection> _mockConnection;
        private Mock<IDbTransaction> _mockTransaction;

        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<IDbContext>();
            _mockConnection = new Mock<IDbConnection>();
            _mockTransaction = new Mock<IDbTransaction>();

            _mockContext.Setup(x => x.GetConnection()).Returns(_mockConnection.Object);
            _mockContext.Setup(x => x.BeginTransaction(_mockConnection.Object)).Returns(_mockTransaction.Object);
        }

        [Test]
        public void ConstructorTest()
        {
            // Act
            var repository = new DbRepository(_mockContext.Object);
        }

        [Test]
        public async Task DeleteOrdersAasyncTest()
        {
            // Setup
            _mockContext.Setup(x => x.ExecuteAsync(_mockConnection.Object, It.IsAny<string>(), It.IsAny<object>(),
                _mockTransaction.Object, null, null)).ReturnsAsync(1).Verifiable();

            var repository = new DbRepository(_mockContext.Object);
            var orderIds = new List<int> { 1, 2 };

            // Act
            await repository.DeleteOrdersAsync(orderIds);

            // Verify
            _mockContext.Verify();
            var result = _mockContext.Invocations[2].Arguments[2];
            Assert.That(result, Has.Property("ids").EqualTo(orderIds));
        }

        [Test]
        public async Task AddOrdersAsyncTest()
        {
            // Setup
            var savedOrders = new List<SalesOrder>();
            _mockContext.Setup(x => x.ExecuteScalarAsync<int>(_mockConnection.Object, It.IsAny<string>(), It.IsAny<object>(),
                _mockTransaction.Object, null, null))
                .ReturnsAsync<IDbConnection, string, object, IDbTransaction, int?, CommandType?, IDbContext, int>(
                (conn, sql, param, tran, timeout, cmdType) =>
                {
                    var order = (SalesOrder)param;
                    savedOrders.Add(order);
                    return order.SalesOrderId;
                })
                .Verifiable();
            _mockContext.Setup(x => x.ExecuteAsync(_mockConnection.Object, It.IsAny<string>(), It.IsAny<object>(),
                _mockTransaction.Object, null, null))
                .ReturnsAsync(10)
                .Verifiable();

            var repository = new DbRepository(_mockContext.Object);
            var orderIds = new List<int> { 1, 2 };
            var orders = orderIds.Select(x =>
                new SalesOrder
                {
                    SalesOrderId = x,
                    OrderItems = new List<SalesOrderItem> { new SalesOrderItem { SalesOrderDetailId = x * 100 } }
                })
                .ToList();

            // Act
            var result = await repository.AddOrdersAsync(orders);

            // Verify
            _mockContext.Verify();
            Assert.AreEqual(orderIds, result);
            var jsonOrders = JsonConvert.SerializeObject(orders);
            var jsonSavedOrders = JsonConvert.SerializeObject(savedOrders);
            Assert.AreEqual(jsonOrders, jsonSavedOrders);
            foreach (var order in savedOrders)
            {
                Assert.IsTrue(order.OrderItems.All(x => x.SalesOrderId == order.SalesOrderId));
            }
        }

        [Test]
        public async Task GetOrdersAsyncTest()
        {
            // Setup
            var response = new List<SalesOrder>
            {
                new SalesOrder { SalesOrderId = 1, OrderItems = new List<SalesOrderItem>{ new SalesOrderItem { SalesOrderDetailId = 10 } } },
                new SalesOrder { SalesOrderId = 1, OrderItems = new List<SalesOrderItem>{ new SalesOrderItem { SalesOrderDetailId = 11 } } },
                new SalesOrder { SalesOrderId = 2, OrderItems = new List<SalesOrderItem>{ new SalesOrderItem { SalesOrderDetailId = 20 } } }
            };
            _mockContext.Setup(x => x.QueryAsync(
                _mockConnection.Object, It.IsAny<string>(),
                It.IsAny<Func<SalesOrder, SalesOrderItem, SalesOrder>>(),
                It.IsAny<object>(),
                null, It.IsAny<bool>(), It.IsAny<string>(), null, null))
                .ReturnsAsync(response.AsEnumerable())
                .Verifiable();

            var repository = new DbRepository(_mockContext.Object);
            var orderIds = new List<int> { 1, 2 };
            var expected = new List<SalesOrder>
            {
                new SalesOrder
                {
                    SalesOrderId = 1,
                    OrderItems = new List<SalesOrderItem>
                    {
                        new SalesOrderItem { SalesOrderDetailId = 10 },
                        new SalesOrderItem { SalesOrderDetailId = 11 }
                    }
                },
                new SalesOrder
                {
                    SalesOrderId = 2,
                    OrderItems = new List<SalesOrderItem>{ new SalesOrderItem { SalesOrderDetailId = 20 } }
                }
            };


            // Act
            var result = await repository.GetOrdersAsync(orderIds);

            // Verify
            _mockContext.Verify();

            var jsonExpected = JsonConvert.SerializeObject(expected);
            var jsonResult = JsonConvert.SerializeObject(result);
            Assert.AreEqual(jsonExpected, jsonResult);
        }

        [TestCase(0, "Order was not updated", TestName = "UpdateOrdersAsync - Should not update")]
        [TestCase(1, "Order was updated", TestName = "UpdateOrdersAsync - Should update")]
        public async Task UpdateOrdersAsyncTest(int affectedRecords, string expectedMessage)
        {
            // Setup
            _mockContext.Setup(x => x.ExecuteScalarAsync<long>(_mockConnection.Object, It.IsAny<string>(), It.IsAny<object>(),
                _mockTransaction.Object, null, null)).ReturnsAsync(affectedRecords).Verifiable();

            var repository = new DbRepository(_mockContext.Object);
            var order = new List<SalesOrder> { new SalesOrder { SalesOrderId = 1, RevisionNumber = 2 } };

            var expected = new List<string> { $"1: {expectedMessage}" };

            // Act
            var result = await repository.UpdateOrdersAsync(order);

            // Verify
            _mockContext.Verify();
            Assert.AreEqual(expected, result);
        }
    }
}