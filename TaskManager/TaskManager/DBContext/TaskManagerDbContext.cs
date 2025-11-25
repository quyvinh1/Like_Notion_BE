using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

namespace TaskManager.DBContext
{
    public class TaskManagerDbContext : IdentityDbContext<User>
    {
        public TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options) : base(options)
        {

        }
        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<ContentBlock> ContentBlocks { get; set; }
        public DbSet<PagePermission> PagePermissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Comment> Comments { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TodoItem>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PagePermission>()
                .HasOne(p => p.Page)
                .WithMany(t => t.SharedWith)
                .HasForeignKey(p => p.PageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PagePermission>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<TodoItem>().HasQueryFilter(t => !t.IsDelete);
        }

    }
}
