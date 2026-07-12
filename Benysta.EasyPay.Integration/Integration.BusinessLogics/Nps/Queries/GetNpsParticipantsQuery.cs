using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Exceptions;

namespace Integration.BusinessLogics.Nps.Queries;

/// <summary>Gets the list of NPS participants with their active/inactive statuses.</summary>
public sealed record GetNpsParticipantsQuery : IRequest<NpsParticipantsResult>;

public sealed class GetNpsParticipantsQueryHandler(
    INpsApiClient apiClient,
    ILogger<GetNpsParticipantsQueryHandler> logger)
    : IRequestHandler<GetNpsParticipantsQuery, NpsParticipantsResult>
{
    public async Task<NpsParticipantsResult> Handle(
        GetNpsParticipantsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var participants = await apiClient.GetParticipantsAsync(cancellationToken);
            return new NpsParticipantsResult(Success: true, Json: participants, Error: null);
        }
        catch (Exception ex) when (
            ex is NpsIntegrationException or HttpRequestException ||
            // HttpClient timeout — as opposed to the caller cancelling the request.
            (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogError(ex, "Failed to fetch NPS participants");
            return new NpsParticipantsResult(Success: false, Json: null, Error: ex.Message);
        }
    }
}
