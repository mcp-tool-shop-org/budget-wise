using BudgetWise.Application.DTOs;

namespace BudgetWise.App.Services.Notifications;

public interface IEngineErrorMessageMapper
{
    (string Title, string Message) Map(IReadOnlyList<BudgetOperationError> errors);
}
