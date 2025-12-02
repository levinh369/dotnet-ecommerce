namespace ProjectTest1.ViewModels
{
    public class OrderStatsViewModel
    {
        public int TotalOrders { get; set; }=0;
        public int TotalProductsSold { get; set; }=0;
        public double? TotalRevenue { get; set; }
        public int TotalOrdersCancel { get; set; } = 0;

    }
}
