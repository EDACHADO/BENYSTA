using Integration.BusinessLogics.Concrete;
using Integration.BusinessLogics.Nps.Commands;
using Integration.BusinessLogics.Nps.Models;
using Integration.BusinessLogics.Nps.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nibbs.Nps.Integration.Messages;

namespace Integration.WebApi.Controllers;

/// <summary>
/// Receives client payment requests and dispatches them to the NIBSS National
/// Payment Stack (NPS) switch as ISO 20022 messages, via the business-logic
/// commands (CQRS/MediatR).
/// </summary>
/// <remarks>
/// Per the NPS integration guide, an HTTP 200 from the switch only acknowledges
/// receipt of the message — the business outcome (e.g. a pacs.002 status report)
/// arrives asynchronously on this institution's inbound callback URL. Dispatch
/// endpoints therefore return 202 Accepted rather than 200 OK.
/// </remarks>
[ApiController]
[Route("api/nibbs")]
[Produces("application/json")]
public class NibbsController(IMediator mediator) : ControllerBase
{
    /// <summary>Sends a credit transfer (pacs.008) to another NPS participant.</summary>
    /// <param name="request">The transfer details (amount, debtor, creditor, beneficiary institution).</param>
    /// <param name="cancellationToken">Aborts the dispatch when the client disconnects.</param>
    /// <response code="202">The switch acknowledged receipt; the business outcome arrives later via webhook.</response>
    /// <response code="422">The switch rejected the message at validation level (admi.002).</response>
    /// <response code="502">The switch could not be reached, could not decrypt the message, or returned an unexpected error.</response>
    [HttpPost("transfers")]
    [ProducesResponseType(typeof(NibbsDispatchResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(NibbsRejectionResult), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendCreditTransfer(
        [FromBody] NpsCreditTransferRequest request,
        CancellationToken cancellationToken)
        => this.ToActionResult(await mediator.Send(new SendCreditTransferCommand(request), cancellationToken));

    /// <summary>Sends a name enquiry (acmt.023) for an account held at another NPS participant.</summary>
    /// <param name="request">The account to verify and the institution holding it.</param>
    /// <param name="cancellationToken">Aborts the dispatch when the client disconnects.</param>
    /// <response code="202">The switch acknowledged receipt; the verification report (acmt.024) arrives later via webhook.</response>
    /// <response code="422">The switch rejected the message at validation level (admi.002).</response>
    /// <response code="502">The switch could not be reached, could not decrypt the message, or returned an unexpected error.</response>
    [HttpPost("name-enquiry")]
    [ProducesResponseType(typeof(NibbsDispatchResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(NibbsRejectionResult), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendNameEnquiry(
        [FromBody] NpsIdVerificationRequest request,
        CancellationToken cancellationToken)
        => this.ToActionResult(await mediator.Send(new SendNameEnquiryCommand(request), cancellationToken));

    /// <summary>Sends a payment status query (pacs.028) for a previously sent credit transfer.</summary>
    /// <param name="request">Identifiers of the original payment being queried.</param>
    /// <param name="cancellationToken">Aborts the dispatch when the client disconnects.</param>
    /// <response code="202">The switch acknowledged receipt; the status report arrives later via webhook.</response>
    /// <response code="422">The switch rejected the message at validation level (admi.002).</response>
    /// <response code="502">The switch could not be reached, could not decrypt the message, or returned an unexpected error.</response>
    [HttpPost("status-query")]
    [ProducesResponseType(typeof(NibbsDispatchResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(NibbsRejectionResult), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendStatusQuery(
        [FromBody] NpsPaymentStatusQuery request,
        CancellationToken cancellationToken)
        => this.ToActionResult(await mediator.Send(new SendPaymentStatusQueryCommand(request), cancellationToken));

    /// <summary>Sends a payment status report (pacs.002) answering an inbound credit transfer.</summary>
    /// <param name="request">The original payment identifiers and the ACSC/RJCT decision.</param>
    /// <param name="cancellationToken">Aborts the dispatch when the client disconnects.</param>
    /// <response code="202">The switch acknowledged receipt of the status report.</response>
    /// <response code="422">The switch rejected the message at validation level (admi.002).</response>
    /// <response code="502">The switch could not be reached, could not decrypt the message, or returned an unexpected error.</response>
    [HttpPost("payment-status-report")]
    [ProducesResponseType(typeof(NibbsDispatchResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(NibbsRejectionResult), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendPaymentStatusReport(
        [FromBody] NpsPaymentStatusReportRequest request,
        CancellationToken cancellationToken)
        => this.ToActionResult(await mediator.Send(new SendPaymentStatusReportCommand(request), cancellationToken));

    /// <summary>Gets the list of NPS participants with their active/inactive statuses.</summary>
    /// <param name="cancellationToken">Aborts the request when the client disconnects.</param>
    /// <response code="200">The raw participants payload as returned by the switch.</response>
    /// <response code="502">The switch could not be reached or returned an unexpected error.</response>
    [HttpGet("participants")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetParticipants(CancellationToken cancellationToken)
        => this.ToActionResult(await mediator.Send(new GetNpsParticipantsQuery(), cancellationToken));
}
