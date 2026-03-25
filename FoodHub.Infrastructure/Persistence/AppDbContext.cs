using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence
{
    public partial class AppDbContext : DbContext
    {
        private readonly IAuditLogService _auditLogService;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IAuditLogService auditLogService)
            : base(options)
        {
            _auditLogService = auditLogService;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    EntityName = entry.Entity.GetType().Name,
                    ActorInfo = _auditLogService.GetActorInfo()
                };
                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.AuditAction = AuditAction.Create;
                            auditEntry.NewValues[propertyName] = GetValue(property.CurrentValue);
                            break;

                        case EntityState.Deleted:
                            auditEntry.AuditAction = AuditAction.Delete;
                            auditEntry.OldValues[propertyName] = GetValue(property.OriginalValue);
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.AuditAction = AuditAction.Update;
                                auditEntry.OldValues[propertyName] = GetValue(property.OriginalValue);
                                auditEntry.NewValues[propertyName] = GetValue(property.CurrentValue);
                            }
                            break;
                    }
                }

                // Refine action for status changes & activation
                if (entry.State == EntityState.Modified)
                {
                    // Detect Activation/Deactivation for Employee or entities with IsActive/Status
                    var statusProp = entry.Properties.FirstOrDefault(p => (p.Metadata.Name == "Status" || p.Metadata.Name == "EmployeeStatus" || p.Metadata.Name == "IsActive") && p.IsModified);
                    if (statusProp != null)
                    {
                        if (entry.Entity is Employee employeeEntity)
                        {
                            auditEntry.AuditAction = employeeEntity.Status == EmployeeStatus.Active ? AuditAction.Activate : AuditAction.Deactivate;
                        }
                        else if (entry.Entity is Reservation resEntity)
                        {
                            auditEntry.AuditAction = resEntity.Status switch
                            {
                                ReservationStatus.Cancelled => AuditAction.Cancel,
                                ReservationStatus.CheckIn => AuditAction.CheckIn,
                                ReservationStatus.NoShow => AuditAction.NoShow,
                                _ => AuditAction.StatusChange
                            };
                        }
                        else
                        {
                            auditEntry.AuditAction = AuditAction.StatusChange;
                        }
                    }
                    else if (entry.Properties.Any(p => p.Metadata.Name == "Role" && p.IsModified))
                    {
                        auditEntry.AuditAction = AuditAction.ChangeRole;
                    }
                }

                // Capture guest info for Reservation if unauthenticated
                if (entry.Entity is Reservation res && !_auditLogService.GetActorInfo().Contains("Employee"))
                {
                    auditEntry.ActorInfo = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        type = "Guest",
                        name = res.CustomerName,
                        phone = res.CustomerPhone
                    });
                }
            }


            return auditEntries.Where(_ => _.HasChanges).ToList();
        }

        private object? GetValue(object? value)
        {
            if (value == null) return null;
            if (value.GetType().IsEnum) return value.ToString();
            return value;
        }

        private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return Task.CompletedTask;

            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            return base.SaveChangesAsync();
        }

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
                warnings.Ignore(
                    Microsoft
                        .EntityFrameworkCore
                        .Diagnostics
                        .RelationalEventId
                        .PendingModelChangesWarning
                );
            });
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // HR / Master Data
        public DbSet<Shift> Shifts { get; set; } = null!;
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<OrderAuditLog> OrderAuditLogs { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

        // Billing
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;

        // Menu Management
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<OptionGroup> OptionGroups { get; set; } = null!;
        public DbSet<OptionItem> OptionItems { get; set; } = null!;
        public DbSet<MenuItemOptionGroup> MenuItemOptionGroups { get; set; } = null!;
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
        public DbSet<InventoryLot> InventoryLots { get; set; } = null!;
        public DbSet<InventoryLotMovement> InventoryLotMovements { get; set; } = null!;
        public DbSet<StockInReceipt> StockInReceipts { get; set; } = null!;
        public DbSet<StockInReceiptItem> StockInReceiptItems { get; set; } = null!;
        public DbSet<StockOutReceipt> StockOutReceipts { get; set; } = null!;
        public DbSet<StockOutReceiptItem> StockOutReceiptItems { get; set; } = null!;
        public DbSet<StockOutReceiptItemLotAllocation> StockOutReceiptItemLotAllocations { get; set; } =
            null!;

        public DbSet<InventoryGroup> InventoryGroups { get; set; } = null!;
        
        // Promotion Management
        public DbSet<Promotion> Promotions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
