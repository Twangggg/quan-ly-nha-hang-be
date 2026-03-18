using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(
                    Microsoft
                        .EntityFrameworkCore
                        .Diagnostics
                        .CoreEventId
                        .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning
                );
            });
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<OrderAuditLog> OrderAuditLogs { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

        // Billing
        // (Invoices and Payments removed)
        public DbSet<Reservation> Reservations { get; set; } = null!;

        // Menu Management
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<OptionGroup> OptionGroups { get; set; } = null!;
        public DbSet<OptionItem> OptionItems { get; set; } = null!;
        public DbSet<SetMenu> SetMenus { get; set; } = null!;
        public DbSet<SetMenuItem> SetMenuItems { get; set; } = null!;

        // Order Item Options
        public DbSet<OrderItemOptionGroup> OrderItemOptionGroups { get; set; } = null!;
        public DbSet<OrderItemOptionValue> OrderItemOptionValues { get; set; } = null!;

        // Table Management
        public DbSet<Table> Tables { get; set; } = null!;
        public DbSet<Area> Areas { get; set; } = null!;

        // Inventory
        public DbSet<Ingredient> Ingredients { get; set; } = null!;
        public DbSet<InventorySettings> InventorySettings { get; set; } = null!;
        public DbSet<InventoryCheck> InventoryChecks { get; set; } = null!;
        public DbSet<InventoryCheckItem> InventoryCheckItems { get; set; } = null!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
        public DbSet<StockInReceipt> StockInReceipts { get; set; } = null!;
        public DbSet<StockInReceiptItem> StockInReceiptItems { get; set; } = null!;
        public DbSet<StockOutReceipt> StockOutReceipts { get; set; } = null!;
        public DbSet<StockOutReceiptItem> StockOutReceiptItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
