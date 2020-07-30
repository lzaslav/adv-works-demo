using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvWorksApi.Models;
using AdvWorksApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdvWorksApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IDbRepository _dbRepository;

        public OrderController(IDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        private List<int> ExtractIdsFromString(string value)
        {
            // TODO: Handle non numeric values inside "ids" parameter
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        }

        // GET api/order?ids=1,2,3
        [HttpGet]
        public async Task<ActionResult<List<SalesOrder>>> Get(string ids)
        {
            var orderIds = ExtractIdsFromString(ids);
            return await _dbRepository.GetOrdersAsync(orderIds);
        }

        // POST api/order
        [HttpPost]
        public async Task<ActionResult<List<int>>> Post([FromBody] List<SalesOrder> orders)
        {
            // TODO: Add validation step.
            return await _dbRepository.AddOrdersAsync(orders);
        }

        // PUT api/order
        [HttpPut]
        public async Task<List<string>> Put([FromBody] List<SalesOrder> orders)
        {
            // TODO: Add validation step.
            return await _dbRepository.UpdateOrdersAsync(orders);
        }

        // DELETE api/order
        [HttpDelete]
        public async Task Delete(string ids)
        {
            var orderIds = ExtractIdsFromString(ids);
            await _dbRepository.DeleteOrdersAsync(orderIds);
        }
    }
}
