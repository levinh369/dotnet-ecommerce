using Microsoft.EntityFrameworkCore;
using ProjectTest1.Models;
using System.Drawing;

namespace ProjectTest1.Repository
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<ColorModel> Colors { get; set; }
        public DbSet<SizeModel> Sizes { get; set; }
        public DbSet<ProductModel> Product { get; set; }
        public DbSet<ProductVariantModel> ProductVariants { get; set; }
        public DbSet<ProductImageModel> ProductImages { get; set; }
        public DbSet<CartModel> CartModels { get; set; }
        public DbSet<CartItemModel> CartItemModels { get; set; }
        public DbSet<UserModel> User { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderDetailModel> OrderDetails { get; set; }
        public DbSet<OrderNotificationModel> OrderNotification { get; set; }
        public DbSet<ConversationModel> Conversations { get; set; }
        public DbSet<MessageModel> Messages { get; set; }
        public DbSet<ReviewModel> Reviews { get; set; }
        public DbSet<ProductColorImageModel> ProductColorImages { get; set; }
        public DbSet<VoucherModel> Vouchers { get; set; }
        public DbSet<UserVoucherModel> UserVouchers { get; set; }
        public DbSet<UserPointsModel> UserPoints { get; set; }
        public DbSet<PointHistoryModel> PointHistories { get; set; }

    }
}
