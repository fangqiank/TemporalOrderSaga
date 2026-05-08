using Client.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(SeedData.Products);
    }
}

file static class SeedData
{
    public static Product[] Products =>
    [
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "机械键盘 Pro", Description = "Cherry MX 轴体，RGB 背光，全键无冲", Price = 599, ImageUrl = "/img/keyboard.svg", Stock = 50, Category = "键盘" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "静电容键盘", Description = "Topre 轴，静音设计，PBT 键帽", Price = 1299, ImageUrl = "/img/keyboard2.svg", Stock = 20, Category = "键盘" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "无线鼠标", Description = "26000DPI，蓝牙/2.4G 双模，续航70小时", Price = 349, ImageUrl = "/img/mouse.svg", Stock = 80, Category = "鼠标" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "电竞鼠标", Description = "轻量化59g，PAW3395 传感器，RGB", Price = 299, ImageUrl = "/img/mouse2.svg", Stock = 60, Category = "鼠标" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "27寸 4K 显示器", Description = "IPS 面板，HDR400，Type-C 90W 反向充电", Price = 2499, ImageUrl = "/img/monitor.svg", Stock = 15, Category = "显示器" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "34寸曲面屏", Description = "WQHD 3440x1440，165Hz，1ms 响应", Price = 3999, ImageUrl = "/img/monitor2.svg", Stock = 10, Category = "显示器" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Name = "降噪耳机", Description = "主动降噪，40小时续航，Hi-Res 认证", Price = 899, ImageUrl = "/img/headphone.svg", Stock = 35, Category = "音频" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Name = "桌面音箱", Description = "2.0 声道，蓝牙5.3，木质箱体", Price = 459, ImageUrl = "/img/speaker.svg", Stock = 40, Category = "音频" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Name = "鼠标垫 XXL", Description = "900x400mm，加厚防滑底，锁边设计", Price = 79, ImageUrl = "/img/mousepad.svg", Stock = 200, Category = "配件" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), Name = "USB-C 拓展坞", Description = "12合1，双HDMI 4K60Hz，100W PD", Price = 369, ImageUrl = "/img/hub.svg", Stock = 45, Category = "配件" },
    ];
}
