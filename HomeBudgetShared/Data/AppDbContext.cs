using HomeBudgetShared.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HomeBudgetShared.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : 
        DbContext(options)
    {
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Currency
            modelBuilder.Entity<Currency>()
                .HasIndex(c => c.Code)
                .IsUnique();

            modelBuilder.Entity<Currency>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Currency>()
                .HasIndex(i => i.Id);

            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne<Currency>()
                .WithMany()
                .HasForeignKey(u => u.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category
            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.UserId, c.Name })
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.UserId);
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.CategoryId);
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.CurrencyId);
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date);
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.IsDeleted);

            modelBuilder.Entity<Transaction>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne<Currency>()
                .WithMany()
                .HasForeignKey(t => t.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne<Category>()
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TransactionItem>()
                .HasIndex(ti => ti.TransactionId);

            modelBuilder.Entity<TransactionItem>()
                .HasOne<Transaction>()
                .WithMany(t => t.Items)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            await Database.EnsureCreatedAsync();
        }

        public async Task<IEnumerable<TTable>> GetAllAsync<TTable>()
            where TTable : class
        {
            return await Set<TTable>().ToListAsync();
        }

        public async Task<IEnumerable<TTable>> GetFilteredAsync<TTable>(
            Expression<Func<TTable, bool>> predicate) where TTable : class
        {
            return await Set<TTable>().Where(predicate).ToListAsync();
        }

        public async Task<TTable?> GetItemByKeyAsync<TTable>(
            object primaryKey)
            where TTable : class
        {
            return await Set<TTable>().FindAsync(primaryKey);
        }

        public async Task<bool> AddItemAsync<TTable>(TTable item)
            where TTable : class
        {
            await Set<TTable>().AddAsync(item);
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateItemAsync<TTable>(TTable item)
            where TTable : class
        {
            Set<TTable>().Update(item);
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteItemAsync<TTable>(TTable item)
            where TTable : class
        {
            Set<TTable>().Remove(item);
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteItemByKeyAsync<TTable>(
            object primaryKey)
            where TTable : class
        {
            var item = await GetItemByKeyAsync<TTable>(primaryKey);
            if (item == null) return false;
            return await DeleteItemAsync(item);
        }
    }
}