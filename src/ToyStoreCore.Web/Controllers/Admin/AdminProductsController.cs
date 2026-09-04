using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Products")]
public class AdminProductsController : Controller
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminProductsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products.Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(products);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(new Product());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Product product,
        IFormFile? imageFile,
        List<string>? galleryUrls,
        List<IFormFile>? galleryFiles)
    {
        if (imageFile is not null && !TryValidateImageFile(imageFile, out var mainImageError))
        {
            ModelState.AddModelError(string.Empty, mainImageError!);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(product);
        }

        if (imageFile is not null)
        {
            product.ImageUrl = await SaveUploadedFileAsync(imageFile);
        }

        product.CreatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await AddGalleryImagesAsync(product.Id, galleryUrls, galleryFiles);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã thêm sản phẩm mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.Include(p => p.Images.OrderBy(i => i.DisplayOrder)).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(product);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Product product,
        IFormFile? imageFile,
        List<string>? galleryUrls,
        List<IFormFile>? galleryFiles,
        List<int>? removeImageIds)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        if (imageFile is not null && !TryValidateImageFile(imageFile, out var mainImageError))
        {
            ModelState.AddModelError(string.Empty, mainImageError!);
        }

        if (!ModelState.IsValid)
        {
            product.Images = await _db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.DisplayOrder).ToListAsync();
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(product);
        }

        if (imageFile is not null)
        {
            product.ImageUrl = await SaveUploadedFileAsync(imageFile);
        }

        _db.Products.Update(product);

        if (removeImageIds is { Count: > 0 })
        {
            var toRemove = await _db.ProductImages.Where(i => i.ProductId == id && removeImageIds.Contains(i.Id)).ToListAsync();
            _db.ProductImages.RemoveRange(toRemove);
        }

        await AddGalleryImagesAsync(id, galleryUrls, galleryFiles);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is not null)
        {
            // Soft delete so past orders keep referencing a valid product row.
            product.IsActive = false;
            await _db.SaveChangesAsync();
        }

        TempData["Message"] = "Đã ẩn sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task AddGalleryImagesAsync(int productId, List<string>? galleryUrls, List<IFormFile>? galleryFiles)
    {
        var nextOrder = (await _db.ProductImages.Where(i => i.ProductId == productId).CountAsync());

        foreach (var url in (galleryUrls ?? new List<string>()).Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            _db.ProductImages.Add(new ProductImage { ProductId = productId, ImageUrl = url.Trim(), DisplayOrder = nextOrder++ });
        }

        foreach (var file in (galleryFiles ?? new List<IFormFile>()).Where(f => f.Length > 0))
        {
            if (!TryValidateImageFile(file, out _))
            {
                continue; // skip invalid files silently for gallery extras
            }

            var savedUrl = await SaveUploadedFileAsync(file);
            _db.ProductImages.Add(new ProductImage { ProductId = productId, ImageUrl = savedUrl, DisplayOrder = nextOrder++ });
        }
    }

    private bool TryValidateImageFile(IFormFile file, out string? error)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            error = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif, .webp).";
            return false;
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            error = "Kích thước ảnh tối đa 5MB.";
            return false;
        }

        error = null;
        return true;
    }

    private async Task<string> SaveUploadedFileAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/products/{fileName}";
    }
}
