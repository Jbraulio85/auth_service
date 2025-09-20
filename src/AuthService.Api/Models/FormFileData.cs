using AuthService.Application.Interfaces;

namespace AuthService.Api.Models;

public class FormFileData : IFileData
{
    public byte[] Data { get; init; }
    public string ContentType { get; init; }
    public string FileName { get; init; }
    public long Size { get; init; }

    public FormFileData(IFormFile formFile)
    {
        ArgumentNullException.ThrowIfNull(formFile);

        ContentType = formFile.ContentType;
        FileName = formFile.FileName;
        Size = formFile.Length;

        using var memoryStream = new MemoryStream();
        formFile.CopyTo(memoryStream);
        Data = memoryStream.ToArray();
    }
}