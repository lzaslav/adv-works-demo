using System;

namespace AdvWorksApi.Models
{
    public class SalesOrderItem
    {
        public long SalesOrderId { get; set; }
        public long SalesOrderDetailId { get; set; }
        public short OrderQty { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPriceDiscount { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}