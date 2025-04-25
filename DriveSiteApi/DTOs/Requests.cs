namespace DriveApi.Controlers;

public class Requests
{
    public string UserEmail { get; set; }
    public string SiteRelativePath { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
}

public class UploadFileRequest
{
    public string UserEmail { get; set; }
    public string SiteRelativePath { get; set; }
    public string FolderAbsolutePath { get; set; }
    public string FolderItemId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
}