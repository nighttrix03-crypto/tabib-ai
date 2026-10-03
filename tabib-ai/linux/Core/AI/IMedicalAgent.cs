using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TabibAI.Linux.Core.Models;

namespace TabibAI.Linux.Core.AI;

public interface IMedicalAgent
{
    Task<MedicalResponse> ProcessAsync(string userInput, IReadOnlyList<ChatMessage> history, CancellationToken ct = default);
    string GenerateReport(MedicalCase caseData);
    string BuildSystemPrompt();
}
