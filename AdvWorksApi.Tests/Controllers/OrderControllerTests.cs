using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvWorksApi.Controllers;
using AdvWorksApi.Models;
using AdvWorksApi.Services;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Controllers
{
    public class OrderControllerTests
    {
        private Mock<IDbRepository> _mockRepository;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<IDbRepository>();
        }

        [Test]
        public void ConstructorTest()
        {
            // Act
            var controller = new OrderController(null);
        }

        [TestCase(new int[] { 1 }, TestName = "Get-SingleId")]
        [TestCase(new int[] { 1, 2 }, TestName = "Get-MultipleIds")]
        [TestCase(new int[] { }, TestName = "Get-NoIds")]
        public async Task GetMethod(int[] ids)
        {
            // Setup
            var orderIds = ids.ToList();
            var expected = new List<SalesOrder> { new SalesOrder { SalesOrderId = 1, Status = OrderStatus.Approved } };
            _mockRepository.Setup(p => p.GetOrdersAsync(orderIds)).ReturnsAsync(expected).Verifiable();

            var controller = new OrderController(_mockRepository.Object);

            // Act
            var result = await controller.Get(string.Join(",", orderIds));

            // Verify
            var jsonExpected = JsonConvert.SerializeObject(expected);
            var jsonResult = JsonConvert.SerializeObject(result.Value);
            Assert.AreEqual(jsonExpected, jsonResult);
            _mockRepository.Verify();
        }

        [TestCase(new int[] { 1 }, TestName = "Post-Single")]
        [TestCase(new int[] { 1, 2 }, TestName = "Post-Multiple")]
        [TestCase(new int[] { }, TestName = "Post-Empty")]
        public async Task PostMethod(int[] ids)
        {
            // Setup
            var orders = ids.Select(x => new SalesOrder { SalesOrderId = x, SalesOrderNumber = $"SO{x}" }).ToList();
            var expected = ids.ToList();
            _mockRepository.Setup(p => p.AddOrdersAsync(orders)).ReturnsAsync(expected).Verifiable();

            var controller = new OrderController(_mockRepository.Object);

            // Act
            var result = await controller.Post(orders);

            // Verify
            var jsonExpected = JsonConvert.SerializeObject(expected);
            var jsonResult = JsonConvert.SerializeObject(result.Value);
            Assert.AreEqual(jsonExpected, jsonResult);
            _mockRepository.Verify();
        }


        [TestCase(new int[] { 1 }, TestName = "Put-Single")]
        [TestCase(new int[] { 1, 2 }, TestName = "Put-Multiple")]
        [TestCase(new int[] { }, TestName = "Put-Empty")]
        public async Task PutMethod(int[] ids)
        {
            // Setup
            var orders = ids.Select(x => new SalesOrder { SalesOrderId = x, SalesOrderNumber = $"SO{x}" }).ToList();
            var expected = ids.Select(x => $"{x}: yes").ToList();
            _mockRepository.Setup(p => p.UpdateOrdersAsync(orders)).ReturnsAsync(expected).Verifiable();

            var controller = new OrderController(_mockRepository.Object);

            // Act
            var result = await controller.Put(orders);

            // Verify
            var jsonExpected = JsonConvert.SerializeObject(expected);
            var jsonResult = JsonConvert.SerializeObject(result);
            Assert.AreEqual(jsonExpected, jsonResult);
            _mockRepository.Verify();
        }

        [TestCase(new int[] { 1 }, TestName = "Delete-SingleId")]
        [TestCase(new int[] { 1, 2 }, TestName = "Delete-MultipleIds")]
        [TestCase(new int[] { }, TestName = "Delete-NoIds")]
        public async Task DeleteMethod(int[] ids)
        {
            // Setup
            var orderIds = ids.ToList();
            _mockRepository.Setup(p => p.DeleteOrdersAsync(orderIds)).Verifiable();

            var controller = new OrderController(_mockRepository.Object);

            // Act
            await controller.Delete(string.Join(',', orderIds));

            // Verify
            _mockRepository.Verify();
        }
    }
}