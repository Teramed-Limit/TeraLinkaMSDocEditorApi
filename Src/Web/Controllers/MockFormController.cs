using Microsoft.AspNetCore.Mvc;
using TeraLinkaMSDocEditorApi.Application.Services;

namespace TeraLinkaMSDocEditorApi.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MockFormController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public MockFormController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public IActionResult GetDocuments()
    {
        return Ok(new
        {
            id1 = Guid.NewGuid(),
            id2 = Guid.NewGuid(),
            id3 = Guid.NewGuid(),
            id4 = Guid.NewGuid(),
            date = "2024-05-02",
            checkbox = "True",
        });
    }
}