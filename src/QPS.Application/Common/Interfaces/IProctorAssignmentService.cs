using QPS.Domain.Entities;

namespace QPS.Application.Common.Interfaces;

public interface IProctorAssignmentService
{
    Task<List<ProctorAssignment>> AutoAssignAsync(Guid quizId, CancellationToken ct = default);
}