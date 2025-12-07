using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orch_back_API.Entities
{
    public class MyJDBContext : DbContext
    {
        public MyJDBContext() { }
        public MyJDBContext(DbContextOptions<MyJDBContext> options) : base(options)
        {

        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Messages> Messages { get; set; }
        public DbSet<Notifications> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations from assembly first
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

            // Then OVERRIDE with PostgreSQL-specific configurations
            ConfigureForPostgreSQL(modelBuilder);
        }

        private void ConfigureForPostgreSQL(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Fix GUID properties to use UUID in PostgreSQL
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(Guid))
                    {
                        property.SetColumnType("uuid");
                        if (property.Name == "Id" && !property.IsNullable)
                        {
                            property.SetDefaultValueSql("gen_random_uuid()");
                        }
                    }
                    
                    // Fix datetime properties - replace any SQL Server defaults
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        var defaultValue = property.GetDefaultValueSql();
                        if (defaultValue?.ToLower().Contains("getutcdate") == true ||
                            defaultValue?.ToLower().Contains("getdate") == true)
                        {
                            property.SetDefaultValueSql("NOW()");
                        }
                        else if (defaultValue == null && property.Name.EndsWith("At", StringComparison.OrdinalIgnoreCase))
                        {
                            // Common pattern: set default for CreatedAt, UpdatedAt, etc.
                            property.SetDefaultValueSql("NOW()");
                        }
                    }

                    // Fix string properties
                    if (property.ClrType == typeof(string))
                    {
                        if (property.GetMaxLength() == null)
                        {
                            property.SetColumnType("text");
                        }
                    }
                }
            }
        }
    }
}
