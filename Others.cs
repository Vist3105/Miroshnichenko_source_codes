using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Store
{
    class Transaction
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime RedactionDateTime { get; set; }
        public double Amount { get; set; }
        public Contragent Reciever { get; set; }
        public Contragent Giver {  get; set; }
        public string Comment { get; set; }

    }

    class Safe
    {
        public int Id { get; set; }
        public double Amount { get; set; } = 0;
    }

    class Contragent
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public Safe Account { get; set; }
    }
    class DataContext : DbContext
    {
        public DataContext() { }
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source = UserData.db");
            }
        }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Contragent> Contragents { get; set; }
        public DbSet<Safe> Safes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contragent>()
                .HasOne(c => c.Account)
                .WithOne()
                .HasForeignKey<Contragent>("AccountId");
        }
    }
}
