using Celigo.Outlook.Commons;
using Celigo.Outlook.Commons.Models;
using Codoxide;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace DriveApi.Controlers;

[Route("api/[controller]")]
[ApiController]
public class DriveController: ControllerBase
{
    private readonly DriveManager _driveManager;
    private readonly ILogger<DriveController> _logger;
    private readonly GraphClientProvider _graphGraphClientProvider;
    private readonly string tenantId = "f373d9c4-e1b2-4c4a-be2c-2c54e3e04e4c";

    public DriveController(
        DriveManager driveManager,
        ILogger<DriveController> logger,
        GraphClientProvider graphGraphClientProvider)
    {
        _driveManager = driveManager;
        _logger = logger;
        _graphGraphClientProvider = graphGraphClientProvider;
    }

    [HttpPost("create-folder")]
    public async Task<IActionResult> CreateFolderInDrive([FromBody] Requests folderData)
    {
        var fileContentInBytes = System.IO.File.ReadAllBytes("/Users/somenathmaji/Downloads/customscript2350.xml");
        var folderItemId = "";
        await _graphGraphClientProvider.GetGraphClient(tenantId)
            .Map(async graphClient =>
            {
                var siteRoot = await graphClient.Sites[folderData.SiteRelativePath].GetAsync();
                var defaultDrive = await graphClient.Sites[siteRoot?.Id].Drive.GetAsync();
                var parentFolderItem = await graphClient.Drives[defaultDrive?.Id].Root.GetAsync();
                var folderId = string.IsNullOrEmpty(folderData.Id) ? parentFolderItem?.Id : folderData.Id;
                var newFolderItem = new DriveItem
                {
                    Name = folderData.Name,
                    Folder = new Folder(),
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "replace" }
                    }
                };
                return await graphClient.Drives[defaultDrive?.Id].Items[folderId].Children.PostAsync(newFolderItem);
            })
            .Map(driveItem =>
            {
                _logger.LogDebug("Successfully created folder with driveItemId: {driveItemId} for user: {user} in site: {site}",
                    driveItem.Id, folderData.UserEmail, folderData.SiteRelativePath);
                folderItemId = driveItem.Id;
                return driveItem.Id;
            })
            .Catch(failure =>
            {
                if (failure.ToException() is ODataError { Error: not null } error)
                {
                    _logger.LogError(failure,
                        "Error occurred while creating folder driveItem in site {site} for user {userEmail} " +
                        "using graph api. ErrorCode {errorCode} ErrorMessage {errorMessage} InnerError {innerError}",
                        folderData.SiteRelativePath,
                        folderData.UserEmail,
                        error.Error.Code,
                        error.Error.Message,
                        error.Error.InnerError);
                    return failure;
                }
                _logger.LogError(failure, "Failed to create driveItem for user: {email} and site: {site}", 
                    folderData.UserEmail, folderData.SiteRelativePath);
                return failure;
            });

        return Ok(folderItemId);
    }
    
    [HttpPost("upload-file")]
    public async Task<IActionResult> UploadFileInDrive([FromBody] UploadFileRequest uploadData)
    {
        var fileId = "";
        await _graphGraphClientProvider.GetGraphClient(tenantId)
            .Map(async graphClient =>
            {
                var siteRoot = await graphClient.Sites[uploadData.SiteRelativePath].GetAsync();
                var defaultDrive = await graphClient.Sites[siteRoot?.Id].Drive.GetAsync();
                var folderId = uploadData.FolderItemId;
                
                // check by path (for testing)
                if (string.IsNullOrEmpty(folderId))
                {
                    var folderDriveItem = await graphClient.Drives[defaultDrive?.Id].Root
                        .ItemWithPath(uploadData.FolderAbsolutePath.Trim('/'))
                        .GetAsync();
                    folderId = folderDriveItem.Id;
                }
                
                var parentFolder = await graphClient.Drives[defaultDrive?.Id].Items[folderId].GetAsync();
                var fileItem = new DriveItem
                {
                    Name = uploadData.FileName,
                    File = new FileObject(),
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "replace" }
                    },
                };
                
                var newFileItem = await graphClient.Drives[defaultDrive?.Id].Items[parentFolder?.Id].Children.PostAsync(fileItem);

                using (var fileStream = new FileStream(uploadData.FilePath, FileMode.Open))
                {
                    return await graphClient.Drives[defaultDrive?.Id].Items[newFileItem?.Id].Content.PutAsync(fileStream);  
                };
            })
            .Map(driveItem =>
            {
                _logger.LogDebug("Successfully uploaded file with driveItemId: {driveItemId} for user: {user} in site: {site}",
                    driveItem.Id, uploadData.UserEmail, uploadData.SiteRelativePath);
                fileId = driveItem.Id;
                return driveItem.Id;
            })
            .Catch(failure =>
            {
                if (failure.ToException() is ODataError { Error: not null } error)
                {
                    _logger.LogError(failure,
                        "Error occurred while uploading file in site {site} for user {userEmail} " +
                        "using graph api. ErrorCode {errorCode} ErrorMessage {errorMessage} InnerError {innerError}",
                        uploadData.SiteRelativePath,
                        uploadData.UserEmail,
                        error.Error.Code,
                        error.Error.Message,
                        error.Error.InnerError);
                    return failure;
                }
                _logger.LogError(failure, "Failed to upload file for user: {email} and site: {site}", uploadData.UserEmail, uploadData.SiteRelativePath);
                return failure;
            });

        return Ok(fileId);
    }
}