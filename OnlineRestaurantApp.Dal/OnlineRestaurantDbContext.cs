using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;

namespace OnlineRestaurantApp.Dal
{
    public class OnlineRestaurantDbContext:DbContext
    {
        public OnlineRestaurantDbContext(DbContextOptions<OnlineRestaurantDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Slider>().ToTable("Slider");
            modelBuilder.Entity<FoodItem>().ToTable("FoodItem");
            modelBuilder.Entity<ItemType>().ToTable("ItemType");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<OrderItemDetails>().ToTable("OrderItemDetails");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<PaymentCard>().ToTable("PaymentCard");
            modelBuilder.Entity<PaymentType>().ToTable("PaymentType");
            modelBuilder.Entity<Employee>().ToTable("Employee");                 
            modelBuilder.Entity<Delivery>().ToTable("Delivery");
            modelBuilder.Entity<DeliveryAddress>().ToTable("DeliveryAddress");
            modelBuilder.Entity<Feedback>()
                            .ToTable("Feedback", "dbo")
                            .Property(f => f.Status)
                            .HasConversion<string>()     // enum <-> string
                            .HasMaxLength(15);


        }
        public DbSet<Category> categories { get; set; }
        public DbSet<Slider> sliders { get; set; }

        public DbSet<FoodItem> foodItems { get; set; }

        public DbSet<ItemType> itemTypes { get; set; }

        public DbSet<User> users { get; set; }

        public DbSet<Role> roles { get; set; }
        //public DbSet<OnlineRestaurantApp.Models.UserLogin> UserLogin { get; set; } = default!;
        public DbSet<Order> orders { get; set; }
        public DbSet<OrderItemDetails> orderItemDetails { get; set; }
        public DbSet<Payment> payments {  get; set; }
        public DbSet<PaymentCard> paymentCards { get; set; }
        public DbSet<PaymentType> paymentTypes {  get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DeliveryAddress> DeliveryAddresses { get; set; }
        public DbSet<Feedback> feedbacks { get; set; } = null!;


    }
}
