using System;


namespace TiendaPlayeras.Web.Models
{
/// <summary>Diseños subidos por el cliente para personalización.</summary>
public class DesignUpload
{
public int Id { get; set; }
public string UserId { get; set; } = string.Empty;
public ApplicationUser? User { get; set; }
public string FileName { get; set; } = string.Empty; // nombre original
public string StoragePath { get; set; } = string.Empty; // ruta interna segura
public long FileSizeBytes { get; set; }
public string ContentType { get; set; } = string.Empty; // image/png, image/svg+xml, application/pdf, image/jpeg
public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
public bool IsActive { get; set; } = true;
}
}