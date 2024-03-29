using DevToys.Api;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DevToys.Web.Services;

[Export(typeof(IClipboard))]
public class Clipboard : IClipboard
{
    public async Task<object?> GetClipboardDataAsync()
    {
        return null!;
    }

    public async Task<FileInfo[]?> GetClipboardFilesAsync()
    {
        return null!;
    }

    public async Task<Image<Rgba32>?> GetClipboardImageAsync()
    {
        return null!;
    }

    public async Task<string?> GetClipboardTextAsync()
    {
        return null!;
    }

    public async Task SetClipboardFilesAsync(FileInfo[]? filePaths)
    {
        //return null!;
    }

    public async Task SetClipboardImageAsync(Image? image)
    {
        //return null!;
    }

    public async Task SetClipboardTextAsync(string? data)
    {
        //return null!;
    }
}
