using Integration.BusinessLogics.Nps.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Integration.WebApi.Controllers;

/// <summary>
/// Receives inbound ISO 20022 messages pushed by the NIBSS National Payment Stack (NPS)
/// to this institution's registered callback URLs (https://&lt;baseURL&gt;/&lt;npsMessageType&gt;).
/// The payload is signed and encrypted XML; processing (decryption, signature validation,
/// message-type detection and handler dispatch) is delegated to the business-logic layer
/// via <see cref="ProcessInboundNpsMessageCommand"/>. Per the NPS integration guide, a
/// successful receipt is acknowledged immediately with HTTP 200.
/// </summary>
[ApiController]
[Consumes("application/xml")]
public class NibbsNPSWebhookController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// NPS inbound message callback. One action serves every registered callback path —
    /// the actual message type is detected from the decrypted XML namespace, not the route.
    /// </summary>
    /// <remarks>
    /// Endpoints: /pacs008 (credit transfer), /pacs002 (payment status report),
    /// /pacs028 (payment status request), /acmt023 (name enquiry request),
    /// /acmt024 (name enquiry report), /pain001 (credit transfer initiation),
    /// /pain002 (customer payment status report) and /admi002 (message reject).
    /// </remarks>
    /// <param name="cancellationToken">Aborts processing when the request is cancelled.</param>
    /// <returns>200 when the message was decrypted, verified and accepted; 400 otherwise.</returns>
    [HttpPost("pacs008")]
    [HttpPost("pacs002")]
    [HttpPost("pacs028")]
    [HttpPost("acmt023")]
    [HttpPost("acmt024")]
    [HttpPost("pain001")]
    [HttpPost("pain002")]
    [HttpPost("admi002")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        string rawXml;
        using (var reader = new StreamReader(Request.Body))
        {
            rawXml = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawXml))
            return BadRequest("Request body is empty.");

        var result = await mediator.Send(
            new ProcessInboundNpsMessageCommand(rawXml, Request.Path),
            cancellationToken);

        // NPS requires an immediate HTTP 200 acknowledgment.
        return result.Accepted ? Ok() : BadRequest(result.Error);
    }
}
