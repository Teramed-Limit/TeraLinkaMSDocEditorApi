using Microsoft.AspNetCore.SignalR;

namespace TeraLinkaMSDocEditorApi.Web.Hubs;

public class DocumentHub : Hub
{
    public async Task SendSaveStatus(string documentId, string status)
    {
        await Clients.All.SendAsync("ReceiveSaveStatus", documentId, status);
    }
} 