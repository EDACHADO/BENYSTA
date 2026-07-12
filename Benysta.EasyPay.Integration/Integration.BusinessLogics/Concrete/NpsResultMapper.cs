using Integration.BusinessLogics.Nps.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Integration.BusinessLogics.Concrete;

/// <summary>
/// Maps the business-layer NPS results returned by the commands and queries to their
/// HTTP contract, so controllers stay free of mapping logic. Implemented as
/// <see cref="ControllerBase"/> extension methods so the standard result helpers
/// (<c>Accepted</c>, <c>Problem</c>, …) — and their ProblemDetails enrichment —
/// keep working. Reusable by every action that dispatches an NPS command or query.
/// </summary>
public static class NpsResultMapper
{
    /// <summary>
    /// Maps a dispatch outcome (credit transfer, name enquiry, status query/report):
    /// 202 on acknowledgement, 422 on an admi.002 rejection, 502 on decryption,
    /// transport or other integration failures.
    /// </summary>
    public static IActionResult ToActionResult(this ControllerBase controller, NpsDispatchResult result)
        => result.Status switch
        {
            NpsDispatchStatus.Accepted => controller.Accepted(new NibbsDispatchResult(
                result.MessageId, result.NibssStatusCode!.Value, result.AcknowledgedBySwitch)),

            NpsDispatchStatus.Rejected => controller.UnprocessableEntity(new NibbsRejectionResult(
                result.ReasonCode, result.RejectedMessageId, result.Error!)),

            NpsDispatchStatus.DecryptionFailure => controller.Problem(
                title: "NPS could not decrypt the message",
                detail: result.Error,
                statusCode: StatusCodes.Status502BadGateway),

            _ => controller.Problem(
                title: "NPS dispatch failed",
                detail: result.Error,
                statusCode: StatusCodes.Status502BadGateway),
        };

    /// <summary>
    /// Maps a participants lookup: 200 with the raw switch payload, 502 when the
    /// switch could not be reached or returned an unexpected error.
    /// </summary>
    public static IActionResult ToActionResult(this ControllerBase controller, NpsParticipantsResult result)
        => result.Success
            ? controller.Content(result.Json!, "application/json")
            : controller.Problem(
                title: "NPS participants lookup failed",
                detail: result.Error,
                statusCode: StatusCodes.Status502BadGateway);
}
