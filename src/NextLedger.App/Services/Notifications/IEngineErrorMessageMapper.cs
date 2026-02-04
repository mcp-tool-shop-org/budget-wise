using NextLedger.Application.DTOs;

namespace NextLedger.App.Services.Notifications;

public interface IEngineErrorMessageMapper
{
    (string Title, string Message) Map(IReadOnlyList<BudgetOperationError> errors);
}
